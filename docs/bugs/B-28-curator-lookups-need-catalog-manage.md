---
bug: B-28
feature: F-95
status: refining
board: 165
severity: high
epic: Question bank
---
# A curator without catalog.manage cannot search subjects and topics in the question form

Technical terms: [glossary](../glossary.md)

## What happens
The question form picks the topic through two lookups: `CatalogApi.ListSubjectsAsync` (`src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionForm.razor:692`) and `CatalogApi.ListTopicsAsync` (`QuestionForm.razor:714`). Both call routes of the catalog that require `catalog.manage` (`src/Modules/Catalog/Simulab.Catalog.Api/SubjectEndpoints.cs:37`, `:38`, `:43`). The role `Curator` is granted `questions.manage` (`QuestionBankModule.cs:33`) and `catalog.browse` (`CatalogModule.cs:37`), not `catalog.manage`. So a curator who is not an admin gets 403 on both lookups, cannot pick a topic, and cannot save a question (a topic is required, F-95 BR3).

Found while refining F-96 (2026-10-12), by reading the grants and the routes. Not reproduced on a running host: F-95 was validated with the seeded administrator, who holds every permission.

## Start
- Depends on: nothing (F-96 is delivered, so the form and `QuestionBankApiClient.cs` are free).
- Waits on (to start): nothing.
- Needed to validate: the local app host and an account that holds the `Curator` role and no other grant (assigned in the roles back office) — owner.
- Suggested path: `/agile:build B-28` after approval.
- Parallel with: any item that does not edit `QuestionForm.razor`, `QuestionBankApiClient.cs`, `ITopicDirectory`-style contracts of `Catalog.Contracts` or `ExamEditionLookupEndpoints.cs`.

## Expected
A user with `questions.manage` and no `catalog.manage` picks a subject and a topic in the question form. The lookups for the question form belong to the question bank (guarded by `questions.manage`, fed by a directory contract of the catalog), the same way F-96 does for editions. Granting `catalog.manage` to `Curator` is a roles decision for the owner, not the fix.

Acceptance criteria:
1. Given the subjects "Matemática" and "Português" and a `Curator` with no `catalog.manage`, when the subject lookup of the question bank is read with the text `matematica`, then it answers 200 with "Matemática" only (case and accents ignored), ordered by name, at most 20. (BR1, BR3)
2. Given a subject with live topics and one deleted topic, when its topic lookup is read by the same curator, then it answers 200 with the live topics ordered by name and without the deleted one. (BR2)
3. Given a subject id that does not exist or was deleted, when its topic lookup is read, then it answers 404 `question.subject_not_found`. (BR2)
4. Given a user without `questions.manage` (a student), when either lookup is read, then it answers 403, and a user with only `catalog.manage` is not enough either. (BR4)
5. Given the question form opened by a user with `questions.manage`, when a subject and then a topic are searched, then the Web calls only routes under `/api/v1/question-bank/` and none under `/api/v1/catalog/`, and the list the form shows is the one the lookup returned. (BR5)
6. Given the subject chosen and then changed or cleared, then the topic is cleared and the topic field offers only the new subject's topics, as before. (BR5)
7. Given a `Curator` signed in through the app host with no other grant, when the question form is opened on screen, then a subject and a topic can be picked and the question saved (checked on screen, validation script). (BR1, BR2, BR5)
8. Given the change, then the form's texts are the same as before and no UI key is added or changed, and the architecture and module-boundary tests stay green: `QuestionBank` reaches the catalog only through `Catalog.Contracts` and `Catalog` does not reference `QuestionBank`. (BR6)

## Cause
Reproduced in the code at refinement (2026-10-12), not on a running host:
- `src/Modules/Catalog/Simulab.Catalog.Api/SubjectEndpoints.cs:37` maps `/subjects` (list, `:38`) and `:43` maps `/subjects/{subjectId}/topics` under `RequireAuthorization(PermissionPolicy.NameFor(CatalogPermissions.Manage))` (`:25`, `:37`).
- `src/Modules/Catalog/Simulab.Catalog.Infrastructure/CatalogModule.cs:37` grants `catalog.browse` to `Student` and `Curator` only; `src/Modules/QuestionBank/Simulab.QuestionBank.Infrastructure/QuestionBankModule.cs:33` grants `questions.manage` to `Curator`. No initial grant gives `catalog.manage` to `Curator`.
- `src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionForm.razor:692` (`SearchSubjectsAsync`) and `:714` (`SearchTopicsAsync`) call those two routes through `CatalogApiClient` with the signed-in user's token. A 403 becomes an `InvalidOperationException` in the lookup (`:699`, `:717`), so the subject lookup never offers a result and a topic cannot be chosen (F-95 BR3 requires one).
- Only these two call sites exist outside the catalog pages (`grep CatalogApi` over `src/Hosts/Simulab.Web`, excluding `Pages/Catalog/`).
- Not a gap that F-96 created: its edition lookups already live in the question bank under `questions.manage` (`ExamEditionLookupEndpoints.cs:26-27`, test `EditionLookupEndpointTests.cs:184`), which is the pattern this fix follows.
- Business-rule duplicates: none found. The rule is "who may read the taxonomy", and it is declared once, by the policy on the catalog routes; the form is its only consumer outside the catalog.
- Caveat: the grants are seeds. A role changed afterwards in the roles back office (F-6) can differ on a running database; the regression test uses the seeded `Curator`.

