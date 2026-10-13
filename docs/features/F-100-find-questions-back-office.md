---
feature: F-100
epic: Question bank
status: validating
board: 146
version: 2
---
# Find questions in the back office

Technical terms: [glossary](../glossary.md)

## Summary
The back office question list gets filters: subject and topic, organizer, edition, type, status, difficulty and statement text. The filters are picked with the UI kit's `AppMultiPickField` (and a search lookup for the edition), the active ones show as removable chips, and they live in the address so a filtered list survives a reload and the way back from the question form. Source: epic Question bank (E-4); Simulae has no question list page.

## Start
- Depends on: F-95 (done), F-96 (done), F-97 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host with a few dozen questions of several topics, editions, types and statuses — owner (F-110 seeds none yet; the curator writes them by hand or the validation script says how many).
- Suggested path: `/agile:autopilot`.
- Parallel with: F-99, F-101.

## Goal
A curator with hundreds of questions finds the ones to review, fix or publish in a few clicks, instead of scrolling a list that only knows "status" and "statement text".

## What exists (verified 2026-10-13)
- `GET /api/v1/question-bank/questions` takes `page`, `pageSize`, `search` and `status` (`QuestionEndpoints.ListAsync`, `QuestionListQuery`); the handler is `QuestionQueries.ListAsync`. Order is `CreatedAt DESC, Id DESC`; search is `NormalizedStatement.Contains(...)`; an unknown `status` is ignored (F-95 BR15).
- `QuestionListItemResponse` already carries topic, subject, difficulty, language, status, type and `Origin` (organizer, exam, notice year, edition, paper number). No response change is needed.
- `Questions.razor` has one `AppSelectField<QuestionStatus?>` in the toolbar and no address state; its column comments say the origin and type filters are F-100.
- A question stores `TopicId`, `ExamEditionId`, `Type`, `Difficulty`, `ContentLanguage` and `Status`, without foreign keys across modules. Subject, exam and organizer exist only in `catalog.*`.
- Indexes on `questions`: `ix_questions_topic`, `ix_questions_status_created_at`, `ix_questions_exam_edition`, `ix_questions_notice_subject`. Nothing on `Type`, `Difficulty` or `ContentLanguage`.
- A curator holds only `questions.manage`. The routes the question form uses today (`/question-bank/subjects`, `/subjects/{id}/topics`, `/editions`) answer that permission, but there is no route that lists the whole taxonomy or the organizers, and `Catalog.Contracts` has no method that gives the editions of an organizer.
- `AppMultiPickField` searches a list the page already holds; `NoticeSubjectDialog` shows the group-and-topic pattern. `AppFilterChip` is the removable chip (AccountEvents, RoleHistory); `AccountEvents.razor` binds filters to the address with `[SupplyParameterFromQuery]` and `replace: true`.
- Annulment (F-101) and review state (F-105) do not exist yet, so the status filter has `Draft` and `Published` only.

## Users and use cases
- UC1 A curator narrows the list by one or more topics or whole subjects.
- UC2 A curator narrows the list by one or more organizers.
- UC3 A curator narrows the list to one exam edition.
- UC4 A curator narrows the list by type, status and difficulty (several values each).
- UC5 A curator combines filters with the statement text search and sees which are active, removes one, or clears all.
- UC6 A curator opens a question, saves, comes back with the browser back button or "Cancel", and finds the same filtered list.

