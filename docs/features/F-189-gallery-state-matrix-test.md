---
feature: F-189
epic: Data type controls
status: building
board: 259
version: 1
---
# Gallery state matrix test for the kit controls

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-29)
- Stopped at: Approved by --assume; build not started
- Next action: Build: write the tests
- Waiting on: nothing

## Summary
`UiGalleryTextKindsTests` (F-183) only checks that each state caption (empty, filled, error, disabled, read-only, long value) appears somewhere in the section "Text kinds", so a control that lacks a state still passes; the review found the OTP and search controls missing states while the test was green. Add a test that reads each control's own column in the gallery and checks every state that applies to it, so a state dropped from one control fails. The F-183 test file is locked and shipped: this is a new test file, or an unlock recorded in the item. Seen in F-183 review #1 and #6; the missing states themselves were fixed there.

## Start
- Depends on: nothing (F-183 is done).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (lane assignment of the lanes run; no door).
- Suggested path: `/agile:autopilot F-189`.
- Parallel with: F-190.

## Goal
A kit control that loses a state in the gallery fails a test, instead of passing because another control still shows that state.

## What already exists
- `UiGallery.razor` section `#gallery-text-kinds` (`src/Hosts/Simulab.Web/Components/Pages/Dev/UiGallery.razor`): three `div` columns; inside, each control kind has an `h3.gallery-kind-title` followed by pairs of `p.gallery-state-caption` and the control.
- Text area, e-mail, web address, code and one-time code each show six states (Empty, Filled, Error, Disabled, Read-only, Long value); search shows three (Empty, Filled, Long value), because `AppSearchField` has no error, disabled or read-only parameter.
- `UiGalleryTextKindsTests` (F-183, locked) only checks that each caption is somewhere in the section.
- `KitTestContext` and `FakeHostEnvironment` render the gallery in Development.

## Users and use cases
- UC1 A developer drops a state from one kit control in the gallery and a test names the control and the state that is missing.

## Business rules
- BR1 The matrix is per control: for each control kind the gallery section lists the states that apply to it, and each caption is followed by that kind's own control.
- BR2 Applicable states: the six states for text area, e-mail, web address, code and one-time code; Empty, Filled and Long value for search.
- BR3 A state caption that follows a kind's title belongs to that kind only; a caption of another kind never counts for it.

## Screens and API
None. Test only; the gallery markup is not changed.

## Docs
- none — a test-only item; no behaviour, contract or module changes.

## Risks
- none — adds one test file and changes no production code.

## Acceptance criteria
- AC1 Given the gallery in Development, when the section "Text kinds" is read, then each of the six kinds has a block started by its own title.
- AC2 Given the block of text area, e-mail, web address, code or one-time code, then it holds the captions Empty, Filled, Error, Disabled, Read-only and Long value, each followed by that kind's control.
- AC3 Given the block of search, then it holds Empty, Filled and Long value, each followed by the search control.
- AC4 Given a copy of the section where one kind lacks a state its block must hold, when the matrix is checked, then the check reports the kind and the state that is missing.
- AC5 All new texts appear in every locale of `UI locales:` (none are added: the test has no UI text).

## Criterion → test

Tests locked: ea261bd259ff66ca5606e6b9d569c1bbe5e3cabc (1 file); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `UiGalleryStateMatrixTests.Read_Development_EachKindHasABlockStartedByItsOwnTitle` |
| AC2 | `UiGalleryStateMatrixTests.Problems_Development_KindWithSixStatesHoldsEachOfThem` |
| AC3 | `UiGalleryStateMatrixTests.Problems_Development_SearchHoldsEmptyFilledAndLongValue` |
| AC4 | `UiGalleryStateMatrixTests.Problems_CaptionRemovedFromOneKind_NamesTheKindAndTheState`, `Problems_ControlOfAnotherKindUnderACaption_NamesTheKindAndTheState`, `Problems_ExpectedKindWithNoBlock_NamesTheKind` |
| AC5 | validation step 1 |

## Decisions
- 2026-10-29 — New test file `UiGalleryStateMatrixTests`; the locked `UiGalleryTextKindsTests` is left as it is, no unlock — the item allows it and a lock keeps shipped tests stable — assumed: new-file-over-unlock (item summary)
- 2026-10-29 — The test reads the rendered DOM by structure (title, caption, control class), not by control ids — an id rename must not break it, a dropped state must — assumed: technical choice
- 2026-10-29 — Search is expected to show three states only — `AppSearchField` has no error, disabled or read-only parameter — assumed: verified in the code
- 2026-10-29 — The matrix check is a pure function over the section's DOM so AC4 can be proved on a copy with a caption removed, with no change to the gallery — assumed: technical choice
- 2026-10-29: tests unlocked (helper skeleton `GalleryStateMatrix.cs` locked by mistake with the tests; owner answered "continue" in the olinda session); relocked with `UiGalleryStateMatrixTests.cs` alone, the test unchanged.
- 2026-10-29 — The session ran on Sonnet 5.5 up to the first lock, then on Opus 5.5 (owner's `/model`); the lane asked opus — confirmed: lane-run.js WORK line

## Out of scope
- Adding error, disabled or read-only states to the search control.
- Other gallery sections (field kinds, lookups): they have their own tests.

## Open questions
- (none)

## Change notes

## Events

- 2026-10-29 assumed — 5 answers
- 2026-10-29 blocked — test wrong: helper skeleton GalleryStateMatrix.cs locked by mistake with the tests; owner answered 'continue' in the olinda session (2026-10-29)
