# Work Breakdown - In-Office Hours Assistant

> Tracked work-item hierarchy derived from the signed-off specs. This file is the source of truth **until an
> Azure DevOps board exists** for this product; at that point the hierarchy below is pushed into ADO (project
> `HR`, team/Area Path TBD) and this file becomes a mirror/reference.
>
> - Product spec: [`product-spec.md`](product-spec.md) (v1.1.0, APPROVED)
> - Technical spec: [`technical-spec.md`](technical-spec.md) (v1.1.3, APPROVED)
> - Hyphens are plain `-` (never em-dash) so titles/descriptions are ADO-ready.

## Status legend

- `[ ]` Not started `[~]` In progress `[x]` Done `[!]` Blocked / gated

## Scope classification

**Single-product -> one Epic.** One product, one user base (TFG Infotech staff + their managers), one set of
goals (G-01..G-04). The roadmap's v2 (leadership roll-up/export G-05, pro-rating, proactive nudges) is
explicitly out of scope (OS-4, OS-6) and unbuildable on today's API, so it does **not** warrant a second Epic.

## Delivery sequencing (dependencies)

```
F11 Pre-Build Gates  ──(TOQ-04 identity is a HARD gate; TOQ-03 Redis before build)──┐
F1  Foundation & Host ───────────────┐                                              │
F2  Domain Logic ────────────┐        ├─> F4 Conversational & Identity ─┐           │
F3  External Integrations ───┘        │                                 ├─> F5/F6/F7 functional
                                       └─────────────────────────────────┘
F8 Observability/Security/Resilience ── cross-cuts F4-F7
F9 Testing ── unit alongside F2; integration/E2E after F5-F7
F10 Infrastructure & Deployment ── parallel; Azure Bot + Teams manifest gated on IT (TR-08)
```

---

## EPIC: In-Office Hours Assistant

A conversational Microsoft Teams assistant that gives TFG Infotech staff self-service visibility of their
in-office hours against the 100-hour monthly goal, anonymised peer standing, motivation logging for shortfall
months, and gives managers team standing plus an at-risk list - all backed by the existing
`Tfg.HrSystems.EmployeeData` API. Serves goals G-01..G-04. v1 only.

---

## F1 - Solution Foundation & Bot Host

Stand up the solution structure and the runnable plain-host bot per CP-08. Foundation every other feature
builds on.

- `[x]` **F1-S1 - Scaffold solution and feature-sliced projects (spec section 8)**
  - Partially done (spike created Domain.Interfaces, Application.ConversationAi, Presentation.Bot). Remaining
    projects added as they are needed by later features.
  - Tasks: `[x]` create `.sln` + spike projects on net10.0; `[ ]` add remaining section-8 modules
    (Domain.Models/Services/Constants/Observability, Application.Handlers/Models/Validators/EmployeeDataApi/
    Graph/HealthCheck, Infrastructure.State) as stubs; `[ ]` enforce dependency rules (section 8 graph).
- `[x]` **F1-S2 - Plain WebApplication host + CloudAdapter on /api/messages (CP-08)**
  - Done via spike: plain host (no ApiShell/AutoWrapper), `CloudAdapter`, `/api/messages`, `/healthz`, anonymous
    auth for the Emulator. Closes the runtime intent of TOQ-05.
  - Tasks: `[x]` host + adapter + route; `[x]` `EmployeeBot : ActivityHandler`; `[x]` verified end-to-end.
- `[ ]` **F1-S3 - DI composition root + per-concern registration extensions**
  - `AddBotAdapter/AddConversationAi/AddEmployeeDataApi/AddGraphDirectory/AddBotState/AddDomainServices/
    AddHandlers/AddValidators/AddHealthChecks/AddObservability` (section 8). Lifetimes per spec.
  - Tasks: `[ ]` registration extension per concern; `[x]` `AddConversationAi`/`AddBotAdapter` (spike); `[ ]`
    wire all into `Program.cs`.
