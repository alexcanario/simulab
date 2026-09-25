---
feature: F-42
epic: Assessment catalog
status: idea
board: 763
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
# State picker for the exam scope

## Summary
F-34 stores the exam's scope detail as free text, so `SP`, `Sao Paulo` and `São Paulo` can sit in the same
column. Offer the 27 Brazilian states as a fixed list in that field when the scope is `State`, keeping free
text for `Municipal` (5570 municipalities are a feature of their own). Raised while refining F-34 (owner,
screen question 2, 2026-09-24). Epic 704 (Portugal exams) has to revisit the list: Portugal has districts,
not states.

## Start
- Depends on: F-34 — the field, the scope enum and the form page it sits on come from there.
- Waits on: nothing.
- Suggested path: `/agile:autopilot F-42` — one field on one screen, with a fixed list.
- Parallel with: none decided — unknown, settled at `/agile:refine`.

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

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-42
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
