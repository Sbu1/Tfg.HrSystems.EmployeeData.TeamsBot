# Data & Contracts — In-Office Hours Assistant

## 📋 Document Control

| Field | Value |
|-------|-------|
| **Version** | 1.1.1 |
| **Status** | ✅ APPROVED |
| **Created** | 2026-06-30 |
| **Last Modified** | 2026-07-01 |
| **Author(s)** | SibusisoSik |

### 📝 Changelog

| Version | Date | Changes | Changed By |
|---------|------|---------|------------|
| 1.0.0 | 2026-06-30 | Initial draft | SibusisoSik |
| 1.1.0 | 2026-07-01 | Review pass: month phrases resolved code-side (not by the LLM); AddMotivation BR-05 pre-check reads subject hours; remove-motivation selector→id flow specified. Reverted to DRAFT for the fix, re-approved. | SibusisoSik |
| 1.1.1 | 2026-07-01 | §13.3 updated to the provisioned dev deployment `gpt-5.4-mini` (TOQ-01) and corrected for GPT-5-series param rules: dropped `temperature:0` (default only) and replaced `max_tokens` with `max_completion_tokens`. Patch. | SibusisoSik |

## ✅ Approval Record

| Version | Status | Approved By | Role | Approval Date |
|---------|--------|-------------|------|---------------|
| 1.0.0 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-06-30 |
| 1.1.0 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-07-01 |
| 1.1.1 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-07-01 |

---

## 💾 Data Models

No relational database. The only persisted data is **transient Redis state**; everything else is in-memory
value objects mapped from Employee Data API responses.

![ERD](diagrams/data/erd.png)
*Source: `diagrams/data/erd.mmd`*

| Entity | Type | Purpose | Product Spec Ref |
|--------|------|---------|-----------------|
| `UserIdentityState` | Redis (key `identity:{aadObjectId}`) | Cache `EmployeeNumber` + `ResolvedAtUtc` to avoid a Graph call per turn | FR-4.1 |
| `ConversationState` | Redis (key `conv:{conversationId}`) | Last N `ConversationTurn` (role + text) for follow-ups; **no employee data stored** | FR-4.2 |
| `EmployeeStanding` | Value object | Caller's current-month hours, gap to 100, `PaceStatus`, `AsAt` | FR-1.1 |
| `MonthHours` | Value object | One month: `CalendarMonth`, `CalendarYear`, `Hours`, `GoalMet` | FR-1.2 |
| `LeaderboardRow` / `Leaderboard` | Value object | Ranked, anonymised peers (`PlayerName`, `Hours`, `IsYou`, `Rank`) | FR-1.3 |
| `TeamMemberStanding` | Value object | Per direct report: `EmployeeName`, `EmployeeNumber`, `Hours`, `PaceStatus`, `ProjectedShortfall` | FR-3.1, FR-3.3 |
| `MotivationView` | Value object | `Id`, `CalendarMonth`, `MotivationTypeValue`, `Description`, `CreatedDate` | FR-2.2 |
| `MotivationType` | Value object | `Id`, `Value` (the 9 API types) | FR-2.1, BR-07 |
| `PaceStatus` | Enum | `OnTrack`, `Behind`, `AtRisk`, `TooEarly` | BR-01, BR-02 |
| `CallerRole` | Enum | `Employee`, `Manager` (derived) | FR-4.1 |

> Field shapes are expressed as C# records in `Domain.Models`. Monetary/seconds fields from the API
> (`timeOnSiteInSeconds`) are ignored; the bot uses `timeInHoursMonthToDate` (AC-4).

## ⏳ Data Lifecycle

| Stage | Trigger | Action | Retention | Owner |
|-------|---------|--------|-----------|-------|
| Identity resolve | First turn / cache miss | Graph lookup → write `UserIdentityState` | TTL **12h**, then re-resolve | `IBotStateStore` |
| Conversation append | Each user turn | Append `ConversationTurn`, trim to last **6** turns | Sliding TTL **30 min** | `IBotStateStore` |
| Hours / team / motivation reads | Per request | Fetched live from the API, mapped to value objects, rendered, **not persisted** | None (request-scoped) | Handlers |
| Motivation write/delete | User action (confirmed) | POST/DELETE to the API | None bot-side (system of record is the API) | Handlers |

## 🧩 Handler Contracts

> Every handler implements `IRequestHandler<TRequest,TResponse>` (`Tfg.Handler.Abstractions`). Every request
> carries the **caller's** clamped `EmployeeNumber` (set by the dispatcher, never by the model). Entry point for
> all is the Bot Framework message activity routed through `EmployeeBot` → intent dispatch.

