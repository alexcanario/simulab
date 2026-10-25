---
feature: F-110
epic: Question bank
status: building
board: 156
version: 2
---
# Sample questions of the municipal guard editions

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-25)
- Stopped at: Change note v2 approved (catalog read-only exception, PNG images)
- Next action: /agile:build F-110 resumes at step 3b
- Waiting on: nothing

## Summary
Seeded questions for the municipal guard editions of F-37, so local work and testers start with real questions without typing them. Simulae has no question seed (its `GuardaMunicipalContentSeed` holds only the taxonomy). Source: epic Question bank (E-4).

Added 2026-10-24: the source is the Manaus 2026 paper (Instituto Consulplan, Técnico Municipal I - Guarda Municipal, booklet type 1, white) and its final answer key, free to use (owner, D-6). The seed holds its 60 multiple-choice questions with their base texts, question 47 annulled (F-101). The question with an image comes from the migration with a fixed image id and its file is stored by an idempotent seeder at app start-up (F-103 BR11). Claude proposes the subject and topic of each question from the F-51 taxonomy; the owner reviews the table at refinement. The essay question waits for F-109. The other editions leave the seed in F-161.

## Start
- Depends on: F-96, F-97, F-101, F-102, F-103 (all done).
- Waits on (to start): nothing. The source paper, the final answer key and the extracted images are with the owner (OneDrive `Simulab/Guarda Municipal/Manaus/2026`), free to use (D-6).
- Needed to validate: the local app host — owner.
- Model: opus (doubt: three projects touched — QuestionBank infrastructure, the Api host start-up and the tests — and 60 transcribed questions near size L)
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-161 (catalog seed clean-up; another module, it keeps the Manaus edition).

## Goal
The Manaus 2026 municipal guard edition holds its real paper — 60 published multiple-choice questions with their base text, images and answer key — in every environment from day one, so curators, testers and the coming simulators (E-5, E-6) work on real questions without typing them.

