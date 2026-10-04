---
feature: F-75
epic: Subject taxonomy
status: refining
board: 119
version: 1
---
# Map notice subjects to the canonical taxonomy

Technical terms: [glossary](../glossary.md)

## Summary
An admin maps each `NoticeSubject` of an edition to N canonical subjects and topics (ADR-0001 #44), so "Raciocínio Lógico-Matemático" points to Mathematics and Logical Reasoning. The simulators show the notice's labels; analytics and recommendations use the canonical taxonomy.

## Start
- Depends on: F-74 (approved, not built yet — `NoticeSubject` exists only after its merge) and F-79 (validating on `feature/F-79` — `Subject` and `Topic` exist only after its merge). The build branches from `main` after both merges, never from their branches.
- Waits on (to start): nothing beyond those two merges.
- Needed to validate: an edition with notice subjects and a few subjects and topics in the local database — the F-37 seed gives the edition; F-51 (if merged first) or Claude through the F-79 and F-74 screens gives the rest (Claude).
- Suggested path: `/agile:screen F-75` → approval → `/agile:build` after F-74 and F-79 are done.
- Parallel with: F-77 (refinement and build: different tables and screens).

## Goal
Link the notice's own vocabulary to the canonical one, so the Exam Simulator can keep showing the notice's labels while analytics (E-10) and recommendations (E-11) compare results across exams.

## What exists (verified 2026-10-04)
- `Subject`, `Topic`, `Area` (F-79) on `feature/F-79`, status `validating`: tables `catalog.subjects`, `catalog.topics`; `DeleteSubjectHandler` refuses with `subject.has_topics`; `DeleteTopicHandler` is a plain soft delete and its comment asks each later referrer to add its guard (F-79 BR9); a topic can move to another subject (F-79 BR10).
- `NoticeSubject` (F-74), status `approved`, no code yet: table `catalog.notice_subjects`, section "Notice subjects" on `/admin/exams/{examId}/editions/{id}` with an add/edit dialog `NoticeSubjectDialog.razor`, endpoints under `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects`.
- No mapping entity anywhere in `src/` (main, F-74 and F-79 branches). Simulae's `ExamNoticeTopic` links a notice straight to topics; there is nothing to import.

## Users and use cases
- UC1 An admin with `catalog.manage` sees, on each notice subject row of an edition, the canonical subjects and topics it covers, and which rows are not mapped yet.
- UC2 In the add/edit notice subject dialog, the admin picks the canonical items the row covers: whole subjects and single topics, any number of them.
- UC3 The admin removes a canonical item from a row's mapping, or clears it.
- UC4 A curator tries to delete a subject or topic that some notice subject maps to and is told it is in use.

## Business rules
- BR1 A mapping entry is either a whole canonical subject or one canonical topic, never both and never neither (`notice_subject.mapping_invalid`). A notice subject has from 0 to 50 entries (`notice_subject.mapping_too_many`).
- BR2 Mapping is optional. A notice subject with no entry is "not mapped"; nothing is refused because of it, publishing an edition included (F-74 BR9 keeps a published edition editable).
- BR3 An entry must point at a subject or topic that exists and is not deleted, else 400 `notice_subject.mapping_target_not_found`.
- BR4 Within one notice subject, a topic may not be mapped together with its own subject as a whole: there was no such overlap before the save and there would be one after → 400 `notice_subject.mapping_overlap`. The check runs on save only; an overlap that a later topic move creates (F-79 BR10) is not refused there and is shown as is.
- BR5 The same entry sent twice in one save is stored once (no error).
- BR6 The same subject or topic may be mapped by several notice subjects of the same edition (a law listed under two headings, ADR-0001 #44).
- BR7 A save replaces the whole mapping of the notice subject: the request carries the full list of entries.
- BR8 A topic entry follows its topic: when the topic moves to another subject (F-79 BR10), the entry now counts under the new subject. The entry stores the topic only, not its subject.
- BR9 Deleting a topic that at least one live notice subject maps to is refused with 409 `topic.in_use`. Deleting a subject that at least one live notice subject maps to as a whole is refused with 409 `subject.in_use` (a subject with topics is still refused first by `subject.has_topics`). "Live" excludes deleted notice subjects and notice subjects of deleted editions.
- BR10 The subject list and the topic section of F-79 know whether each item is in use, so the delete action is disabled with its reason before the click, like `subject.has_topics`.
- BR11 Deleting a notice subject or an edition (F-74 BR10) leaves its mapping rows in place; they no longer count for BR9 because their notice subject is not live.
- BR12 Every endpoint requires `catalog.manage`; the mapping field and its display appear only where the F-74 section already appears (F-74 BR11).
- BR13 The canonical names are shown as typed (not translated, F-79 BR3); a topic is shown with its subject ("Mathematics › Fractions"), a whole subject with a marker meaning "whole subject".

## Screens and API
Screens follow the rules `ui` and `ui-project`; the detailed screen section and mockup come from `/agile:screen F-75`.
- `/admin/exams/{examId}/editions/{id}`, section "Notice subjects" (F-74): each row shows its mapped items as chips under the label, or a "Not mapped" marker; the footer adds the count of rows not mapped (UC1).
- `NoticeSubjectDialog.razor` (F-74): new field "Covers" after the number of questions — a multi-pick with search over subjects and topics, grouped by subject, where a subject can be picked whole; picked items show as removable chips (UC2, UC3). One dialog edits the whole row.
- `/admin/subjects` and `/admin/subjects/{id}` (F-79): delete disabled with the in-use reason (BR10).
- GET `/api/v1/catalog/taxonomy` — every live subject with its live topics, alphabetical, for the picker (one call per dialog opening).
- POST / PUT `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects[/{id}]` (F-74) — the body gains `mappings: [{ "subjectId": guid | null, "topicId": guid | null }]`; omitted or empty means not mapped.
- GET `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects` (F-74) — each row gains `mappings: [{ "subjectId", "subjectName", "topicId", "topicName" }]`.
- GET `/api/v1/catalog/subjects` and GET `/api/v1/catalog/subjects/{subjectId}/topics` (F-79) — each item gains `inUse` (BR10).
- Error codes: `notice_subject.mapping_invalid`, `notice_subject.mapping_too_many`, `notice_subject.mapping_target_not_found`, `notice_subject.mapping_overlap`, `subject.in_use`, `topic.in_use`.
- Table `catalog.notice_subject_mappings`: `id`, `tenant_id`, `notice_subject_id` (FK, cascade), `subject_id` (FK, restrict, nullable), `topic_id` (FK, restrict, nullable), audit columns, column comments; check constraint "exactly one of `subject_id`, `topic_id`"; unique index `ux_notice_subject_mappings_tenant_notice_subject_target` over (`tenant_id`, `notice_subject_id`, `subject_id`, `topic_id`) `NULLS NOT DISTINCT`; indexes on `subject_id` and `topic_id` for BR9.

## Acceptance criteria
- AC1 Given a notice subject "Raciocínio Lógico-Matemático", when the admin maps it to Mathematics (whole) and to the topic Logical Reasoning › Propositions and saves, then both entries are stored and the row shows both chips (UC2, BR1, BR13).
- AC2 Given an entry with both `subjectId` and `topicId`, or with neither, when saved, then 400 `notice_subject.mapping_invalid` and nothing changes (BR1).
- AC3 Given 51 entries, when saved, then 400 `notice_subject.mapping_too_many` (BR1).
- AC4 Given an unknown or deleted subject or topic id in the body, when saved, then 400 `notice_subject.mapping_target_not_found` (BR3).
- AC5 Given Mathematics (whole) and the topic Mathematics › Fractions in one save, then 400 `notice_subject.mapping_overlap`; given Mathematics (whole) and Logical Reasoning › Propositions, then it is stored (BR4).
- AC6 Given a row mapped to Mathematics (whole) and topic T of Logic, when a curator moves T to Mathematics, then the move succeeds and the row shows both entries, T now under Mathematics (BR4, BR8).
- AC7 Given the same topic twice in one save, then it is stored once (BR5).
- AC8 Given two notice subjects of one edition, when both map the same topic, then both are stored (BR6).
- AC9 Given a row mapped to A and B, when the admin saves it with only C, then the mapping is exactly C; saved with an empty list, the row shows "Not mapped" (UC3, BR7, BR2).
- AC10 Given an edition with three rows of which one is not mapped, when the section loads, then that row shows "Not mapped" and the footer says one row is not mapped; the edition can still be published (UC1, BR2).
- AC11 Given a topic mapped by a live notice subject, when a curator deletes it, then 409 `topic.in_use` and its delete action was disabled with the reason; given a subject mapped whole, then 409 `subject.in_use` with the same screen behaviour (UC4, BR9, BR10).
- AC12 Given a topic mapped only by a deleted notice subject or by a notice subject of a deleted edition, when the curator deletes the topic, then it is deleted (BR9, BR11).
- AC13 Given the taxonomy endpoint, when called, then it returns the live subjects alphabetically, each with its live topics alphabetically, and no deleted item (Screens and API).
- AC14 Given a user without `catalog.manage`, when the taxonomy endpoint or a notice subject endpoint with mappings is called, then 403 (BR12).
- AC15 Given two rows inserted directly with the same notice subject, `tenant_id` null and the same topic, then PostgreSQL rejects the second; given a row with both `subject_id` and `topic_id`, then the check constraint rejects it (schema).
- AC16 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-10-04 — A mapping entry is a whole subject or one topic — notices often name only a subject, others name single laws or topics (owner, question 1).
- 2026-10-04 — A topic together with its own whole subject on one row is refused — the whole subject already covers it; keeps analytics weights clean (owner, question 2).
- 2026-10-04 — The same canonical item may be mapped by several rows of one edition — real notices repeat a law under two headings (owner, question 3).
- 2026-10-04 — Deleting a mapped subject or topic is refused with 409, delete disabled with its reason — the F-79 `subject.has_topics` pattern; a silent unmap would leave holes nobody sees (owner, question 4).
- 2026-10-04 — Mapping is optional; unmapped rows are marked and counted, publishing is not blocked — F-74 keeps published editions editable (owner, question 5).
- 2026-10-04 — The mapping is edited in the F-74 add/edit dialog, new field "Covers" — one place to edit a notice subject (rule `ui-project`) (owner, question 6).
- 2026-10-04 — F-86, when built, copies the mappings too; a dated line was added to its summary — the mapping is the slow part to retype (owner, question 7).
- 2026-10-04 — No new package, production or tests — EF Core, the MudBlazor kit, xunit, bunit and Testcontainers.PostgreSql already cover it (owner, question 8).
- 2026-10-04 — One table with nullable `subject_id` and `topic_id` and a check constraint, the topic entry storing only the topic — a topic move (F-79 BR10) needs no update of the mappings (Claude, technical).
- 2026-10-04 — The overlap of BR4 is checked on save only, not on a topic move — refusing a move because of an edition's mapping would couple the taxonomy screens to every notice (Claude, technical).
- 2026-10-04 — Mapping rows are link rows without soft delete: a save replaces them, and a deleted notice subject keeps them but they stop counting — they carry no history of their own, and keeping them lets an undeleted row come back mapped (Claude, technical).
- 2026-10-04 — At most 50 entries per notice subject — the widest real heading lists about 20 laws; a ceiling stops a runaway request (Claude, technical).
- 2026-10-04 — The picker loads the whole taxonomy in one call (`GET /api/v1/catalog/taxonomy`) — the F-51 guard taxonomy is 23 subjects and 70 topics; not measured at larger sizes, revisit with E-9 imports (Claude, technical).
- 2026-10-04 — Mapping lives in the `Catalog` module, `catalog` schema — epic decision; plain foreign keys inside one schema (Claude, epic decision).

## Out of scope
- AI suggestion of the closest topic or a new draft topic — epic E-9.
- Aliases and lookup by alias — F-77.
- The student's view of the syllabus with its topics — F-76.
- Copying notice subjects and their mapping from another edition — F-86.
- Mapping the municipal guard editions — F-78.
- Weight and minimum per notice subject — epic E-6.
- Weighting the entries of one mapping (how much of a row is Mathematics vs. Logic) — not asked by any epic yet.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
<!-- Filled by /agile:ship. -->
