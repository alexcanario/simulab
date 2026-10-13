---
feature: F-100
epic: Question bank
status: building
board: 146
version: 1
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

## Screens and API
No mockup: the epic marks this item "Screen design: no" and it reuses kit parts on an existing page; the owner may ask for `/agile:screen` before approving.

### Screen — `/admin/questions` (changed)
- Pattern: `Exams.razor` toolbar (fields in the `app-table-filters` row) plus `AccountEvents.razor` (address-bound filters).
- Toolbar, in this order, each field with its own id (`questions-filter-*`): Status (multi), Type (multi), Difficulty (multi, with "Not set"), Subject and topic (grouped multi), Organizer (multi), Edition (search lookup, one value). The text search stays the table's own search box.
- The pickers are `AppMultiPickField` (Status, Type, Difficulty, Subject/topic, Organizer); the edition is the lookup field the question form already uses.
- Below the toolbar, a row of `AppFilterChip` per picked value (a subject chip says "Subject: X (all topics)") and a "Clear all" text button, shown only while a filter is active; removing a chip reloads from the first page.
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
| `Questions.Filter.Clear` | Clear all | Limpar tudo | Limpar tudo |
| `Questions.Filter.Active` | Active filters | Filtros ativos | Filtros ativos |
| `Questions.Filter.SubjectAll` | {0} (all topics) | {0} (todos os tópicos) | {0} (todos os tópicos) |
| `Questions.Filter.Difficulty.NotSet` | Not set | Não definida | Não definida |
| `Questions.Filter.LoadFailed` | The list of options did not load. | A lista de opções não carregou. | A lista de opções não carregou. |
| `Questions.Empty.Filters` (changed) | No question matches the chosen filters. | Nenhuma questão corresponde aos filtros escolhidos. | Nenhuma questão corresponde aos filtros escolhidos. |

Reused as they are: `Questions.Filter.Status`, `Questions.Status.*`, `Common.Filter.All`, `Common.Table.*`, `Lookup.*`, the type names the Source and Type columns already show. The three-language table is completed at the build for every key it adds; `ResourceParityTests` guards it.

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
- AC11 Given active filters, then one chip per value shows; removing a chip reloads from the first page without that value; "Clear all" removes every filter and the search. (UC5)
- AC12 Given the taxonomy cannot be loaded, then the subject picker shows its load error with retry and the question list still loads. (Screens)
- AC13 Given the architecture rules, then `QuestionBank` still references other modules only through their `Contracts`, and the list queries on 50,000 questions stay within the interactive budget measured at the build (`EXPLAIN` on the test container; an index is added only if the measurement asks for it). (BR10)
- AC14 All new texts appear in pt-BR, pt-PT and en. The keyboard and accessible names of the pickers and chips are checked on screen (WCAG 2.2 AA).

## Criterion → test

| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC2 | pending |
| AC3 | pending |
| AC4 | pending |
| AC5 | pending |
| AC6 | pending |
| AC7 | pending |
| AC8 | pending |
| AC9 | pending |
| AC10 | pending |
| AC11 | pending |
| AC12 | pending |
| AC13 | pending |
| AC14 | pending |

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

## Events

- 2026-10-13 question — asked 12, from context 2
