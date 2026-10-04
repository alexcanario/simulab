---
feature: F-79
epic: Subject taxonomy
status: approved
board: 117
version: 1
---
# Subjects and topics back office

Technical terms: [glossary](../glossary.md)

## Summary
An admin registers the canonical taxonomy in two levels (ADR-0001 #43): `Subject`, with an optional `Area` from a fixed seeded list, and `Topic` under its subject. A subject in use cannot be deleted. Lives in the `Catalog` module (`catalog` schema) and is edited under `catalog.manage` (epic decisions, 2026-10-04).

## Start
- Depends on: F-33 to F-37 (the `Catalog` module, done).
- Waits on (to start): nothing.
- Needed to validate: nothing beyond the local app host — Claude.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: none (every other feature of the epic depends on it).

## What exists
Verified in `src/` on 2026-10-04:
- No taxonomy entity exists: `Subject`, `Topic` and `Area` are new.
- The `Catalog` module gives the pattern to follow: global entities (`TenantId` null) with soft delete (`TenantEntity`), a normalized name under a unique index `NULLS NOT DISTINCT` with deleted rows included (`IssuingAuthorityConfiguration`), domain rules returning `Result` (`IssuingAuthority`), delete guards in the handler (`DeleteIssuingAuthorityHandler`), endpoints under `/api/v1/catalog/...` (`ExamEndpoints`), the parent page with a child section (`ExamForm` + `ExamEditionsSection`, F-35), and the "Content" navigation section gated by `catalog.manage` (`NavigationItems.cs`).
- Nothing references a topic yet: `NoticeSubject` arrives with F-74/F-75 and `Question` with epic E-4. So the only "in use" guard this item can enforce is a subject that still has topics (BR8); each later item that points at `Topic` adds its own guard (BR9).
- Simulae (`ContentCatalog`) has three levels with only a name each and no tests for the taxonomy entities; this item is new code in the Simulab pattern, not an import.

## Goal
Give the product one canonical vocabulary of subjects and topics, curated by the catalog team, that the notice subjects (F-74, F-75), the question bank and the analytics can point at.

## Users and use cases
- UC1 A curator (holder of `catalog.manage`) lists the subjects, searches by name and filters by area.
- UC2 A curator creates a subject with a name and, optionally, an area.
- UC3 A curator edits a subject's name and area.
- UC4 A curator deletes a subject that has no topics.
- UC5 A curator opens a subject's page and sees its topics.
- UC6 A curator adds a topic to a subject.
- UC7 A curator edits a topic's name and may move it to another subject.
- UC8 A curator deletes a topic.
- UC9 A user without `catalog.manage` sees none of these pages and calls none of these endpoints.

## Business rules
- BR1 `Area` is a fixed list seeded by a migration in `catalog.areas` (a stable `code` and a `display_order`); there is no screen to manage it. Its name is shown from the resource key `Area.<Code>` in the reader's language. The list is `## Approved list`, in that order. A new area is a migration plus three resource entries.
- BR2 `Subject : TenantEntity` maps to `catalog.subjects`. Global data: `TenantId` null; delete is a soft delete.
- BR3 A subject's `Name` is required, trimmed, from 2 to 150 characters (`subject.name_required`, `subject.name_too_long`). It is one name as typed, not translated (content, like a question).
- BR4 A subject's `AreaId` is optional; when given it must match an area, else 400 `subject.area_invalid`.
- BR5 A subject's name is unique by its normalized form (`CatalogText.Normalize`: case and accents ignored): unique index over `TenantId` and `NormalizedName`, `NULLS NOT DISTINCT`, deleted rows included, so a deleted subject keeps its name taken (the F-33/F-34 precedent). A duplicate is 409 `subject.name_taken`.
- BR6 `Topic : TenantEntity` maps to `catalog.topics`, with a required `SubjectId` foreign key (`Restrict`). A topic's `Name` is required, trimmed, from 2 to 200 characters (`topic.name_required`, `topic.name_too_long`), one name as typed.
- BR7 A topic's name is unique within its subject by its normalized form: unique index over `TenantId`, `SubjectId` and `NormalizedName`, `NULLS NOT DISTINCT`, deleted rows included. A duplicate is 409 `topic.name_taken`. The same name under two subjects is allowed.
- BR8 Deleting a subject that has at least one topic, deleted topics excluded, is refused with 409 `subject.has_topics` (no count in the text, like `exam.has_editions`). The subject list shows the topic count, so the screen disables the delete with the reason before the click.
- BR9 A topic has no referrer in this item, so deleting it is a plain soft delete. Every later item that adds a reference to `Topic` (F-75 mapping, E-4 questions) adds its own delete guard and its error code.
- BR10 Editing a topic may change its subject (move). The target subject must exist and not be deleted, else 404 `subject.not_found`; the name must be free in the target subject (BR7), else 409 `topic.name_taken`.
- BR11 Lists are ordered alphabetically by normalized name: subjects in the list, topics inside a subject. There is no manual order; the notice's order lives in `NoticeSubject` (F-74).
- BR12 The subject list is paged and searchable by name, with an area filter (including "no area"), like the other catalog lists. A subject's topics come back in one call, not paged.
- BR13 Unknown or deleted ids answer 404 `subject.not_found` / `topic.not_found`.
- BR14 Domain rules return `Result`, never exceptions (`Subject.Create/Update`, `Topic.Create/Update`).
- BR15 One permission covers it all: `catalog.manage` gates the endpoints, the menu entry, the pages and their actions (F-33 BR4).
- BR16 Every UI text, area name and new error code exists in pt-BR, pt-PT and en in `SharedResources`.

## Screens and API
Screens follow the rules `ui` and `ui-project`; the detailed screen section and mockup come from `/agile:screen F-79`.
- `/admin/subjects` — "Subjects" in the Content menu section. Page header with Add; `AppDataTable` with columns name, area, topic count; search by name; area filter; row actions edit (dialog) and delete (disabled with reason when the topic count is above zero). Create and edit in a dialog (name, area select with an empty option). Empty and empty-search states.
- `/admin/subjects/{id}` — the subject page: its name and area, and a "Topics" section listing its topics alphabetically, with Add, edit (dialog: name, subject select for a move) and delete (confirm). Empty state: "No topics yet". Breadcrumbs: Content / Subjects / <name>.
- GET /api/v1/catalog/areas — the areas in display order (code, display order); the name is localized on the screen.
- GET /api/v1/catalog/subjects?search=&areaId=&withoutArea=&page=&pageSize= — paged list with area and topic count.
- GET /api/v1/catalog/subjects/{id} — one subject.
- POST /api/v1/catalog/subjects — create.
- PUT /api/v1/catalog/subjects/{id} — update.
- DELETE /api/v1/catalog/subjects/{id} — soft delete (BR8).
- GET /api/v1/catalog/subjects/{subjectId}/topics — the subject's topics.
- POST /api/v1/catalog/subjects/{subjectId}/topics — create.
- PUT /api/v1/catalog/topics/{id} — update, body carries `subjectId` (BR10).
- DELETE /api/v1/catalog/topics/{id} — soft delete.
- Error codes: `subject.not_found`, `subject.name_required`, `subject.name_too_long`, `subject.name_taken`, `subject.area_invalid`, `subject.has_topics`, `topic.not_found`, `topic.name_required`, `topic.name_too_long`, `topic.name_taken`.

## Acceptance criteria
- AC1 Given a database with the previous migration, when the new migration runs, then `catalog.areas` holds the nine areas of `## Approved list` in that order, and `catalog.subjects` and `catalog.topics` exist with the unique indexes of BR5 and BR7 `NULLS NOT DISTINCT` and a restricted foreign key from topics to subjects. (BR1, BR2, BR5, BR6, BR7)
- AC2 Given the areas endpoint, when the screen shows them in pt-BR, pt-PT and en, then each area has its name in that language. (BR1, BR16)
- AC3 Given `/admin/subjects`, when the curator adds "Direito Constitucional" with area Law, then it appears in the list with area Law and 0 topics. (UC1, UC2)
- AC4 Given a subject "Português", when another subject "portugues" is saved, then 409 `subject.name_taken`; after "Português" is deleted, saving "Português" again is still 409. (BR5)
- AC5 Given two rows inserted directly with `TenantId` null and the same normalized name, when the second is saved, then PostgreSQL rejects it. (BR5)
- AC6 Given a name of 1 character or of 151 characters, then 400 `subject.name_required` or `subject.name_too_long`; given an unknown area id, then 400 `subject.area_invalid`; nothing is written. (BR3, BR4)
- AC7 Given subjects with areas Law and none, when the curator filters by Law, then only Law subjects show; filtering by "no area" shows only those without one; searching "constit" finds "Direito Constitucional". (UC1, BR12)
- AC8 Given a subject with one topic, then its delete action is disabled with the reason, and a `DELETE` sent to the Api answers 409 `subject.has_topics`; once the topic is deleted, the subject can be deleted and leaves the list, its row still in the table with `IsDeleted` true. (UC4, BR8)
- AC9 Given a subject page, when the curator adds "Controle de constitucionalidade" and "Aplicabilidade das normas", then both show in alphabetical order. (UC5, UC6, BR11)
- AC10 Given a topic "Crase" in "Português", when another "crase" is added to the same subject, then 409 `topic.name_taken`; added to another subject, it is created. (BR7)
- AC11 Given a topic in subject A, when the curator edits it and picks subject B, then it leaves A's section and shows in B's; when B already has a topic with that normalized name, then 409 `topic.name_taken` and it stays in A. (UC7, BR10)
- AC12 Given a topic, when the curator confirms its deletion, then it leaves the section and the subject's topic count drops by one. (UC8, BR9)
- AC13 Given unknown or deleted ids, then 404 `subject.not_found` or `topic.not_found`. (BR13)
- AC14 Given `Subject.Create` with an empty name, when it runs, then it returns a failed `Result` with `subject.name_required` and throws nothing; the same for `Topic.Create`. (BR14)
- AC15 Given a signed-in Student, then the Subjects menu entry is absent, both pages answer Not Found, and every new endpoint answers 403 `identity.forbidden`. (UC9, BR15)
- AC16 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR16)

