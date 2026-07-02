# Problem & Goals — In-Office Hours Assistant

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
| 1.1.0 | 2026-06-30 | Review pass: moved G-05 out of v1 goals into a Future Objectives (v2) note so all v1 goals trace to in-scope items | SibusisoSik |

## ✅ Approval Record

| Version | Status | Approved By | Role | Approval Date |
|---------|--------|-------------|------|---------------|
| 1.0.0 | ✅ APPROVED | SibusisoSik | Product Manager | 2026-06-30 |
| 1.1.0 | ✅ APPROVED | SibusisoSik | Product Manager | 2026-06-30 |

---

## ❓ Problem Statement

Employees are expected to spend **100 hours per calendar month** in the office, but today they have
**no way to see their own running total** — they are told to self-track with no tool to do it. The hours
data does exist: managers receive a **large spreadsheet weekly**, but:

- Managers **don't always share** those hours with their employees, so employees stay in the dark.
- Managers **can't readily see who is at risk** of missing 100 hours in time to encourage them.
- Both sides effectively **wait until the month-end spreadsheet** to learn whether the goal was met — by
  which point it is too late to change the outcome.

**Cost of inaction:** the goal is missed for lack of timely visibility, not lack of effort; managers can't
intervene proactively; transparency depends on a manual, inconsistent spreadsheet hand-off.

The core pain is **timely, self-service, two-sided visibility of progress toward the 100-hour goal**,
replacing a late and inconsistently-shared weekly spreadsheet.

## 🎯 Goals & Success Criteria

| # | Goal | Target (numeric or verifiable) | Measurement |
|---|------|-------------------------------|-------------|
| G-01 | **Self-service visibility** — any employee sees their own month-to-date hours + gap to 100h on demand | Employee can retrieve current MTD hours and gap-to-100 in Teams in a single ask, any time, without contacting their manager | Capability exists; adoption = % of staff who use it per month |
| G-02 | **Early intervention** — at-risk employees visible mid-month, not month-end | At any point in the month a manager can list every team member projected to miss 100h; available from day 1, not month-end | Capability exists; lead time (days before month-end the at-risk list is available) |
| G-03 | **Higher goal attainment** — more employees actually reach 100h | Capture month 1 as baseline; **+10 percentage-point** increase in the share of employees reaching 100h within **3 months** of launch | Monthly % of active employees reaching 100h, from the hours data |
| G-04 | **Retire the manual spreadsheet** — no more weekly manual hand-off | Weekly manual spreadsheet distribution stops; the assistant becomes the trusted source | Binary (manual send discontinued); % of managers relying on the tool |

> Hierarchy surfaced from the data: **CTO → Manager → Employee**. v1 goals are G-01…G-04 — each is served by
> one or more in-scope items.

### 🔮 Future Objectives (v2 — not v1 goals)

> Recorded for continuity; **out of v1 scope** (see Scope OS-6) and therefore not part of the v1 goal set
> above. Carried into the eventual v2 spec.

| # | Objective | Why deferred |
|---|-----------|--------------|
| G-05 | **Leadership self-service export** — top management retrieves/exports hours for all employees beneath them | The documented API has no top-management roll-up or export endpoint; unbuildable on today's backend. The Top-management/CTO persona is therefore a v2 concern |

## 💭 Decision Context

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| PG-DC-01 | G-03 measured against a month-1 baseline captured at launch | Assume a fixed prior baseline | The current attainment rate is unknown until the tool is live; baseline must be measured, not assumed (→ OQ-1) |
| PG-DC-02 | Keep leadership export as goal G-05 but mark it v2 | Drop it entirely; build it in v1 | The need is real (leadership visibility) but unbuildable on today's API; retained as a goal, deferred in scope |

> Reasoning trail: see `reasoning.md` § Problem & Goals reasoning.
