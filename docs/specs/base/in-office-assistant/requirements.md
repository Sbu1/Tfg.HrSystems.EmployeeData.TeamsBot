# Requirements — In-Office Hours Assistant

## 📋 Document Control

| Field | Value |
|-------|-------|
| **Version** | 1.0.0 |
| **Status** | ✅ APPROVED |
| **Created** | 2026-06-30 |
| **Last Modified** | 2026-06-30 |
| **Author(s)** | SibusisoSik |

### 📝 Changelog

| Version | Date | Changes | Changed By |
|---------|------|---------|------------|
| 1.0.0 | 2026-06-30 | Initial draft, materialised from prior plan-mode capture | SibusisoSik |

## ✅ Approval Record

| Version | Status | Approved By | Role | Approval Date |
|---------|--------|-------------|------|---------------|
| 1.0.0 | ✅ APPROVED | SibusisoSik | Product Manager | 2026-06-30 |

---

## 🛠️ Functional Requirements

### Area 1 — Employee self-view

| # | Requirement | Description | Acceptance Criteria | Priority |
|---|------------|-------------|---------------------|----------|
| FR-1.1 | Current-month self view | Employee asks how they are tracking this month | On request, the bot returns MTD in-office hours, the 100h goal, the gap remaining, and an on-track / behind / at-risk status (per the pace rule BR-01), with an "as at" date | Must |
| FR-1.2 | 6-month history | Employee asks for their recent history | Bot lists up to the last 6 months — hours per month plus a goal-met indicator (≥100h); shows fewer months if fewer exist | Must |
| FR-1.3 | Peer standing (current month) | Employee asks how they compare to their team | Bot shows an anonymised ranked leaderboard of the employee's team (`player1…`, with "you" highlighted) and each one's hours, plus the employee's own rank position | Should |

### Area 2 — Motivations

| # | Requirement | Description | Acceptance Criteria | Priority |
|---|------------|-------------|---------------------|----------|
| FR-2.1 | Log a motivation | Employee (self) or manager (team member) records why a month fell short | Picks a motivation type from the API list, a qualifying month, and a description; saved against employee + type + month; multiple types per month allowed; write confirmed (FR-4.4) | Must |
| FR-2.2 | View motivations | See logged motivations | Employee sees own; manager sees a team member's; shows type, month, description, date logged | Must |
| FR-2.3 | Remove a motivation | Delete a logged motivation by selection | Employee for own, manager for a team member; removal confirmed and acknowledged | Should |

### Area 3 — Manager team view & at-risk

| # | Requirement | Description | Acceptance Criteria | Priority |
|---|------------|-------------|---------------------|----------|
| FR-3.1 | "Team this month" | Manager asks for current team standing | Per direct report: name, current MTD hours, and on-track/behind/at-risk status (BR-01) | Must |
| FR-3.2 | "Team history" | Manager asks for team history | Per direct report: hours per month over N months (default 6), with goal-met indicators | Should |
| FR-3.3 | "Who's at risk" | Manager asks who may miss the goal | List of direct reports projected to miss 100h this month (BR-01), sorted by largest projected shortfall | Must |

### Area 4 — Conversational experience & identity

| # | Requirement | Description | Acceptance Criteria | Priority |
|---|------------|-------------|---------------------|----------|
| FR-4.1 | Identity & role resolution | Bot identifies the user and their role | Bot resolves the Teams user → employee number + role; employee commands available to all; team commands gated to managers, scoped to their direct reports only | Must |
| FR-4.2 | Guided + natural-language interaction | Bot understands free text and offers shortcuts | Bot accepts free-form requests and offers quick-action buttons/cards; a help/onboarding message lists what it can do, role-appropriately | Must |
| FR-4.3 | Unknown user | Bot can't map the user | If the Teams user can't be mapped to an employee number, the bot explains and points to HR/support; shows no data and allows no lookup | Must |
| FR-4.4 | Write confirmation | Confirm before/after writes | Logging or removing a motivation is confirmed before commit, and success/failure is acknowledged | Must |

