---
feature: F-90
epic: Subject taxonomy
status: building
board: 133
version: 1
---
# Where a subject or topic is used

Technical terms: [glossary](../glossary.md)

## Summary
On the subject list and the topics section of F-79, a curator sees which exam editions and notice subjects map a subject or topic, so a delete refused as "in use" (F-75 BR9, BR10) can be resolved without hunting edition by edition. Captured from the F-75 screen design (owner, 2026-10-04).
Refined 2026-10-21: a topic is also in use while questions point at it (F-95 BR13), so its "where used" shows the questions too, as a count with a link to the filtered question list.

## Start
- Depends on: F-75.
- Waits on (to start): nothing.
- Needed to validate: the local app host with the seeded taxonomy, at least one edition with mapped notice subjects and one question on a topic (seed or created during validation) — Claude prepares them in the validation script.
- Model: opus (a message between modules: the question bank answers the catalog through a new contract)
- Suggested path: `/agile:refine` → `/agile:build`
- Parallel with: F-107 (question bank only; it touches no catalog screen).

## Goal
A curator whose delete is refused as "in use" sees, in one place, exactly what holds the subject or topic, and reaches the screen that releases it in one click.

## What exists
- `SubjectResponse.InUse` and `TopicResponse.InUse` (F-75 BR10, F-95): the list and the topics section disable delete with `Subjects.Delete.DisabledInUse` / `Topics.Delete.DisabledInUse` (`Subjects.razor:100`, `TopicsSection.razor:43`).
- A subject is in use only when a live notice subject maps it whole (`SubjectStore.IsMappedWholeAsync`, `SubjectQueries.cs:96`); `subject.has_topics` is checked first (`DeleteSubjectHandler`).
- A topic is in use when a live notice subject maps it (`TopicStore.IsMappedAsync`) or when any `ITopicUsageCheck` says so (`DeleteTopicHandler`, `TopicQueries.cs:35`). The only check today is `QuestionTopicUsageCheck` (QuestionBank): any live question, of any status, with that `TopicId`.
- `CatalogModuleDbContext.LiveNoticeSubjectMappings` already excludes deleted notice subjects and notice subjects of deleted editions (F-75 BR9, BR11).
- The question list filters by topic through the query string `?topic=<id>` (`QuestionListQueryString.TopicKey`), behind `questions.manage`.
- The edition page `/admin/exams/{examId}/editions/{id}` holds `NoticeSubjectsSection`, where a mapping is edited (F-75).
- Items that touched this code since F-75: F-77 (aliases), F-95 (question usage check), F-96 (`UsageChecks`), F-100 (question list filters).

## Users and use cases
- UC1 A curator on `/admin/subjects` opens "Where used" on a subject in use and sees each live notice subject that maps it whole, with its exam and edition, and opens that edition's page from a row.
- UC2 A curator on a subject page opens "Where used" on a topic in use and sees each live notice subject that maps it, with its exam and edition, and how many questions point at it.
- UC3 A curator who also holds `questions.manage` follows the question count to the question list already filtered by that topic.
- UC4 A curator who holds `catalog.manage` but not `questions.manage` sees the question count without a link.

## Business rules
- BR1 "Where used" is read-only. It lists what makes `InUse` true; it changes nothing. A mapping is released on the edition page (F-75), a question on its own page.
- BR2 A subject's "where used" lists only the live notice subjects that map it whole (the rule of `subject.in_use`, F-75 BR9). Mappings of its topics are shown on each topic.
- BR3 A topic's "where used" lists the live notice subjects that map it and the number of live questions, of any status, whose topic it is (F-95 BR13).
- BR4 "Live" is the F-75 BR9 rule: a deleted notice subject, or one of a deleted edition, is not listed.
- BR5 Each mapping row shows the exam name, the edition (notice year, and position when it has one) and the notice subject name, ordered by exam name (normalized), notice year descending, position, then the notice subject's own order. Each row links to the edition page.
- BR6 The question count links to `/admin/questions?topic=<id>` only when the reader holds `questions.manage`; otherwise it is plain text. A count of zero is not shown.
- BR7 The action "Where used" appears on a row only when its `InUse` is true; it sits with the row actions, next to the disabled delete.
- BR8 An item that is no longer in use when the dialog opens (someone released it after the list loaded) shows the empty state "Nothing uses this item any more" and the list reloads when the dialog closes.
- BR9 The catalog asks each module for its count through a contract; a module that later references a topic adds its own answer and never edits the catalog (the F-95 pattern).
- BR10 One permission gates the endpoints, the action and the dialog: `catalog.manage` (F-79 BR15). The question link is gated by `questions.manage`, checked by permission and not by role name.
- BR11 Unknown or deleted ids answer 404 `subject.not_found` / `topic.not_found`.
- BR12 Every new UI text exists in pt-BR, pt-PT and en in `SharedResources`.

