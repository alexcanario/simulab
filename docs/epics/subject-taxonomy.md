---
epic: subject-taxonomy
status: draft
board: 3
---
# Subject taxonomy

Technical terms: [glossary](../glossary.md)

## Goal
Give every exam edition a syllabus in its own words and give the whole product one canonical vocabulary of subjects and topics, so the simulators show what the notice says and analytics and recommendations compare across exams. Source: `product/brief.md` capability 2, ADR-0001 #43 to #46.

## What exists
The `Catalog` module is complete (F-33 to F-37): `Organizer`, `IssuingAuthority`, `Exam`, `ExamEdition`, their admin pages under `Pages/Catalog`, student browsing (F-36) and the municipal guard seed (F-37, without the taxonomy, its BR13). The `catalog.manage` permission guards the back office.

Missing: any taxonomy entity (`Subject`, `Topic`, `Area`, `NoticeSubject`, aliases), verified by a search of `src/` on 2026-10-04.

Simulae (read-only source, `src/Modules/ContentCatalog`): three levels `Subject` → `KnowledgeDomain` → `Topic` (each with only `Nome`), `ExamNoticeTopic` linking a notice to topics directly, the admin pages `TaxonomySubjectsPage`, `TaxonomyDomainsPage`, `TaxonomyTopicsPage` with delete-blocked dialogs, and `GuardaMunicipalContentSeed` (9/23/70). It has no notice subject, no area attribute and no alias: the two-level model is a remap, not a rename.

## Start
- Depends on: epic Assessment catalog (E-2), features F-33 to F-37 done.
- Waits on: nothing.

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|
| F-79 | Subjects and topics back office | Admin CRUD of `Subject` (optional `Area` from a fixed list) and `Topic`; delete blocked when in use | Must | L | - | - | yes | idea |
| F-51 | Import the municipal guard taxonomy | Simulae's 9/23/70 remapped to two levels, plus "Local knowledge" with one topic per city | Should | S | F-79 | - | no | idea |
| F-74 | Notice subjects per edition | An edition records its `NoticeSubject` rows: label, grouping and number of questions as the notice states them | Must | M | F-79 | - | yes | idea |
| F-75 | Map notice subjects to the canonical taxonomy | Each `NoticeSubject` maps to N canonical subjects and topics | Must | M | F-74 | - | yes | idea |
| F-76 | Syllabus for students | The student sees an edition's notice subjects and the topics they cover | Should | S | F-75 | - | no | idea |
| F-77 | Subject and topic aliases | `SubjectAlias` and `TopicAlias`, admin-managed, with a lookup ready for the AI import | Should | M | F-79 | - | no | idea |
| F-78 | Notice subjects of the municipal guard editions | The F-37 editions get their notice subjects and mapping | Could | S | F-51, F-75 | the notices as source — owner | no | idea |

## Execution plan
1. F-79 — every other feature needs the model — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: none.
2. F-51 — real taxonomy data early, to refine the next screens against — path: `/agile:refine` → `/agile:build` — parallel with: F-74.
3. F-74 — the notice side of the model — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: F-51.
4. F-75 — the link that makes the two vocabularies useful — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: F-77.
5. F-76 — the student-facing half, closes the first release cut — path: `/agile:refine` → `/agile:build` — parallel with: F-77, F-78.
6. F-77 — serves the AI-assisted exam import (E-9) most; may move to that epic's start — path: `/agile:refine` → `/agile:build` — parallel with: F-76.
7. F-78 — real syllabus for the guard editions once the source is in hand — path: `/agile:refine` → `/agile:build` — parallel with: F-76, F-77.

## Waiting outside the epic
| What | For | Who provides it | Who asks |
|---|---|---|---|
| The official notices of the F-37 editions (labels and question counts) | F-78 | owner | Claude, at F-78 refinement |

## First release cut
F-79, F-51, F-74, F-75 and F-76 (owner, 2026-10-04). F-77 and F-78 may follow the first release.

## Out of scope
- Weight and minimum score per notice subject — epic Exam Simulator (E-6), with the other scoring rules (owner, 2026-10-04).
- Deeper topics (an optional parent on `Topic`) — ADR-0001 #46, not now.
- Linking a question to a topic — epic Question bank (E-4).
- AI suggestion of the closest topic or a new draft topic — epic AI-assisted exam import (E-9).
- An admin screen for areas — the list is fixed and seeded (owner, 2026-10-04).

## Decisions
- 2026-10-04 — The taxonomy lives in the `Catalog` module (`catalog` schema), not in a new module — `NoticeSubject` links an edition to topics with plain foreign keys inside one schema; a module of its own would need a cross-module reference for the core link.
- 2026-10-04 — Weight and minimum per notice subject move to epic E-6 — consistent with the Assessment catalog decision of 2026-09-23: scoring rules take the shape the simulator needs (owner, question 1).
- 2026-10-04 — `Area` is a fixed seeded list with localized names, no admin screen — few areas, rarely changed; a new area is a migration (owner, question 2).
- 2026-10-04 — The taxonomy and the notice subjects are edited under `catalog.manage` — the same curators run the catalog; no new permission to assign (owner, question 3).
- 2026-10-04 — First release cut is F-79, F-51, F-74, F-75, F-76 — the student sees a real syllabus; aliases arrive with the AI import that uses them most (owner, question 4).
- 2026-10-04 — Board ids: the epic is GitHub issue #3 (E-3, migrated from Azure Boards 692); F-51 is #88 (was 777).

## Related
- Discussions: none
- Decisions: `docs/decisions/ADR-0001-foundation.md` (#8 global data, #43 to #46 taxonomy)
- Epics that consume this one: E-4 Question bank, E-6 Exam Simulator, E-9 AI-assisted exam import, E-10 Performance analytics, E-11 Study recommendations
