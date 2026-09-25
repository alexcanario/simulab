---
feature: F-43
epic: Foundation and identity
status: idea
board: 765
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
# Visual language and kit composition

## Summary
The Simulab screens read as a single column of plain fields on an empty card, while Simulae's back office — whose
palette Simulab already copied — reads as sectioned cards, multi-column form grids, radio cards, status chips and a
summary aside. Give the UI kit the page-composition patterns it lacks, write down the visual language (typography,
spacing, radius and where the accent colour appears in content), and migrate every existing screen to it. Decided in
D-2 (owner, 2026-09-25) after F-34 was rejected on screen for its look. Absorbs B-16.

## Start
- Depends on: F-34 — it must merge first, so the kit gains `AppLookupField` and the exam and issuing-authority
  screens exist to migrate. B-16's file also arrives with that merge.
- Waits on: nothing. The direction is decided in D-2; the reference is Simulae's approved back office.
- Suggested path: `/agile:refine` with `/agile:screen` on the exam form as the reference screen, then
  `/agile:build` — D-2 requires the owner to approve that mockup before any code.
- Parallel with: none — it rewrites the theme and the kit that every other screen uses.

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
- 2026-09-25 — Keep Simulae's base palette; add composition patterns and a written visual language instead of new
  hues — D-2 option B.
- 2026-09-25 — Copy Simulae's composition closely, not as loose inspiration — the owner already validated those
  screens.
- 2026-09-25 — Migrate the kit, the gallery and every existing screen in this item — `ui.md` requires one pattern
  defined once.
- 2026-09-25 — Absorb B-16 (field hint contrast) — same theme file, same kit fields.

## Out of scope
- A new brand: palette, logo and identity of Simulab's own (D-2 option C, parked).
- The exam session screens' distraction-free exception (parked in D-2 until the first such screen exists).

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-43
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
