# Reasoning Trail — In-Office Hours Assistant

> Audit trail for the spec process. **Process note:** this product spec's thinking was developed through
> structured, one-question-at-a-time Q&A conducted *inside plan mode* across two working sessions (the
> `/specs product` workflow run informally), not via separate `/reasoning` skill invocations. Each section
> below records the equivalent Understand → Evaluate → Challenge → Conclude output. Decisions in the
> signed-off specs trace back here.

## Brief reasoning

| Field | Value |
|-------|-------|
| **Mode** | Audit + interrogation (read supplied material, then interrogated gaps) |
| **Source material** | `Employee Data API` README + example payloads (`Downloads/README.md`); `HR Architecture and Frameworks.txt`; `JourneyInformationCollection` questionnaire |
| **Persona** | none (general reasoning) |
| **Date** | 2026-06-30 |

### Conclusions

- The problem is **timely, self-service, two-sided visibility** of progress toward 100h, replacing a late
  and inconsistently-shared weekly spreadsheet.
- The backend already exists and is rich (10 endpoints across employee/manager/motivations/badges); the new
  work is a **delivery + experience** layer, not new backend capability.
- Delivery channel pivoted to a **Microsoft Teams chatbot**, superseding the web/PWA stack in the
  architecture doc.

### Decision context appendix

- The architecture doc's Next.js/PWA stack was treated as reference for the *prior* plan; the channel change
  to Teams is recorded as decision D-1 and deferred to the technical spec for stack reconciliation.

---

## Problem & Goals reasoning

| Field | Value |
|-------|-------|
| **Mode** | Interrogation |
| **Source material** | none (built on the brief) |
| **Persona** | Problem Analyst |
| **Date** | 2026-06-30 |

### Conclusions

- Cost of inaction: the goal is missed for lack of timely visibility, not lack of effort; managers can't
  intervene proactively; transparency depends on a manual, inconsistent hand-off.
- Five goals confirmed: self-service visibility (G-01), early intervention (G-02), higher attainment
  (G-03, +10pts within 3 months of a pilot vs a month-1 baseline), retire the manual spreadsheet (G-04),
  leadership self-service export (G-05).
- A **Top management / CTO** persona surfaced from the API's `cto` field (hierarchy CTO → Manager → Employee).
  Feasibility flag: the documented API has no top-management roll-up or export endpoint.

### Decision context appendix

- `PG-DC-01` — G-03 baseline is unknown until live; captured as OQ-1, measured in month 1.
- `PG-DC-02` — G-05 (leadership export) kept as a goal but flagged infeasible against today's API → later
  deferred to v2 in scope.

---

## Scope reasoning

| Field | Value |
|-------|-------|
| **Mode** | Interrogation |
| **Source material** | none |
| **Persona** | Scope Definer |
| **Date** | 2026-06-30 |

### Conclusions

- v1 in scope: employee self-view (current + history + peer standing), motivations (add/view/remove),
  manager team view + at-risk, delivered in Teams (IS-1…IS-7).
- Resolved three scope tensions: identity → **identity-aware** (not number entry); nudges → **pull-only**
  (proactive messaging is a fast-follow); leadership export → **deferred to v2** (no API support today).
- v1 personas fixed to **Employee + Manager**.

### Decision context appendix

- `S-DC-01` — identity-aware access replaces number entry (D-3); adds an identity→employee mapping dependency.
- `S-DC-02` — proactive/scheduled messaging out of v1 (OS-4); v1 is pull-only.
- `S-DC-03` — top-management roll-up/export deferred to v2 (OS-6); CTO persona is a v2 concern.
- `S-DC-04` — **badges/gamification removed from v1** (D-4); original "manager assigns badges" requirement
  dropped, badge API endpoints go unused.

---

## Requirements reasoning

> Four confirmed scope areas: 1) Employee self-view · 2) Motivations · 3) Manager team & at-risk ·
> 4) Conversational experience & identity. Worked area-by-area.

### Area 1 — Employee self-view

| Field | Value |
|-------|-------|
| **Mode** | Interrogation |
| **Source material** | API README example payloads |
| **Persona** | Requirements Analyst |
| **Date** | 2026-06-30 |

#### Conclusions

- FR-1.1 current-month self view; FR-1.2 6-month history; FR-1.3 anonymised peer leaderboard with "you"
  highlighted.
- Defined the **working-day linear pace** business rule and named its hidden assumptions (public holidays
  ignored; flat 100h goal).
- Edge cases: zero-hour entries render normally; fewer than 6 months shows what exists; no peers → just the
  employee; the employee's own current-month figure comes from the employee record, not the masked peer
  list, and must be merged to rank.