## Business rules
- BR1 Filters combine with AND between filters and OR among the values of one filter. A filter with no value does not restrict.
- BR2 Subject and topic: the picker is one `AppMultiPickField` over the taxonomy, topics grouped by subject; picking a subject's group option means "every topic of that subject", resolved by the Api at query time (a topic added later is included). Subject and topic picks are a union.
- BR3 Organizer: any number of organizers; the Api turns them into their editions through `Catalog.Contracts`. Edition: at most one, chosen by search (as the question form does). Organizer and edition together are an intersection.
- BR4 Type, status and difficulty accept any number of values. Difficulty offers 1 to 5 and "Not set" (a question with no difficulty).
- BR5 The text search stays the F-95 statement search (`NormalizedStatement`, ignoring case and accents); it is not widened to options or explanation.
- BR6 An unknown or malformed filter value is ignored, never a 400 (decision of F-95 for `status`). A filter list is capped at 100 values; extra values are ignored.
- BR7 The order stays newest first (`CreatedAt DESC, Id DESC`); paging, `pageSize` clamps and the "never shows deleted questions" rule of F-95 BR15 are unchanged. A change of filter returns to the first page.
- BR8 The filters are kept in the address: `status`, `type`, `difficulty`, `subject`, `topic`, `organizer`, `edition`, `search` (repeated keys for lists). Opening such an address applies the filters; a value that no longer resolves (a deleted topic, an unknown edition) is dropped from the pickers without an error.
- BR9 Only the permission `questions.manage` is needed for the list, the taxonomy route and the organizer route (F-95 BR14); no new permission.
- BR10 The Api reads other modules only through `Catalog.Contracts`; the module never reads `catalog.*` tables.
- BR11 The pickers of subject/topic and organizer share a limit of 100 picked ids in total on the page, so the address stays inside the hosts' request line (change note v2); the Api keeps 100 values per list (BR6).

## Screens and API
No mockup: the epic marks this item "Screen design: no" and it reuses kit parts on an existing page; the owner may ask for `/agile:screen` before approving.

### Screen — `/admin/questions` (changed)
- Pattern: `Exams.razor` toolbar (fields in the `app-table-filters` row) plus `AccountEvents.razor` (address-bound filters).
- Toolbar, in this order, each field with its own id (`questions-filter-*`): Status (multi), Type (multi), Difficulty (multi, with "Not set"), Subject and topic (grouped multi), Organizer (multi), Edition (search lookup, one value). The text search stays the table's own search box.
- The pickers are `AppMultiPickField` (Status, Type, Difficulty, Subject/topic, Organizer); the edition is the lookup field the question form already uses.
- Each `AppMultiPickField` draws its own removable chips (a subject chip says "X (all topics)") and its own "Clear"; removing a chip reloads from the first page. There is no second chip row (change note v2). One page-level "Clear all" text button sits in the toolbar, shown only while a filter or the search is active; it clears every filter and the table's search box (`AppDataTable.ClearSearchAsync`, new kit method with a bUnit test).
- The pickers of subject/topic and organizer share one limit: at most 100 picked ids in total (subject, topic and organizer together); at the limit the unpicked options are disabled with a short hint, as `NoticeSubjectDialog` does. The Api keeps 100 values per list (BR6) so a longer address from outside is still answered.
- `Questions.Empty.Filters` ("No question matches the chosen filters.") shows when any filter is set and nothing matches; the result count is announced as every table does (`Common.Table.Results.*`).
- The taxonomy and organizer lists load once with the page (`AppMultiPickState` Loading, LoadFailed with retry, Empty). A failure of one list does not block the list of questions.
- Keyboard and names: every picker has a visible label and the kit's own accessible name; chips are removable by keyboard with an `aria-label` that names the value (`Common.RemoveFilter`-style key, reuse the one `AccountEvents` uses).

### Api
- `GET /api/v1/question-bank/questions` gains repeated query parameters: `status`, `type`, `difficulty` (`1`–`5` or `none`), `subject` (ids), `topic` (ids), `organizer` (ids), and a single `edition` (id). `page`, `pageSize` and `search` are unchanged. No new error code.
- New `GET /api/v1/question-bank/taxonomy`: subjects with their topics (id, name), ordered by name; for the filter picker; permission `questions.manage`.
- New `GET /api/v1/question-bank/organizers`: organizers (id, acronym, name), ordered by acronym; permission `questions.manage`.
- `Catalog.Contracts` gains what these need and nothing else: organizers listing and editions of organizers (`IExamEditionDirectory`), and the topics of subjects in bulk (`ITopicDirectory` or `ISubjectDirectory`). The Catalog implements them over its own context.
- Typed client: `QuestionBankApiClient.ListQuestionsAsync` takes a filter object; new `GetTaxonomyAsync` and `ListOrganizersAsync`.

