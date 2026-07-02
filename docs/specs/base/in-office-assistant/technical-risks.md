# Technical Risks — In-Office Hours Assistant

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
| 1.1.0 | 2026-07-01 | Review pass: added TA-07 (ApiShell hosts Bot Framework), TR-09 (ApiShell↔BF conflict + spike), TR-10 (custom Redis IStorage), TOQ-05. Reverted to DRAFT for the fix, re-approved. | SibusisoSik |
| 1.1.1 | 2026-07-01 | TOQ-05 spike (inspection): ApiShell bundles AutoWrapper response-wrapping + Vault-at-startup → conflict confirmed; TR-09 mitigated via CP-08 (plain host); TOQ-05 narrowed to a dev/CI runtime confirm. Patch. | SibusisoSik |

## ✅ Approval Record

| Version | Status | Approved By | Role | Approval Date |
|---------|--------|-------------|------|---------------|
| 1.0.0 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-06-30 |
| 1.1.0 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-07-01 |
| 1.1.1 | ✅ APPROVED | SibusisoSik | Engineering Lead | 2026-07-01 |

---

## 🔒 Technical Assumptions

| # | Description | Impact if wrong | Owner | Status |
|---|-------------|-----------------|-------|--------|
| TA-01 | Microsoft Graph `employeeId` is populated for TFG Infotech users and numerically matches the Employee Data API's `employeeNumber` | Identity resolution fails; bot unusable for affected users | HR Systems / IT | open |
| TA-02 | `GET /api/managerteam` returns exactly the caller's direct reports (and empty for non-managers) | Role gating wrong / over-broad team visibility | HR Systems | validated |
| TA-03 | `timeInHoursMonthToDate` is authoritative and current to T-1 | Pace/at-risk wrong; trust lost | HR Systems / data | validated |
| TA-04 | Azure OpenAI is enabled in the TFG tenant with a `gpt-4o-mini` deployment | No NLU; only button-driven flows work | IT / Platform | open |
| TA-05 | A Redis instance is available in-cluster on OpenShift | No shared state; degraded multi-pod behaviour | Platform | open |
| TA-06 | The Employee Data API is reachable internally via the CA gateway in prod | Bot can't fetch data in prod | Security / Backend | open |
| TA-07 | `Tfg.ApiShell` can host a Bot Framework `CloudAdapter` + `/api/messages` with Connector JWT auth (or the CP-08 plain-host fallback applies) | Host won't start / auth conflicts on the bot endpoint | Eng | open |

## ⚠️ Technical Risks

| # | Risk | Likelihood | Impact | Mitigation | Owner | Status |
|---|------|-----------|--------|-----------|-------|--------|
| TR-01 | Graph `employeeId` unpopulated/mismatched → identity fails | Med | High | Validate coverage pre-build (AC-2 gate); if patchy, a Vault/config override map as fast-follow; clear FR-4.3 message meanwhile | HR Systems / IT | open |
| TR-02 | Azure OpenAI latency/throttling breaches NFR-1 | Med | Med | `gpt-4o-mini` + `temperature 0` + low `max_tokens`; timeout + retry/backoff; typing indicator; button fallback bypasses the LLM | Eng | mitigated |
| TR-03 | Clamp/ownership bug → cross-employee data leak | Med | High | Centralised clamp in the dispatcher; `remove_motivation` ownership pre-check; targeted unit/integration tests (TS-04) | Eng | mitigated |
| TR-04 | Redis outage → state loss | Med | Low | Proceed stateless: re-resolve identity from Graph, reset conversation context; reconnect | Eng | accepted |
| TR-05 | Intent misclassification → wrong tool/answer | Med | Med | Constrained function schema; `clarify` intent; write confirmation (FR-4.4); buttons for the common paths | Eng | mitigated |
| TR-06 | PII leakage to the LLM | Low | High | Intent-only design (TD-14): only the user's text + function defs are sent; log redaction; no employee data in prompts | Eng | mitigated |
| TR-07 | Pace/at-risk errors at month boundaries / around holidays | Med | Med | Pure `Domain.Services` with unit tests; early-month guard (BR-02); holidays-ignored is a stated v1 constraint (AC-1) | Eng | mitigated |
| TR-08 | Teams tenant-admin approval / app registration delays launch | Med | Med | Engage IT early on Azure Bot + Teams app registration & policy (R-7) | IT | open |
| TR-09 | `Tfg.ApiShell` middleware conflicts with the Bot Connector on `/api/messages` — **confirmed**: ApiShell bundles `AutoWrapper.Core` (wraps responses, breaking the Connector) and loads Vault at startup | Med | High | **Adopt CP-08**: plain ASP.NET Core minimal host runs the `CloudAdapter` + `/api/messages` with no AutoWrapper; reuse ApiShell only for config/observability/health. Runtime confirm in dev/CI (TOQ-05) | Eng | mitigated |
| TR-10 | Custom Redis `IStorage` concurrency/ETag bugs corrupt or lose bot state | Low | Med | Implement the Bot Framework `IStorage` contract faithfully (ETag optimistic concurrency); concurrency integration tests; state is non-critical (TR-04) | Eng | mitigated |

