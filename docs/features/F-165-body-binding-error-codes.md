---
feature: F-165
epic: Foundation and identity
status: idea
board: 231
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
# Stable error code for an unreadable request body

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: Captured as an idea from the F-150 review (finding 1)
- Next action: /agile:refine F-165
- Waiting on: nothing

## Summary
Every 400 the Api answers because it cannot read a request body (a value that is not a GUID, a wrong type, malformed JSON) carries a problem with a stable `code`, so a client can branch on it as the rule `api-contracts` asks (problem details plus a code, one entry per field). Today the framework answers that 400 with no `code`: an empty body in Production, a problem without code in Development. Found by the F-150 review (finding 1, 2026-10-26); it applies to every route, not only `PUT /api/v1/identity/users/{id}/tenant`. Already done by F-150: the Development exception handler keeps that 400 instead of turning it into a 500 (`ExceptionHandlerOptions.StatusCodeSelector` in `src/Hosts/Simulab.Api/Program.cs`); what is left is the code and the field entry.

## Start
- Depends on: F-150 (it brings the `StatusCodeSelector` this item builds on).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine
- Model: unknown — settled at /agile:refine
- Suggested path: `/agile:refine` → `/agile:build`
- Parallel with: unknown — settled at /agile:refine
- Evidence: F-150 `UserTenantTests.SetTenant_ATenantThatIsNotAGuid_Is400AndChangesNothing` asserts only the status; with the body `{"tenantId":"x"}` the app host answered `PUT non-GUID: 400` (smoke of 2026-10-25) and the review read no `code` in that answer.

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