## What exists
Verified on 2026-10-25 in this worktree (branch from `main` at `0342c69`, which holds F-96 to F-109):
- No question is seeded anywhere: the `question_bank` migrations are schema and backfills only (`Persistence/Migrations`, last `20261024122942_AddShortAnswerQuestions` and F-109's rubric).
- The Manaus 2026 edition has the fixed id `0198f370-0004-7000-8000-000000000002` (F-37) and nine notice subjects seeded by F-78 (group 8 ids), whose labels and numbers of questions match the paper's sections (10, 5, 5, 5, 5, 5, 5, 5, 15).
- Taxonomy (F-51): topics have fixed ids `0198f370-0007-7000-8000-<n>`; a name already taken was reused (F-51 BR6), so on some databases a topic has another id.
- `questions` (schema `question_bank`): one `topic_id` (required), `exam_edition_id`, `paper_number`, `notice_subject_id` (optional, only with an edition), `statement_html` plus the derived `statement_text` and `normalized_statement`, `status`, optional `base_text_id`, `explanation_html`, `difficulty`; unique (tenant, edition, paper number) among live rows. No foreign key to `catalog`.
- `question_options`: `position` (0 = A), `is_correct`, `text_html`. A published single-choice question has 2 to 5 options and exactly one correct (`QuestionRules`, `SingleChoiceKey`).
- `question_versions`: version 1 is written at publish with the options as JSON; annulment adds a version with `annulled = true` and a reason and sets the status `Annulled`, only from `Published` (`Question.cs:132`).
- `base_texts`: title, normalized title, `text_html`. The paper has one base text ("Calma, gente", questions 01 to 08); the excerpts of questions 15, 18 and 19 are part of their statements.
- Images (F-103): row `question_images` (blob name, content type, size, owner kind and id); blob `question-images/<guid>` in the private container `documents` through `IFileStorage`; HTML `<img src="/question-images/<id>" alt="...">`. A row with no owner is swept after 24 hours (`QuestionImageSweep`). The paper holds three content images: two cartograms (question 16) and one climogram (question 17), PNG, 119 to 144 KB: `img/...prova.pdf-6-0.png` (cartogram 1, GDP per capita), `img/...prova.pdf-6-2.png` (cartogram 2, infant mortality) and `questao 17, imagem 1.png` (climogram), checked against the paper pages (change note v2).
- Sanitizer allow-list (`QuestionHtmlSanitizer`): p, br, strong, b, em, i, u, s, sub, sup, ul, ol, li, blockquote, table parts, a, img.
- Startup seeding: `SeedAdmin` is called from `Simulab.Api/Program.cs:183` after the migrations; QuestionBank has no seeder. Staging applies migrations at start (`AzureDeployment.cs:158`).
- `QuestionType` already holds `Essay` (F-109), but the essay of this paper stays out (D-6, decision of 2026-10-25).

## Users and use cases
- UC1 A curator opens the questions of the Manaus 2026 edition on a database where the seed ran and finds the 60 questions, numbered as the paper, each with its topic, notice subject, options and key; questions 01 to 08 share the base text "Calma, gente".
- UC2 A curator opens question 16 or 17 and sees its images.
- UC3 A curator opens question 47 and sees it annulled, with the exam board's reason, and its version history.
- UC4 A curator edits, unpublishes, annuls or deletes a seeded question; the change stays.

## Business rules
- BR1 The seed is one data migration of the `question_bank` schema. It runs once per database, like any migration (F-37 BR1, F-78 BR1), in every environment, staging included.
- BR2 The seed writes the questions of `## Question classification`, booklet type 1, with `paper_number` = the question number, `content_language` `pt-BR`, the Manaus 2026 edition, the listed notice subject and topic, four options A to D and the listed key as the only correct option. All rows are global (`TenantId` null), with fixed ids and frozen literals, the derived columns included (`statement_text`, `normalized_statement`, the base text's normalized title); a test proves every literal equals what the domain computes today and that the HTML is already what the sanitizer returns.
- BR3 The whole seed is skipped when the Manaus 2026 edition is missing or deleted, or already holds at least one question, live or deleted (decision of 2026-10-25; like F-78 BR3), or when the database has no `catalog` schema (the check reads `catalog.exam_editions` read-only, guarded by `to_regclass`; change note v2).
- BR4 A topic is found by its seeded id, then by normalized name within its subject (F-51 BR6). A question whose topic is not found, or is deleted, is skipped; the others are seeded. A notice subject is found by its seeded id, then by the edition and normalized label; when not found, the question is seeded with no notice subject. The lookups read `catalog.topics`, `catalog.subjects` and `catalog.notice_subjects` read-only, inside the same guard; this migration is the one exception to ADR-0005's rule, limited to reading (change note v2).
- BR5 Every seeded question is `Published` with its version 1, as `PublishQuestionHandler` writes it; no review entry is written (decision of 2026-10-25).
- BR6 Question 47 is published with key D and then annulled: version 2 has `annulled = true`, `answer_key_changed = false` and the reason "Anulada pela banca no gabarito definitivo."; its status is `Annulled` (F-101).
- BR7 The base text "Calma, gente" (Ivan Ângelo, Veja São Paulo, 10/09/2003) is one `base_texts` row, linked to questions 01 to 08; their versions copy its HTML (F-102 BR4).
- BR8 The three images are `question_images` rows with fixed ids, owned by their question (`owner_kind` `Question`), so the sweep never removes them, each with its alternative text and content type `image/png`. Their files are embedded in the Api and written to `IFileStorage` at Api start-up, right after the migrations, when the row exists and is live and the blob is missing; an existing blob is never overwritten. A failed write is logged and tried again at the next start.
- BR9 The seed never updates a row: a curator's later edit, unpublish, annulment or delete stays. `Down` removes the seeded rows by id when no curator has changed them since (no version above the seeded ones); otherwise it leaves that question.
- BR10 Nothing else is seeded: no essay question, no explanation, no difficulty, no other booklet type, no catalog row.

## Screens and API
No new route, screen or endpoint. The seeded questions appear in the questions back office (F-100 search, F-96 edition origin), the images through F-103's route `/question-images/{id}`. Error codes: none.

## Acceptance criteria
- AC1 Given an empty database, when the migrations are applied, then the Manaus 2026 edition holds exactly the questions of `## Question classification`, each with that paper number, topic, notice subject, four options A to D and that key as the only correct option, all with a null `TenantId`. (UC1, BR1, BR2)
- AC2 Given the seed literals, then every question rebuilt through the domain is valid for publishing, its stored HTML equals what `QuestionHtmlSanitizer` returns, and its `statement_text` and `normalized_statement` equal what the domain computes; the base text's normalized title too. (BR2)
- AC3 Given the seeded database, then every question except 47 is `Published` with exactly one version, number 1, whose options equal the question's; and no review entry exists for them. (BR5)
- AC4 Given the seeded database, then question 47 is `Annulled`, version 1 holds key D, version 2 has `annulled = true`, `answer_key_changed = false` and the listed reason. (UC3, BR6)
- AC5 Given the seeded database, then questions 01 to 08 point at one base text "Calma, gente", and their versions hold its HTML. (UC1, BR7)
- AC6 Given a database where the Manaus edition already holds one question typed by a curator before the seed migration, when it runs, then no seeded question is written and the migration succeeds; the same when the edition is soft-deleted, and when the database has no `catalog` schema. (BR3)
- AC7 Given a database where a topic of `## Question classification` exists under another id, the questions of that topic point at it; given a soft-deleted topic, its questions are skipped and the others seeded. (BR4)
- AC8 Given the seeded database, then the three image rows exist with their fixed ids, owned by questions 16 and 17, and the sweep run 25 hours later leaves them. (UC2, BR8)
- AC9 Given an Api start-up where the image rows exist and the blobs do not, then the three blobs are written with the embedded bytes; on a second start, nothing is written; when an image row is deleted, its blob is not written. (BR8)
- AC10 Given the seeded database, when a curator edits a seeded question and the migrations are applied again, then the edit stays and no duplicate appears; when the migration is rolled back, the untouched seeded rows are gone and the edited question stays. (UC4, BR9)
- AC11 Given the Api host, when the migrations ran, then the OpenAPI document still returns 200, EF reports no pending model change, and the architecture tests pass. (BR1)
- AC12 The seed adds no UI text; the three UI locales and the missing-key test are unchanged and green.
- On screen only (validation script): the questions list of Manaus 2026 shows 60 questions; question 01 shows the base text; question 16 shows two images; question 47 shows the annulment; a sample of statements matches the paper (UC1 to UC3).

## Criterion → test
| Criterion | Test |
|---|---|
| AC1 | `SeedManausGuardQuestionsTests.Migrate_EmptyDatabase_TheEditionHoldsExactlyTheClassifiedQuestionsWithTheirKeys` |
| AC2 | `SeedManausGuardQuestionsTests.Migrate_EmptyDatabase_EverySeededQuestionIsWhatTheDomainAndTheSanitizerMake` |
| AC3 | `SeedManausGuardQuestionsTests.Migrate_EmptyDatabase_EveryQuestionBut47IsPublishedWithOneVersionThatHoldsItsOptions_AndNoReviewEntry` |
| AC4 | `SeedManausGuardQuestionsTests.Migrate_EmptyDatabase_Question47IsAnnulledWithKeyDThenTheBoardsReason` |
| AC5 | `SeedManausGuardQuestionsTests.Migrate_EmptyDatabase_Questions01To08ShareTheBaseTextAndTheirVersionsHoldItsHtml` |
| AC6 | `SeedManausGuardQuestionsMigrationTests.Migrate_TheEditionAlreadyHoldsAQuestion_SeedsNothingAndSucceeds` (live, deleted), `Migrate_TheEditionIsDeleted_SeedsNothingAndSucceeds`, `Migrate_NoCatalogSchema_SeedsNothingAndSucceeds` |
| AC7 | `SeedManausGuardQuestionsMigrationTests.Migrate_ATopicUnderAnotherId_TheQuestionsOfThatTopicPointAtIt`, `Migrate_ADeletedTopic_SkipsItsQuestionsAndSeedsTheOthers` |
| AC8 | `SeedManausGuardQuestionsTests.Migrate_EmptyDatabase_TheThreeImagesAreOwnedByQuestions16And17_AndTheSweepKeepsThem` |
| AC9 | `SeedQuestionImageFilesTests.Start_WritesTheThreeSeededImagesWithTheEmbeddedBytes`, `ASecondStart_WritesNothing`, `AMissingBlob_IsWrittenAgain_ButNotTheBlobOfADeletedRow` |
| AC10 | `SeedManausGuardQuestionsRollbackTests.ACuratorsEdit_StaysWhenMigratingAgain_AndTheRollbackRemovesOnlyTheUntouchedRows` |
| AC11 | `SeedManausGuardQuestionsTests.Migrate_ApiHost_ServesTheOpenApiDocument_AndHasNoPendingModelChange`; `Simulab.ArchitectureTests` (whole project) |
| AC12 | `ResourceParityTests.Every_key_exists_in_every_language` (no resource changed) |

## Decisions
- 2026-10-24 — The source is the Manaus 2026 paper, booklet type 1, and its final answer key, free to use — D-6. confirmed: D-6
- 2026-10-25 — The seed is a data migration with frozen literals, run once per database, skipped whole when the edition is deleted — same pattern as F-37, F-51 and F-78. confirmed: D-6, F-78
- 2026-10-25 — Question 47 is published with key D, then annulled. D is inferred, not the board's preliminary key: the owner's folder holds only the final key, where the question is `#` in every booklet type; D is the option closest to Lei 13.675/2018 art. 9 § 2 VII, which lists municipal guards as operational members of SUSP, while D says "strategic". Annulment needs a published question with one correct option, and the annulled key counts for no student. asked
- 2026-10-25 — The essay question stays out and becomes its own item — the board deducts 0.25 per error, while F-109's rubric only adds maximum points per criterion; the aspect points were lost in the PDF extraction. asked
- 2026-10-25 — The questions are seeded `Published`, version 1, no review entry — the owner checks them in the validation script. asked
- 2026-10-25 — An edition that already holds any question skips the whole seed — it neither mixes with a curator's text nor collides with the paper-number index. asked
- 2026-10-25 — Images: rows in the migration, files written by a start-up step in the Api beside `SeedAdmin` — a migration cannot store a blob. asked
- 2026-10-25 — Out of scope: booklet types 2 to 4, explanations and difficulty — the same questions in another order; an AI-written explanation needs a person to review it (brief); no answer data to calibrate difficulty. asked
- 2026-10-25 — `## Question classification` approved as proposed, the five `?` rows on the closest topic. asked
- 2026-10-25 — Technical: the image bytes come from the files the owner already extracted from the paper PDF (folder `img/` and `questao 17, imagem 1.png` beside the paper), checked against the paper pages, and are committed as embedded resources of `Simulab.QuestionBank.Infrastructure`; the seeder runs on every start, since three existence checks are cheap and a failed write retries by itself.
- 2026-10-25 — Technical: fixed ids follow `0198f370-<group>-7000-8000-<number>` with new groups 10 (questions), 11 (options), 12 (versions), 13 (base texts), 14 (images).
- 2026-10-25 — Technical: a missing topic skips only its question (BR4), the way F-78 BR4 skips a missing mapping target.
- 2026-10-25 — Exception to ADR-0005 (a module never touches another module's tables): this seed migration reads `catalog.exam_editions`, `catalog.topics`, `catalog.subjects` and `catalog.notice_subjects` read-only, at migration time only, inside a `DO` block guarded by `to_regclass`, because BR3 and BR4 cannot be met otherwise; the Api migrates the catalog before the question bank, and a database without the catalog schema (the QuestionBank migration tests) skips the seed. Option A of the build stop, owner, change note v2. asked
- 2026-10-25 — Build: the design passes of step 4b are skipped: no project, API contract, message between modules or schema change (a data migration and a start-up step).
- 2026-10-25: plan — 1 seed migration skeleton, the 60 questions, base text and Q47 annulment inside one guarded `DO` block → AC1, AC2, AC3, AC4, AC5; 2 guards and lookups (edition holds questions or is deleted, no catalog schema, topic under another id or deleted) → AC6, AC7; 3 `Down` that keeps what a curator changed → AC10; 4 image rows in the migration plus the embedded PNGs written at Api start-up → AC8, AC9; 5 existing suites that count or delete seeded data adjusted → affected tests green, AC11, AC12; 6 flow page `images-in-questions.md` → `techdocs.js check`; 7 gate, coverage, scan, validation script → `agile gate GREEN`

## Out of scope
- The essay question of the paper — F-163.
- Booklet types 2 to 4.
- Explanations and difficulty of the questions.
- The other guard editions and any catalog change — F-161.
- Topics missing from the F-51 taxonomy (rows marked `?` take the closest one).

## Docs
- flow: update docs/flows/images-in-questions.md — seeded images are written at Api start-up.

## Open questions
- (none)

## Change notes
### v2 — 2026-10-25
- What: the seed migration reads the catalog schema read-only (edition, subjects, topics, notice subjects) inside a `DO` block guarded by `to_regclass`, and skips the whole seed when the catalog schema is missing; recorded as the one exception to ADR-0005's rule. The images are PNG (119 to 144 KB), content type `image/png`, from the three files named in `## What exists`.
- Why: the build found that BR3 and BR4 need a read of the catalog tables, which ADR-0005 forbids, and that the QuestionBank migration tests migrate only the `question_bank` schema, so an unguarded read fails with 42P01; the extracted images are PNG, not JPEG.
- Affected: What exists (images), BR3, BR4, BR8, AC6; other criteria unchanged.
- Re-approved: 2026-10-25

## Question classification
Proposed at refinement and approved by the owner on 2026-10-25. Answer key: booklet type 1 (white), final; question 47 is `#` (annulled) and is seeded with key D before its annulment (BR6). Notice subject = the Manaus 2026 notice subject of F-78 the question falls under. `?` marks a row with no close topic in the F-51 taxonomy.

| # | Notice subject | Subject › Topic | Key | Note |
|---|---|---|---|---|
| 01 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | B | base text "Calma, gente" |
| 02 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | B | base text |
| 03 | Língua Portuguesa | Sintaxe › Sintaxe, Concordância e Regência | B | base text |
| 04 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | D | base text |
| 05 | Língua Portuguesa | Sintaxe › Sintaxe, Concordância e Regência | D | base text |
| 06 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | B | base text |
| 07 | Língua Portuguesa | Ortografia e Pontuação › Ortografia | A | base text; accent of "por quê" |
| 08 | Língua Portuguesa | Leitura e Interpretação › Interpretação de textos | D | base text |
| 09 | Língua Portuguesa | Produção Textual › Comunicação escrita | D | official writing |
| 10 | Língua Portuguesa | Produção Textual › Comunicação escrita | D | forms of address |
| 11 | Informática | Informática › Sistemas operacionais Windows | D | |
| 12 | Informática | Informática › Pacotes Microsoft Office | D | Word |
| 13 | Informática | Informática › Pacotes Microsoft Office | A | Excel |
| 14 | Informática | Informática › Conceitos de hardware ? | C | cloud computing |
| 15 | Informática | Informática › Segurança na internet | D | denial of service |
| 16 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | B | two cartograms (images) |
| 17 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | B | climogram (image) |
| 18 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | D | |
| 19 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | A | |
| 20 | Geografia e História de Manaus | Conhecimentos locais › Manaus (AM) | A | |
| 21 | Ética e Direitos Humanos | Direitos Humanos › Proteção de direitos fundamentais | A | use of force at a peaceful protest |
| 22 | Ética e Direitos Humanos | Direitos Humanos › Legislação de direitos humanos | D | international guidelines |
| 23 | Ética e Direitos Humanos | Direitos Humanos › Legislação de direitos humanos | B | racial discrimination |
| 24 | Ética e Direitos Humanos | Direitos Humanos › Legislação de direitos humanos | A | abandoned elderly person |
| 25 | Ética e Direitos Humanos | Direitos Humanos › Direitos humanos e cidadania ? | D | public-service ethics, offered advantage |
| 26 | Direito Constitucional | Direito Constitucional › Direitos fundamentais | C | nationality |
| 27 | Direito Constitucional | Direito Constitucional › Direitos fundamentais | C | ADPF, structural racism |
| 28 | Direito Constitucional | Direito Constitucional › Princípios constitucionais ? | C | state of defense |
| 29 | Direito Constitucional | Direito Constitucional › Princípios constitucionais ? | D | tenure and reinstatement of a civil servant |
| 30 | Direito Constitucional | Direito Constitucional › Constituição Federal (art. 144) | D | public security |
| 31 | Direito Penal | Direito Penal › Crimes específicos | C | |
| 32 | Direito Penal | Direito Penal › Crimes específicos | C | |
| 33 | Direito Penal | Direito Penal › Tipificação de crimes | B | crimes against honor |
| 34 | Direito Penal | Direito Penal › Crimes específicos | C | crimes against the public administration |
| 35 | Direito Penal | Direito Penal › Crimes específicos | D | theft of livestock |
| 36 | Direito Processual Penal | Direito Penal › Procedimentos processuais | D | |
| 37 | Direito Processual Penal | Direito Penal › Processo penal | C | in-the-act arrest |
| 38 | Direito Processual Penal | Direito Penal › Procedimentos processuais | D | crime scene |
| 39 | Direito Processual Penal | Direito Penal › Processo penal | D | unlawful evidence |
| 40 | Direito Processual Penal | Direito Penal › Processo penal | D | |
| 41 | Legislação de trânsito | Legislação Federal › Código de Trânsito | C | |
| 42 | Legislação de trânsito | Legislação Federal › Código de Trânsito | D | |
| 43 | Legislação de trânsito | Legislação Federal › Código de Trânsito | A | |
| 44 | Legislação de trânsito | Legislação Federal › Código de Trânsito | D | |
| 45 | Legislação de trânsito | Legislação Federal › Código de Trânsito | D | |
| 46 | Legislação específica | Estatuto do Desarmamento › Lei 10.826/2003 (Estatuto do Desarmamento) | C | |
| 47 | Legislação específica | Estatuto das Guardas Municipais › Lei 13.022/2014 (Estatuto das Guardas) ? | # | annulled; national public-security system |
| 48 | Legislação específica | Direito Penal › Crimes específicos | D | drug trafficking |
| 49 | Legislação específica | Legislação Municipal › Legislação municipal específica | C | |
| 50 | Legislação específica | Estatuto das Guardas Municipais › Lei 13.022/2014 (Estatuto das Guardas) | D | complaint against the guard |
| 51 | Legislação específica | Direito Penal › Crimes específicos | C | abuse of authority |
| 52 | Legislação específica | Legislação Federal › ECA (Lei 8.069/1990) | D | |
| 53 | Legislação específica | Direito Penal › Crimes específicos | C | torture law |
| 54 | Legislação específica | Direito Penal › Crimes específicos | D | |
| 55 | Legislação específica | Legislação Federal › Lei Maria da Penha (Lei 11.340/2006) | B | |
| 56 | Legislação específica | Direito Administrativo › Legislação administrativa | B | administrative misconduct |
| 57 | Legislação específica | Legislação Municipal › Organização municipal | C | |
| 58 | Legislação específica | Direito Administrativo › Legislação administrativa | B | access to information; true/false items inside multiple choice |
| 59 | Legislação específica | Direito Administrativo › Legislação administrativa | B | administrative procedure |
| 60 | Legislação específica | Legislação Municipal › Estatuto dos Funcionários Públicos Municipais | B | Lei Municipal 1.118/1971 |

## Events

- 2026-10-25 question — asked 8, from context 1
- 2026-10-25 blocked — false premise: seed reads catalog tables, ADR-0005
- 2026-10-25 change — v2: catalog read-only exception to ADR-0005, PNG images
