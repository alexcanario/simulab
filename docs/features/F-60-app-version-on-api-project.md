---
feature: F-60
epic: Foundation and identity
status: idea
board: 99
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# The app version lives on the Api project

## Summary
The app has no version today: `grep -rn "<Version>"` over `src`, `tools` and `tests` (`.csproj` and `.props`) finds nothing (2026-10-02). agile@canary 0.0.97 expects one `<Version>` on `src/Hosts/Simulab.Api/Simulab.Api.csproj` and nowhere else — never `Directory.Build.props`, `SharedKernel`, a module, a `Contracts` project, `ServiceDefaults` or `AppHost` — so `/agile:ship` bumps it once per ship (a feature raises MINOR, a bug raises PATCH). Raised by `/agile:sync` to 0.0.97 (capability since 0.0.79); the owner chose to capture it on 2026-10-02.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:autopilot F-60` (small and clear) — unknown, settled at `/agile:refine`.
- Parallel with: unknown — settled at `/agile:refine`.