### Texts (resource keys; en / pt-BR / pt-PT)
| Key | en | pt-BR | pt-PT |
| --- | --- | --- | --- |
| `Questions.Filter.Type` | Type | Tipo | Tipo |
| `Questions.Filter.Difficulty` | Difficulty | Dificuldade | Dificuldade |
| `Questions.Filter.Subject` | Subject and topic | Disciplina e tópico | Disciplina e tópico |
| `Questions.Filter.Organizer` | Organizer | Banca | Banca |
| `Questions.Filter.Edition` | Edition | Edição | Edição |
| `Questions.Filter.Status.Placeholder` | Status name | Nome da situação | Nome do estado |
| `Questions.Filter.Type.Placeholder` | Type name | Nome do tipo | Nome do tipo |
| `Questions.Filter.Difficulty.Placeholder` | A number from 1 to 5 | Um número de 1 a 5 | Um número de 1 a 5 |
| `Questions.Filter.Subject.Placeholder` | Subject or topic name | Nome da disciplina ou do tópico | Nome da disciplina ou do tópico |
| `Questions.Filter.Organizer.Placeholder` | Organizer name | Nome da banca | Nome da banca |
| `Questions.Filter.Edition.Placeholder` | Exam, organizer or year | Concurso, banca ou ano | Concurso, banca ou ano |
| `Questions.Filter.Options` | {0}: options | {0}: opções | {0}: opções |
| `Questions.Filter.Picked` | {0}: picked | {0}: escolhidos | {0}: escolhidos |
| `Questions.Filter.Count` | Picked: {0} | Escolhidos: {0} | Escolhidos: {0} |
| `Questions.Filter.None` | No filter | Sem filtro | Sem filtro |
| `Questions.Filter.ClearField` | Clear | Limpar | Limpar |
| `Questions.Filter.Clear` | Clear all | Limpar tudo | Limpar tudo |
| `Questions.Filter.SubjectAll` | {0} (all topics) | {0} (todos os tópicos) | {0} (todos os tópicos) |
| `Questions.Filter.Loading` | Loading options… | Carregando opções… | A carregar opções… |
| `Questions.Filter.LoadFailed` | The list of options did not load. | A lista de opções não carregou. | A lista de opções não carregou. |
| `Questions.Filter.Limit` | Up to 100 subjects, topics and organizers can be picked together. | Dá para escolher até 100 disciplinas, tópicos e bancas ao todo. | É possível escolher até 100 disciplinas, tópicos e bancas no total. |
| `Questions.Empty.Filters` (changed) | No question matches the chosen filters. | Nenhuma questão corresponde aos filtros escolhidos. | Nenhuma questão corresponde aos filtros escolhidos. |

Reused as they are: `Questions.Filter.Status`, `Questions.Status.*`, `Questions.Type.*`, `Questions.Difficulty.NotSet`, `Questions.Source.Unresolved` (an edition that could not be read), `Common.RemoveItem`, `Common.TryAgain`, `Common.Table.*`, `Lookup.*`. `ResourceParityTests` and `QuestionFilterResourcesTests` guard the table.

## Approved list
1. Status — multi, `Draft` and `Published`.
2. Type — multi, `SingleChoice` and `TrueFalse`.
3. Difficulty — multi, 1 to 5 and "Not set".
4. Subject and topic — grouped multi; a subject group means all its topics.
5. Organizer — multi.
6. Edition — one, by search.
7. Statement text — the existing search box.