- `[ ]` **F1-S4 - Configuration schema + strongly-typed options (spec section 15)**
  - All `Bot/AzureOpenAI/Graph/EmployeeApi/Redis/Pace/Identity/Conversation` keys; per-env `appsettings`;
    secrets sourced from Vault later (F8/F10). Dev key currently hardcoded (spike).
  - Tasks: `[ ]` options classes + binding; `[ ]` per-env appsettings; `[x]` AzureOpenAI options (spike).

## F2 - Domain Logic (Pace, Leaderboard, Calendar)

Pure, framework-free domain per section 8 `Domain.*` and decisions A-DC-02 / TR-DC-02. Unit-tested to >=90%
(spec section 19). No external dependencies -> fully buildable now.

- `[x]` **F2-S1 - Domain value objects, enums, and constants** - DONE
  - `Domain.Models`: `PaceStatus`/`CallerRole` enums, `PaceResult`, `EmployeeStanding`, `MonthHours`,
    `LeaderboardRow`/`Leaderboard`, `TeamMemberStanding`, `MotivationView`, `MotivationType`.
    `Domain.Constants`: `PaceDefaults` (goal 100, early-month 30%), `IntentNames` (the 11 intents).
- `[x]` **F2-S2 - WorkingDayCalendar + PaceCalculator (BR-01/02/03/04)** - DONE
  - `WorkingDayCalendar` (Mon-Fri, BR-03) + `PaceCalculator.Evaluate` (KA-04) returning `PaceResult`
    (status + expected + projected month-end); goal/threshold injected (TR-DC-02). Unit tests TS-01 green
    (TooEarly / OnTrack / boundary / AtRisk).
  - **RESOLVED (product ruling 2026-07-02):** `Behind` = below pace but recovery to the goal is still achievable
    in the remaining working days; `AtRisk` = below pace and unrecoverable even at a max daily effort. Added a
    config-driven `Pace:MaxHoursPerWorkingDay` tunable (default 10) - **needs adding to the config schema
    (spec section 15)** when F1-S4 lands. This makes at-risk a stronger/later signal than the original linear
    projection.
- `[x]` **F2-S3 - LeaderboardBuilder (FR-1.3, EC-04)** - DONE
  - `LeaderboardBuilder.Build(selfHours, maskedPeers)` (KA-05) merges the caller, ranks desc, highlights "You",
    surfaces `YourRank`; no-peers case (EC-03) handled. Unit tests TS-02 green.
- `[ ]` **F2-S4 - Validators + centralised access clamp (BR-08)** - MOVED to the Application layer
  - Deferred: `Application.Validators` (month format `YYYYMM`, type id, qualifying-month BR-05) and the clamp
    (AP-05) live in the Application layer per section 8 and are built with the dispatcher (F4), where they are
    used. Kept here as a pointer; will be tracked under F4.

## F3 - External Integrations (EDA, Graph, Azure OpenAI, Redis state)

Client adapters behind Domain ports (KA-01/02/03/06). Consumed by all functional features.

- `[x]` **F3-S1 - Employee Data API typed client (KA-02)** - DONE
  - `IEmployeeDataClient` (Domain.Interfaces) returning domain carriers (`EmployeeHours`, `TeamMemberMonths`,
    `MotivationView`, `MotivationType`); `Application.EmployeeDataApi` client + internal JSON DTOs + mapping;
    `AddEmployeeDataApi` registration with `AddStandardResilienceHandler` (transient-fault handling); status
    handling per 22.4 (404 delete -> already-removed; 400 POST -> `EmployeeDataApiValidationException`). Mapping
    unit tests green (employee months/peers, empty-team = non-manager, month grouping, 404, 400).
  - Note: transient resilience via `Microsoft.Extensions.Http.Resilience` (standard handler) rather than a
    hand-rolled Polly policy - same intent, team-standard.
- `[~]` **F3-S2 - Microsoft Graph directory client + identity resolution (KA-01, FR-4.1)** - client DONE (TDD); token handler pending
  - `IEmployeeDirectory` + `GraphEmployeeDirectory` -> `GET /v1.0/users/{aadObjectId}?$select=employeeId`;
    missing id / 404 -> null (unmapped, FR-4.3). `AddGraphDirectory` registration. Mapping unit tests green.
  - Tasks: `[x]` `GetEmployeeNumberAsync` + null/404 handling; `[x]` registration; `[ ]` client-credentials
    bearer token DelegatingHandler (needs Graph creds + Vault, F8-S3; verification gated on TOQ-04).
