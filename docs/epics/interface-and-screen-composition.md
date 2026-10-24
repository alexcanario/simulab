---
epic: interface-and-screen-composition
status: agreed
board: 211
---
# Interface and screen composition

Technical terms: [glossary](../glossary.md)

## Goal
One visual and composition standard on every screen: the Control Alt Systems identity in the theme and the UI kit, every form and list in the new composition of D-4 (section cards, summary aside, one amber action per page), and the tests that keep the kit honest (library classes, section selectors, contrast). Split out of Foundation and identity on 2026-10-24 (owner): the D-4 redesign is an area with many features across modules.

## Start
- Depends on: F-1 (UI kit) and F-43 (visual language), in Foundation and identity; F-125 (identity in the theme and kit, done).
- Waits on: nothing.

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|
| F-114 | Field kinds look like what they do | Pickers are told apart from text boxes by their look | Must | - | - | - | no | done |
| F-125 | Control Alt Systems identity in the theme and the UI kit | Every screen takes the identity in one change | Must | - | - | - | yes | done |
| F-148 | Control Alt Systems palette v2 in the theme and kit | The amber palette of tokens v2 | Must | - | F-125 | - | no | done |
| F-126 | Question form in the new composition | The reference screen of D-4 | Must | - | F-125, F-148 | - | yes | done |
| F-134 | Home, error and diagnostics pages in the new composition | The first pages a visitor sees | Must | - | F-125 | - | no | done |
| B-18 | The selected radio card description is not pinned by a contrast test | Contrast guard | Must | - | - | - | no | done |
| B-27 | AppLookupField live region may keep the previous count | Screen reader hears the right count | Must | - | - | - | no | done |
| F-140 | Check library class names against the package CSS | A wrong MudBlazor class fails a test, not the screen | Must | S | - | - | no | idea |
| F-133 | Identity screens in the new composition | Sign-in, sign-up, account and terms acceptance in the new look | Must | L | F-125 | - | no | idea |
| F-129 | Base text form in the new composition | Curators get the same form frame as the question form | Must | S | F-125 | - | no | idea |
| F-127 | Exam form in the new composition | Same frame on the exam form | Must | M | F-125 | - | no | idea |
| F-128 | Exam edition form in the new composition | Same frame on the edition form and its sections | Must | M | F-125 | - | no | idea |
| F-132 | Question bank lists in the new composition | Questions and base texts lists in the new look | Must | M | F-125 | - | no | idea |
| F-131 | Catalog screens in the new composition | Catalog lists, details and search in the new look | Must | L | F-125 | - | no | idea |
| F-159 | Review screens in the new composition | The review queue and the review page in the new look | Must | S | F-125 | - | no | idea |
| F-130 | Admin screens in the new composition | Users, roles and account events in the new look | Should | M | F-125 | - | no | idea |
| F-153 | Control font weight and trailing field icons | Lighter controls and icons at the end of the field, as the reference screen | Should | S | - | - | no | idea |
| F-149 | Option rows as keys in the question form | Keycap letters and a green correct key | Should | M | F-126 | - | no | idea |
| F-135 | Sticky section index on long forms | Long forms show where the user is and jump to a section | Should | M | F-125 | - | yes | idea |
| F-119 | A list that keeps its state in the address follows a menu click | The menu entry resets a filtered list | Should | S | F-100 | - | no | idea |
| F-120 | Architecture test for section id selectors | A screen test cannot aim at a missing id | Should | S | - | - | no | idea |
| F-138 | Keyboard shortcuts on forms | Save, main action and section jumps from the keyboard | Could | M | F-125 | - | no | idea |

## Execution plan
1. F-140 — the cheap guard first, before more screens name kit classes — path: `/agile:autopilot` — parallel with: none
2. F-133 — the screens every student and tester sees — path: `/agile:refine` → `/agile:build` — parallel with: F-129
3. F-129 — small form, same frame as F-126 — path: `/agile:autopilot` — parallel with: F-133
4. F-127 — exam form — path: `/agile:refine` → `/agile:build` — parallel with: none
5. F-128 — edition form, the largest catalog form — path: `/agile:refine` → `/agile:build` — parallel with: F-132
6. F-132 — question bank lists — path: `/agile:refine` → `/agile:build` — parallel with: F-128
7. F-131 — catalog screens; cut in two parts at refinement (lists, then details and search) — path: `/agile:refine` → `/agile:build` — parallel with: none
8. F-159 — review screens — path: `/agile:autopilot` — parallel with: F-130
9. F-130 — admin screens — path: `/agile:refine` → `/agile:build` — parallel with: F-159
10. F-153 — kit font weight and field icons — path: `/agile:autopilot` — parallel with: none (touches every control)
11. F-149 — option rows as keys — path: `/agile:refine` → `/agile:build` — parallel with: F-119, F-120
12. F-135 — sticky section index, a new kit pattern — path: `/agile:screen` in refinement — parallel with: F-119, F-120
13. F-119, F-120 — small fixes and guards — path: `/agile:autopilot` — parallel with: any
14. F-138 — shortcuts conflict with the browser's — path: `/agile:discuss` first — parallel with: none

## Waiting outside the epic
| What | For | Who provides it | Who asks |
|---|---|---|---|
| nothing | - | - | - |

## First release cut
F-140, F-133, F-129, F-127, F-128, F-132, F-131, F-159

## Out of scope
- Global search in the app bar (F-156) and in-app notifications (F-157): app features, not composition; they stay in Foundation and identity.
- Question form features from the reference screen: inline student preview (F-136), publishing readiness meter (F-137), draft autosave (F-155), field polish (F-154), review queue count (F-158); they stay in Question bank.
- Partner brand (F-139): Partner tenants.
- Freezing the pt-PT and en manual (F-124): Foundation and identity.

## Decisions
- 2026-10-24 — Interface and screen composition is its own epic, not part of Foundation and identity — the D-4 redesign touches every module's screens and has more than twenty items.
- 2026-10-24 — F-124 moves back to Foundation and identity; F-149 (from Question bank) and F-153 (from Foundation and identity) join this epic — F-124 is about the manual; F-149 and F-153 change the kit's look.
- 2026-10-24 — F-159 is new and F-133 takes `AcceptTerms.razor` — no item covered the review screens or the terms acceptance page.
- 2026-10-24 — The first release cut is the eight Must items still open — testers should see one coherent app, back office included.

## Retro notes

## Related
- Discussions: D-4, D-6