## Acceptance criteria
- AC1 Given questions of several topics, when the curator picks two topics, then only questions of those topics are listed; with no topic picked, every topic is listed. (UC1, BR1, BR2)
- AC2 Given a subject picked as a whole and a topic of another subject, when the list loads, then it holds questions of every topic of the first subject plus that topic; a topic added to the subject afterwards appears without changing the filter. (BR2)
- AC3 Given editions of two organizers, when one organizer is picked, then only questions of that organizer's editions are listed; authored questions (no edition) are not. (UC2, BR3)
- AC4 Given an edition and an organizer that does not own it, when both are set, then the list is empty and says so. (UC3, BR3)
- AC5 Given questions of both types and statuses, when the curator picks `TrueFalse` and `Published`, then only published true/false questions are listed; picking both statuses lists both. (UC4, BR1, BR4)
- AC6 Given questions with and without difficulty, when "Not set" and 5 are picked, then questions with no difficulty or difficulty 5 are listed. (BR4)
- AC7 Given filters and a statement word, when the list loads, then only questions that satisfy every filter and contain the word are listed, still newest first and paged. (UC5, BR1, BR5, BR7)
- AC8 Given a request with an unknown `type`, a malformed id or an unknown `status`, then the value is ignored and the list answers 200; a user without `questions.manage` gets 403 on the list, the taxonomy and the organizers. (BR6, BR9)
- AC9 Given a list filtered by organizer and status, when the curator opens a question, saves and goes back, then the same filters, search and page are applied from the address. (UC6, BR8)
- AC10 Given an address with a topic that was deleted, when the page opens, then the page loads, that value is not shown in the picker and the list is not restricted by it. (BR8)
- AC11 Given active filters, then each picker shows one chip per picked value; removing a chip reloads from the first page without that value; the page-level "Clear all" removes every filter and the search. (UC5)
- AC12 Given the taxonomy cannot be loaded, then the subject picker shows its load error with retry and the question list still loads. (Screens)
- AC13 Given the architecture rules, then `QuestionBank` still references other modules only through their `Contracts`, and the list queries on 50,000 questions stay within the interactive budget measured at the build (`EXPLAIN` on the test container; an index is added only if the measurement asks for it). (BR10)
- AC14 All new texts appear in pt-BR, pt-PT and en. The keyboard and accessible names of the pickers and chips are checked on screen (WCAG 2.2 AA).
- AC15 Given 100 picked ids across subject, topic and organizer, then no further option of those pickers can be picked and the address stays within the request-line limit: a list request with 100 ids sent through the typed client is answered 200 by the Api. (BR6, change note v2)

## Criterion → test

Tests locked: 65fc466fe5ca28dacc84413ac7975f6c0f70c1fc (7 files); unlocks: 1