- `[x]` **F3-S3 - Conversation intent service (KA-03, section 22.1) - function calling** - DONE (impl; live integration test in F9)
  - `IConversationIntentService.ResolveIntentAsync` -> `IntentResult` via `gpt-5.4-mini` function calling;
    intent-only payloads (no PII, TC-DC-02). Contract (`IntentResult`, `ConversationTurn`) + `IntentResultMapper`
    (TDD) + `ConversationIntentTools` (11 tool defs, TDD) + `AzureOpenAiConversationIntentService` + registration.
    Token cap omitted (avoids the `max_tokens` rejection - same fix as the spike; no api-version pin needed).
  - Not yet consumed by `EmployeeBot` - the dispatcher that calls this lands in F4. Live AOAI call is
    integration-tested with recorded responses under F9.
- `[ ]` **F3-S4 - Bot state store - custom Redis IStorage + IBotStateStore (KA-06, A-DC-04)**
  - `RedisStorage : IStorage` over StackExchange.Redis with ETag optimistic concurrency; identity cache
    (`identity:{aadObjectId}`, TTL 12h) + conversation (`conv:{id}`, last 6 turns, sliding 30 min).
  - Tasks: `[ ]` `RedisStorage` implementing the BF `IStorage` contract (TR-10); `[ ]` `IBotStateStore`;
    `[ ]` concurrency/ETag tests. (Depends on F11 TOQ-03 Redis provisioning.)

## F4 - Conversational Experience & Identity (FR-4)

The turn pipeline: identity/role gating, intent dispatch, guided+NL UX, help, unknown-user, write confirmation.

- `[~]` **F4-S1 - Identity and role resolution in the turn (FR-4.1, BR-08)** - resolver DONE (TDD); Redis cache pending
  - `CallerIdentityResolver`: AAD object id -> employee number (F3-S2) + role via `managerteam` (Manager when
    non-empty, TA-02); unmapped -> null (FR-4.3). Wired into `EmployeeBot`. 3 unit tests green.
  - Tasks: `[x]` resolver + role derivation; `[x]` bot wires it (unmapped -> HR message); `[ ]` identity cache
    in Redis (F3-S4, 12h TTL); `[ ]` command gating lives in the dispatcher (F4-S2, done).
- `[~]` **F4-S2 - Intent dispatch + code-side parameter resolution** - resolvers DONE (TDD); dispatch wiring pending
  - Route `IntentResult` -> the right handler; resolve `monthPhrase`->`YYYYMM`, `motivationType`->id,
    `targetEmployeeName`->report number, `selector`->`MotivationId` (section 11 dispatcher note); apply clamp.
  - Tasks: `[x]` `MonthResolver` (phrase->YYYYMM) + `MotivationTypeResolver` (name->id, BR-07) in
    `Application.Validators`, unit-tested; `[ ]` role gating (team intents -> Manager only, EC-05);
    `[ ]` `targetEmployeeName`->number + `selector`->id resolvers (need team/motivation lists); `[ ]` dispatcher
    that builds the handler request + invokes it; `[ ]` clamp integration.
  - Also closes the relocated F2-S4 validator work (month format / type id now live here).
- `[x]` **F4-S3 - Guided + natural language, quick-action cards, help/onboarding (FR-4.2)** - DONE
  - Free text -> intent end-to-end; `CardFactory` renders a role-aware welcome card + per-view result cards with
    `Action.Submit` quick-action buttons; `QuickActionParser` turns a tapped button's `data.intent` into an
    `IntentResult` that **bypasses the LLM**; `OnMembersAdded` sends the welcome card; help shows it too.
  - Tasks: `[x]` help/onboarding + clarify; `[x]` Adaptive Cards per view; `[x]` quick-action buttons + bot
    handling of taps (unit-tested parser + E2E button-bypass turn). Card *visual polish* iterated in the Emulator.
