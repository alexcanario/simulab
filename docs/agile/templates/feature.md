---
feature: F-<number>
epic: <epic name>
status: idea
board: <work item id or ->
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
# <Feature name>

Technical terms: [glossary](../glossary.md)

## Summary
<!-- 2-3 lines captured from the chat. -->

## Start
<!-- What this item needs before it can start, and how to run it. Filled when the item is created, confirmed at refinement. -->
- Depends on: <items that must be done first: F-/B- ids, an epic, an item in another repository as `<owner>/<repo>#<n>`; while an id is not delivered, the approved item is blocked by itself and released when it ships — or "nothing">
- Waits on (to start): <what stops the work from starting and is not an item: an owner decision, a person or team, an access, an environment — who provides it — or "nothing">
- Needed to validate: <what only the validation needs (a real clone, an environment, a person or device) — who provides it — or "nothing"; never blocks a start or an approval>
- Model: <opus|sonnet> (<signal>|no signal|doubt|project line: always <model>) — written by the refinement, see references/model-choice.md "Main session"
- Suggested path: <`/agile:refine` → `/agile:build` | `/agile:autopilot <id>` (small and clear) | `/agile:discuss` first (open direction) | `/agile:screen` during refinement>
- Parallel with: <ids that can run at the same time in a worktree — or "none">

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
- <kind>: update docs/<folder>/<slug>.md#<section> — <what changes>

## Acceptance criteria
<!-- Given / When / Then. Each one is covered by a test. Always include the localization criterion. -->
- AC1 Given <context>, when <action>, then <result>.
- AC<n> All new texts appear in every locale of `UI locales:`.

## Criterion → test
<!-- Written by /agile:build at its start, one row per criterion above, in the same order. Test: the test name(s), `pending` (no test yet) or `validation step <n>` (only checkable on screen). -->
| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC<n> | pending |

<!-- test-lock.js lock (the test phase of /agile:build) writes `Tests locked: <sha> (<n> files); unlocks: <n>` itself, right under the heading of this section. -->

## Decisions
<!-- date — decision — reason — source. Technical decisions made by Claude are recorded here too. The source: `asked`, `confirmed: <path or item id>` or `changed: <old rule> (<where>)` (references/decisions.md in the plugin). -->
- <YYYY-MM-DD> — <decision> — <reason> — asked

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
<!-- Written at the end of build, in two subsections with the same steps and the same code blocks (`scripts/validation-script.check.js`). At most 8 steps the product owner follows on screen. Each step names the way to the screen (entry point, list, row, control) from docs/agile/ui-map.md. -->
### English
1. <Step> → <expected result>
   - [ ] 1.1 <Sub-step> → <expected result>
2. [ ] <Step with one unit, its own box> → <expected result>

### pt-BR
1. <Passo> → <resultado esperado>
   - [ ] 1.1 <Subpasso> → <resultado esperado>
2. [ ] <Passo de uma unidade, caixa própria> → <resultado esperado>

## Events
<!-- Written only by events.js (`add`; the gate and the test lock write theirs): one line per event, `- <date> <kind> — <note>`. Kinds: override, question, assumed, rework, test-fail, blocked, change, correction, stop. -->

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