| Criterion | Test |
| --- | --- |
| AC1 | `QuestionFilterEndpointTests.List_ByTopics_KeepsOnlyTheQuestionsOfThePickedOnes`; screen: `QuestionsFilterPageTests.Filter_ATopic_IsSentAsTopic` |
| AC2 | `QuestionFilterEndpointTests.List_BySubject_MeansEveryTopicOfIt_AndIsAUnionWithPickedTopics`, `QuestionFilterDirectoryTests.ListTaxonomy_WithIds_KeepsOnlyTheLiveSubjectsAmongThem`; screen: `QuestionsFilterPageTests.Filter_AWholeSubject_IsSentAsSubject_AndItsChipSaysAllTopics` |
| AC3 | `QuestionFilterEndpointTests.List_ByOrganizer_KeepsTheQuestionsOfItsEditions_AndLeavesOutAuthoredOnes`, `QuestionFilterDirectoryTests.ListOrganizerEditions_AreTheLiveEditionsOfAnyStatus_ByOrganizer`; screen: `QuestionsFilterPageTests.Filter_AnOrganizer_IsSentAsOrganizer`, `QuestionsFilterPageTests.Filter_AnEdition_IsSentAsEdition` |
| AC4 | `QuestionFilterEndpointTests.List_ByAnEditionAndAForeignOrganizer_IsEmpty` |
| AC5 | `QuestionFilterEndpointTests.List_ByTypeAndStatus_KeepsWhatMatchesEveryFilter`; screen: `QuestionsFilterPageTests.Filter_TypeAndStatus_AreSentAsRepeatedKeys_FromTheFirstPage`, `QuestionsPageTests.Filter_ByStatus_ReloadsTheListFromTheFirstPageWithThatStatus` |
| AC6 | `QuestionFilterEndpointTests.List_ByDifficultyNoneAndFive_KeepsBothOfThem`; screen: `QuestionsFilterPageTests.Filter_DifficultyNotSetAndFive_AreSentAsNoneAndFive` |
| AC7 | `QuestionFilterEndpointTests.List_FiltersAndSearchTogether_AreNewestFirstAndPaged`; screen: `QuestionsFilterPageTests.Filter_AndSearch_AreSentTogether` |
| AC8 | `QuestionFilterEndpointTests.List_ValuesThatAreNotUnderstood_AreIgnored`, `QuestionStatusNumberEndpointTests.List_StatusGivenAsItsNumber_MeansThatStatus`, `QuestionFilterEndpointTests.Lookups_WithoutThePermission_AnswerForbidden`, `QuestionFilterEndpointTests.Taxonomy_ListsEverySubjectWithItsTopicsByName_ForACuratorWithNoCatalogPermission`, `QuestionFilterEndpointTests.Organizers_ListsEveryLiveOrganizerByAcronym_ForACuratorWithNoCatalogPermission` |
| AC9 | `QuestionListQueryStringTests` (read, write, round trip, carried keys); screen: `QuestionsFilterPageTests.Filter_IsWrittenToTheAddress`, `QuestionsFilterPageTests.Address_WithFilters_IsAppliedAndShownInThePickers`, `QuestionsFilterPageTests.Add_FromAFilteredList_CarriesTheFiltersToTheForm`, `QuestionsFilterPageTests.Edit_FromAFilteredList_CarriesTheFiltersToTheForm`, `QuestionsFilterPageTests.Form_Cancel_ReturnsToTheListWithTheFiltersItWasOpenedWith` |
| AC10 | `QuestionFilterEndpointTests.List_IdsThatNothingHas_AreDropped`; screen: `QuestionsFilterPageTests.Address_WithATopicThatNoLongerExists_LoadsAndDropsIt` |
| AC11 | `QuestionsFilterPageTests.Chips_OnePerPickedValue_AndRemovingOneReloadsWithoutIt`, `QuestionsFilterPageTests.ClearAll_RemovesEveryFilterAndTheSearch_AndHidesItself`, `QuestionsFilterPageTests.ClearAll_IsShownWhileOnlyTheSearchIsSet`, `AppDataTableClearSearchTests` |
| AC12 | `QuestionsFilterPageTests.Taxonomy_FailsToLoad_ShowsItsErrorWithTryAgain_AndTheListStillLoads` |
| AC13 | `ModuleBoundaryTests` (the module still reaches others only through `Contracts`); the `EXPLAIN` measurement at 50,000 rows is quoted in `## Decisions` (validation step 6) |
| AC14 | `QuestionFilterResourcesTests`, `ResourceParityTests`; keyboard and accessible names of the pickers and chips: validation step 5 |
| AC15 | `QuestionFilterEndpointTests.List_HundredTopicIds_AreReadWhole`, `QuestionFilterEndpointTests.List_MoreIdsThanTheCap_ReadsOnlyTheFirstOnes`, `QuestionBankApiClientFilterTests.List_AHundredPickedIds_StayInsideTheRequestLineOfTheHosts`; screen: `QuestionsFilterPageTests.Limit_AtAHundredPickedIds_TheOtherOptionsAreDisabledAndTheHintSaysWhy` |