- `[x]` **F4-S4 - Unknown-user path + write confirmation (FR-4.3, FR-4.4)** - DONE
  - Unmapped user -> "contact HR" message, no data/no lookup. Add/remove now return a **confirmation card**
    (`Confirmation` payload) with a Confirm button carrying the resolved params; the `*_confirmed` tap commits.
    On commit the target is **re-clamped** (add: caller-or-direct-report, TR-03) and remove re-checks ownership
    (BR-08) - tampered button payloads can't bypass access control. Success/failure acknowledged.
  - Tasks: `[x]` unknown-user; `[x]` confirm-before-commit prompt; `[x]` commit-on-confirmed-tap + re-clamp + ack
    (dispatcher unit tests incl. tampered-target rejection; E2E confirm turn).

## F5 - Employee Self-View (FR-1)

- `[~]` **F5-S1 - GetMyHours (FR-1.1)** - handler DONE (TDD); card + DI pending
  - `GetMyHoursHandler` (`IRequestHandler<GetMyHoursRequest, EmployeeStanding>` via `Tfg.Handler.Abstractions`
    v1.0.3) composes `IEmployeeDataClient` + `IPaceCalculator` -> MTD, gap, pace status, "as at" (T-1 via
    injected `TimeProvider`). 2 unit tests green (standing/gap/pace/as-at; gap 0 when met).
  - Tasks: `[x]` `GetMyHoursRequest` DTO; `[x]` handler + unit tests; `[ ]` Adaptive Card (Presentation, with
    F4); `[ ]` DI registration in `AddHandlers` (with F1-S3/F4); `[ ]` end-to-end via TestAdapter (F9).
  - Decision: handlers use the team-standard `Tfg.Handler.Abstractions` package (confirmed on the TFG feed),
    not a locally-defined `IRequestHandler`. Its `HandleAsync` carries a `Dictionary<string,string>? context` arg.
- `[~]` **F5-S2 - GetMyHistory (FR-1.2)** - handler DONE (TDD); card + DI pending
  - `GetMyHistoryHandler` -> `IReadOnlyList<MonthHours>` most-recent-first, goal-met flagged, capped at 6
    (EC-02). 2 unit tests green (ordering + goal-met; 6-month cap).
  - Tasks: `[x]` `GetMyHistoryRequest` DTO; `[x]` handler + unit tests; `[ ]` history card (F4); `[ ]` DI/E2E.
- `[~]` **F5-S3 - GetPeerStanding (FR-1.3)** - handler DONE (TDD); card + DI pending
  - `GetPeerStandingHandler` -> `Leaderboard` via `LeaderboardBuilder` (F2-S3); own figure merged (EC-04),
    no-peers -> just the caller (EC-03). 2 unit tests green.
  - Tasks: `[x]` `GetPeerStandingRequest` DTO; `[x]` handler + unit tests; `[ ]` leaderboard card (F4); `[ ]` DI/E2E.

## F6 - Motivations (FR-2)

- `[~]` **F6-S1 - Motivation types + list motivations (FR-2.2, BR-07)** - handlers DONE (TDD); cards + DI pending
  - `GetMotivationTypesHandler` (live from API, DC-DC-02) + `ListMotivationsHandler` (caller or a manager's
    report via clamped target). 2 unit tests green.
  - Tasks: `[x]` DTOs; `[x]` both handlers + tests; `[ ]` list card (F4); `[ ]` DI/E2E; `[ ]` "none recorded" (EC-08, card).
- `[~]` **F6-S2 - Add motivation (FR-2.1, BR-05/06)** - handler DONE (TDD); confirm flow + DI pending
  - `AddMotivationHandler` -> upsert; enforces BR-05 (reads subject hours; completed month < goal, current month
    Behind/AtRisk; else throws `MotivationNotAllowedException` - not reliant on the API 400); detects same
    type+month for the BR-06 `Updated` flag. `MotivationView` gained `MotivationTypeId` for that match. 4 unit
    tests green incl. met-goal decline + upsert + current-month-behind.
  - Tasks: `[x]` DTOs + result; `[x]` handler pre-check + upsert + tests; `[ ]` confirm flow (F4/card, FR-4.4);
    `[ ]` DI/E2E (TS-03 end-to-end).
