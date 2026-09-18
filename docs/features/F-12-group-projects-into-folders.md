---
feature: F-12
epic: Foundation and identity
status: idea
board: 716
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title and summary only (/agile:idea).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Group projects into folders

## Summary
Technical chore, after F-4 ships. Move the projects into grouped folders on disk, with solution folders in `Simulab.slnx` that mirror them: `src/Hosts/` (Api, Web, AppHost, ServiceDefaults), `src/BuildingBlocks/` (SharedKernel, Persistence, Email), `src/Modules/Identity/` (as today), and the same division under `tests/`. One `Directory.Build.props` per group, importing the root one and holding only what the group shares; a single `Directory.Packages.props` stays at the root so versions live in one place. Updates project references, the architecture tests that read paths, `profile.md` (layout) and the app host. Decided by the owner on 2026-09-18 (move on disk; one packages file; separate item so F-4 merges clean).

## Goal
<!-- Why this feature exists, in one or two sentences. -->

## Users and use cases
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <Rule>.

## Screens and API
<!-- Routes, main components, endpoints (always /api/v1/...), error codes. -->
- <Route> — <purpose>
- <METHOD> /api/v1/<resource> — <purpose>
- Error codes: `<area>.<error>`

## Acceptance criteria
<!-- Given / When / Then. Each one is covered by a test. Always include the localization criterion. -->
- AC1 Given <context>, when <action>, then <result>.
- AC<n> All new texts appear in pt-BR, pt-PT and en.

## Decisions
<!-- date — decision — reason. Technical decisions made by Claude are recorded here too. -->
- <YYYY-MM-DD> — <decision> — <reason>

## Out of scope
- <Item>

## Open questions
<!-- Approval is blocked while any line here is not answered or marked "deferred (owner, YYYY-MM-DD)". -->
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