## Decisions
- 2026-10-13 — An unknown or malformed filter value is ignored, not refused — same as F-95 for `status`; a stale link still opens (confirmed: F-95 `## Decisions`, `List_UnknownStatus_IsIgnored`).
- 2026-10-13 — The list and the new routes need `questions.manage` and no new permission (confirmed: F-95 BR14).
- 2026-10-13 — Subject and topic: grouped multi-pick; a subject group means all its topics, resolved by the Api at query time (owner, asked; chosen over cascading selects and subject only).
- 2026-10-13 — Edition: one edition, by search; several editions are not offered (owner, asked). The list of editions can reach hundreds, which `AppMultiPickField` (a list held by the page) does not suit.
- 2026-10-13 — Organizer: multi-pick of a short list from a new route (owner, asked).
- 2026-10-13 — Type, status and difficulty accept several values; AND between filters, OR within one (owner, asked).
- 2026-10-13 — Filters live in the address, with removable chips and "Clear all" (owner, asked).
- 2026-10-13 — Order stays newest first; column sorting is not built here — it was deferred to this item by a column comment of F-95, and it becomes F-118 (owner, asked).
- 2026-10-13 — Statement text search only; options and explanation are not searched (owner, asked).
- 2026-10-13 — No extra filter (content language, authored versus from an edition) now, both go to F-118; no new package (owner, asked).
- 2026-10-13 — The taxonomy and organizer lists are two small routes of `QuestionBank` guarded by `questions.manage`, and `Catalog.Contracts` gets the matching methods — the curator has no `catalog.manage`, and the module reads other modules only through contracts (Claude; F-96 BR9 and B-28 are the precedent).
- 2026-10-13 — The address holds subject ids and topic ids separately, so a whole-subject pick stays "all topics" after a reload (Claude).
- 2026-10-13 — F-100 approved ("aprovado f-100"), with no screen mockup and no new package (owner).
- 2026-10-13 — No new index is promised: the build measures the filtered list with `EXPLAIN` on the test container with about 50,000 questions and adds an index on `(Type)`, `(Difficulty)` or `(ContentLanguage)` only if the measurement asks for it; not measured yet (Claude).

- 2026-10-13 — Design passes (`system-design`, then `architect`, both opus), verified against the files (`project.md:64`, `AppMultiPickField.razor:104-135`, `AppDataTable.razor:78,233`, `OrganizerEndpoints.cs:25`, `SubjectEndpoints.cs:35`): accepted with the changes below (Claude).
- 2026-10-13 — Catalog contracts: `ISubjectDirectory.ListTaxonomyAsync(IReadOnlyCollection<Guid>? subjectIds)` returns the existing `TaxonomySubjectResponse` (null = every subject; a missing entry = unknown subject, empty `Topics` = known with none); `IExamEditionDirectory.ListOrganizersAsync` and `ListOrganizerEditionsAsync` over the same joins as `FindAsync`; `QuestionBank.Contracts` holds only its own records (`QuestionTaxonomySubjectResponse`, `OrganizerLookupResponse`) (architect).
- 2026-10-13 — Endpoint names `GetQuestionTaxonomy` and `ListQuestionOrganizers`: `GetTaxonomy` and `ListOrganizers` are taken by Catalog and operation ids are unique (architect).
- 2026-10-13 — A list filter always applies `Contains` when it has values and never skips the predicate because "all values were picked": the rule of `project.md:64` (a future annulled status must not slip in); status and type keep `Enum.TryParse` plus `IsDefined` at the Api so `status=1` still means what it meant (architect).
- 2026-10-13 — List parameters bind as `string[]?` and are parsed leniently; one place (`QuestionListQuery.Sanitized`) dedupes and caps at 100 per list; an id that is well formed but unknown is dropped, and when every organizer is unknown the edition alone applies (system-design, architect).
- 2026-10-13 — The interactive budget of AC13 is p95 under 300 ms per list call (count and page) on 50,000 rows, measured once at the build with `EXPLAIN (ANALYZE, BUFFERS)` in a scratch run and quoted here; no timing test is committed (a wall-clock test is flaky under load), the committed tests assert behavior and the architecture rules (Claude).
- 2026-10-13 — Any index the measurement asks for is recorded as a decision before it is added; the candidate for the newest-first order is `(created_at DESC, id DESC)`, not the three single-column indexes of the earlier note (system-design, architect).
- 2026-10-13 — Topics of a subject picked as a whole stay enabled in the picker: the pick is a union, so it is harmless and needs no new text (Claude).
- 2026-10-13 — The reader of repeated query keys is shared with `CatalogQueryString` (second use) in one Web helper (architect).
- 2026-10-13 — v2 change note: no second chip row, one page "Clear all" that also clears the search, 100 picked ids in total on the page (owner, asked).