- `[~]` **F6-S3 - Remove motivation (FR-2.3)** - handler DONE (TDD); selector/confirm flow + DI pending
  - `RemoveMotivationHandler` -> verifies the id is in the subject's own set before DELETE (BR-08 ownership);
    unowned id refused without an API call; delete 404 -> not-deleted (from the client, 22.4). 2 unit tests green.
  - Tasks: `[x]` DTOs; `[x]` handler + ownership check + tests; `[ ]` selector->id + confirm flow (F4);
    `[ ]` DI/E2E.

## F7 - Manager Team View & At-Risk (FR-3)

- `[~]` **F7-S1 - Team this month (FR-3.1)** - handler DONE (TDD); card + DI pending
  - `GetTeamThisMonthHandler` -> per report: name, current-month hours, pace status + projected shortfall (via
    shared `TeamStandingMapper`). No-data member reads 0h (EC-07). 1 unit test green.
  - Tasks: `[x]` DTO; `[x]` handler + mapper + unit test; `[ ]` team card (F4); `[ ]` DI/E2E; `[ ]` pagination (EC-06).
- `[~]` **F7-S2 - Team history (FR-3.2)** - handler DONE (TDD); card + DI pending
  - `GetTeamHistoryHandler` -> `IReadOnlyList<TeamMemberHistory>` (per report, monthly hours + goal-met, recent
    first). **Deviation:** section 11 typed this as `TeamMemberStanding` which can't hold per-month data - added
    `TeamMemberHistory` to represent FR-3.2 faithfully. 1 unit test green.
  - Tasks: `[x]` DTO + `TeamMemberHistory`; `[x]` handler + unit test; `[ ]` history card (F4); `[ ]` DI/E2E.
- `[~]` **F7-S3 - Who's at risk (FR-3.3)** - handler DONE (TDD); card + DI pending
  - `GetAtRiskHandler` -> reports projected to miss the goal, sorted by largest shortfall desc; too-early/on-track
    excluded. **Interpretation:** after the Behind=recoverable ruling, "projected to miss" (FR-3.3/BR-01,
    projected < 100) spans **both** `Behind` and `AtRisk` statuses, so the at-risk list includes recoverable-but-
    behind reports (serves early intervention, G-02) - not only the unrecoverable `AtRisk` status. 1 unit test green.
  - Tasks: `[x]` DTO; `[x]` handler + sort + unit test; `[ ]` at-risk card (F4); `[ ]` DI/E2E.

## F8 - Observability, Security & Resilience (cross-cutting)

- `[ ]` **F8-S1 - Observability (spec section 17)**
  - Per-intent latency histogram; per-dependency call metrics; identity/intent/clamp/`429` counters;
    correlation id per activity threaded through logs + outbound calls (AP-07); stdout logs for Dynatrace.
  - Tasks: `[ ]` metrics + `/metrics`; `[ ]` counters; `[ ]` correlation id middleware.
- `[ ]` **F8-S2 - Error handling & resilience (spec section 16, BR-09)**
  - Graceful "service unreachable" for EDA down; friendly `400` correction; Graph-down uses cache then FR-4.3;
    AOAI throttle -> buttons; Redis down -> stateless re-resolve; Polly retry+breaker.
  - Tasks: `[ ]` failure-mode handlers; `[ ]` typing indicator (NFR-1); `[ ]` button fallback (TR-02/05).
- `[ ]` **F8-S3 - Security hardening (spec section 18, NFR-4)**
  - Centralised clamp + delete ownership (TR-03); no PII to the LLM (TC-DC-02, TR-06) + log redaction; secrets
    from Vault (bot password, Graph secret, AOAI key, Redis, CA-gateway); TLS on all hops.
  - Tasks: `[ ]` clamp audit + refusal counter; `[ ]` log redaction; `[ ]` Vault wiring (move dev key off
    hardcode).

## F9 - Testing & Quality (spec section 19)

Unit tests for Domain live under F2. This feature covers the shared harness, integration, E2E, and perf.

