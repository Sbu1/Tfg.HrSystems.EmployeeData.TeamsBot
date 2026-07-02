# Architecture — In-Office Hours Assistant

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
| 1.1.0 | 2026-07-01 | Review pass: Redis `IStorage` specified as a custom implementation (ETag semantics); ApiShell↔Bot Framework hosting approach + fallback documented. Reverted to DRAFT for the fix, re-approved. | SibusisoSik |
| 1.1.1 | 2026-07-01 | TOQ-05 spike finding (ApiShell bundles AutoWrapper response-wrapping + Vault-at-startup): elevated CP-08 (plain ASP.NET Core host, ApiShell for config/observability only) from fallback to the recommended host approach. Patch — selects an already-documented option. | SibusisoSik |

## ✅ Approval Record

| Version | Status | Approved By | Role | Approval Date |
|---------|--------|-------------|------|---------------|
| 1.0.0 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-06-30 |
| 1.1.0 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-07-01 |
| 1.1.1 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-07-01 |

---

## 🏗️ Architecture Overview

A single stateless ASP.NET Core service (a plain minimal host — CP-08 — reusing `Tfg.ApiShell` for
config/observability/health only) implements a Microsoft Teams bot. It runs
as horizontally-scaled pods on OpenShift behind a route; the Bot Framework Connector delivers Teams activities
to `POST /api/messages`. Each turn it resolves the caller's identity (Microsoft Graph, cached in Redis),
classifies intent with Azure OpenAI (`gpt-4o-mini`, function calling — sent only the user's text, never PII),
dispatches to a use-case handler that reads/writes the **Employee Data API**, computes pace/leaderboard in pure
domain logic, and renders the result as an Adaptive Card or text. Shared state (identity cache + recent
conversation context) lives in **Redis** so it survives pod churn. No relational database.

![Architecture](diagrams/architecture/overview.png)
*Source: `diagrams/architecture/overview.mmd`*

## 🧱 Architectural Patterns

| # | Pattern | Applies To | Justification | Product Spec Ref |
|---|--------|-----------|--------------|-----------------|
| AP-01 | Clean Architecture (layered, inward deps) | Whole solution | Team non-negotiable; isolates testable domain logic from integrations | NFR-4, FR-1.1 |
| AP-02 | Direct-DI handler per use case (CQRS-lite, no MediatR) | Application layer | Team idiom; one testable handler per command/query; no DB ⇒ no Repository/UoW | FR-1.x, FR-2.x, FR-3.x |
| AP-03 | Adapter/Gateway behind Domain ports | Employee Data API, Graph, Azure OpenAI | Swappable integrations + fakes for tests + a single place for resilience | FR-4.1, BR-09 |
| AP-04 | Bot Framework `IStorage` over Redis | State | Shared, durable state across HPA pods (no affinity) | NFR-2, NFR-5 |
| AP-05 | Security clamp in the dispatcher | Tool dispatch | Caller's employee number overrides any model value; delete ownership-checked | NFR-4, BR-08 |
| AP-06 | Resilience (timeout + retry + circuit-breaker, Polly) | All outbound clients | Bounded latency; graceful "service unreachable" never a raw error | NFR-1, BR-09 |
| AP-07 | Observability (logs→Dynatrace, `/metrics`, correlation id) | Whole solution | Team non-negotiable; diagnose in prod without code changes | NFR-2 |

## 🛠️ Tech Stack

| Layer | Technology | Rationale | Standard Compliant |
|-------|-----------|-----------|-------------------|
| Language / runtime | C# / .NET 10 | Matches backend + C:\Dev portfolio | Yes |
| Host | **Plain ASP.NET Core minimal host (CP-08, recommended)** + `Tfg.ApiShell` components for config (Vault), logging/observability, and health checks | The TOQ-05 inspection found ApiShell bundles **AutoWrapper.Core** (response-wrapping) and loads **Vault at startup**; AutoWrapper would wrap `/api/messages` responses and break the Bot Connector. A plain host runs the `CloudAdapter` + `/api/messages` with `BotFrameworkAuthentication` cleanly, reusing ApiShell only for cross-cutting concerns (no AutoWrapper over the bot route). Full `Shell<T>` hosting stays possible only if `/api/messages` is excluded from AutoWrapper + default auth — to be confirmed by the runtime spike in dev/CI (TR-09) | Deviation: documented (CP-08); ApiShell used for config/observability only |
| State store impl | **Custom** `IStorage` over StackExchange.Redis | Bot Framework ships Memory/Blob/Cosmos providers only — no Redis provider; we implement `Read`/`Write`/`Delete` with **ETag optimistic concurrency** + JSON serialisation | Deviation: custom provider — documented (TD-7) |
| Bot framework | Microsoft.Bot.Builder (CloudAdapter, ActivityHandler) | Standard Teams bot plumbing | Yes |
| NLU | Azure OpenAI `gpt-4o-mini` via `Azure.AI.OpenAI` SDK (function calling) | In-tenant, cheap, intent-only | Deviation: first LLM use on the team — documented (TD-13) |
| UI | Adaptive Cards | Structured Teams views (FR-4.2) | Yes |
| State store | Redis (in-cluster) via Bot Framework `IStorage` | Shared state across pods | Deviation: reference uses `Tfg.Cache.InMemory`; Redis needed for multi-pod state — documented (TD-7) |
| Resilience | Polly | Timeouts/retries/circuit-breakers on outbound calls | Yes |
| Secrets | HashiCorp Vault (via ApiShell config) | Team convention | Yes |
| Deploy | OpenShift (`build/k8s`), Azure DevOps `Tfg.Build.Templates` | Team convention | Yes |
| Observability | Dynatrace (logs/metrics) + correlation ids | Team convention | Yes |

## 📦 Module Structure & Dependencies

> Assembly prefix `EmployeeData.TeamsBot` (drops `Tfg.HrSystems.`, mirroring `EmployeeDiscountManagement.*`).
> Solution file `Tfg.HrSystems.EmployeeData.TeamsBot.sln`.

### Module list

| Module | Layer | Responsibility |
|--------|-------|---------------|
| `EmployeeData.TeamsBot.Domain.Models` | Domain | Value objects: `PaceStatus`, `MonthHours`, `EmployeeStanding`, `LeaderboardRow`, `Leaderboard`, `TeamMemberStanding`, `MotivationView`, `MotivationType`, `CallerRole` |
| `EmployeeData.TeamsBot.Domain.Interfaces` | Domain | Ports: `IEmployeeDirectory`, `IEmployeeDataClient`, `IConversationIntentService`, `IPaceCalculator`, `ILeaderboardBuilder`, `IBotStateStore` |
| `EmployeeData.TeamsBot.Domain.Services` | Domain | Pure logic: `PaceCalculator`, `LeaderboardBuilder`, `WorkingDayCalendar` |
| `EmployeeData.TeamsBot.Domain.Observability` | Domain | Metric names + log message constants |
| `EmployeeData.TeamsBot.Domain.Constants` | Domain | Goal (100h), early-month threshold, intent names |
| `EmployeeData.TeamsBot.Application.Handlers` | Application | One direct-DI handler per use case |
| `EmployeeData.TeamsBot.Application.Models` | Application | Handler request/response DTOs; `IntentResult` |
| `EmployeeData.TeamsBot.Application.Validators` | Application | Parameter validation (month format, motivation type id, qualifying month) |
| `EmployeeData.TeamsBot.Application.HealthCheck` | Application | Readiness/liveness contributors |
| `EmployeeData.TeamsBot.Application.EmployeeDataApi` | Application | Typed `HttpClient` + DTOs for the Employee Data API |
| `EmployeeData.TeamsBot.Application.Graph` | Application | Microsoft Graph directory client (`employeeId` lookup, token) |
| `EmployeeData.TeamsBot.Application.ConversationAi` | Application | Azure OpenAI client; function/tool definitions; intent resolution |
| `EmployeeData.TeamsBot.Infrastructure.State` | Infrastructure | Redis-backed Bot Framework `IStorage` + `IBotStateStore` |
| `EmployeeData.TeamsBot.Presentation.Bot` | Presentation | `Program.cs` (ApiShell), `Dependencies/*Registration.cs`, `BotController` (`/api/messages`), `EmployeeBot` ActivityHandler, `Cards/` Adaptive Card builders |
| `EmployeeData.TeamsBot.Tests` | Test | Unit + integration tests (under `test/`) |

### Dependency graph

| Module | Depends On | Must NOT Depend On |
|--------|-----------|-------------------|
| `Domain.*` | (nothing outside Domain) | Application, Infrastructure, Presentation, any SDK |
| `Application.Handlers` | Domain.Interfaces, Domain.Models, Domain.Services, Application.Models | Infrastructure, Presentation |
| `Application.EmployeeDataApi` / `.Graph` / `.ConversationAi` | Domain.Interfaces, Domain.Models, Application.Models | Presentation; each other |
| `Application.Validators` / `.Models` / `.HealthCheck` | Domain.* | Infrastructure, Presentation |
| `Infrastructure.State` | Domain.Interfaces, Domain.Models | Application.Handlers, Presentation |
| `Presentation.Bot` | Application.*, Infrastructure.* (composition root only), Domain.* | — (it is the top) |

![Module dependencies](diagrams/architecture/modules.png)
*Source: `diagrams/architecture/modules.mmd`*

## 🔑 Key Abstractions

| # | Abstraction | Purpose | Operations | Owner module |
|---|------------|---------|------------|--------------|
| KA-01 | `IEmployeeDirectory` | Map a Teams user to their employee number | `Task<int?> GetEmployeeNumberAsync(string aadObjectId, CancellationToken ct)` | Domain.Interfaces |
| KA-02 | `IEmployeeDataClient` | All Employee Data API access | `GetEmployeeAsync(int employeeNumber)`, `GetManagerTeamAsync(int employeeNumber, int months)`, `GetMotivationsAsync(int employeeNumber, int lastXMonths)`, `AddMotivationAsync(AddMotivationRequest req)`, `DeleteMotivationAsync(int id)`, `GetMotivationTypesAsync()` | Domain.Interfaces |
| KA-03 | `IConversationIntentService` | Classify the user's message into an intent + parameters (no PII sent) | `Task<IntentResult> ResolveIntentAsync(string userText, IReadOnlyList<ConversationTurn> history, CancellationToken ct)` | Domain.Interfaces |
| KA-04 | `IPaceCalculator` | Working-day pace + at-risk status | `PaceStatus Evaluate(int mtdHours, DateOnly asAt)` | Domain.Interfaces |
| KA-05 | `ILeaderboardBuilder` | Merge caller's own figure into masked peers and rank | `Leaderboard Build(EmployeeStanding self, IReadOnlyList<LeaderboardRow> maskedPeers)` | Domain.Interfaces |
| KA-06 | `IBotStateStore` | Read/write identity cache + conversation context | `GetIdentityAsync(string aadObjectId)`, `SetIdentityAsync(string aadObjectId, int employeeNumber, TimeSpan ttl)`, `GetConversationAsync(string conversationId)`, `AppendTurnAsync(string conversationId, ConversationTurn turn)` | Domain.Interfaces |
| KA-07 | `IRequestHandler<TRequest,TResponse>` | One per use case (GetMyHours, GetMyHistory, GetPeerStanding, ListMotivations, AddMotivation, RemoveMotivation, GetMotivationTypes, GetTeamThisMonth, GetTeamHistory, GetAtRisk) | `Task<TResponse> HandleAsync(TRequest request, CancellationToken ct)` | Application.Handlers (interface from `Tfg.Handler.Abstractions`) |

## 🧷 Service Registration Strategy

Composition root is `Presentation.Bot`. Per the TOQ-05 finding, `Program.cs` builds a **plain
`WebApplication`** (minimal host, CP-08) rather than `Tfg.ApiShell.Shell<T>`, and reuses ApiShell's
**config (Vault), logging/observability, and health-check** components as libraries — so AutoWrapper never
wraps `/api/messages`. Registration keeps the reference's per-concern extension-method style:

- `AddBotAdapter()` — `CloudAdapter`, `BotFrameworkAuthentication`, `IBot` → `EmployeeBot`.
- `AddConversationAi(config)` — Azure OpenAI client (`Azure.AI.OpenAI`) + `IConversationIntentService`.
- `AddEmployeeDataApi(config)` — typed `HttpClient` (`IEmployeeDataClient`) with Polly + CA-gateway handler.
- `AddGraphDirectory(config)` — `IEmployeeDirectory` (client-credentials Graph client).
- `AddBotState(config)` — the **custom** `RedisStorage : IStorage` (ETag-based) + `IBotStateStore`, `UserState`, `ConversationState`.
- `AddBotAdapter()` — `CloudAdapter` + `BotFrameworkAuthentication`; `/api/messages` is the only auth-gated bot route (Connector JWT). On the plain host there is **no AutoWrapper** over the pipeline, so bot responses pass through unwrapped (TC-07). The runtime spike (TOQ-05) in dev/CI confirms the plain host + adapter start cleanly.
- `AddDomainServices()` — `IPaceCalculator`, `ILeaderboardBuilder`, `WorkingDayCalendar`.
- `AddHandlers()` — every `IRequestHandler<,>` (scoped) via direct DI; **no MediatR**.
- `AddValidators()`, `AddHealthChecks()`, `AddObservability()`.

Lifetimes: clients/handlers scoped; `IStorage`, configuration, and the Azure OpenAI client singleton; the Graph
token is cached with refresh. No deviation from team defaults beyond the documented Redis + Azure OpenAI additions.

## 💭 Decision Context

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| A-DC-01 | Outbound clients under `Application.{Concern}` | Put them in Infrastructure | Mirrors the reference repo (`Application.Hcm`, `Application.HttpServices`) |
| A-DC-02 | Pace/leaderboard as pure `Domain.Services` | Put compute in Application/handlers | They are the only real domain logic; pure + unit-testable in Domain |
| A-DC-03 | Card building stays in `Presentation.Bot` | A Domain/Application `ICardBuilder` port | Adaptive Cards are a Teams/presentation concern; handlers return data DTOs |
| A-DC-04 | Redis `IStorage` (not `Tfg.Cache.InMemory`) | Per-pod in-memory | HPA multi-pod with no affinity needs shared state (TD-6/TD-7) |

> Reasoning trail: see `reasoning.md` § Architecture reasoning.