## Approved list
Areas, in display order (code — en / pt-BR / pt-PT):
1. `Languages` — Languages / Linguagens / Linguagens
2. `Mathematics` — Mathematics / Matemática / Matemática
3. `LogicalReasoning` — Logical Reasoning / Raciocínio Lógico / Raciocínio Lógico
4. `NaturalSciences` — Natural Sciences / Ciências da Natureza / Ciências da Natureza
5. `HumanSciences` — Human Sciences / Ciências Humanas / Ciências Humanas
6. `Law` — Law / Direito / Direito
7. `InformationTechnology` — Information Technology / Tecnologia da Informação / Tecnologias de Informação
8. `Administration` — Administration and Management / Administração e Gestão / Administração e Gestão
9. `SpecificKnowledge` — Specific Knowledge / Conhecimentos Específicos / Conhecimentos Específicos

## Decisions
- 2026-10-04 — A deleted subject or topic keeps its name taken — same rule as the rest of the Catalog (F-33 BR9/BR11), no new rule (owner, refinement).
- 2026-10-04 — A topic can move to another subject in its edit dialog — fixes a wrong registration without delete and re-create, which would lose future mappings (owner, refinement).
- 2026-10-04 — Subject and topic names are one name as typed, not translated; only the area is localized — the taxonomy is content, like a question (owner, refinement).
- 2026-10-04 — Topics are alphabetical, no manual order — the notice's order lives in `NoticeSubject` (owner, refinement).
- 2026-10-04 — Screens: subject list plus subject page with a topics section — the Exams → Editions pattern of F-35 (owner, refinement).
- 2026-10-04 — The subject list shows area and topic count and filters by area — the count anticipates the delete block; the filter helps once the list is long (owner, refinement).
- 2026-10-04 — The nine areas of `## Approved list` are approved as listed — ENEM's four areas plus the public exam groupings from Simulae's top level (owner, refinement).
- 2026-10-04 — No new package: the tests use xunit, bunit and Testcontainers.PostgreSql already in `Directory.Packages.props` (owner, refinement).
- 2026-10-04 — `Area` is a seeded table with a code and display order, names in resource files by code — a foreign key keeps subjects consistent and the names follow the UI language (Claude, technical).
- 2026-10-04 — The "in use" guard for topics is deferred to the items that create the references (F-75, E-4) — nothing points at a topic in this item (Claude, premise verified in `src/`).
- 2026-10-04 — Routes under `/api/v1/catalog/`, topic update and delete at `/api/v1/catalog/topics/{id}` so a move does not depend on the old parent in the route (Claude, technical).
- 2026-10-04 — Approved without a `/agile:screen` mockup: the two screens reuse the list-plus-dialog pattern of `/admin/issuing-authorities` and the parent page with a child section of `ExamForm` + `ExamEditionsSection` (owner, "aprovo F-79").
- 2026-10-04 — Limits: subject name 150, topic name 200 characters, in `CatalogLimits` (Claude, technical — the organizer and exam widths).

## Out of scope
- Aliases (`SubjectAlias`, `TopicAlias`) — F-77.
- Importing the municipal guard taxonomy — F-51.
- Notice subjects and their mapping — F-74, F-75.
- Student view of the syllabus — F-76.
- Deeper topics (parent on `Topic`) — ADR-0001 #46.
- A screen to manage areas — fixed list (epic decision).
- Translating subject and topic names.

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
