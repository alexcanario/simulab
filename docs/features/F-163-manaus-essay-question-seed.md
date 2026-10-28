---
feature: F-163
epic: Question bank
status: building
Autopilot: built-pending
board: 229
version: 1
---
# Seed the essay question of the Manaus 2026 paper

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-28)
- Stopped at: Approved by autopilot --assume (2026-10-28); build next
- Next action: Build the seed migration and its tests in this worktree
- Waiting on: nothing

## Summary
Seed the essay question of the Manaus 2026 municipal guard paper (Instituto Consulplan, booklet type 1): theme, three supporting texts, 20 to 30 lines, 20 points, with its rubric. The board's grid adds points for three macro-structural aspects (4 + 4 + 3) and, for three micro-structural ones (2 + 3 + 4), takes points off per error (0.50, 0.25 and 0.25). F-109's rubric only adds a maximum per criterion; the refinement settled that this is enough (BR3, `## Decisions`). The 60 multiple-choice questions of the same paper are seeded by F-110, which left this one out (decision of 2026-10-25). Source: refinement of F-110.

## Start
- Depends on: F-109 (done), F-110 (done).
- Waits on (to start): nothing. The owner supplied the paper's markdown (2026-10-28); the grid reads: macro-structural 4.00 + 4.00 + 3.00 = 11; micro-structural 2.00 (syntax, 0.50 per error) + 3.00 (morphosyntax, 0.25) + 4.00 (deviations, 0.25) = 9; total 20.
- Needed to validate: the local app host — owner.
- Model: sonnet (no signal: a data migration on the F-110 pattern, no door, no new module, no schema change, size M)
- Suggested path: `/agile:build` (no mockup: no new screen).
- Parallel with: none known. F-151 and F-178 (ideas) build on the essay kind, not on this seed.

## Goal
The Manaus 2026 edition holds its essay question — the theme, the three supporting texts and the rubric of the exam board — published in every environment from day one, so curators and the coming essay simulators (F-178) and correction items (F-151, F-152) work on the real essay.

