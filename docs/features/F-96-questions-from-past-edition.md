---
feature: F-96
epic: Question bank
status: building
board: 142
version: 1
---
# Questions from a past exam edition

Technical terms: [glossary](../glossary.md)

## Summary
A question can come from a past exam edition: it records the `ExamEdition`, its number in the paper and, optionally, the `NoticeSubject` it belongs to. The question shows a source line (organizer, exam, year), the edition screen lists its questions in paper order, and an edition or notice subject that live questions point at cannot be deleted. A question without an edition is an `AuthoredQuestion`. Source: epic Question bank (E-4), brief capability 3 "Origin".

## Start
- Depends on: F-95 (done).
- Waits on (to start): nothing. The two owner questions that blocked it (what the source line shows, which sources may be reproduced) were answered on 2026-10-12.
- Needed to validate: the local app host and one edition with notice subjects (the F-37 seed has them) — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-97 or F-98. F-98 also changes `Question`: expect a small conflict in `Question.cs` and the question responses.

## Goal
Give every past-exam question its provenance, so the simulators (E-5, E-6) can reproduce a paper in its order and show where a question came from, and so the transcription mode (F-99) has an edition and a number to fill.

## What exists (verified 2026-10-12)
- `QuestionBank` (F-95, version 0.28.0): `Question` has type, status, topic id, content language, statement, base text, options, explanation and difficulty; no origin of any kind. Routes live under `/api/v1/question-bank/questions`, gated by `questions.manage`.
- `Catalog` has `ExamEdition` (exam, organizer, notice year, optional position, notice reference, notice URL, applied-on date, `Draft`/`Published`; a published edition cannot be deleted, a draft is soft-deleted together with its notice subjects) and `NoticeSubject` (group, label, question count, order). No other module reads them today; the only cross-module reads are `ITopicDirectory` and the delete guard `ITopicUsageCheck` (F-95).
- `Catalog.Contracts` has no edition directory; the edition and notice subject routes require `catalog.manage` (F-35 BR18, F-74 BR11).
- Permissions: `Curator` holds `questions.manage` and `catalog.browse` but not `catalog.manage` (`CatalogModule.cs:37`, `QuestionBankModule.cs:33`). The question form's subject and topic lookups call `catalog.manage` routes, so a curator who is not an admin most likely gets 403 there today (read in the code, not measured on a running host). It is captured as B-28 (board #165) and is not fixed here; this item's own lookups avoid the problem (BR9).
- Simulae stores `ExamNoticeId` on the question plus a copied year and organizer (`Question.cs:20-61`); it has no paper number and no notice subject on the question. Simulab derives year and organizer from the edition and never copies them.
- UI kit: `AppLookupField`, `AppSectionCard`, `AppDataTable`, `AppStatusChip`, `AppRowActions`, `AppTruncatedText`, `AppSelectField`. It has no number field: the build checks whether `AppTextField` takes a numeric type and otherwise adds `AppNumberField` with its gallery entry.
- Glossary: `AuthoredQuestion` exists as a concept (a question with no edition); `PaperNumber` and `SourceLine` are added with this item.

## Users and use cases
- UC1 A curator records, on a question, the edition it came from, its number in the paper and, optionally, its notice subject.
- UC2 A curator changes or clears that origin; clearing it makes the question authored.
- UC3 An admin opens an edition and sees its questions in paper order, and adds one with the edition already chosen.
- UC4 A curator sees a question's source line in the form, the preview and the list.
- UC5 An admin deletes an edition or a notice subject; it is refused while live questions point at it.

