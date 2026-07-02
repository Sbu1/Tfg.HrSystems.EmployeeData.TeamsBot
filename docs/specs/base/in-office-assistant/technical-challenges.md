# Technical Challenges — In-Office Hours Assistant

## 📋 Document Control

| Field | Value |
|-------|-------|
| **Version** | 1.1.0 |
| **Status** | ✅ APPROVED |
| **Created** | 2026-06-30 |
| **Last Modified** | 2026-07-01 |
| **Author(s)** | SibusisoSik |

### 📝 Changelog

| Version | Date | Changes | Changed By |
|---------|------|---------|------------|
| 1.0.0 | 2026-06-30 | Initial draft | SibusisoSik |
| 1.1.0 | 2026-07-01 | Review pass: added TC-07 (host a Bot Framework adapter inside `Tfg.ApiShell`) + CP-08 (plain-host fallback). Reverted to DRAFT for the fix, re-approved. | SibusisoSik |

## ✅ Approval Record

| Version | Status | Approved By | Role | Approval Date |
|---------|--------|-------------|------|---------------|
| 1.0.0 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-06-30 |
| 1.1.0 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-07-01 |

---

## ⚠️ Core Technical Challenges

| # | Challenge | Why it is hard | Where the risk lives | Product Spec Ref |
|---|----------|---------------|---------------------|-----------------|
| TC-01 | Resolve a Teams user → employee number + role | No employee number in the Teams activity; depends on Microsoft Graph `employeeId` being populated; role must be derived without an org-chart service | Identity resolver; Graph data quality | FR-4.1, FR-4.3, AC-2 |
| TC-02 | Compute pace / at-risk / leaderboard bot-side from raw hours | Backend returns raw hours only; working-day maths, early-month guard, and merging the caller's own figure into a masked peer set are all bot responsibilities | Domain pace/leaderboard logic | FR-1.1, FR-1.3, FR-3.1, FR-3.3, BR-01, BR-02 |
| TC-03 | Enforce all privacy/access control bot-side while API auth is disabled | The bot is the *only* gate; any tool parameter could be abused; delete-by-id has no server-side ownership check | Tool dispatcher clamp; delete ownership check | NFR-4, BR-08, AC-6 |
| TC-04 | Cheap, in-tenant NLU within the latency budget, without leaking PII | Must interpret free-form text + route to a tool in ≤8s p95 across round-trips, keep employee data out of the model, and degrade gracefully on ambiguity | Conversation/intent service | FR-4.2, NFR-1, NFR-4 |
| TC-05 | Maintain identity + conversation state across HPA-scaled, affinity-less pods | Teams turns can land on different pods; in-memory state splits | Distributed state store | NFR-2, NFR-5 |
| TC-06 | Degrade gracefully when any of 4 externals is unavailable | Employee Data API, Graph, Azure OpenAI, or Redis can each fail; the bot must never show a raw error or stale guess | Resilience layer around each client | BR-09, NFR-2 |
| TC-07 | Host a Bot Framework adapter inside the REST-oriented `Tfg.ApiShell` host | ApiShell adds its own header/auth/health middleware for REST APIs; the Bot Connector needs its own JWT auth and raw request body on `/api/messages`, which can conflict | Host composition / middleware order | NFR-2, FR-4.1 |

## 🔧 Engineering Constraints

| # | Constraint | Source | Implication |
|---|-----------|--------|-------------|
| EC-01 | SOLID + Clean Architecture are non-negotiable; CQRS optional via direct-DI handlers, **no MediatR** | `/architectural-patterns` | Layered modules, inward dependencies, handler-per-use-case wired by direct DI |
| EC-02 | No relational database | This solution (all data lives in the Employee Data API) | Repository / Unit-of-Work patterns do **not** apply; only transient Redis state |
| EC-03 | Mirror the `Tfg.HrSystems.EmployeeDiscountManagementSystem` repo structure | User directive | Feature-sliced projects `{Component}.{Layer}.{Concern}`; `build/`, `docs/`, `src/`, `test/`; outbound clients under `Application.{Concern}` |
| EC-04 | Host via `Tfg.ApiShell`; secrets via HashiCorp Vault; deploy on OpenShift via `build/k8s` + Azure DevOps `Tfg.Build.Templates`; observe via Dynatrace | Reference repo + team standard | Composition + config + health-check + observability follow the ApiShell hook model |
| EC-05 | Employee Data API auth is disabled; internal-only + CA-gateway in prod | Product AC-6 / TD-11 | The bot is the access-control layer; its outbound calls use the CA-gateway auth pattern |
| EC-06 | LLM must be in-tenant (Azure/Entra) — no consumer/public LLM | POPIA / NFR-4 | Azure OpenAI `gpt-4o-mini` in the TFG tenant; intent-only prompts |
| EC-07 | Microsoft Teams + Bot Framework; tenant-admin app registration required | Delivery channel D-1 | Azure Bot resource + Teams app manifest; IT approval is a launch dependency (R-7) |

## 🧭 Candidate Patterns

| # | Pattern | Addresses | Rationale | Trade-offs |
|---|--------|-----------|-----------|-----------|
| CP-01 | Clean Architecture layering | TC-02, TC-03 | Isolates pure domain logic (pace/leaderboard) for unit testing; keeps integrations swappable | More projects than a single-app bot |
| CP-02 | Direct-DI handler per use case (CQRS-lite) | TC-02, TC-03, TC-04 | Team idiom; one testable handler per command/query; no mediator overhead | Slight boilerplate per use case |
| CP-03 | Adapter / Gateway behind ports | TC-01, TC-04, TC-06 | The 3 externals (Employee Data API, Graph, Azure OpenAI) sit behind interfaces; enables fakes + resilience | Indirection |
| CP-04 | Bot Framework `IStorage` over Redis | TC-05 | Shared, durable state across pods; standard BF pattern | New infra dependency |
| CP-05 | Security clamp in the dispatcher | TC-03 | Caller's employee number overrides any model-emitted value; delete ownership-checked | Must be centralised and tested |
| CP-06 | Timeout + retry/circuit-breaker (Polly) + graceful fallback | TC-06 | Bounded latency; never a raw error/stale data (BR-09) | Tuning thresholds |
| CP-07 | Intent-only LLM (function calling), formatting in code | TC-04, NFR-4 | Model sees only the question text; cards built in code; keeps PII out and the model small | Card logic is hand-built per view |
| CP-08 | Plain-host fallback if ApiShell can't host the adapter | TC-07 | If a pre-build spike shows ApiShell conflicts with the Bot Connector pipeline, host on a plain ASP.NET Core minimal host (documented deviation) and keep ApiShell only for config/observability wiring | Loses some ApiShell convenience |

## 💭 Decision Context

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| TC-DC-01 | NLU via Azure OpenAI `gpt-4o-mini` | Claude Opus/Haiku; self-hosted; Azure CLU | Cheapest sensible, in-tenant, keeps tool-use design; Opus overkill + external + ~100× cost |
| TC-DC-02 | Intent-only design (no PII to the model) | Let the model format answers from data | Removes PII exposure (POPIA/NFR-4) and lets a tiny model suffice; cards are built in code anyway |
| TC-DC-03 | Bot is the sole access-control layer for v1 | Wait for backend auth | API auth disabled; clamp + ownership checks + internal-only network are the v1 mitigation (TD-11) |

> Reasoning trail: see `reasoning.md` § Technical Challenges reasoning.
