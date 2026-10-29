---
epic: data-type-controls
status: agreed
board: 250
Autopilot: F-183 built (2026-10-28)
---
# Data type controls

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: agreed (2026-10-27)
- Stopped at: epic agreed by the owner; F-181 to F-188 captured as ideas
- Next action: /agile:refine F-183 (owner; waits on nothing)
- Waiting on: nothing

## Goal
Every data type has one input control, one display formatter and one table column in the UI kit, and every screen picks its control by the type of its data. Numbers stop being typed as free text, one value stops being split in two fields, and the build refuses the old patterns. The catalog is `docs/design/data-type-controls.md`.

## Start
- Depends on: F-1 (UI kit and gallery), F-114 (field kinds), F-168 (owns `ExamEditionForm` and `NoticeSubjectDialog`, where the decimal and duration fields are; validating).
- Waits on: nothing outside the items.

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|
| F-181 | Number controls: integer, decimal, score and year | Points, cut-offs, counts and years are typed as numbers in the user's culture | Must | L | F-168 | - | no | idea |
| F-182 | Duration and numeric range controls | Exam duration is one field; answer size is one range | Must | M | F-181, F-168 | - | no | idea |
| F-183 | Text kind controls: text area, e-mail, URL, code, one-time code, search | Identity and catalog fields stop repeating their setup page by page | Must | L | - | - | no | done |
| F-184 | Choice controls: switch, segmented, yes/no, level, language | Difficulty shows as a level, settings apply at once, yes/no/unset is one control | Must | L | - | - | yes | idea |
| F-185 | Date range and relative time | Period filters take a custom range; audit lists say "3 days ago" | Should | M | - | - | no | idea |
| F-186 | Data type controls enforced by the build | A page cannot go back to `InputType.Number` or a raw MudBlazor picker | Must | S | F-181, F-182, F-183, F-184, F-185 | - | no | idea |
| F-187 | Reserve controls: money, percentage, time, date-time, month | The types the app will hold are ready in the kit | Should | L | F-181 | - | no | idea |
| F-188 | Reserve controls: phone, tax id, color, tree pick, slider, image, user | The rest of the catalog is ready in the kit | Should | L | F-183 | - | yes | idea |
| F-189 | Gallery state matrix test for the kit controls | A control that loses a state in the gallery fails a test | Should | S | - | - | no | idea |
| F-190 | One http(s) address check for the URL fields | The rule has one copy instead of four | Should | S | - | - | no | idea |

## Execution plan
1. F-183 — waits on nothing, the most fields migrated (6 e-mail, 4 one-time code, URL, 3 text areas) — path: `/agile:refine` → `/agile:build` — parallel with: F-184
2. F-184 — waits on nothing; level control needs a mockup — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: F-183
3. F-181 — after F-168 merges; base of every number control — path: `/agile:refine` → `/agile:build` — parallel with: F-185
4. F-182 — after F-181; replaces the F-168 duration pair — path: `/agile:refine` → `/agile:build` — parallel with: F-185
5. F-185 — independent; lower value — path: `/agile:autopilot` (small, clear) — parallel with: F-181, F-182
6. F-186 — after every migration; closes the first release — path: `/agile:autopilot` — parallel with: none
7. F-187 — after F-181 (reuses the number format) — path: `/agile:refine` → `/agile:build` — parallel with: F-188
8. F-188 — after F-183; tree pick and image need a mockup — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: F-187

## Waiting outside the epic
| What | For | Who provides it | Who asks |
|---|---|---|---|
| Nothing outside the items | - | - | - |

## First release cut
F-181, F-182, F-183, F-184, F-185, F-186

## Out of scope
- Screen composition of the forms (sections, aside, layout): epic `interface-and-screen-composition`.
- Exam session controls (timer, answer sheet): declared exception, F-169.
- Field font weight and icon side: F-153.

## Decisions
- 2026-10-27 — One control per data type, catalog in `docs/design/data-type-controls.md` — one behavior everywhere (`ui.md`).
- 2026-10-27 — Reserve types (money, percentage, time, date-time, month, phone, tax id, color, tree pick, slider, image, user) are built now, shown only in the gallery until a screen uses them — owner's request.
- 2026-10-27 — Decimal input accepts only the culture separator; percentage is stored 0–100; level shows dots with the level name — owner.
- 2026-10-27 — Each feature migrates its own fields on the screens now; the composition items (F-128, F-130, F-133) take the new controls as they come — the enforcement can ship sooner.
- 2026-10-27 — First release cut: F-181 to F-186 (every migration and the enforcement); the reserve features stay out, no screen uses them — owner.
- 2026-10-27 — A separate epic, not part of `interface-and-screen-composition` — different goal, 8 features — owner.
- 2026-10-27 — The enforcement (F-186) comes before the reserve features: the architecture tests need only the migrated screens — corrects the order of the catalog draft.
- 2026-10-27 — The difficulty level gauge of F-154 is delivered by F-184 (level control); F-154 keeps the subject lookup and the counters — owner, similar-items card.
- 2026-10-27 — Epic agreed — owner ("Aprovo o épico").

## Retro notes
- F-183: lesson: the test phase locked a gallery test that only checks a state caption appears somewhere in the section, so a control missing a state passed; a state-matrix test should check each control's own column.
- F-183: lesson: `validation-ticks.js evidence` printed 0 of 17 tested while the item file carried 9 hand-ticked boxes; the ticks live outside the file, so a tick typed into the file is lost from the evidence.
- F-183: lesson: `gate.js ship --fresh` runs about 21 minutes and the shell tool stops a foreground command at 10 minutes, so the skill's "foreground, never background" cannot hold for this project's suite.
- F-183: item decision: the search box keeps its aria-label and has no visible label, hint or error (BR7).
- F-183: item decision: code and one-time code fields clean the value on leave, not on each key (Blazor Server rewrites the value under the caret).

## Related
- Catalog: [data-type-controls.md](../design/data-type-controls.md)
- Epic: [interface-and-screen-composition](interface-and-screen-composition.md)