## Screens and API
- `/admin/subjects` (`Subjects.razor`) [changed]: row action "Where used" when `InUse` (BR7); opens the dialog.
- `/admin/subjects/{id}` (`TopicsSection.razor`) [changed]: topic row action "Where used" when `InUse`; opens the dialog.
- `WhereUsedDialog` [new]: title "Where <name> is used"; a list of mapping rows (BR5), each a link to the edition page; for a topic, a line "Used by <n> questions" (link per BR6); empty state (BR8); Close.
- GET `/api/v1/catalog/subjects/{id}/where-used` [new] — the whole-subject mappings (BR2). `catalog.manage`.
- GET `/api/v1/catalog/topics/{id}/where-used` [new] — the topic mappings and the question count (BR3). `catalog.manage`.
- Response `WhereUsedResponse`: `mappings` (each `examId`, `examName`, `editionId`, `noticeYear`, `position` nullable, `noticeSubjectId`, `noticeSubjectName`) and `questionCount` (0 for a subject).
- Error codes: `subject.not_found`, `topic.not_found` (existing). No new code.

### Wireframe: Subject list row
```text
| Name                   | Area | Topics |               |
| Direito Constitucional | Law  | 0      | [?][e][x-]    |
                        [?] Where used [new]
                        [x-] delete disabled: in use
```

### Wireframe: Where used dialog
```text
+------------------------------------------------+
| Where "Crase" is used                    [new] |
|                                                |
| Notice subjects                                |
|  Guarda Municipal SP - 2025 - Guarda    >      |
|    Lingua Portuguesa                           |
|  Guarda Municipal SP - 2023             >      |
|    Portugues                                   |
|                                                |
| Questions                                      |
|  Used by 12 questions                   >      |
|  (plain text without questions.manage)         |
|                                                |
|                                     [ Close ] |
+------------------------------------------------+
Empty: "Nothing uses this item any more."
```

## Docs
- flow: new docs/flows/subject-topic-where-used.md — how the dialog gathers the mappings and each module's count, and where each row leads.

## Acceptance criteria
- AC1 Given a subject mapped whole by two live notice subjects of two editions, when the curator opens "Where used" on its row, then both appear with exam, edition and notice subject in BR5 order, each linking to its edition page. (UC1, BR2, BR5)
- AC2 Given a subject whose only whole mapping belongs to a deleted notice subject or a deleted edition, then its row is not in use, has no "Where used" action, and the endpoint returns no mapping. (BR4, BR7)
- AC3 Given a subject whose topics are mapped but which is not mapped whole, then its "where used" lists no mapping of those topics. (BR2)
- AC4 Given a topic mapped by one live notice subject and pointed at by 3 questions (one draft, one in review, one published) and one deleted question, when the curator opens "Where used", then the mapping appears and the count reads 3. (UC2, BR3, BR4)
- AC5 Given a curator with `questions.manage`, when they click the question count, then the question list opens filtered by that topic; given a curator with `catalog.manage` only, then the count shows without a link. (UC3, UC4, BR6, BR10)
- AC6 Given a topic used only by questions, then the dialog shows no mapping row and the question count. (BR3)
- AC7 Given a row not in use, then it has no "Where used" action. (BR7)
- AC8 Given a subject released by another curator after the list loaded, when the curator opens "Where used", then the empty state shows and closing the dialog reloads the list. (BR8)
- AC9 Given a second module's answer to the count contract (test double), then its count is added to the topic's question count without a change to the catalog. (BR9)
- AC10 Given a signed-in Student, then both endpoints answer 403 `identity.forbidden`. (BR10)
- AC11 Given an unknown or deleted id, then 404 `subject.not_found` / `topic.not_found`. (BR11)
- AC12 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR12)