- 2026-10-13 — Measured (AC13, scratch run on the PostgreSQL test container, 50,000 questions, about 200 topics, 300 editions, half authored, 20 % true/false, 40 % no difficulty, `ANALYZE` done, `EXPLAIN (ANALYZE, BUFFERS)` of the count and of the page, newest first): no filter 11.8 / 13.9 ms; 2 topics 0.8 / 0.6; 30 topics 5.8 / 6.0; 150 editions 18.5 / 19.5; 300 editions 16.7 / 18.7; one edition 0.3 / 0.3; true/false + published 8.8 / 8.7; none or 5 9.8 / 10.6; 2 topics + search 0.7 / 0.6. Every case is far under the 300 ms budget, uses `ix_questions_topic` or `ix_questions_exam_edition` where a topic or an edition is picked, and the unfiltered cases are the sequential scan the list already had; no index is added (Claude). The scratch test was deleted; the numbers are from this run only.
- 2026-10-13 — Tests unlocked once (one `unlock`, the owner said yes to two cards while it was open), for errors of the tests themselves and none for a criterion: the order assertion of two tests compared with `OrdinalIgnoreCase` while the Api orders by the normalized name like the catalog list; and expectations of `QuestionsFilterPageTests` (row counts the seed does not produce, a duplicated organizer, a URL read right after a click, found by `UiTestTimingTests`). Locked again at `65fc466f` (owner, asked).

- 2026-10-13 — Review finding 1 (minor): the page reads the address only when created, so a menu click to the plain URL leaves the filters in place — accepted as a new item F-119 (the catalog list has it since F-36); not fixed here (Claude; shown to the owner in the build report).
- 2026-10-13 — Review finding 2 (minor): a failed edition lookup left the edition filter applied but invisible — fixed: the field shows "Edition not available" with the filter, so it can be cleared (Claude).
- 2026-10-13 — Review finding 3 (minor): the numeric status of F-95 had no test — fixed: `QuestionStatusNumberEndpointTests` (Claude).
- 2026-10-13 — Review finding 4 (minor): the item's text table did not match the built keys — fixed (Claude).
- 2026-10-13 — Review finding 5 (minor): two page tests kept `async` with a dummy await — fixed (Claude).
- 2026-10-13 — Review coverage (opus reviewer, read the diff of 42 files, the item file, profile, rules and the lock): checked and fine: security (both routes under `questions.manage`, lenient string parsing, 100-value cap, no secrets), data (no migration; new directory methods under the soft-delete filters and the exam join), contracts (routes, unique operation ids, OpenAPI and routes docs regenerated, no new error code), localization (22 keys in 3 locales, reused keys exist), locks (no locked path changed after `65fc466f`; the one unlock matches its Decisions line), tests (exact id sets, every criterion mapped), rules (module boundaries through `Contracts`, one type per file, English names, `Contains` never skipped); not checked: none.

## Out of scope
- Sorting by column or by paper number — F-118.
- Filter by content language and by origin (authored or from an edition) — F-118.
- Searching options and explanation — would need new normalized columns and a migration.
- Filters by annulment (F-101) and by review state (F-105): those states do not exist yet.
- Saving a named filter set, exporting the filtered list, bulk actions on the result.
- Anything a student sees (E-5, E-6).

## Open questions
- (none)

## Change notes

### v2 — 2026-10-13
- What: the separate row of `AppFilterChip` is dropped (each `AppMultiPickField` already draws its removable chips and a "Clear"); one page-level "Clear all" also clears the search; the pickers of subject/topic/organizer share a limit of 100 picked ids.
- Why: the design pass found every pick would show twice and that 100 topics + 100 subjects + 100 organizers (about 13 KB of address) pass Kestrel's 8 KB request line (414 on reload and on the Web to Api call).
- Affected: AC11 (text), new AC15, Screens and API (chip row, limit); BR6 unchanged (the Api keeps 100 per list).
- Re-approved: 2026-10-13, by the owner's answer to the two cards of the design pass ("Só os chips dos campos", "100 ids no total na tela").