| Handler | Input Contract | Output Contract | Entry Point | Product Spec Ref |
|---------|---------------|-----------------|-------------|-----------------|
| `GetMyHoursHandler` | `GetMyHoursRequest(int EmployeeNumber)` | `EmployeeStanding` | Bot message | FR-1.1 |
| `GetMyHistoryHandler` | `GetMyHistoryRequest(int EmployeeNumber)` | `IReadOnlyList<MonthHours>` (≤6) | Bot message | FR-1.2 |
| `GetPeerStandingHandler` | `GetPeerStandingRequest(int EmployeeNumber)` | `Leaderboard` | Bot message | FR-1.3 |
| `ListMotivationsHandler` | `ListMotivationsRequest(int EmployeeNumber, int? TargetEmployeeNumber, int LastXMonths=6)` | `IReadOnlyList<MotivationView>` | Bot message | FR-2.2 |
| `GetMotivationTypesHandler` | `GetMotivationTypesRequest()` | `IReadOnlyList<MotivationType>` | Bot message (supports Add) | FR-2.1, BR-07 |
| `AddMotivationHandler` | `AddMotivationRequest(int EmployeeNumber, int TargetEmployeeNumber, int MotivationTypeId, string CalendarMonth, string Description)` | `AddMotivationResult(int Id, bool Updated)` | Bot message | FR-2.1, BR-05, BR-06 |
| `RemoveMotivationHandler` | `RemoveMotivationRequest(int EmployeeNumber, int MotivationId)` | `RemoveMotivationResult(bool Deleted)` | Bot message | FR-2.3 |
| `GetTeamThisMonthHandler` | `GetTeamThisMonthRequest(int ManagerEmployeeNumber)` | `IReadOnlyList<TeamMemberStanding>` | Bot message | FR-3.1 |
| `GetTeamHistoryHandler` | `GetTeamHistoryRequest(int ManagerEmployeeNumber, int Months=6)` | `IReadOnlyList<TeamMemberStanding>` | Bot message | FR-3.2 |
| `GetAtRiskHandler` | `GetAtRiskRequest(int ManagerEmployeeNumber)` | `IReadOnlyList<TeamMemberStanding>` (at-risk, sorted by shortfall desc) | Bot message | FR-3.3 |

> **Clamp rules (AP-05):** for self-handlers `EmployeeNumber` = caller. For team-handlers `ManagerEmployeeNumber`
> = caller and the result set defines the allowed reports. For `AddMotivation`/`ListMotivations` with a
> `TargetEmployeeNumber`, the dispatcher verifies the target is in the caller's manager-team set (or equals the
> caller); otherwise the request is refused. `RemoveMotivation` first lists the caller's (or target report's)
> motivations and confirms the `MotivationId` is in that set before calling DELETE (BR-08).
>
> **Month resolution (code-side, not the LLM).** The model returns a raw `monthPhrase` string (e.g. "June",
> "last month", "202605"); the **dispatcher** resolves it to a `CalendarMonth` (`YYYYMM`) using the current date
> (`TimeProvider`) before building the handler request. The LLM is never relied on to compute dates. An
> unresolvable/ambiguous phrase → `clarify`.
>
> **AddMotivation BR-05 pre-check (in `AddMotivationHandler`).** Before POST, the handler (1) resolves the
> `MotivationTypeId` from the live type list (BR-07); (2) reads the subject's hours for `CalendarMonth` (via
> `GetEmployeeAsync` for self, or the manager-team history for a report) and **enforces BR-05** — a completed
> month must be `< 100h`, the current month must be behind/projected-to-miss; otherwise it declines with an
> explanation (does not rely solely on the API `400`); (3) checks for an existing same-key motivation and, if
> found, returns `Updated=true` after confirming the upsert (BR-06).
>
> **RemoveMotivation selector→id flow.** The `remove_motivation` intent yields a free-text `selector`. The
> dispatcher lists the caller's (or target report's) motivations and matches the selector. If exactly one
> matches → confirm then DELETE. If zero/many match → the bot renders the motivations as a card with a
> **"Remove" button per row** carrying `data.intent=remove_motivation_confirmed` + the concrete `motivationId`;
> the button tap invokes `RemoveMotivationHandler` with that id (ownership already guaranteed by the list source).

## 📨 Internal Events & Messages

Not applicable — the bot has no internal event bus or message broker (no Kafka; each turn is a synchronous
request/response). Cross-component communication is direct method calls behind the Domain ports.

## 🌐 API & Integration Contracts

### 13.1 Employee Data API — REST / JSON