## Criterion → test

Tests locked: 274bc91bade297100717f3e5aaee3e145798be56 (7 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `WhereUsedEndpointTests.Subject_MappedWholeByNoticeSubjectsOfSeveralEditions_ListsThemInTheBr5Order`; `SubjectsWhereUsedPageTests.WhereUsed_SubjectMappedByTwoEditions_ListsBothInOrderWithLinksToTheEditionPages` |
| AC2 | `WhereUsedEndpointTests.Subject_WhoseOnlyMappingIsDeleted_IsNotInUseAndListsNoMapping` (both cases); `SubjectsWhereUsedPageTests.Load_OnlyTheRowInUse_HasTheWhereUsedAction` |
| AC3 | `WhereUsedEndpointTests.Subject_WhoseTopicsAreMappedButNotItself_ListsNoMapping` |
| AC4 | `TopicWhereUsedEndpointTests.Topic_MappedAndUsedByQuestionsOfEveryStatus_ShowsTheMappingAndCountsTheLiveQuestions`; `WhereUsedEndpointTests.Topic_MappedByLiveAndDeletedNoticeSubjects_ListsOnlyTheLiveOnes` |
| AC5 | `TopicsWhereUsedSectionTests.WhereUsed_WithQuestionsManage_TheCountLinksToTheFilteredQuestionList`; `TopicsWhereUsedSectionTests.WhereUsed_WithCatalogManageOnly_TheCountIsPlainText` |
| AC6 | `TopicWhereUsedEndpointTests.Topic_UsedOnlyByQuestions_HasNoMappingAndTheCount`; `TopicsWhereUsedSectionTests.WhereUsed_TopicUsedOnlyByQuestions_ShowsNoMappingAndTheCount` |
| AC7 | `SubjectsWhereUsedPageTests.Load_OnlyTheRowInUse_HasTheWhereUsedAction`; `TopicsWhereUsedSectionTests.Load_OnlyTheTopicInUse_HasTheWhereUsedAction` |
| AC8 | `SubjectsWhereUsedPageTests.WhereUsed_SubjectReleasedSinceTheListLoaded_ShowsTheEmptyStateAndReloadsTheListOnClose`; `TopicsWhereUsedSectionTests.WhereUsed_TopicReleasedSinceTheSectionLoaded_ShowsTheEmptyStateAndReloadsTheSectionOnClose`; `TopicWhereUsedEndpointTests.Count_IsAboveZeroExactlyWhileTheListSaysTheTopicIsInUse` |
| AC9 | `TopicUsageCountTests.CountAsync_TwoChecks_AddsBothCounts`; `TopicWhereUsedSecondModuleTests.TopicCount_AddsTheSecondModulesAnswerToTheQuestions` |
| AC10 | `WhereUsedEndpointTests.BothEndpoints_ForAStudent_Answer403Forbidden` |
| AC11 | `WhereUsedEndpointTests.Subject_UnknownOrDeleted_Answers404SubjectNotFound`; `WhereUsedEndpointTests.Topic_UnknownOrDeleted_Answers404TopicNotFound` |
| AC12 | `WhereUsedResourcesTests.EveryNewText_ExistsInTheThreeLanguages`; `ResourceParityTests` (existing) |

## Decisions
- 2026-10-21 — Only live mappings count (deleted notice subjects and editions excluded) — it shows exactly what blocks the delete — confirmed: F-75 BR9
- 2026-10-21 — Questions of any status count as a use of the topic — same rule that blocks the delete today — confirmed: F-95 BR13
- 2026-10-21 — A topic's "where used" shows the mappings and the question count with a link to the filtered list; questions are counted, not listed — without the questions a topic used only by them stays unexplained, and the question list already lists them — asked
- 2026-10-21 — Shown in a "Where used" dialog opened from the row actions, only when `InUse` — weighs neither the list nor the page; a tooltip cannot hold links — asked
- 2026-10-21 — A curator without `questions.manage` sees the count without the link — the count is the reason for the refusal; the link would lead to Not Found — asked
- 2026-10-21 — A subject's dialog shows only whole-subject mappings — that is what `subject.in_use` counts; topic uses live on each topic — asked
- 2026-10-21 — Each mapping row links to the edition page — the mapping is released there; one way to edit an item — asked
- 2026-10-21 — Unmapping from the dialog is out of scope — the dialog only reads; editing stays on the edition — asked
- 2026-10-21 — Routes `/where-used` and type `WhereUsedResponse`, not "usage" — "usage" is taken by `AiUsage` in the glossary — Claude, technical
- 2026-10-21 — The question count comes through a catalog contract that each referencing module implements (the F-95 `ITopicUsageCheck` pattern); its exact shape (a count method on the existing check or a sibling interface) is settled by the design pass at build — the catalog must not read the question bank's tables — Claude, technical
- 2026-10-21 — No paging in the dialog: mappings per subject or topic are bounded by the editions of the catalog (tens) — Claude, technical
- 2026-10-21 — No new package — the item uses MudBlazor, EF Core and the test stack already referenced — Claude, verified in `Directory.Packages.props`
- 2026-10-22: design passes (system-design, architect; opus) — accepted: contracts `WhereUsedResponse` and `WhereUsedMappingResponse` in `Catalog.Contracts`; `ISubjectQueries` / `ITopicQueries.WhereUsedAsync` returning null for the 404; one shared `WhereUsedMappings` projection over `LiveNoticeSubjectMappings` (BR4 stays in one place), ordered by exam normalized name, notice year descending, normalized position, display order, id; the action through `AppRowActions.MoreActions`, after Delete (no kit change; three actions fit `MaxVisible`); the list reloads on every close; a 404 in the dialog shows the existing `ErrorText` message; the count stays labelled "questions" in v1; architect finding 1 (`TopicsSection` ends the action handler with `InvokeAsync(StateHasChanged)`, project rule F-95/F-86, with a bUnit test); finding 2 (no mockup, so no `frontend` agent: each use case is one vertical step, API and screen together); finding 3 (the manual change is `docs/manual/pt-BR/subjects.md`, no new page). Dropped: nothing.
- 2026-10-22: design coverage — scope (item file in full), contracts (routes, callers and implementers of `ITopicUsageCheck`, F-107 worktree), data (indexes, `LiveNoticeSubjectMappings`, `DeleteExamHandler` guard for the inner join), simplicity (reuse of `TopicUsageChecks`, `QuestionListQueryString.Build`, `ErrorText`, `MoreActions`, `ConfigureHost`); not checked: none.
- 2026-10-22: design trade-off 4 Where the question count lives — owner chose as proposed (`CountAsync` on `ITopicUsageCheck`)
- 2026-10-22: plan — 1 tests first for every criterion a test reaches, with skeletons → red, then locked; 2 subject where-used end to end (contracts, `WhereUsedMappings`, query, endpoint, `CatalogApiClient`, `WhereUsedDialog`, `Subjects.razor` action) → AC1, AC2, AC3, AC7, AC8, AC10, AC11 (subject); 3 topic where-used end to end (`ITopicUsageCheck.CountAsync`, `TopicUsageChecks.CountAsync`, query, endpoint, `TopicsSection.razor` action, count line gated by `questions.manage`) → AC4, AC5, AC6, AC9, AC10, AC11 (topic); 4 texts in three locales → AC12; 5 OpenAPI document, flow page, pt-BR manual → OpenAPI 200, `techdocs.js check`; 6 gate, app host screen check, scan → `agile gate GREEN`, `agile scan GREEN`
- 2026-10-22: traps — T1 sibling `ITopicUsageCount`: an in-use row could open on the empty state (avoided); T2 every module's count added into `questionCount`: a non-question module would be labelled "questions" (taken, BR9 requires it; an idea when the first such module appears); T3 a second copy of the live rule or reuse of `ListByEditionsAsync`: drift from `InUse` or in-memory sort (avoided).

## Out of scope
- Unmapping or editing from the dialog (BR1).
- Listing the questions one by one in the dialog.
- Grouping a subject's topic uses in the subject's dialog (BR2).
- A "where used" for other catalog items (organizers, exams, editions).

## Open questions
- (none)

## Change notes

## Validation script

## Events

- 2026-10-21 question — asked 6, from context 2

## Delivery