- `[~]` **F9-S1 - Test project + harness** - core done; WireMock/in-memory IStorage pending
  - `EmployeeData.TeamsBot.Tests` (xUnit) with doubles: `FakeEmployeeDataClient`, `FakeEmployeeDirectory`,
    `FakeConversationIntentService`, `StubHttpMessageHandler` (client mapping), `FixedTimeProvider`,
    `StubWorkingDayCalendar`; Bot Framework `TestAdapter` harness (`TestBot`).
  - Tasks: `[x]` project + doubles + TestAdapter harness; `[ ]` WireMock (HTTP-layer), in-memory `IStorage`
    (with F3-S4 Redis).
- `[~]` **F9-S2 - Integration tests - all 10 handlers + failure paths** - largely covered
  - All 10 handlers have unit tests (happy + key failure/edge). Role-gating (TS-04) + backend-failure (TS-05)
    covered E2E. `[ ]` remaining: explicit EDA `5xx`/`400` propagation per handler via WireMock (F9-S1 dep).
- `[~]` **F9-S3 - End-to-end scenarios TS-01..TS-05 via TestAdapter** - 5 turns green
  - `BotTurnTests`: happy read (standing), TS-04 role gating (declined), unknown-user (HR), TS-05 backend
    failure (graceful, BR-09), TS-03 add-motivation (logged). TS-01/TS-02 (pace/leaderboard) are unit-covered.
  - Also: added a **BR-09 turn-level safety net** in `EmployeeBot` (catch -> "service unreachable"); per-failure-
    mode detail + logging remain F8-S1/S2.
- `[ ]` **F9-S4 - Performance test (NFR-1 <=8s p95)**
  - k6 / NBomber over a turn (LLM + API round-trips).
  - Tasks: `[ ]` load script; `[ ]` baseline + p95 assertion.

## F10 - Infrastructure & Deployment (spec section 14)

- `[ ]` **F10-S1 - Azure DevOps pipeline (Tfg.Build.Templates)**
  - build -> unit/integration gate -> image -> deploy; branch->env (dev/main/tag).
  - Tasks: `[ ]` pipeline YAML; `[ ]` test gate; `[ ]` image + push.
- `[ ]` **F10-S2 - OpenShift manifests + Redis wiring**
  - `deployment`, `service`, `route`, `config-map`, `hpa` under `build/k8s`; Redis connection secret (depends
    on F11 TOQ-03); HPA for NFR-2/NFR-5.
  - Tasks: `[ ]` manifests; `[ ]` config-map/secrets; `[ ]` HPA.
- `[ ]` **F10-S3 - Azure Bot resource + Teams app manifest**
  - Azure Bot (messaging endpoint -> prod route); Teams manifest (`bots` scope `personal`), single-tenant;
    sideload -> tenant-admin approval (EC-07/TR-08).
  - Tasks: `[ ]` Azure Bot + app id/password (Vault); `[ ]` Teams manifest; `[ ]` sideload + approval (IT).
- `[x]` **F10-S4 - Containerisation & local dev compose** - DONE (mirrors the Transaction Backend convention)
  - `build/docker/Dockerfile` - runtime-only image over a pre-published output (`COPY publish /app`, chmod +x,
    net10 `aspnet:10.0-bookworm-slim`), matching `Soulful.StoreSystems.TransactionBackend.Payment`.
  - `src/docker-compose.yml` (named stack, `container_name` per service, pinned images) runs only the local
    **dependencies** - Redis + a WireMock Employee Data API stub (`src/wiremock/mappings`) - **not** the app; the
    dev runs the bot from the IDE/CLI against them. README "Run locally" + "Container image" sections. `docker
    compose config` validated.
  - Tasks: `[x]` Dockerfile (build/docker); `[x]` src/docker-compose.yml + WireMock mappings; `[x]` README.
    Note: bot's AOAI key stays in `appsettings.Development.json` (git-ignored, IDE run); Graph identity still
    needs creds for a full turn (not stubbed).
  - Note: the deployment **environments** (dev/test/prod) are covered across F1-S4 (per-env `appsettings`),
    F10-S1 (branch->env deploy), and F10-S2 (per-env config-maps/secrets) - no separate env ticket needed.