Base URL (test): `https://tst-tfg-hrsystems-employeedata-api.apps.ocptst.ho.fosltd.co.za`. Auth disabled (v1);
in prod reached internal-only via the CA gateway (TD-11). The API returns **raw JSON** (no wrapper envelope).

#### Enumeration / Code Reference Tables

**Motivation types** (`GET /api/motivationtype`):

| id | value |
|----|-------|
| 1 | Annual Leave |
| 2 | Family Responsibility Leave |
| 3 | Injury Leave |
| 4 | Parental Leave |
| 5 | Pre-Maternity Leave |
| 6 | Sick Leave |
| 7 | Study Leave |
| 8 | Sports Leave |
| 9 | WFH with permission |

**HTTP status handling:** `200` success; `400` validation (surface a friendly correction); `404` (delete of
unknown id) → treat as already-removed; `5xx`/timeout → BR-09 "service unreachable".

#### GET Employee — `GET /api/employee?employeeNumber={n}` (FR-1.1, FR-1.2, FR-1.3)

**Request:**
```
GET /api/employee?employeeNumber=123456
```
**Response — 200:**
```json
{
  "employeeMonths": [
    { "employeeNumber": "123456", "cto": "Van der Vyver, Jan", "manager": "Kruger, Gert", "employeeName": "Du Toit, Werner", "calendarMonth": 6, "calendarYear": 2026, "timeOnSiteInSeconds": 86987, "timeInHoursMonthToDate": 84 },
    { "employeeNumber": "123456", "cto": "Van der Vyver, Jan", "manager": "Kruger, Gert", "employeeName": "Du Toit, Werner", "calendarMonth": 5, "calendarYear": 2026, "timeOnSiteInSeconds": 418212, "timeInHoursMonthToDate": 116 }
  ],
  "teamCurrentMonth": [
    { "playerName": "player1", "calendarMonth": 6, "calendarYear": 2026, "timeOnSiteInSeconds": 157589, "timeInHoursMonthToDate": 43 },
    { "playerName": "player2", "calendarMonth": 6, "calendarYear": 2026, "timeOnSiteInSeconds": 153159, "timeInHoursMonthToDate": 42 }
  ]
}
```
> The caller's own current-month figure is the latest `employeeMonths` entry; it is **merged** into
> `teamCurrentMonth` and ranked for the leaderboard (EC-04). `asAt` = previous day (T-1, RA-DC-02).

#### GET ManagerTeam — `GET /api/managerteam?employeeNumber={n}&months={m}` (FR-3.x, role derivation)

**Request:**
```
GET /api/managerteam?employeeNumber=3333333&months=2
```
**Response — 200 (manager):**
```json
[
  { "employeeNumber": "1234567", "cto": "Van der Vyver, Jan", "manager": "Kruger, Gert", "employeeName": "Du Toit, Werner", "calendarMonth": 6, "calendarYear": 2026, "timeOnSiteInSeconds": 86987, "timeInHoursMonthToDate": 124, "badges": [] },
  { "employeeNumber": "1111111", "cto": "Van der Vyver, Jan", "manager": "Kruger, Gert", "employeeName": "Duminy, JD", "calendarMonth": 6, "calendarYear": 2026, "timeOnSiteInSeconds": 153159, "timeInHoursMonthToDate": 142, "badges": [] }
]
```
**Response — 200 (not a manager):**
```json
[]
```
> An empty array ⇒ caller is not a manager (EC-05 / role gating). The `badges` array is tolerated and ignored
> (badges out of scope, OS-7).

#### GET Motivation — `GET /api/motivation?employeeNumber={n}&lastXMonths={m}` (FR-2.2)

**Request:**
```
GET /api/motivation?employeeNumber=1234567&lastXMonths=3
```
**Response — 200:**
```json
[
  { "id": 4, "employeeNumber": 1234567, "calendarMonth": "202606", "description": "Employee was on 3 days leave", "motivationTypeId": 1, "createdDate": "2026-06-17T10:03:32.17+02:00", "createdBy": "System", "modifiedDate": "2026-06-17T10:03:32.17+02:00", "modifiedBy": "System", "motivationTypeValue": "Annual Leave" }
]
```
**Response — 200 (none):**
```json
[]
```

#### POST Motivation (upsert) — `POST /api/motivation` (FR-2.1, BR-06)

**Request:**
```json
{ "employeeNumber": 1234567, "motivationTypeId": 1, "calendarMonth": "202606", "description": "On annual leave for 3 days" }
```
**Response — 200:**
```json
{ "id": 4 }
```
> Uniqueness = (`employeeNumber`, `motivationTypeId`, `calendarMonth`); re-posting updates (upsert). The bot
> detects an existing same-key motivation first (via GET) to render the "update it?" confirmation (BR-06).

