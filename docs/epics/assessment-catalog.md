---
epic: assessment-catalog
status: draft
board: 691
---
# Assessment catalog

## Goal
Give Simulab the spine every content epic hangs on: who runs an exam (`Organizer`), which exam it is (`Exam`) and which paper was actually applied (`ExamEdition`). Without it there is nothing to attach a syllabus, a question or a simulation to, and no way for a student to pick a target exam. Source: `product/brief.md` capability 1, ADR-0001 rounds 8 and 43 to 46.

## What exists
Identity is complete (F-1 to F-20, F-25, F-26). `Simulab.Persistence` gives the `ModuleDbContext` base, the audit and soft-delete interceptors, the tenant filter and the unique-index helper (`NULLS NOT DISTINCT`, ADR-0001 #8). The UI kit has `AppDataTable` with server-side paging, `AppRowActions`, `AppConfirmDialog`, `AppSelectField`, `AppPageHeader` and the empty/loading/error states. `identity.roles.manage` is the pattern every module permission follows.

Missing: the `Catalog` module (no project exists), a picker component for a parent record (organizer, exam), and `Simulab.Storage`.

Simulae (read-only source, `src/Modules/ContentCatalog`, 141 `.cs` files, 75 test files):
- `ExamBoard` (`Nome`, `Sigla`, `Descricao`, `SiteOficial`, `Status`) with `ExamBoardListPage` and `ExamBoardFormPage` under `/admin/catalogo/bancas`;
- `Exam` (`Name`, `IssuingAuthority`, `Scope`, `ScopeDetail`) with `ContestListPage` and `RegisterExamPage`;
- `ExamNotice` (`ContestId`, `ExamBoardId`, `NoticeYear`, `PublicationDate`, `Status`) with `ExamNoticeFormDialog` — the closest thing to `ExamEdition`, but keyed by year and board instead of by paper;
- `GuardaMunicipalContentSeed` plus its tests;
- `ExamBoardSelect.razor`, the parent picker to convert into the kit.

Simulae has no edition, no assessment type and no content language; the model below is new work, not a rename.

## Features
| Id | Feature | Value | Priority | Size | Depends on | Screen design | Status |
|---|---|---|---|---|---|---|---|
| F-33 | Catalog module and organizers back office | The `Catalog` module, its schema and permission, plus admin CRUD of exam boards, certifying bodies and universities | Must | L | - | yes | idea |
| F-34 | Exams back office | Admin CRUD of exams under an organizer, with assessment type, scope and content language | Must | M | F-33 | yes | idea |
| F-35 | Exam editions back office | Admin CRUD of editions: the paper actually applied, with its year, the job it selects for, its application date, its notice link and its publication state | Must | L | F-34 | yes | idea |
| F-36 | Catalog browsing for students | A signed-in student searches and filters the published catalog and opens an exam edition | Must | M | F-35 | yes | idea |
| F-37 | Catalog seed from Simulae | The municipal guard organizers, exams and editions remapped into the new model, so the next epics have real data | Should | S | F-35 | no | idea |

## Order
1. F-33 — nothing in the catalog exists before the module and its owner record do.
2. F-34 — an exam needs its organizer.
3. F-35 — an edition needs its exam; it is what questions, syllabus and simulations attach to.
4. F-36 — the student-facing half; the simulators and the target exam depend on it.
5. F-37 — real data is worth more once the three shapes are stable.

## First release cut
F-33, F-34, F-35 and F-36 (all Must). F-37 is Should and may follow the first release.

## Out of scope
- `NoticeSubject`, the notice's syllabus, the canonical `Subject`/`Topic` taxonomy and the aliases — epic Subject taxonomy (692), ADR-0001 #43 to #46.
- Questions, base texts, answer keys and the review workflow — epic Question bank (693).
- Scoring rules, wrong-answer penalty and cut-off per edition — epic Exam Simulator (695): the rules exist to score an attempt, and their shape follows the simulator (owner, question 1).
- Sections and booklets — decided when epic 695 is refined (owner, 2026-09-20).
- Uploading the notice document — epic AI-assisted exam import (698), which already has to keep the original file and bring `Simulab.Storage` (owner, question 2).
- A `Job`/position entity and a contest `Stage` entity: an edital that opens several jobs with different papers becomes one edition per paper (owner, 2026-09-20).
- Private catalogs per institution (multi-tenancy) — epic Institutions (703).
- Portuguese exams — epic Portugal exams (704).
- An audit trail of catalog changes: registered as an idea if the owner wants it, not part of this epic.

## Decisions
- 2026-09-23 — The `Catalog` module skeleton (five projects, `catalog` schema, `DbContext`, first migration, `catalog.manage` permission) ships inside F-33 instead of a separate technical slice — organizer CRUD alone does not fill a session, and F-3's precedent applied because Identity was already L.
- 2026-09-23 — Scoring rules per edition move to epic 695 — refining them before the simulator exists risks building the wrong shape (owner, question 1).
- 2026-09-23 — An edition keeps only the official link to its notice, as a field in F-35; the document upload arrives with epic 698 — nothing reads the file before the import does (owner, question 2).
- 2026-09-23 — First release cut is F-33 to F-36 — without student browsing the catalog cannot be used to pick a target exam, which every simulator needs (owner, question 3).
- 2026-09-23 — The Simulae seed is its own feature (F-37), after F-35 — the municipal guard data has to be remapped to the new model and tested, and that would push F-35 past L (owner, question 4).

## Related
- Discussions: none
- Decisions: `docs/decisions/ADR-0001-foundation.md` (#8 global data, #27 content language, #43 to #46 taxonomy)
- Epics that consume this one: 692 Subject taxonomy, 693 Question bank, 695 Exam Simulator, 698 AI-assisted exam import
