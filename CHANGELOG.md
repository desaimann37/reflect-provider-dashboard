# Changelog

All notable changes to the Reflect Provider Dashboard extension.

## [0.1.0] - 2026-10-17

### Added
- **Provider area** (`/Provider`, policy `ProviderOnly`):
  - Dashboard, My caseload, patient details with seven tabs, RTM worklist, and Outcomes report.
  - Create forms for management time and notes, and an edit page for the care plan.
  - Single-click confirmed RTM period sign-off.
- **Domain model** separating product analytics (`EngagementSession`, pseudonymous) from clinical data:
  - `Patient`, `PatientAppLink`, `CarePlan`, `Observation`, `ProviderTimeEntry`, `ClinicalNote`, `RtmPeriodReview`, `PhiAccessLog`, `ProviderProfile`.
  - Repository interfaces for each.
- **RTM calculator**:
  - 30-day device-supply periods and calendar-month management time.
  - 2026 CPT codes 98975, 98978, 98986, 98979, 98980 and 98981.
  - Readiness states (on track, at risk, ready to sign, signed, not monitoring) with plain-English reasons.
- **Insight engine**:
  - PHQ-9 item 9 safety flag, PHQ-9/GAD-7 worsening and improvement, inactivity, engagement drop, mood decline and mood lift, no data since enrollment, RTM at risk / ready.
  - Engagement and RTM rules are skipped for paused care plans.
- **Pre-session brief**:
  - Rule-based by default.
  - Optional Claude (`claude-opus-5-5`, low effort, server-side fallback) from de-identified input only.
  - Falls back to rules on any error or refusal.
- **Firebase/GA4-shaped analytics events** and `EngagementEventMapper`, which turns them into sessions the way the production sync job would.
- **Synthetic data seeder**: 2 providers and 23 fictional patients across 13 clinical personas, relative to today.
- **PHI access log** written on every patient view and brief request. Patient pages send `Cache-Control: no-store`.
- **Stand-ins**, isolated in `Standins/` and `wwwroot/standins/`:
  - Page shell with sidebar, header, breadcrumb, theme toggle and accessibility panel (theme, color-blind mode, text size).
  - Demo sign-in with user switcher, `ICurrentUserService`, and `GetCspNonce()` plus the CSP header (strict nonce mode by default).
  - Shared JS services: `MessageService`, `displayLoading`, `js-confirm-submit`, `dt-client`, `initTabDataTableFix`, `initTabPersistence`.
- **Vendored libraries** at the spec's versions, plus Chart.js 4.4.9. IBM Plex `.woff2` fonts. Reflect XR logos from the official brand package (shell only).
- **83 automated tests**: domain rules plus spec-compliance checks.
- **Documentation**: README, setup, architecture, APIs, demo script, two-page summary report.
