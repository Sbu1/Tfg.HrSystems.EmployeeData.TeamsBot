# Risks & Assumptions — In-Office Hours Assistant

## 📋 Document Control

| Field | Value |
|-------|-------|
| **Version** | 1.1.0 |
| **Status** | ✅ APPROVED |
| **Created** | 2026-06-30 |
| **Last Modified** | 2026-06-30 |
| **Author(s)** | SibusisoSik |

### 📝 Changelog

| Version | Date | Changes | Changed By |
|---------|------|---------|------------|
| 1.0.0 | 2026-06-30 | Initial draft, materialised from prior plan-mode capture | SibusisoSik |
| 1.1.0 | 2026-06-30 | Review pass: AC-1 & AC-6 set to validated (confirmed v1 decisions); AC-1 reclassified as a constraint; added explicit AC-2 pre-build gate note | SibusisoSik |

## ✅ Approval Record

| Version | Status | Approved By | Role | Approval Date |
|---------|--------|-------------|------|---------------|
| 1.0.0 | ✅ APPROVED | SibusisoSik | Product Manager | 2026-06-30 |
| 1.1.0 | ✅ APPROVED | SibusisoSik | Product Manager | 2026-06-30 |

---

## 🔒 Assumptions & Constraints

> Status values: `open`, `validated`, `invalidated`.

| # | Type | Description | Impact if wrong | Owner | Status |
|---|------|-------------|-----------------|-------|--------|
| AC-1 | Constraint | Working-day pace ignores public holidays (Mon–Fri only) in v1 | People look "at risk" around holidays | Product | validated |
| AC-2 | Assumption | A reliable Teams-identity → employee-number mapping exists / can be provided | Users blocked at FR-4.3; bot unusable | HR Systems / IT | open |
| AC-3 | Assumption | Manager role is derivable from the API / org hierarchy (e.g. has a team) | Team commands can't be gated correctly | HR Systems | validated |
| AC-4 | Assumption | `timeInHoursMonthToDate` is the authoritative, timely hours figure | Tool loses trust as a source | HR Systems / data | validated |
| AC-5 | Constraint | The 100h goal is flat for everyone (no pro-rating) in v1; pro-rating = v2 | Unfair at-risk flags for part-time/new/leave staff | HR / Product | validated |
| AC-6 | Constraint | API auth stays disabled for v1; the bot is the access-control layer | Privacy enforced bot-side only | Security / backend | validated |

> **AC-2 is a hard pre-build gate.** Everything rests on a working identity → employee-number mapping; the
> existence and population coverage of that mapping must be validated with IT/HR **before build** (tracked as
> OQ-3 / R-1). The technical spec may be drafted in parallel, but build does not start until AC-2 is closed.

## ⚠️ Risks & Mitigations

> Status values: `open`, `mitigated`, `accepted`.

| # | Risk | Likelihood | Impact | Mitigation | Owner | Status |
|---|------|-----------|--------|------------|-------|--------|
| R-1 | Identity mapping incomplete → users blocked | Med | High | Confirm the mapping source before launch | HR Systems / IT | open |
| R-2 | No motivation authorship (`createdBy = System`) → no audit / silent edits | High | Med | Accept for v1, or request a backend `addedBy` field | Backend / Product | accepted |
| R-3 | API auth disabled → direct callers bypass bot privacy | Med | High | Network-restrict the API (internal-only) + CA-gateway auth on the bot's calls; enable full auth before broad prod | Security / Backend | mitigated |
| R-4 | Flat 100h unfair to part-time/leave/new joiners → false at-risk | Med | Med | Pro-rating rule (v2) and/or motivation flow + manager discretion | HR / Product | accepted |
| R-5 | Hours data lags / inconsistent → tool distrusted | Med | High | Data currency confirmed T-1; bot always shows an "as at" timestamp | HR Systems | mitigated |
| R-6 | Managers keep using the spreadsheet → tool doesn't displace it | Med | Med | Deliberate spreadsheet retirement + leadership backing | Management | open |
| R-7 | Teams bot needs tenant-admin approval / registration | Med | Med | Engage IT early on app registration & policy | IT | open |

## ❓ Open Questions

> Status values: `open`, `closed`.

| # | Question | Owner | Deadline | Status |
|---|----------|-------|----------|--------|
| OQ-1 | Baseline % reaching 100h (for G-03) | Product | Month 1 of launch | open |
| OQ-2 | Is the 100h goal pro-rated or flat? | HR | 2026-06-26 | closed (flat for v1; pro-rating v2) |
| OQ-3 | Source / owner of the identity → employee-number mapping | IT / HR | Before build | open |
| OQ-4 | Will API auth be enabled / how is the API secured for prod? | Security / Backend | Before prod | open |
| OQ-5 | Refresh cadence of in-office hours data | HR Systems | 2026-06-26 | closed (daily / T-1) |

## 💭 Decision Context

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| RA-DC-01 | Flat 100h goal for v1; pro-rating deferred to v2 | Pro-rate from day one | Pro-rating needs leave/FTE data and rules not available for v1; flat is simple and transparent, with motivations + manager discretion as the relief valve (closes OQ-2) |
| RA-DC-02 | Data currency = T-1; always show an "as at" timestamp | Assert real-time; hide currency | The source totals include through the previous day; surfacing "as at \<date\>" preserves trust without a backend change (closes OQ-5, validates AC-4) |
| RA-DC-03 | Accept no motivation authorship for v1 | Block until backend adds `addedBy` | Authorship is a nice-to-have audit detail; not worth blocking v1; logged as accepted risk R-2 |

> Reasoning trail: see `reasoning.md` § Risks & Assumptions reasoning.