## ❓ Open Questions

| # | Question | Owner | Deadline | Status |
|---|----------|-------|----------|--------|
| TOQ-01 | Is Azure OpenAI enabled in the tenant, in which region, and what is the `gpt-4o-mini` deployment name? | IT / Platform | Before build | open |
| TOQ-02 | CA-gateway specifics (endpoint, auth header/cert) for the bot → Employee Data API call in prod | Security / Backend | Before prod | open |
| TOQ-03 | Redis provisioning on OpenShift (instance, HA, connection secret in Vault) | Platform | Before build | open |
| TOQ-04 | Confirmation of Graph `employeeId` population coverage (shared with product OQ-3 / AC-2) | IT / HR | Before build | open |
| TOQ-05 | Runtime spike (dev/CI — needs internet + TFG feed creds + Vault): confirm the **CP-08 plain host** + `CloudAdapter` start and `/api/messages` returns **401 without a valid Connector JWT**. (Inspection already confirmed the ApiShell AutoWrapper/Vault conflict → CP-08 selected; sandbox run blocked by no outbound network to restore `Microsoft.Bot.Builder.*`.) | Eng | Before build | open |

## 🔁 Coverage of Product-Spec Risks

| Product Spec Risk | Technical Mitigation | Owner |
|-------------------|---------------------|-------|
| R-1 (identity mapping incomplete) | TR-01 (pre-build validation gate; override-map fast-follow; FR-4.3 message) | HR Systems / IT |
| R-2 (no motivation authorship) | Accepted product-side; bot adds no authorship; no technical mitigation attempted | Backend / Product |
| R-3 (API auth disabled bypasses privacy) | TD-11 internal-only + CA gateway (TA-06); bot-side clamp (TR-03) | Security / Backend |
| R-4 (flat 100h unfair) | Out of technical scope (v2 pro-rating); pace constant is configurable (§Config) to ease a future change | HR / Product |
| R-5 (data lag → distrust) | "As at" T-1 timestamp rendered on every hours view (RA-DC-02) | Eng |
| R-6 (managers keep the spreadsheet) | Non-technical (adoption) | Management |
| R-7 (Teams tenant-admin approval) | TR-08 (engage IT early) | IT |

## 🔁 Coverage of Product-Spec Assumptions

| Product Spec Assumption | Binds technical decision? | Mapped technical assumption | Rationale |
|-------------------------|---------------------------|----------------------------|-----------|
| AC-1 (ignore public holidays) | Yes | TA-03 (data/pace) | `WorkingDayCalendar` counts Mon–Fri only; configurable to add a holiday calendar later |
| AC-2 (identity mapping exists) | Yes | TA-01 | Whole identity resolver depends on it; hard pre-build gate (TR-01) |
| AC-3 (manager role derivable) | Yes | TA-02 | Role gating uses `managerteam` non-empty result |
| AC-4 (`timeInHoursMonthToDate` authoritative) | Yes | TA-03 | All pace/leaderboard maths consume this field |
| AC-5 (flat 100h, no pro-rating) | Yes | TA-03 | Goal is a constant (`100`) in `Domain.Constants`/config; pro-rating is v2 |
| AC-6 (API auth disabled, bot is access layer) | Yes | TA-06 | Drives the clamp design (TR-03) + internal-only network (TD-11) |

## 💭 Decision Context

| # | Decision | Alternatives Considered | Rationale |
|---|----------|------------------------|-----------|
| TR-DC-01 | Accept conversation-context loss on Redis outage | Add a second durable store | Context is non-critical; re-resolving identity is cheap; avoids extra infra |
| TR-DC-02 | Make the 100h goal + early-month threshold + working-day rule configuration values | Hard-code them | Eases the v2 pro-rating/holiday changes (R-4, AC-1) without a redesign |

> Reasoning trail: see `reasoning.md` § Technical Risks reasoning.