## Business rules
- BR1 The subject lookup is a route of `QuestionBank`, `GET /api/v1/question-bank/subjects?search=&take=`, guarded by `questions.manage` only. It lists live subjects whose name contains the text (case and accents ignored, the catalog's own normalization), ordered by name, `take` between 1 and 20 (default 20). An empty text lists the first ones. Subjects with no topic are listed, as the catalog route does today.
- BR2 The topic lookup is `GET /api/v1/question-bank/subjects/{subjectId}/topics`, guarded by `questions.manage` only. It lists the live topics of the subject ordered by name; the form narrows them by the typed text, as today. A subject that does not exist or was deleted is 404 `question.subject_not_found`.
- BR3 The catalog gives the question bank the subjects through a new contract in `Catalog.Contracts`, `ISubjectDirectory`, implemented over the catalog's own context (the pattern of `ITopicDirectory` and `IExamEditionDirectory`). `QuestionBank` never reads `catalog.*` tables and never calls a catalog route.
- BR4 Pages and endpoints check permissions, never role names: both routes need `questions.manage`; `catalog.manage` alone does not open them. The catalog routes and their permission are not changed.
- BR5 The question form's subject and topic lookups use only the two routes above, through `QuestionBankApiClient`; the form no longer injects `CatalogApiClient`. Its behavior is otherwise the same (up to 20 candidates, topic cleared when the subject changes, topic field disabled until a subject is chosen).
- BR6 No UI text is added or changed, and no role or grant is changed.

## Fix
- `Catalog.Contracts`: `ISubjectDirectory` (`SearchAsync(search, take)` returning `SubjectDirectoryEntry(Id, Name)`; `ListTopicsAsync(subjectId)` returning the live `TopicDirectoryEntry` list, or `null` when the subject is not there) and its record.
- `Catalog.Infrastructure`: `SubjectDirectory` over `CatalogModuleDbContext`, registered in `CatalogModule`, with the soft-delete filters and the normalized name the subject list already uses.
- `QuestionBank`: `SubjectLookupResponse` and `TopicLookupResponse` in its contracts, the two reads in `IQuestionQueries` and `QuestionQueries`, the routes in a new `SubjectLookupEndpoints.cs` mapped from `QuestionBankEndpoints`, and the error code `question.subject_not_found`.
- `Simulab.Web`: `QuestionBankApiClient` gets `SearchSubjectsAsync` and `ListTopicsAsync`; `QuestionForm.razor` uses them in place of `CatalogApi`.
- Business-rule duplicates: none to fix or defer (see Cause).

## Regression test
- `SubjectLookupEndpointTests` in `tests/Modules/QuestionBank/Simulab.QuestionBank.Tests/`, the shape of `EditionLookupEndpointTests`: AC1 to AC4 (the curator's 200 with the seeded `Curator` is the one seen failing before the fix: the route does not exist, 404).
- `QuestionFormTests` in `tests/Hosts/Simulab.Web.Tests/QuestionBank/`: AC5 and AC6, a fake handler records the paths and asserts that none starts with `/api/v1/catalog/` (fails before the fix: the form calls the catalog routes).
- `SubjectDirectoryTests` in the catalog's test project: live subjects only, the normalized search, topics of a deleted subject are `null`. (BR3)
- AC7 is a validation step, not a test; AC8 is the existing architecture and module-boundary tests plus the missing-key test, all green.

## Decisions
- 2026-10-12: the lookups are routes of `QuestionBank` under `questions.manage`, fed by a directory contract of the catalog — owner, F-96 refinement and this bug's `## Expected`; granting `catalog.manage` to `Curator` is a roles decision bigger than this bug.
- 2026-10-12: a new `ISubjectDirectory` rather than growing `ITopicDirectory` — Claude, technical. Reason: `ITopicDirectory` answers "what do I know about these topic ids"; searching subjects and listing a subject's topics is another question, and `IExamEditionDirectory` set the precedent of one directory per question.
- 2026-10-12: the topic lookup returns the subject's whole topic list and the form narrows it, as today — Claude, technical. Reason: a subject has a short topic list (`QuestionForm.razor:705`); no new parameter on the route.
- 2026-10-12: the missing-topic dead end stays out of scope, not registered — owner chose "Fora do escopo, sem registrar" (the recommended one). Reason: creating a subject or topic from the form is a roles and screen decision; the owner asks when wanted.
- 2026-10-12: subjects with no topic stay in the subject lookup — owner chose "Sim, igual a hoje" (the recommended one). Reason: the fix only changes who may call; hiding them would also change what the administrator sees.

## Out of scope
- Creating a subject or a topic from the question form, or giving `Curator` any catalog grant.
- The edition page keeps `catalog.manage` (F-96 decision).
- Other screens of the Web that call `catalog.manage` routes: none found outside the catalog pages.

## Open questions
- (none)

## Validation script
Written at the build (`/agile:build B-28`, with the commands for the owner). It will cover: sign in with the account that holds only `Curator`, open `/admin/questions/new`, search a subject, pick a topic, fill the question and save; then repeat as the administrator to see the same lists. (AC7)

## Events

- 2026-10-12 question — asked 2, from context 0

## Delivery
