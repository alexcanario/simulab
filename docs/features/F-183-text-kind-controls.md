---
feature: F-183
epic: Data type controls
status: idea
board: 253
version: 1
---
# Text kind controls: text area, e-mail, URL, code, one-time code, search

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured by the breakdown of the epic data-type-controls.
- Next action: `/agile:refine F-183` (owner); it waits on nothing.
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
