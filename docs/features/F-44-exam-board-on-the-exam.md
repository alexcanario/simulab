---
feature: F-44
epic: Assessment catalog
status: idea
board: 767
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
# Exam board on the exam, and the issuing authority's acronym off the screen

## Summary
Two changes the owner asked for after reading F-43's mockup (2026-09-25). First, the exam gains the **exam
board** (`Organizer`) as an extra field on its form and as a filter on `/admin/exams`, beside the issuing
authority it already has: the owner wants to see and filter exams by board before the editions of F-35 exist.
The exam keeps belonging to its issuing authority — the owner chose not to swap the two, because F-34 v2 split
them precisely to stop a board being picked where the contracting body is required. Second, the issuing
authority's **acronym leaves the screen**: it disappears from the dialog and the list columns and the picker
shows the name alone, while the column stays in the table, optional and without its unique index, so nothing
already typed is lost.

The board field on the exam is a stopgap with a known end: when F-35 brings editions, the board belongs to the
edition, and this field either becomes the edition's default or goes away. Written down here so the trade-off
is not rediscovered later.

## Start
- Depends on: F-34 (`done`, merged as `3386b40`) — the exam, the issuing authority, their screens and
  `AppLookupField` all come from there. Not blocked.
- Waits on: nothing. The three decisions were taken by the owner on 2026-09-25: the board is an extra field
  and not a swap, the acronym is hidden and not dropped, and both live in their own item instead of inside
  F-43.
- Suggested path: `/agile:refine` → `/agile:build`. It carries a migration (an optional board column on
  `catalog.exams`, and the acronym's unique index dropped), so it is not an autopilot candidate.
- Parallel with: unknown — settled at `/agile:refine`. It touches the same three Catalog screens F-43
  restyles, so running both at once would conflict; the likely order is F-43 first, then this one.

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