#### Decision context appendix

- `R-DC-01` — pace = `100 × (working days elapsed ÷ working days in month)`, Mon–Fri; at-risk = projected
  month-end total < 100. Early-month guard added in refinement.

### Area 2 — Motivations

| Field | Value |
|-------|-------|
| **Mode** | Interrogation |
| **Source material** | API README (motivation + motivationtype endpoints) |
| **Persona** | Requirements Analyst + Domain Analyst |
| **Date** | 2026-06-30 |

#### Conclusions

- FR-2.1 log, FR-2.2 view, FR-2.3 remove; employee for own, manager for a team member.
- BR — qualifying month (completed month under 100h, or current month once behind); BR — one per type per
  month (upsert); types come from the API (9 types) as a pick list, never free-typed ids.
- Gap surfaced: authorship not stored (`createdBy = System`) → employee- vs manager-logged is
  indistinguishable in data (→ risk R-2).

#### Decision context appendix

- `R-DC-02` — re-logging the same type+month updates (upsert), with a confirm prompt.

### Area 3 — Manager team & at-risk

| Field | Value |
|-------|-------|
| **Mode** | Interrogation |
| **Source material** | API README (managerteam endpoint) |
| **Persona** | Requirements Analyst |
| **Date** | 2026-06-30 |

#### Conclusions

- Three discrete commands (user preference: explicit, no single default view): FR-3.1 team this month,
  FR-3.2 team history (default 6 months), FR-3.3 who's at risk (sorted by largest projected shortfall).
- Manager sees **only their direct reports**; skip-level roll-up is the deferred v2 export.
- Edge cases: non-manager → "no team found"; large team → summarise/paginate; member with no data → 0h.

#### Decision context appendix

- `R-DC-03` — manager scope = direct reports only; reuses the pace BR from Area 1.

### Area 4 — Conversational experience & identity

| Field | Value |
|-------|-------|
| **Mode** | Interrogation |
| **Source material** | none |
| **Persona** | Requirements Analyst |
| **Date** | 2026-06-30 |

#### Conclusions

- FR-4.1 identity & role resolution; FR-4.2 guided + natural-language interaction; FR-4.3 unknown user
  handling; FR-4.4 write confirmation.
- BRs: privacy (self + masked peers; managers see only direct reports); backend-unavailable → graceful
  "service unreachable" message, never a raw error or stale guess.

#### Decision context appendix

- `R-DC-04` — employee commands available to all; team commands gated to managers, scoped to direct reports.

---

## Risks & Assumptions reasoning

| Field | Value |
|-------|-------|
| **Mode** | Interrogation |
| **Source material** | none (review across the whole spec) |
| **Persona** | Risk Examiner |
| **Date** | 2026-06-30 |

### Conclusions

- Six assumptions (AC-1…AC-6), seven risks (R-1…R-7), five open questions (OQ-1…OQ-5).
- Sharpest items: identity→employee mapping is a hard launch dependency (AC-2/R-1/OQ-3); disabled API auth
  means the bot is the only access-control layer (AC-6/R-3/OQ-4); no motivation authorship (R-2); flat 100h
  goal may be unfair to part-time/new/leave staff (AC-5/R-4 — flat confirmed for v1, pro-rating v2).

### Decision context appendix

- `RA-DC-01` — flat 100h goal for v1, pro-rating deferred to v2 (closes OQ-2).
- `RA-DC-02` — data currency = T-1 (totals through the previous day); the bot always shows an "as at"
  timestamp (closes OQ-5, validates AC-4, updates NFR-3).

---

## Loop-backs

| Date | Triggered from | Returned to | Reason | Resolution |
|------|----------------|-------------|--------|------------|
| 2026-06-26 | Requirements (Area 4) | Scope | User dropped badges mid-Requirements | Scope amended: OS-7 added, original badge requirement removed (D-4); badge endpoints go unused |
| 2026-06-26 | Problem & Goals | Scope | Top-management export (G-05) infeasible vs current API | Deferred to v2: OS-6; CTO persona is a v2 concern |
| 2026-06-26 | Requirements (Area 1) | Risks & Assumptions | Pace rule exposed hidden assumptions | AC-1 (holidays), AC-5 (flat goal) recorded; early-month guard added |
| 2026-06-30 | Materialisation (this session) | — | Identity *mechanism* (Graph employeeId), bot stack, state store, etc. decided | Captured in the **technical** spec working notes; product spec stays at business altitude (OQ-3 remains open here) |
| 2026-06-30 | Technical spec (Phase 1) | Product spec review | LLM cost/PII reconsidered | Product spec already signed off; tech-spec switched NLU to Azure OpenAI `gpt-4o-mini` with an intent-only (no-PII) design — no product-spec change needed |