## Validation script
Needed to validate: the local app host with four questions of two subjects, two types, both statuses, one of them from an edition — owner (step 2 creates them; the sign-in is the development administrator of `appsettings.Development.json`, `admin@simulab.local`). Not in place now, and this screen was never seen by Claude: the containers of the app host (`simulab-postgres`, `simulab-redis`, `simulab-mailpit`) exited when Claude tried to start it, so everything below is also the first look at the page in a browser.

1. Start the app host from this worktree, with its own database (no migration changes, but the data stays apart), and open the Web address the dashboard shows. Sign in as the administrator.

   PowerShell 7:
   ```powershell
   $env:Database__Name = "simulab_f100"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   Git Bash:
   ```bash
   Database__Name=simulab_f100 dotnet run --project src/Hosts/Simulab.AppHost
   ```
   Expected: the dashboard lists `api` and `web` as running; **Questions** shows in the **Content** menu.
2. Make four questions (Questions, Add; the seeded subjects "Ortografia e Pontuação" and "Direito Administrativo" are there), each with a short statement:
   - Q1: single choice, subject "Ortografia e Pontuação", difficulty 3, two options with one correct, then **Publish**.
   - Q2: true/false, same subject, no difficulty, leave it a draft.
   - Q3: single choice, subject "Direito Administrativo", difficulty 5, two options with one correct, then **Publish**.
   - Q4: single choice, subject "Direito Administrativo", no difficulty, a draft, and in **Origin** pick a seeded edition and number 1.
3. Filters (AC1 to AC7): on the list, **Status** = Published shows Q3 and Q1. Add **Status** = Draft: all four. Remove Draft. **Type** = True/false with **Status** Published: nothing, and the text says no question matches the chosen filters. **Clear all**. **Difficulty** = Not set and 5: Q3, Q4, Q2. **Subject and topic**: pick the whole subject "Direito Administrativo": Q4 and Q3, and its chip says "(all topics)". **Organizer** = the organizer of the edition of Q4 together with that subject: only Q4. Type a word of Q4's statement in the search box with the filters on: Q4 remains.
4. Chips and clear (AC11): each picked value is a chip under its field; remove one with its **x** and the list reloads from the first page; **Clear** on a field empties only that field; **Clear all** empties every filter and the search box.
5. Address (AC9, AC10): with two filters set, open Q3 and choose **Cancel**: you are back on the same filtered list. Copy the address of the list into another tab: the same filters and the same rows. Edit the address to add `&topic=00000000-0000-0000-0000-000000000001`: the page still loads and the number of rows does not change.
6. Language and permission (AC14): switch to pt-BR in the app bar: the fields read Situação, Tipo, Dificuldade, Disciplina e tópico, Banca, Edição and the button Limpar tudo. Sign up a second account in a private window and sign in: no Questions item, and `/admin/questions` shows the not-found page.
7. Keyboard only, no mouse (AC14): Tab to **Subject and topic**; Arrow down opens the list; Arrow keys move, Space picks, Escape closes; Tab reaches the chip's remove button and Enter removes it; Tab to **Clear all** and press Enter. The picked count under each field is read out ("Picked: n").
8. Stop the app host (Ctrl+C) before building again. Optional: `DROP DATABASE simulab_f100;` when you are done.

## Events

- 2026-10-13 question — asked 12, from context 2
- 2026-10-13 change — v2: no second chip row, page Clear all, 100 picked ids in total
- 2026-10-13 question — asked 2, from context 0
- 2026-10-13 blocked — test wrong: the order assertion of two tests used OrdinalIgnoreCase; the Api orders by the normalized name like the catalog list
- 2026-10-13 rework — review: a failed edition lookup hid the applied edition filter
- 2026-10-13 rework — review: numeric status of F-95 had no test
- 2026-10-13 rework — review: item text table out of date
- 2026-10-13 rework — review: two page tests kept a dummy await
- 2026-10-13 test-fail — 2 new warning(s): MSB3061 in C:/Program Files/dotnet/sdk/10.0.401/Microsoft.Common.CurrentVersion.targets; MSB3026 in C:, 1 red