## Business rules
- BR1 `Question` gets three optional columns: `ExamEditionId`, `PaperNumber`, `NoticeSubjectId`. The ids are held without a foreign key across schemas, as `TopicId` is. A question with no `ExamEditionId` is authored; no flag is stored for that.
- BR2 With an edition the paper number is required, a whole number from 1 to 500. A number or a notice subject without an edition is refused. The notice subject is optional; when given it must be a live notice subject of the same edition.
- BR3 Within one edition, a live question's number is unique: a unique index over `TenantId`, `ExamEditionId`, `PaperNumber` `NULLS NOT DISTINCT`, filtered to rows not deleted, so a deleted draft frees its number. A duplicate is 409 `question.paper_number_taken`.
- BR4 Year, exam, organizer and position are never stored on the question. They are read from the edition when the question is shown, so a corrected notice year corrects every source line.
- BR5 The edition may be `Draft` or `Published`, and a `Published` question may point at a `Draft` edition. Whoever shows questions to students (E-5, E-6) reads the edition's status.
- BR6 The origin is edited in place, on a `Draft` or a `Published` question, like the rest of the content. It takes no part in the publish check of F-95 BR4: an origin never blocks publishing.
- BR7 The source line is `<organizer acronym> · <exam name> · <notice year>`, built by the screens from the origin parts; there is none for an authored question. The position and the notice reference are not part of it. There is no rights marker: whether a source may be reproduced is the owner's call when the question is transcribed (owner, 2026-10-12).
- BR8 Delete guards, by the same inverted dependency as the topic guard: `Catalog.Contracts` declares `IExamEditionUsageCheck` and `INoticeSubjectUsageCheck`, `QuestionBank` implements both, and the delete handlers of the catalog ask every registered check. Deleting an edition while a live question (any status) points at it is 409 `exam_edition.has_questions`; deleting a notice subject likewise is 409 `notice_subject.has_questions`. The catalog never references `QuestionBank`.
- BR9 `Catalog.Contracts` also gets `IExamEditionDirectory`, implemented by the catalog over its own context: find one edition with its exam and organizer, find many, search editions by text, list an edition's notice subjects in display order, find notice subjects by id. `QuestionBank` never reads `catalog.*` tables. The form's lookups are routes of `QuestionBank` guarded by `questions.manage`, so a curator needs no `catalog.manage` to fill the origin.
- BR10 The edition's questions come back in one route of `QuestionBank`, paged, ordered by number, listing `Draft` and `Published` and never deleted ones. The existing question list keeps its filters; filtering by edition is F-100.
- BR11 A reference that no longer resolves (an edition removed behind the module's back) never throws: the source line is simply absent, the ids stay.
- BR12 Pages and endpoints check permissions, never role names: every new route and screen action needs `questions.manage`. The edition page keeps `catalog.manage`; its question section needs `questions.manage` as well.

## Screens and API
No mockup: the screens reuse the patterns of F-95 and F-74 (epic: screen design "no").

### API (`/api/v1/question-bank`, all `questions.manage`)
- `POST /questions` and `PUT /questions/{id}` — `SaveQuestionRequest` gains optional `examEditionId`, `paperNumber`, `noticeSubjectId`; all absent means authored.
- `QuestionResponse` and `QuestionListItemResponse` gain `Origin` (null when authored): `QuestionOriginResponse(ExamEditionId, ExamId, ExamName, OrganizerAcronym, OrganizerName, NoticeYear, Position, PaperNumber, NoticeSubjectId, NoticeSubjectLabel)`.
- `GET /editions?search=&take=` — lookup of editions of any status: `{ id, examName, organizerAcronym, noticeYear, position, status }`, ordered by exam name then notice year descending, at most 20.
- `GET /editions/{editionId}` — the same item, for prefilling the form.
- `GET /editions/{editionId}/notice-subjects` — `{ id, group, label, questionCount }` in display order.
- `GET /editions/{editionId}/questions?page=&pageSize=` — `{ id, paperNumber, statementExcerpt, noticeSubjectId, noticeSubjectLabel, status, contentLanguage }` ordered by number, a page response like `QuestionPageResponse`.
- Catalog: `DELETE` of an edition and of a notice subject answer the two new 409 codes; no new catalog route.
- Error codes: `question.exam_edition_not_found` (404), `question.notice_subject_not_found` (404), `question.paper_number_required` (400), `question.paper_number_invalid` (400), `question.origin_requires_edition` (400), `question.paper_number_taken` (409), `exam_edition.has_questions` (409), `notice_subject.has_questions` (409).
- Migration `question_bank`: three nullable columns, a check that the three are all null or edition and number are both set, the unique index of BR3 (`ux_questions_tenant_edition_paper_number`), an index on `exam_edition_id`, column comments. Existing rows stay null: all authored.

### Screens
- **Question form** (`/admin/questions/new`, `/admin/questions/{id}`): a new section card "Origin" after Classification (`question-section-origin`). Fields: edition (`AppLookupField`, shows `<acronym> · <exam> · <year>` and the position under it, searches by exam, acronym or year), paper number (number field, required once an edition is chosen), notice subject (`AppSelectField`, disabled until an edition is chosen, grouped by its group, optional, cleared when the edition changes). Clearing the edition clears the other two. The aside gets a "Source" row with the source line, or the "Authored" text. `/admin/questions/new?examEditionId=<id>` opens with the edition preselected (the edition page's "Add question"). Refusals map by code: `exam_edition_not_found` to the edition field (cleared), `notice_subject_not_found` to the notice subject, `paper_number_*` and `origin_requires_edition` to the number, `paper_number_taken` to the number with the text "number already used in this edition".
- **Question preview dialog**: the source line and "Question <n>" above the statement, when there is an origin.
- **Question list** (`/admin/questions`): one new column "Source" (source line and `#<n>`, an em dash with the visually hidden text "Authored" when there is none); not sortable, not filterable (F-100).
- **Edition page** (`/admin/exams/{examId}/editions/{id}`): a new section card "Questions" below the notice subjects, for a saved edition and for who holds `questions.manage`. `AppDataTable` of 25 per page, ordered by number: number, statement excerpt, notice subject, status chip, row action edit (to `/admin/questions/{id}`); header action "Add question" (to the form with the edition). States: loading, empty ("No questions from this edition yet"), load error (retry), forbidden (section not rendered).
- Typed client: `QuestionBankApiClient` gets the four read routes; `CatalogApiClient` is not used by the origin section.

## Acceptance criteria
- AC1 Given a database with the F-95 migration and questions in it, when the new migration runs, then the columns, the check, the index of BR3 and the edition index exist and every existing question has a null origin. (BR1, BR3)
- AC2 Given a published edition with a notice subject, when a curator saves a question with that edition, number 12 and the notice subject, then it is stored and the response carries the origin parts (acronym, exam name, year, position, number, subject label). (UC1, BR1, BR2, BR4)
- AC3 Given a request with no origin fields, then the question is stored authored with a null origin, and the same holds for every question that existed before. (BR1)
- AC4 Given an edition and no number, then 400 `question.paper_number_required`; given a number 0 or 501, then 400 `question.paper_number_invalid`; given a number or a notice subject and no edition, then 400 `question.origin_requires_edition`; nothing is written. (BR2)
- AC5 Given an unknown or deleted edition, then 404 `question.exam_edition_not_found`; given a notice subject of another edition or a deleted one, then 404 `question.notice_subject_not_found`; nothing is written. (BR2)
- AC6 Given a live question numbered 12 in an edition, when another is saved with 12 in the same edition, then 409 `question.paper_number_taken`; in another edition it is stored; after the first one (a draft) is deleted it is stored; two rows inserted directly with the same edition and number are rejected by PostgreSQL. (BR3)
- AC7 Given a published question with an origin, when a curator changes its number, moves it to another edition or clears the origin, then it saves, stays published, and a cleared origin leaves it authored with the number free again. (UC2, BR6)
- AC8 Given a `Draft` edition, when a question is saved and then published, then both succeed and the response still carries the origin. (BR5)
- AC9 Given an edition with a live question, when an admin confirms its deletion, then 409 `exam_edition.has_questions` and the edition stays; once its questions are deleted, the deletion succeeds. (UC5, BR8)
- AC10 Given a notice subject a live question points at, when an admin deletes it, then 409 `notice_subject.has_questions`; once the question is deleted or its subject cleared, the deletion succeeds. (UC5, BR8)
- AC11 Given drafts and published editions, when the lookup searches "guarda", "consulplan" or "2024", then it finds the matching editions of any status, at most 20; the notice subjects route returns the edition's subjects in display order; an unknown edition is 404. (BR9)
- AC12 Given an edition with 30 questions, drafts and published, and one deleted, when its questions route is read with page size 25, then the first page holds numbers 1 to 25 in order, the second the rest, and the deleted one is absent; an unknown edition is 404. (UC3, BR10)
- AC13 Given a user without `questions.manage`, then every new route answers 403; given a `Curator` with no `catalog.manage`, then the lookups and the edition's questions answer 200. (BR9, BR12)
- AC14 Given the question form, when the curator picks an edition, then the notice subject select lists that edition's subjects; when the edition is changed or cleared, then the subject and the number are cleared; a refused save marks the field of its code and moves focus to the error summary. (UC1, UC2)
- AC15 Given a question with an origin, then the form aside, the preview dialog and the list show the source line `ACRONYM · Exam · Year`; given an authored question, the list shows the em dash with the hidden text. (UC4, BR7)
- AC16 Given a saved edition with questions and a user with `questions.manage`, when the page opens, then the section lists them by number, 25 per page, the edit action opens the question, and "Add question" opens the form with the edition chosen; given no questions, the empty text; given no `questions.manage`, the section is absent. (UC3, BR12)
- AC17 Given the migration and the modules, then `Catalog` still does not reference `QuestionBank` and the architecture and module-boundary tests are green. (BR8, BR9)
- AC18 All new texts and error codes appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Criterion → test

Tests locked: 8d6dfe986152df4732b61c8e5d790333a8c91e40 (6 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | AddQuestionOriginMigrationTests.Up_AddsTheOriginColumnsCheckAndIndexes_AndKeepsEveryExistingQuestionAuthored |
| AC2 | QuestionOriginEndpointTests.Create_WithEditionNumberAndNoticeSubject_StoresItAndReturnsTheOriginParts; QuestionOriginEndpointTests.List_CarriesTheOriginOfAQuestionFromAnEdition |
| AC3 | QuestionOriginEndpointTests.Create_WithNoOriginFields_StoresAnAuthoredQuestion; AddQuestionOriginMigrationTests.Up_AddsTheOriginColumnsCheckAndIndexes_AndKeepsEveryExistingQuestionAuthored |
| AC4 | QuestionOriginEndpointTests: Create_WithAnEditionAndNoNumber_IsRefusedWithPaperNumberRequired, Create_WithANumberOutsideOneToFiveHundred_IsRefusedWithPaperNumberInvalid, Create_WithTheBoundaryNumbers_IsStored, Create_WithANumberAndNoEdition_IsRefusedWithOriginRequiresEdition, Create_WithANoticeSubjectAndNoEdition_IsRefusedWithOriginRequiresEdition; AddQuestionOriginMigrationTests.TheDatabase_RefusesANumberOrASubjectWithNoEdition_AndAnEditionWithNoNumber |
| AC5 | QuestionOriginEndpointTests: Create_WithAnUnknownEdition_IsRefusedWithExamEditionNotFound, Create_WithADeletedEdition_IsRefusedWithExamEditionNotFound, Create_WithANoticeSubjectOfAnotherEdition_IsRefusedWithNoticeSubjectNotFound, Create_WithADeletedNoticeSubject_IsRefusedWithNoticeSubjectNotFound |
| AC6 | PaperNumberUniquenessTests (5 tests); AddQuestionOriginMigrationTests.TheDatabase_RefusesARepeatedNumberInAnEdition_ButNotAnotherEditionOrADeletedRow |
| AC7 | QuestionOriginEndpointTests: Update_OfAPublishedQuestion_ChangesTheNumberMovesTheEditionAndClearsTheOrigin, Update_ClearingOnlyTheNoticeSubject_KeepsTheEditionAndTheNumber |
| AC8 | QuestionOriginEndpointTests.PublishQuestion_FromADraftEdition_Succeeds_AndKeepsTheOrigin |
| AC9 | OriginDeleteGuardTests: DeleteEdition_WithALiveQuestion_IsRefusedWithHasQuestions_AndSucceedsOnceTheyAreGone, DeleteEdition_WithAPublishedQuestion_IsRefusedWithHasQuestions, DeleteEdition_AfterTheOriginIsCleared_Succeeds |
| AC10 | OriginDeleteGuardTests: DeleteNoticeSubject_WithALiveQuestion_IsRefusedWithHasQuestions_AndSucceedsOnceTheSubjectIsCleared, DeleteNoticeSubject_AfterTheQuestionIsDeleted_Succeeds, DeleteNoticeSubject_NoQuestionPointsAt_Succeeds_WhileASiblingIsGuarded |
| AC11 | EditionLookupEndpointTests: Search_FindsEditionsOfAnyStatus_ByExamNameAcronymOrYear, Search_IgnoresCaseOfTheText, Search_ReturnsAtMostTwentyEditions, FindEdition_ReturnsTheLookupItem_AndAnUnknownOneIs404, NoticeSubjects_ComeInDisplayOrder_AndAnUnknownEditionIs404 |
| AC12 | EditionLookupEndpointTests: EditionQuestions_ArePagedInPaperOrder_WithoutTheDeletedOnes, EditionQuestions_OfAnUnknownEdition_Is404 |
| AC13 | EditionLookupEndpointTests: EveryRoute_WithoutTheManagePermission_IsForbidden, EveryRoute_ForACuratorWithNoCatalogPermission_Answers200; QuestionOriginEndpointTests.Create_WithAnOriginAsACuratorWithNoCatalogPermission_Succeeds |
| AC14 | pending |
| AC15 | pending |
| AC16 | pending |
| AC17 | ModuleBoundaryTests (existing: Application_ReferencesItsOwnDomainAndContractsOnly, EveryLayer_ReachesAnotherModuleOnlyThroughItsContracts), run again at the end |
| AC18 | pending |

## Decisions
- 2026-10-12 — The source line is organizer acronym, exam and year; position and notice reference stay on the edition — owner, refinement; it is what the brief note asks (source, organizer, year) and fits one line.
- 2026-10-12 — No rights marker in v1; the line is always shown for an edition question and what may be reproduced is the owner's call at transcription — owner, refinement; there is no rule to model yet, and a flag can arrive with one.
- 2026-10-12 — Paper number required with an edition, a whole number from 1 to 500, unique among live questions of the edition — owner, refinement; the simulator (E-6) needs the order and the transcription (F-99) increments it. The 500 matches the notice subject's question count limit.
- 2026-10-12 — Notice subject optional, and it must belong to the question's edition — owner, refinement; an edition may have no notice subjects yet.
- 2026-10-12 — Deleting an edition or a notice subject is refused while live questions point at it — owner, refinement; nothing disappears silently, same stance as the topic guard.
- 2026-10-12 — A published question may point at a draft edition — owner, refinement; transcription happens before the edition is published, and E-5 and E-6 read the edition's status.
- 2026-10-12 — The curator's lookups are routes of `QuestionBank` under `questions.manage`, fed by a new directory contract of the catalog — owner, refinement; granting `catalog.manage` to `Curator` is a roles decision bigger than this item. The subject and topic lookups of F-95 keep their gap, captured as a bug.
- 2026-10-12 — The edition's questions are a section on the edition page, paged, ordered by number, with its own route under `/editions/{id}/questions` — owner, refinement; F-100 keeps ownership of the filters of the question list.
- 2026-10-12 — Year, organizer and exam are read from the edition, never copied to the question (Simulae copies them) — Claude; one place decides, and a corrected year corrects every line.
- 2026-10-12 — The origin is three plain columns on `Question`, not a separate entity or a value object table — Claude; it is one optional group of fields with no lifecycle of its own, and the unique index and check constraint read it directly.
- 2026-10-12 — Origin errors use their own codes instead of `topic`-style reuse — Claude; each maps to one field of the form.
- 2026-10-12 — The list gets a "Source" column although filters stay in F-100 — Claude; the summary says the question shows its source line, and a column adds no filter.
- 2026-10-12 — Only `Catalog.Contracts` and `QuestionBank` change in the backend; no catalog migration — Claude; the catalog gains read contracts and two delete guards, not tables.
- 2026-10-12 — A curator without `catalog.manage` cannot open the edition page, so the "Questions" section there is reached in practice by admins; a curator reaches the same questions through `/admin/questions` — Claude, consequence of BR12; the page's permission is not changed by this item.
- 2026-10-12 — No new package — Claude; nothing here needs one.
- 2026-10-12 — F-96 approved ("aprovo F-96"), with no new package and no mockup (owner).
- 2026-10-12 — Design passes (`system-design`, then `architect`) are asked at the build: the item adds a contract between modules, a schema change and a unique index — Claude.

- 2026-10-12 — Design passes ran (`system-design`, then `architect`) and were verified against the files (module-boundary test, `TopicUsageChecks`, `IQuestionQueries`, `IQuestionStore`): no owner stop, no new package, no catalog migration — Claude.
- 2026-10-12 — Precision of BR3: the unique index is filtered by `is_deleted = false AND exam_edition_id IS NOT NULL`; without the second clause `NULLS NOT DISTINCT` makes every authored question collide — Claude, architect finding.
- 2026-10-12 — `ix_questions_notice_subject` is added next to the edition index, because `INoticeSubjectUsageCheck` (AC10) filters by it — Claude.
- 2026-10-12 — The four edition read routes are methods of `IQuestionQueries`, implemented in `QuestionQueries` over `IExamEditionDirectory` (as `ITopicDirectory` today), so `Application` and `Api` get no new module reference; responses are records in `QuestionBank.Contracts` with optional ids `= null`; the edition status travels as a string because a contracts project references the shared kernel only — Claude, architect finding.
- 2026-10-12 — BR11 in the response: when the edition does not resolve, `Origin` stays non-null with ids and paper number and empty names (`NoticeYear` null); screens build no line when the exam name is empty — Claude, the same stance `QuestionQueries` takes for a topic.
- 2026-10-12 — A duplicate number is pre-checked (`IsPaperNumberTakenAsync`) and the `23505` translation stays as the race fallback, like the catalog's stores — Claude.
- 2026-10-12 — The three usage checks share a small static helper that takes the `InUseAsync` delegate; no base interface in `Contracts` — Claude.
- 2026-10-12 — The number field is `AppTextField` with `int?` and `InputType.Number`; no `AppNumberField` is added — Claude, verified at `ExamEditionForm.razor:77-82`.

- 2026-10-12 — tests unlocked (two lookup tests used notice years 2031, 2032 and 2029, which the catalog refuses with `exam_edition.notice_year_invalid`; a test defect, not a criterion): `EditionLookupEndpointTests` rewritten with 2015, 2016 and 2014 — owner, "destravar e reescrever".
- 2026-10-12 — The check `ck_questions_origin` needs `paper_number IS NOT NULL` besides the range: `NULL BETWEEN 1 AND 500` is unknown, which a check accepts, so an edition with no number would have passed (seen by the migration test) — Claude.

## Out of scope
- Filters by edition, organizer or notice subject on the question list — F-100.
- The transcription mode, with edition and notice subject kept and the number incremented — F-99.
- Rights per source, or any restriction on what is reproduced — a later item when the owner has a rule.
- Booklets or colour versions of the same paper (`Section`, glossary: not decided) — each would be its own edition or a later item.
- Checking that the notice subject agrees with the question's topic through the mapping, or suggesting the topic from it — a later idea.
- Seeded questions for the guard editions — F-110.
- Question versions and answer key history — F-98.
- Scoring, weights and the student view of the source line — epics E-5 and E-6.
- The 403 of the subject and topic lookups for a curator without `catalog.manage` — B-28 (board #165).

## Open questions
- (none)

## Change notes

## Validation script

## Events

- 2026-10-12 question — asked 8, from context 0
- 2026-10-12 blocked — test wrong: two tests used notice years 2031, 2032 and 2029, which the catalog refuses (exam_edition.notice_year_invalid); test defect, not a criterion

## Delivery