## ⚙️ Business Rules & Edge Cases

| # | Rule / Edge case | Applies to | Description |
|---|------------------|-----------|-------------|
| BR-01 | On-pace / at-risk (working-day linear) | FR-1.1, FR-3.1, FR-3.3 | Expected hours by today = `100 × (working days elapsed ÷ working days in month)`, working days = Mon–Fri. *Behind* = MTD below expected; *at risk* = projected month-end total (`MTD ÷ fraction of working days elapsed`) below 100 |
| BR-02 | Early-month guard | FR-1.1, FR-3.3 | Before ~30% of the month's working days have elapsed, show pace but label it "too early to call at-risk" to avoid noisy early projections |
| BR-03 | Public holidays ignored (v1) | BR-01 | Working-day count uses weekdays only and ignores public holidays (assumption AC-1) |
| BR-04 | Flat 100h goal (v1) | BR-01 | The 100h goal is flat for everyone; no pro-rating for part-time/new/leave staff in v1 (pro-rating = v2; assumption AC-5) |
| BR-05 | Qualifying month for motivations | FR-2.1 | Allowed only for a completed month under 100h, or the current month once behind/projected to miss; declined (with explanation) for months that met the goal |
| BR-06 | One motivation per type per month (upsert) | FR-2.1 | Re-logging the same type+month updates the existing entry; the bot surfaces "you already have a {type} motivation for {month} — update it?" |
| BR-07 | Motivation types from the API | FR-2.1 | The 9 motivation types come from the API and are presented as a pick list, never free-typed ids |
| BR-08 | Privacy | FR-1.x, FR-3.x, FR-4.1 | Employees see only their own data + masked peers; managers see only their direct reports; never cross-team |
| BR-09 | Backend unavailable | All | On API failure the bot says the hours service is unreachable and to retry — never a raw error or a stale guess |
| EC-01 | Zero-hour entries | FR-1.3, FR-3.1 | Zero-hour figures (e.g. `player5 = 0`) render normally, not as errors |
| EC-02 | Fewer than 6 months of data | FR-1.2, FR-3.2 | Show what exists |
| EC-03 | No peers / not on a team | FR-1.3 | Leaderboard shows just the employee (or "no peers this month") |
| EC-04 | Own current-month figure not in peer list | FR-1.3 | The employee's own current-month figure comes from the employee record, not the masked peer list, and must be merged in to rank |
| EC-05 | Caller is not a manager | FR-3.x | "No team found"; team commands unavailable / politely declined (role gating) |
| EC-06 | Large team | FR-3.1, FR-3.2 | Bot summarises / paginates rather than dumping a huge list |
| EC-07 | Team member with no current-month data | FR-3.1 | Shown as 0h / no-data, not an error |
| EC-08 | No motivations logged | FR-2.2 | "None recorded" |
| EC-09 | Manager-logged motivation, pull-only | FR-2.1 | When a manager logs on an employee's behalf, the employee is not notified (OS-4) — they see it next time they look |
| EC-10 | Ambiguous request | FR-4.2 | Bot asks to clarify or offers buttons |

## 💭 Decision Context

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| R-DC-01 | Working-day-linear pace with early-month guard | Calendar-day linear; no pace status | Working days reflect office expectation better; the early-month guard avoids false at-risk noise (BR-01, BR-02) |
| R-DC-02 | Upsert on type+month for motivations | Allow duplicates; block re-logging | Matches the API's uniqueness key and gives a clean "update it?" UX (BR-06) |
| R-DC-03 | Manager scope = direct reports only | Skip-level / org roll-up | Matches what the API returns and the privacy rule; roll-up is the v2 export (BR-08) |
| R-DC-04 | Three explicit manager commands | One combined default team view | User preference for explicit commands over an opinionated default |

> Reasoning trail: see `reasoning.md` § Requirements reasoning.