#### DELETE Motivation — `DELETE /api/motivation/{id}` (FR-2.3)

**Request:**
```
DELETE /api/motivation/4
```
**Response — 200:** (empty body, success) · **404:** no motivation with that id.

#### GET MotivationType — `GET /api/motivationtype` (FR-2.1, BR-07)

**Request:**
```
GET /api/motivationtype
```
**Response — 200:**
```json
[ { "id": 1, "value": "Annual Leave" }, { "id": 6, "value": "Sick Leave" }, { "id": 9, "value": "WFH with permission" } ]
```
> See the complete 9-row reference table above.

### 13.2 Microsoft Graph — REST (identity resolution, FR-4.1)

Auth: app registration, **client-credentials**, `User.Read.All` (application) with admin consent. Token from
`https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token` (`scope=https://graph.microsoft.com/.default`),
cached until expiry.

#### Resolve employeeId

**Request:**
```
GET https://graph.microsoft.com/v1.0/users/{aadObjectId}?$select=employeeId
Authorization: Bearer {app-token}
```
**Response — 200:**
```json
{ "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users(employeeId)", "employeeId": "1234567" }
```
**Response — 200 (unpopulated):**
```json
{ "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users(employeeId)", "employeeId": null }
```
> `employeeId` null/non-numeric ⇒ unmapped user → FR-4.3 (explain + point to HR; no data, no lookup).

### 13.3 Azure OpenAI — Chat Completions with function calling (intent only, FR-4.2, TD-14)

In-tenant deployment of `gpt-5.4-mini` (dev resource `aoai-teamsbot-aue`, Australia East, Data Zone Standard;
v1 Responses API endpoint). **Only the user's message text + short history are sent — never employee
data.** The functions are the intents; the model returns a `tool_call` naming the intent and parameters.

> **GPT-5-series param rules (differ from GPT-4o):** do **not** send `temperature` other than the default, and
> use `max_completion_tokens` instead of `max_tokens`. Sending `temperature:0` or `max_tokens` returns a `400`.
> The request block below reflects these rules; the `Azure.AI.OpenAI` client handles the mapping when configured
> for this deployment.

#### Enumeration / Code Reference Table — intents (function names)

| Function (intent) | Parameters | Maps to handler |
|-------------------|-----------|-----------------|
| `get_my_hours` | — | GetMyHoursHandler |
| `get_my_history` | — | GetMyHistoryHandler |
| `get_peer_standing` | — | GetPeerStandingHandler |
| `list_motivations` | `targetEmployeeName?` (string, manager only) | ListMotivationsHandler |
| `add_motivation` | `motivationType` (string), `monthPhrase` (string, as said — resolved to YYYYMM in code), `description` (string), `targetEmployeeName?` | AddMotivationHandler |
| `remove_motivation` | `selector` (string, e.g. month+type) | RemoveMotivationHandler |
| `get_team_this_month` | — | GetTeamThisMonthHandler |
| `get_team_history` | `months?` (int, default 6) | GetTeamHistoryHandler |
| `get_at_risk` | — | GetAtRiskHandler |
| `help` | — | help/onboarding text |
| `clarify` | `reason` (string) | ask the user to clarify (EC-10) |

> Parameters are **names/strings** the user said — never employee numbers, ids, or computed dates. In code:
> `motivationType` → a valid `motivationTypeId` against the live type list (BR-07); `monthPhrase` → `YYYYMM`
> via the current date; `targetEmployeeName` → a report's number only within the caller's manager-team set
> (clamp); `selector` → a concrete `motivationId` via the list-and-match / button flow above.

#### Request (complete, standalone)
```json
{
  "model": "gpt-5.4-mini",
  "messages": [
    { "role": "system", "content": "You route a TFG employee's in-office-hours question to exactly one function. Never invent employee numbers. If the request is ambiguous or unsupported, call clarify. Output only a tool call." },
    { "role": "user", "content": "how am I doing this month?" }
  ],
  "tools": [
    { "type": "function", "function": { "name": "get_my_hours", "description": "The user's own current-month hours and progress to the 100h goal.", "parameters": { "type": "object", "properties": {}, "required": [] } } }
  ],
  "tool_choice": "auto",
  "max_completion_tokens": 200
}
```
#### Response (complete, standalone)
```json
{
  "id": "chatcmpl-xxxx",
  "object": "chat.completion",
  "choices": [
    { "index": 0, "finish_reason": "tool_calls", "message": {
        "role": "assistant",
        "tool_calls": [ { "id": "call_1", "type": "function", "function": { "name": "get_my_hours", "arguments": "{}" } } ]
    } }
  ],
  "usage": { "prompt_tokens": 180, "completion_tokens": 12, "total_tokens": 192 }
}
```
> The dispatcher reads `choices[0].message.tool_calls[0].function.name/arguments`, maps to the handler, and
> **clamps** the employee number to the caller. If `finish_reason` is not `tool_calls`, treat as `clarify`.