## What exists
Verified on 2026-10-28 in this worktree (branch from `main` at `5e779e29`):
- F-109 delivered `QuestionType.Essay`: the key is a rubric of up to 10 criteria (name up to 100 characters, description up to 500, whole maximum points 1 to 100) and an optional size in lines (1 to 100, minimum not above maximum); a published essay needs at least one criterion (`RubricKey`, `QuestionLimits`). The rubric is stored in `question_rubric_criteria` (position, name, description, maximum points) and copied to `question_versions.rubric` (JSON column, PascalCase property names as EF's `ToJson` writes them); `answer_size_min` and `answer_size_max` are columns of `questions`.
- F-110 seeded the 60 multiple-choice questions as the data migration `SeedManausGuardQuestions` (frozen literals, fixed ids `0198f370-<group>-7000-8000-<n>`, groups 10 to 14, one guarded PL/pgSQL block, `Down` that keeps what a curator changed). It skips when the edition holds any question, which is true after it ran, so the essay needs its own guard (BR4).
- The Manaus 2026 edition has the fixed id `0198f370-0004-7000-8000-000000000002`; `SeedManausExamRules` already set its essay maximum to 20.00 and its essay cut-off to 10.00.
- Paper numbers 1 to 60 are taken by the multiple-choice questions; the unique index is (tenant, edition, paper number) among live rows and `PaperNumberMax` is 500.
- The F-51 taxonomy has the topic "Redação (nível superior)" (seeded id `0198f370-0007-7000-8000-000000000002`) under the subject "Produção Textual"; the nine notice subjects of F-78 match the sections of the objective questions only.
- The paper (OneDrive `Simulab/Guarda Municipal/Manaus/2026`, `...-prova.md`, pages 23 to 25): the general orientations (20 to 30 lines, manuscript, 20 points), the grid of aspects, Texto I (profile of the municipal guard, with a list of five items), Texto II (art. 4 of Lei 13.675/2018, 16 principles), Texto III (Gomes, 2024, five paragraphs) and the instruction with the theme "A ressignificação da Guarda Municipal: da vigilância patrimonial à mediação social como estratégia de segurança urbana".
- No essay question is seeded anywhere. No exam is composed from the seeded questions yet (E-5, E-6).

## Users and use cases
- UC1 A curator opens the questions of the Manaus 2026 edition on a database where the seed ran and finds the essay as question 61: type Essay, published, with the three supporting texts as its base text, the theme in its statement, a size of 20 to 30 lines and the six criteria of the rubric.
- UC2 A curator previews it and sees what a student will see: the supporting texts, the instruction, the writing box with the limit "Between 20 and 30 lines", then the rubric.
- UC3 A curator edits, unpublishes or deletes the seeded essay; the change stays.

## Business rules
- BR1 The seed is one data migration of the `question_bank` schema, run once per database in every environment, staging included (F-110 BR1).
- BR2 It writes one question: type `Essay`, status `Published`, content language `pt-BR`, the Manaus 2026 edition, paper number 61, topic "Produção Textual › Redação (nível superior)", no notice subject, `TenantId` null, fixed ids, frozen literals (the derived columns included), `answer_size_min` 20 and `answer_size_max` 30. Its statement is the instruction of the paper with the theme in quotation marks; its base text is one `base_texts` row titled "Prova discursiva: textos motivadores" holding Texto I, Texto II and Texto III with their titles and sources, as one HTML the sanitizer leaves unchanged. Its version 1 is written as a publish writes it, with the base text HTML and the rubric; no review entry.
- BR3 The rubric has the six criteria of `## Approved list`, in the order of the grid, with the exam board's whole maximum points (total 20). The three macro-structural criteria add points (4, 4, 3). The three micro-structural criteria hold the points kept when the candidate makes no error, and their description carries the deduction: "2 points, minus 0.50 per error, never below 0"; "3 points, minus 0.25 per error"; "4 points, minus 0.25 per error". A grader reads the deduction in the description; the rubric gets no deduction field (decision of 2026-10-28). Names and descriptions are in Portuguese, as the paper's text: they are content, not UI text.
- BR4 The seed is skipped whole, without an error, when the catalog schema is missing, when the edition is missing or deleted, and when a question with paper number 61 or with the fixed essay id already exists in the edition (live or deleted). It does not depend on the 60 multiple-choice questions being there. The topic is found by its seeded id, then by normalized name in its subject; a missing or deleted topic skips the essay (F-110 BR4).
- BR5 The seed never updates a row. `Down` removes the essay, its rubric rows, its version and its base text only when nobody changed them since (the question never updated, no version above 1); otherwise it leaves them (F-110 BR9).
- BR6 Nothing else is seeded: no model answer, no explanation, no difficulty, no other booklet type, no exam rule (the edition's essay maximum and cut-off were set by `SeedManausExamRules`).

## Screens and API
No new route, screen or endpoint. The essay appears in the questions back office (list, form, preview) as F-109 shows any essay. Error codes: none. UI text: none added.

## Acceptance criteria
- AC1 Given an empty database migrated through F-110, when the migration runs, then the Manaus 2026 edition holds one `Essay` question, paper number 61, `Published`, with that topic, no notice subject, null `TenantId`, `answer_size_min` 20 and `answer_size_max` 30, and the 60 multiple-choice questions are unchanged. (UC1, BR1, BR2)
- AC2 Given the seed literals, then the essay rebuilt through the domain is valid for publishing, its stored HTML equals what `QuestionHtmlSanitizer` returns, its base text HTML too, and its `statement_text` and `normalized_statement` equal what the domain computes. (BR2)
- AC3 Given the seeded database, then the rubric rows are the six criteria of `## Approved list` in that order, with maximum points summing to 20, and the three micro-structural ones carry the per-error deduction in their description. (UC1, BR3)
- AC4 Given the seeded database, then the essay has exactly one version, number 1, whose rubric JSON equals the rubric rows and whose base text HTML equals the base text; and no review entry exists for it. (BR2)
- AC5 Given the seeded database, then the base text "Prova discursiva: textos motivadores" is linked to the essay and contains the three texts with their sources. (BR2)
- AC6 Given a database where the Manaus edition already holds a question with paper number 61, or with the essay's fixed id, live or deleted, when the migration runs, then nothing is written and it succeeds; the same when the edition is soft-deleted and when the database has no `catalog` schema. (BR4)
- AC7 Given a database where the topic exists under another id, the essay points at it; given a soft-deleted topic, the essay is skipped and the migration succeeds. (BR4)
- AC8 Given the seeded database, when a curator edits the essay and the migrations are applied again, then the edit stays and no duplicate appears; when the migration is rolled back, an untouched essay and its rows are gone and an edited one stays. (UC3, BR5)
- AC9 Given the Api host, when the migrations ran, then the OpenAPI document still returns 200, EF reports no pending model change and the architecture tests pass. (BR1)
- AC10 The seed adds no UI text; the three UI locales and the missing-key test are unchanged and green.
- On screen only (validation script): the essay shows in the list of Manaus 2026 as question 61 of type Essay; its form shows the six criteria; the preview shows the three texts, the theme and "Between 20 and 30 lines"; the texts and the grid match the paper (UC1, UC2).

## Criterion → test

Tests locked: 415138c5f9a84b8a052d3de1752e3f6463d849f8 (2 files); unlocks: 0

| Criterion | Test |
|---|---|
| AC1 | `SeedManausEssayTests.Migrate_EmptyDatabase_TheEditionHoldsTheEssayAsQuestion61_AndTheOtherSixtyAreUnchanged` |
| AC2 | `SeedManausEssayTests.Migrate_EmptyDatabase_TheEssayIsWhatTheDomainAndTheSanitizerMake` |
| AC3 | `SeedManausEssayTests.Migrate_EmptyDatabase_TheRubricIsTheSixCriteriaOfTheGrid_WithTheDeductionInTheDescription` |
| AC4 | `SeedManausEssayTests.Migrate_EmptyDatabase_TheEssayHasOneVersionThatHoldsItsRubricAndBaseText_AndNoReviewEntry` |
| AC5 | `SeedManausEssayTests.Migrate_EmptyDatabase_TheBaseTextHoldsTheThreeTextsWithTheirSources` |
| AC6 | `SeedManausEssayMigrationTests.Migrate_ThePaperNumberIsTaken_SeedsNothingAndSucceeds` (live, deleted), `Migrate_TheFixedIdIsTaken_SeedsNothingAndSucceeds`, `Migrate_TheEditionIsDeleted_SeedsNothingAndSucceeds`, `Migrate_NoCatalogSchema_SeedsNothingAndSucceeds` |
| AC7 | `SeedManausEssayMigrationTests.Migrate_TheTopicIsUnderAnotherId_TheEssayPointsAtIt`, `Migrate_TheTopicIsDeleted_SkipsTheEssayAndSucceeds` |
| AC8 | `SeedManausEssayMigrationTests.Rollback_AnUntouchedEssay_LeavesWithItsRows_AndMigratingAgainWritesItOnce`, `Rollback_AnEditedEssay_Stays` |
| AC9 | `SeedManausEssayTests.Migrate_ApiHost_ServesTheOpenApiDocument_AndHasNoPendingModelChange`; `Simulab.ArchitectureTests` (whole project) |
| AC10 | `ResourceParityTests.Every_key_exists_in_every_language` (no resource changed) |
| On screen only | validation steps 3 to 5 |

## Approved list
Rubric of the essay (name — maximum points — kind):
1. Content knowledge and understanding of the proposed theme and of the requested text genre — 4 — adds points.
2. Development of the argument, objectivity and informativeness within the theme — 4 — adds points.
3. Coherence and textual progression — 3 — adds points.
4. Syntactic structure (truncated or juxtaposed periods; missing, excess or duplicated elements; cohesion) — 2 — points kept: minus 0.50 per error.
5. Morphosyntax (pronoun placement, agreement, connectors, regency, verb tenses and modes, vocabulary choice, formality) — 3 — points kept: minus 0.25 per error.
6. Deviations (accentuation, spelling, hyphenation at line end, capital letters, punctuation) — 4 — points kept: minus 0.25 per error.
The names and descriptions are written in Portuguese in the migration, from the paper's grid; the English above is the contract the tests read.

## Decisions
- 2026-10-28 — The rubric needs no deduction criterion: the three micro-structural aspects are criteria whose maximum is the points kept and whose description states the deduction per error. The result equals the board's arithmetic (kept = maximum − deduction × errors, never below 0), with no schema change and no change to F-109's rubric; F-151 and F-152 show the description to the grader. — assumed: autopilot (the alternative, a deduction field on the criterion, widens a module contract for one paper).
- 2026-10-28 — The summary's "0.25 per error" was imprecise: the paper deducts 0.50 for syntactic structure and 0.25 for the other two. The seed follows the paper. — confirmed: paper pages 23 to 25.
- 2026-10-28 — Topic "Produção Textual › Redação (nível superior)", the only essay topic of the F-51 taxonomy, though the post is not a higher-education one — assumed: autopilot (it names the skill, not the level).
- 2026-10-28 — Paper number 61 and no notice subject: the nine notice subjects match the sections of the 60 objective questions — assumed: autopilot.
- 2026-10-28 — The three supporting texts are one base text titled "Prova discursiva: textos motivadores" (F-102: one base text per question) — assumed: autopilot.
- 2026-10-28 — Published with version 1, no review entry, size 20 to 30 lines as the paper's orientations say — assumed: autopilot (same as F-110).
- 2026-10-28 — The guard is its own (paper number 61 or the fixed id), not "the edition holds any question", because the 60 questions are there after F-110 — assumed: autopilot.
- 2026-10-28 — Out of scope: model answer, explanation, difficulty, booklet types 2 to 4 — assumed: autopilot (same as F-110).
- 2026-10-28 — No new package, no door (no endpoint, no permission), no new module; the ADR-0005 exception of F-110 applies (the migration reads `catalog.exam_editions`, `catalog.topics` and `catalog.subjects` read-only, guarded by `to_regclass`) — confirmed: F-110 change note v2.
- 2026-10-28 — Technical: fixed ids follow F-110's pattern: question group 10 number 61, version group 12 number 611, base text group 13 number 2, rubric criteria a new group 15 numbers 1 to 6.
- 2026-10-28 — Technical: the frozen literals are checked against the domain and the sanitizer by tests, as in F-110 (AC2).
- 2026-10-28 — Build: the design passes of step 4b are skipped: no project, API contract, message between modules or schema change (a data migration).
- 2026-10-28 — Reuse: the F-110 seed pattern and its helpers (`Id`, `Quote`, the guard block, the topic lookup, `Down`); `QuestionHtmlSanitizer` and `ComparableText` as the oracle of the frozen literals.
- 2026-10-28 — Shortcuts: none.

## Out of scope
- A model answer, an explanation and a difficulty for the essay.
- The other booklet types of the paper.
- A deduction field in the rubric (F-109 stays as it is).
- Grading, the correction queue and the suggested grade — F-150, F-151, F-152.
- The 60 multiple-choice questions — F-110 (done).

## Docs
- flow: update docs/flows/open-answer-and-essay-questions.md — a section "Seeded essay": the Manaus 2026 essay and how its micro-structural criteria carry the per-error deduction in the description.

## Open questions
- (none)

## Change notes
(none)

## Events

- 2026-10-28 assumed — 9 answers
