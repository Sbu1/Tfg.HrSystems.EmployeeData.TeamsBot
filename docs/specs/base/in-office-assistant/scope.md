# Scope — In-Office Hours Assistant

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

## ➕ In Scope

| # | Item | Rationale (traces to goal) |
|---|------|---------------------------|
| IS-1 | Employee: see own current-month MTD hours + gap to 100h | G-01 |
| IS-2 | Employee: see own last-6-months history | G-01 |
| IS-3 | Employee: see standing vs anonymised peers, current month | G-01, G-03 |
| IS-4 | Employee &/or manager: add / view / remove motivations for shortfall months (one per type per month) | G-01, G-03 |
| IS-5 | Manager: team view — current + historic hours per direct report per month | G-02 |
| IS-6 | Manager: at-risk list — direct reports projected to miss 100h this month | G-02 |
| IS-7 | Delivery as a conversational assistant inside Microsoft Teams, with identity-aware access | All |

## ➖ Out of Scope

| # | Item | Rationale for exclusion |
|---|------|------------------------|
| OS-1 | Backend changes (e.g. badge auto-assignment) | Backend team's responsibility; auto-assign not yet built server-side |
| OS-2 | Editing/correcting raw in-office hours | Hours are read-only from source; only motivations are written |
| OS-3 | Standalone web / PWA / mobile app | Superseded by the Teams channel (D-1) |
| OS-4 | Proactive / scheduled messaging (bot speaks first) | v1 is pull-only; nudges are manager-initiated; proactive messaging is a fast-follow |
| OS-5 | Number-entry lookup of arbitrary employees | Replaced by identity-aware access (D-3) |
| OS-6 | Top-management roll-up + export (G-05) | Deferred to **v2** — no API support today (no cross-manager roll-up / export); the Top-management/CTO persona is therefore a v2 concern |
| OS-7 | Badges / gamification | Removed by user decision — no clear need for v1 (D-4); badge API endpoints go unused |

> v1 personas = **Employee** + **Manager**. Scope tensions resolved: identity → identity-aware (D-3);
> nudges → pull-only (OS-4); export → v2 (OS-6); badges → removed (D-4).

## 💭 Decision Context

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| S-DC-01 | Identity-aware access (resolve the Teams user) | Ask the user for an employee number | Removes spoofing/number-entry friction and enforces privacy; adds an identity→employee mapping dependency (D-3) |
| S-DC-02 | Pull-only for v1 | Proactive nudges / scheduled messages | Keeps v1 simple and avoids notification-policy concerns; proactive messaging is a deliberate fast-follow (OS-4) |
| S-DC-03 | Top-management roll-up/export deferred to v2 | Build leadership export in v1 | No API support exists today; building it would require backend work outside v1 (OS-6) |
| S-DC-04 | Badges removed from v1 | Keep manager-assigns-badges + badge viewing | No clear v1 need; removing it cut a whole requirement cleanly (D-4) |

> Reasoning trail: see `reasoning.md` § Scope reasoning.