### 13.4 Bot Framework — `POST /api/messages` (Bot Connector protocol)

The Connector posts an `Activity`; the bot replies with an `Activity` (text or Adaptive Card attachment). The
bot exposes **no business REST API**, so the team's standard response envelope does not apply to its own surface.

#### Inbound activity (complete, standalone)
```json
{
  "type": "message",
  "id": "1700000000000",
  "channelId": "msteams",
  "conversation": { "id": "19:meeting_abc@thread.v2" },
  "from": { "id": "29:1Abc...", "aadObjectId": "8b6f...-...-...", "name": "Werner du Toit" },
  "recipient": { "id": "28:bot-app-id", "name": "In-Office Hours Assistant" },
  "text": "how am I tracking this month?",
  "serviceUrl": "https://smba.trafficmanager.net/za/"
}
```
#### Outbound reply — Adaptive Card attachment (complete, standalone, my-hours view)
```json
{
  "type": "message",
  "attachments": [
    { "contentType": "application/vnd.microsoft.card.adaptive",
      "content": {
        "type": "AdaptiveCard", "version": "1.5",
        "body": [
          { "type": "TextBlock", "size": "Medium", "weight": "Bolder", "text": "Your in-office hours — June 2026" },
          { "type": "TextBlock", "text": "84 of 100 hours (as at 29 Jun)", "wrap": true },
          { "type": "TextBlock", "text": "Status: Behind — 16h to go", "color": "Warning", "wrap": true }
        ],
        "actions": [
          { "type": "Action.Submit", "title": "My history", "data": { "intent": "get_my_history" } },
          { "type": "Action.Submit", "title": "How's my team?", "data": { "intent": "get_peer_standing" } }
        ],
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json"
      } }
  ]
}
```
> Quick-action `Action.Submit` `data.intent` is dispatched directly (no LLM call needed for button taps).

## 🌱 Seed Data

Not applicable — the bot persists no reference data. Motivation types are fetched live from
`GET /api/motivationtype` at runtime (BR-07).

## ⚡ Integration Failure Modes

| Integration Point | Detection | Response | User Impact | Recovery |
|-------------------|-----------|----------|-------------|----------|
| Employee Data API | Timeout / `5xx` / network | "The hours service is unreachable right now — please try again shortly." (BR-09) | No data this turn | Retry on next ask; Polly retry+breaker |
| Employee Data API `400` | `400` on POST motivation | Friendly correction (e.g. "that month has met the goal — no motivation needed", BR-05) | Write declined | User corrects input |
| Microsoft Graph | Token failure / `5xx` / `employeeId` null | If unresolved + no cached identity → FR-4.3 message; if cached identity exists, proceed | Possibly blocked (unmapped) | Use cached identity; re-resolve next turn |
| Azure OpenAI | Timeout / throttle (`429`) / `5xx` | Fall back to quick-action buttons + "I didn't catch that — pick an option below." | Degraded NLU, buttons still work | Retry with backoff; buttons bypass the LLM |
| Redis (state) | Connection error | Proceed stateless: re-resolve identity from Graph; conversation context reset | Lost follow-up context | Reconnect; next turn re-caches |
| Bot Connector (outbound send) | Send `Activity` fails | Log + correlation id; no user-visible retry loop | Reply may not arrive | Connector/SDK retry; user can re-ask |

## 💭 Decision Context

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| DC-DC-01 | Bot exposes no business REST API (only `/api/messages`) | Add REST endpoints | Teams is the only channel; team response-envelope rule N/A to the bot's surface |
| DC-DC-02 | Motivation types fetched live (no seed) | Seed/cache the 9 types | Source of truth is the API; avoids drift; cheap call, cacheable short-term |
| DC-DC-03 | LLM parameters are names/strings, resolved to ids/numbers in code | Let the model emit ids/numbers | Keeps PII/identifiers out of the model and enforces the clamp (TD-14, AP-05) |
| DC-DC-04 | Accept conversation-context loss on Redis outage | Second durable store | Context is non-critical; re-resolve identity and continue (TR-DC-01) |

> Reasoning trail: see `reasoning.md` § Data & Contracts reasoning.
