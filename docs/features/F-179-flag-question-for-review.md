---
feature: F-179
epic: Exam Simulator
status: idea
board: 248
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
With an external ticket (`/agile:idea feature --ticket PRJ-123`): the id is the ticket's, `feature: PRJ-123`, the file docs/features/PRJ-123-<slug>.md, and an optional header line `ticket-url: <URL>` right after `board:`.
Status flow: idea -> refining -> approved -> building -> validating -> done
- cancelled: exit from idea or refining only, as a duplicate; the file stays, with "Duplicate of <id> (<date>): <where the improvement went>" under the header.
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
- blocked: set by `/agile:change <id> --block "<reason>"` from refining, approved, building or validating; the line `Blocked (<date>): <what stops it> — unblocked by <who> — returns to <status>` sits under the header and `--unblock` returns the item to that status.
Remove these comments when the file leaves `idea`.
-->
# Flag a question for review

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured.
- Next action: Owner types `/agile:refine F-179`.
- Waiting on: the owner

## Summary
During a simulation the student flags a question to come back to it later; the answer sheet shows which questions are flagged. Seen in the Stitch reference of F-169 (`docs/features/mockups/F-169-take-exam-edition.stitch.png`, the "Flag for review" button) and left out of F-169's scope, which shows only answered, blank and current.

## Start
- Depends on: F-169 (the simulator screen and its answer sheet).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: unknown — settled at /agile:refine.
- Parallel with: unknown — settled at /agile:refine.

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

<!-- One block per screen the item adds or changes (a page, dialog, window or mobile screen; none for a command or an API alone). The whole screen, main state first, `[new]` / `[changed]` on what the item touches, 60 columns at most; the build copies it into the flow page. -->
### Wireframe: <screen name>
```text
+------------------------------+
| <Screen title>               |
| [ Field ____________ ] [new] |
+------------------------------+
```

## Security
<!-- Only when the item touches a door (references/security-section.md); remove the whole section when it touches no door. Each threat gets one `AC<n> [sec]` criterion below. -->
### Surfaces
- S1 <route / action / webhook / upload / hook> — <who can reach it>
### Threats
- T1 (S1, <S|T|R|I|D|E>) <threat in one line> → AC<n> [sec]
### Personal data
<!-- Only when the item handles personal data; the retention comes from the answer to question 9b when the project has one. -->
| Datum | Lives in | Read by | Kept for | Legal basis |
| --- | --- | --- | --- | --- |

## Docs
<!-- Written at refinement (references/tech-pages.md): one line per technical page the item touches, or `- none — <reason>`. Kinds: flow (behavior added or changed), how-to (a way of working), reference (a contract or a setting), architecture (a module or an external dependency). The build writes the pages with the code; the ship checks each one and refreshes the index of each theme. -->
- flow: new docs/flows/<slug>.md — <why>

## Acceptance criteria
<!-- Given / When / Then. Each one is covered by a test. Always include the localization criterion. -->
- AC1 Given <context>, when <action>, then <result>.
- AC<n> All new texts appear in every locale of `UI locales:`.

## Criterion → test
<!-- Written by /agile:build at its start, one row per criterion above, in the same order. Test: the test name(s), `pending` (no test yet) or `validation step <n>` (only checkable on screen). -->
| Criterion | Test |
| --- | --- |
| AC1 | pending |

## Decisions
<!-- date — decision — reason — source. Technical decisions made by Claude are recorded here too. The source: `asked`, `confirmed: <path or item id>` or `changed: <old rule> (<where>)` (references/decisions.md in the plugin). -->
- 2026-10-27 — Epic Exam Simulator — flagging belongs to taking the paper in the simulator — asked
- 2026-10-27 — No similar item (223 items read and the board); F-172 is the review after an attempt, not a flag during it — Claude (technical)

## Out of scope
- <Item>

## Open questions
<!-- Approval is blocked while any line here is not answered or marked "deferred (owner, YYYY-MM-DD)". -->
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build. -->

## Events

## Delivery
<!-- Filled by /agile:ship. -->
