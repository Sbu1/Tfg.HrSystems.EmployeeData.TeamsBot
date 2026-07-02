# Brief — In-Office Hours Assistant

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

## Context — why this work exists

TFG Infotech staff are expected to spend **100 hours per calendar month** in the office. The hours data
exists — managers receive a large spreadsheet weekly — but employees have no self-service way to see their
own running total, managers can't readily spot who is at risk of missing the goal in time to help, and
both sides effectively wait until the month-end spreadsheet to learn the outcome, by which point it is too
late to change it.

The mission (from Gert) is to give staff a way to track their in-office hours against the goal, see history
and peer standing, explain shortfalls, and (originally) earn badges. A backend already exists —
`Tfg.HrSystems.EmployeeData` (ASP.NET Core / .NET 10, Clean Architecture + CQRS, auth currently disabled,
test base URL live) — exposing an individual employee view, a manager team view, motivations (CRUD + types),
and badges (CRUD + types). Peer data is masked (`player1…`) so it can safely drive gamification scenarios.

This work is also a **Developer AI Experiment**: the journey of building it (decisions, issues, successes,
agent tools/skills used, token burn) is a tracked deliverable alongside the product, captured per developer
in a `JourneyInformationCollection` questionnaire.

## The brief

Staff need visibility of their own month-to-date in-office hours toward the 100-hour goal, their last
6 months of history, and how they compare to (anonymised) peers this month. Where the goal is missed, the
employee or their manager can log a **motivation** (one per leave-type per month, multiple types allowed).
Managers get a team view with per-member current and historic hours and an at-risk list. The experience is
to be delivered as a **conversational bot inside Microsoft Teams**, with **identity-aware** access — the bot
identifies the Teams user and their role rather than asking for an employee number, so employees see only
themselves and managers drill into their own team.

> The brief was refined through one-question-at-a-time interrogation across two working sessions. Two
> material changes were made and folded in: the delivery channel was pivoted from a web/PWA stack to a
> Microsoft Teams chatbot, and **gamification badges were dropped from v1** for lack of a clear need.
> The reasoning trail behind these is in `reasoning.md`.