## F11 - Pre-Build Gates & Spikes

The open TOQ/AC gates. **These gate downstream build work** - resolve before/at the noted point.

- `[x]` **F11-S1 - Spike: CP-08 host + CloudAdapter runtime confirm (TOQ-05)** - DONE
  - Closed via the F1-S2 spike: plain host starts, `/api/messages` serves, model replies end-to-end. (The
    "401 without a valid Connector JWT" variant is deferred to F10-S3 when real bot credentials are set.)
- `[!]` **F11-S2 - Gate: Graph employeeId population coverage (TOQ-04 / AC-2 / OQ-3 / TA-01)** - HARD pre-build gate
  - Validate with IT/HR that Teams identity -> `employeeId` is populated and matches API `employeeNumber` across
    the pilot population. Everything in F4/F5/F6/F7 rests on this.
  - Tasks: `[ ]` confirm mapping source + owner; `[ ]` measure coverage %; `[ ]` decide override-map fast-follow
    (TR-01) if gaps.
- `[!]` **F11-S3 - Gate: Redis provisioning on OpenShift (TOQ-03 / TA-05)** - before build (blocks F3-S4)
  - Instance, HA posture, Vault connection secret.
  - Tasks: `[ ]` request instance from Platform; `[ ]` HA decision; `[ ]` Vault secret.
- `[!]` **F11-S4 - Gate: CA-gateway auth for bot -> Employee Data API in prod (TOQ-02 / TA-06)** - before prod
  - Endpoint + auth specifics for the internal call (test has auth disabled, AC-6).
  - Tasks: `[ ]` confirm gateway endpoint; `[ ]` auth mechanism + credential (Vault); `[ ]` prod network path.

---

## Traceability (spec -> work item)

| Spec item | Covered by |
|-----------|------------|
| FR-1.1 / 1.2 / 1.3 | F5-S1 / F5-S2 / F5-S3 |
| FR-2.1 / 2.2 / 2.3 | F6-S2 / F6-S1 / F6-S3 |
| FR-3.1 / 3.2 / 3.3 | F7-S1 / F7-S2 / F7-S3 |
| FR-4.1 / 4.2 / 4.3 / 4.4 | F4-S1 / F4-S3 / F4-S4 / F4-S4 |
| BR-01..04 (pace) | F2-S2 |
| BR-05/06 (motivation upsert) | F6-S2 |
| BR-07 (types pick list) | F6-S1 |
| BR-08 (privacy/clamp) | F2-S4, F4-S1 |
| BR-09 (backend unavailable) | F8-S2 |
| EC-01..10 | AC on F5/F6/F7 + F4-S3 (EC-10) |
| NFR-1 latency | F8-S1, F9-S4 |
| NFR-2 availability | F10-S2 (HPA), F8-S1 |
| NFR-3 freshness ("as at") | F5-S1 (+ all hours cards) |
| NFR-4 security/privacy | F8-S3, F2-S4 |
| NFR-5 scale | F10-S2 |
| 10 handler contracts (section 11) | F5/F6/F7 |
| Domain services (KA-04/05) | F2-S2/S3 |
| Clients KA-01/02/03/06 | F3-S2/S1/S3/S4 |
| Config schema (section 15) | F1-S4 |
| Observability (section 17) | F8-S1 |
| Security (section 18) | F8-S3 |
| Error handling (section 16) | F8-S2 |
| Testing (section 19) | F9 + F2 unit |
| Deployment (section 14) | F10 |
| TOQ-02/03/04/05 | F11-S4/S3/S2/S1 |
| TR-01..11 | mitigations as tasks/AC across F2/F3/F4/F8/F11 |

## Not covered (intentionally)

- v2 scope: G-05 leadership roll-up/export (OS-6), pro-rating (BR-04/AC-5), proactive nudges (OS-4). Excluded
  per product spec; revisit when the backend supports roll-up/export.
- OS-1/2/3/5/7 (backend changes, editing raw hours, standalone app, number-entry lookup, badges) - out of scope.
