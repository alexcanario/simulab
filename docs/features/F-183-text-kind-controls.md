---
feature: F-183
epic: Data type controls
status: refining
Autopilot: stop 1 (2026-10-28)
board: 253
version: 1
---
# Text kind controls: text area, e-mail, URL, code, one-time code, search

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: refining (2026-10-28)
- Stopped at: Refinement started by the epic run data-type-controls; premises being checked
- Next action: Stop 1: answer the question round and approve F-183 (owner)
- Waiting on: nothing

## Summary
The kit gains `AppTextAreaField` (auto-grow, counter), `AppEmailField` (trim, lower-case, `inputmode=email`, `Autocomplete` required), `AppUrlField` (http(s) check, open-link adornment), `AppCodeField` (no spaces, monospace), `AppOtpField` (6 digits, paste fills all) and `AppSearchField`, in the gallery. The screens move to them: the e-mail fields of `SignIn`, `SignUp`, `ForgotPassword`, `ResetPassword`, `CheckEmail` and `VerifyEmail`, the one-time code fields of `SignIn` and `Security` (3), the notice URL and the multi-line fields of `IssuingAuthorityDialog`, `OrganizerDialog` and `AiDiagnostics`. Catalog: `docs/design/data-type-controls.md`, section 2.1.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-184.

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner

## Events

- 2026-10-28 question — asked 3, from context 5