---

# Technical Spec reasoning

> Technical spec started 2026-06-30. Developed through one-question-at-a-time Q&A this session (audit of the
> reference repo `Tfg.HrSystems.EmployeeDiscountManagementSystem`, the `/architectural-patterns` standard, and
> the Employee Data API README), then materialised. Equivalent Understand→Conclude captured below.

## Technical Challenges reasoning

| Field | Value |
|-------|-------|
| **Mode** | Audit + interrogation |
| **Source material** | `Tfg.HrSystems.EmployeeDiscountManagementSystem` (reference repo), `/architectural-patterns`, Employee Data API README, `HR Architecture and Frameworks.txt` |
| **Persona** | Technical Architect |
| **Date** | 2026-06-30 |

### Conclusions

- Hardest parts: identity→employee+role resolution (rests on Graph `employeeId`); bot-side pace/at-risk +
  leaderboard maths; privacy enforced entirely bot-side while API auth is disabled; cheap in-tenant NLU within
  the latency budget; state across HPA pods; graceful degradation of 4 externals.
- Constraints: SOLID + Clean Architecture (no DB ⇒ no Repository/UoW), direct-DI handlers (no MediatR), mirror
  the reference structure, host via `Tfg.ApiShell`, secrets via Vault, OpenShift deploy, in-tenant LLM only.

### Decision context appendix

- `TC-DC-01` — NLU via Azure OpenAI `gpt-4o-mini` (not Claude/Opus): cost + in-tenant governance.
- `TC-DC-02` — intent-only design keeps PII out of the model.

---

## Architecture reasoning

| Field | Value |
|-------|-------|
| **Mode** | Audit + interrogation |
| **Source material** | reference repo structure, `/architectural-patterns`, `Tfg.ApiShell` usage in the reference |
| **Persona** | Technical Architect |
| **Date** | 2026-06-30 |

### Conclusions

- Single stateless ASP.NET Core service (Bot Framework over `Tfg.ApiShell`), HPA on OpenShift, Redis for shared
  bot state, calling Employee Data API + Microsoft Graph + Azure OpenAI.
- Feature-sliced Clean Arch mirroring the reference, prefix `EmployeeData.TeamsBot`; outbound clients under
  `Application.{Concern}`; pure pace/leaderboard logic in `Domain.Services`; Redis state in `Infrastructure.State`.
- Direct-DI handler per use case; composition root in `Presentation.Bot` via the ApiShell hook.

### Decision context appendix

- `A-DC-01` — clients in Application (per reference), not Infrastructure.
- `A-DC-02` — pace/leaderboard as pure Domain services (the only real domain logic); card rendering stays in Presentation.

---

## Data & Contracts reasoning

### External integrations (Employee Data API, Microsoft Graph, Azure OpenAI, Bot Connector)

| Field | Value |
|-------|-------|
| **Mode** | Audit |
| **Source material** | Employee Data API README (example payloads), Microsoft Graph + Azure OpenAI + Bot Framework references |
| **Persona** | Technical Architect + Domain Analyst |
| **Date** | 2026-06-30 |

#### Conclusions

- No relational DB; only transient Redis state (identity cache + conversation). Contracts are the 4 external
  interfaces + the internal handler contracts. No internal event bus.
- Security clamp: caller's employee number overrides any model-emitted value; `delete_motivation` ownership-checks.

#### Decision context appendix

- `DC-DC-01` — bot exposes no business REST API (only Bot Framework `/api/messages`); team response envelope N/A to the bot's own surface.
- `DC-DC-02` — motivation types fetched live (no seed data).

---

## Technical Risks reasoning

| Field | Value |
|-------|-------|
| **Mode** | Interrogation |
| **Source material** | none (review across Phases 1–3 + product §14/§15/§16) |
| **Persona** | Risk Examiner |
| **Date** | 2026-06-30 |

### Conclusions

- Sharpest technical risks: Graph `employeeId` unpopulated (identity fails); clamp/ownership bug (privacy leak);
  Azure OpenAI latency/throttling vs NFR-1; Redis outage; intent misclassification; PII leakage to the model.
- Every product AC- mapped binding/non-binding to a technical assumption (TA-); every product R- mapped to a
  technical mitigation.

### Decision context appendix

- `TR-DC-01` — accept conversation-context loss on Redis outage (re-resolve identity; graceful), rather than add a second store.
