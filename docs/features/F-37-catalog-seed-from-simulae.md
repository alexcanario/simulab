---
feature: F-37
epic: Assessment catalog
status: approved
board: 756
version: 1
---
# Catalog seed from Simulae

## Summary
Bring the municipal guard catalog into the new model as data every environment starts with: exam boards, issuing authorities, exams and editions, so the epics that follow have real rows instead of invented ones. Simulae's `GuardaMunicipalContentSeed` only carries the seven boards (and the subject taxonomy, which is not part of this item); the authorities, exams and editions are new work built from the owner's source document and a web research of the official notices (2026-09-29). The data ships as a one-time data migration of the `catalog` schema, with its tests.

## Goal
A fresh database of any environment already lists the municipal guard exams a first student would look for, with the notice each one was applied under, so the exam simulator and the question bank epics can attach to real editions.

## Start
- Depends on: F-35 (`done`) and F-36 (`done`), both merged into `main`.
- Waits on (to start): nothing.
- Needed to validate: the app host started from the item's worktree and an Admin account signed in by the owner (Claude does not enter credentials).
- Suggested path: the seed data as one reviewed list with its sources, the data migration, the integration tests, then a look at the admin and student screens.
- Can run beside it: nothing inside `Catalog`; an item outside it could, with the owner's `--worktree`.

## What exists
- `Organizer` (the exam board), `IssuingAuthority`, `Exam` and `ExamEdition` with their rules answering `Result`, unique indexes over `NormalizedName` (and the edition's exam, year, position and board), all with `NULLS NOT DISTINCT` (`ExamConfiguration.cs:76`, `ExamEditionConfiguration.cs:81`).
- Nothing seeds catalog data today: `Program.cs:116` applies migrations only when `Database:ApplyMigrationsOnStart` is on (Development by default), and a release applies them from the pipeline, which is why the seed is a migration.
- The student catalog lists only exams that have a `Published` edition (`PublishedExamQueries.cs`). An exam with no published edition is admin-only.
- Simulae source, verified in `D:\dev\_icontrol\simulae\repo`: `GuardaMunicipalContentSeed.cs` seeds 7 boards and the taxonomy (9 subjects, 23 domains, 70 topics); its five tests count rows, check accents, idempotence and coexistence with a minimal taxonomy. It has **no** exam, authority or edition. The board table comes from US #661: AOCP (Curitiba, Recife), Consulplan (Manaus), FGV (Salvador), Copeve/Ufal (Maceió), and Vunesp, FCC, Cebraspe with no city.
- Premise corrected from the summary: "organizers, exams and editions" is not in Simulae's seed; only the organizers are. The rest is built here from `guarda_municipal_concursos.md` plus research.

## Users and use cases
- UC1 An admin opens the exam boards, issuing authorities, exams and editions back office on a new environment and finds the municipal guard data already there.
- UC2 A signed-in student browses the catalog on a new environment and finds the guard exams whose notice is confirmed, each with its published edition.
- UC3 An admin edits or deletes a seeded row in the back office; the change stays.
- UC4 A developer on any environment gets the same rows, with the same ids.

## Business rules
- BR1 The seed is one data migration of the `catalog` schema. It runs once per database, from the pipeline or on start, like any migration; nothing seeds at application start.
- BR2 Every row is global: `TenantId` is null. Every row goes through the same audit columns the interceptor would set.
- BR3 Ids are fixed GUIDs written in the migration, so a test, a link and a later item can point at a seeded row.
- BR4 The migration holds frozen literal values, including the normalized columns. A test proves every literal equals what the domain computes today (`CatalogText.Normalize`, `Result` rules), so a later change of the rules cannot silently drift the seed.
- BR5 The seed never updates or re-creates a row: an admin's edit or delete (soft delete) is final. Re-running is impossible by design; a second seed is a second migration.
- BR6 Seven exam boards (kind `ExamBoard`), exactly the Simulae list: Instituto AOCP, Instituto Consulplan, Fundação Getulio Vargas (FGV), Copeve/Ufal, Vunesp, FCC, Cebraspe. Vunesp, FCC and Cebraspe have no exam yet and stay (Simulae decision R6, 2026-09-16).
- BR7 Six issuing authorities, one per city: Prefeitura de Curitiba, Prefeitura de Manaus, Prefeitura de Salvador, Prefeitura do Recife, Prefeitura de Goiânia, Prefeitura de Maceió. Acronyms are `PM-<CITY>` in uppercase without accents (the field is required and `Prefeitura` alone repeats).
- BR8 Six exams, one per authority: assessment type `PublicServiceExam`, scope `Municipal`, scope detail the city, content language `pt-BR`. The name follows the notice where a primary source names it (`Guarda Civil Municipal` for Salvador and Maceió) and is `Guarda Municipal` otherwise. Recife and Goiânia have an exam and no edition: no notice or board exists yet (2026-09-29).
- BR9 Rio de Janeiro is not seeded: its "Força Municipal" call is an internal selection of career guards, not a public exam a student can practise (Portal de Concursos do Rio, 2026-09-29).
- BR10 Four editions, one per exam that has a notice: Curitiba, Manaus, Salvador, Maceió. Board, year, position, reference, link and application date come from the sheet in `## Sources`.
- BR11 An edition is `Published` only when its notice reference and its official link are both confirmed by a primary source (the notice, the prefeitura or the board's own site); otherwise it is `Draft`. Any other field a primary source does not confirm is left empty. The notice year is required by the model, so an edition whose year cannot be confirmed is not seeded and is reported.
- BR12 Before the data is frozen, Claude opens each source of `## Sources` in the built-in browser and compares it with the sheet; a difference changes the sheet, never the other way round.
- BR13 The subject taxonomy of Simulae's seed is not imported here; it is registered as an `idea` for epic 692 with the 9/23/70 tree as its source.

## Screens and API
No new route, screen or endpoint. The seeded rows appear in the screens and endpoints that already exist (`/admin/...` catalog pages and `/api/v1/` published exams). Error codes: none.

## Acceptance criteria
- AC1 Given an empty database, when the `catalog` migrations are applied, then it holds 7 organizers, 6 issuing authorities, 6 exams and 4 editions, all with a null `TenantId`.
- AC2 Given the seeded database, when the boards are listed, then the seven names of BR6 exist, each acronym is stored uppercase, and Instituto AOCP appears once.
- AC3 Given the seeded database, then every seeded organizer, authority, exam and edition rebuilt through its domain `Create` returns success, and its stored normalized columns equal what the domain computes (BR4).
- AC4 Given the seeded database, then Recife and Goiânia each have an exam and no edition, and Rio de Janeiro has nothing.
- AC5 Given the seeded database, then every edition points to an existing exam and an existing board, its notice year is inside the model's range, and no two editions break the unique index.
- AC6 Given the seeded database, then an edition is `Published` exactly when it has a notice reference and an absolute http or https notice link, and `Draft` otherwise.
- AC7 Given the seeded database, when the published catalog is queried as a student, then it returns the exams whose edition is `Published` and none of the others.
- AC8 Given a seeded row soft-deleted by an admin, when the migrations are applied again, then the row stays deleted and no duplicate appears.
- AC9 Given the seed, then no row carries a value the sheet in `## Sources` marks as not verified.
- AC10 Given the Api host, when the catalog migration ran, then the OpenAPI document still returns 200 and the architecture tests pass.
- AC11 The seed adds no UI text; the three UI locales and the missing-key test are unchanged and green.

## Decisions
- 2026-09-29 — Approved by the owner ("aprovado", answering the approval request for F-37).
- 2026-09-29 — The seed contains boards, authorities, exams and editions, not only Simulae's boards — the owner asked for the four levels and to research the missing data (owner, question 1).
- 2026-09-29 — The owner allowed reading `D:\acanario\downloads\guarda_municipal_concursos.md`, outside the repository, read-only (owner, question 2).
- 2026-09-29 — A one-time data migration, not a start-up seeder — the seed goes to every environment, an admin's delete must not come back at the next restart, and Simulae used a migration for its first seed (owner, question 3 and follow-up).
- 2026-09-29 — The taxonomy stays in epic 692 (owner, question 4).
- 2026-09-29 — An edition is `Published` only with a confirmed reference and link, `Draft` otherwise — the seed goes to production, and a student must never see uncertain data (owner, follow-up).
- 2026-09-29 — Recife and Goiânia get an exam without an edition (owner, follow-up); the student catalog will not show them until an admin adds and publishes an edition. Rio de Janeiro is left out because its call is an internal selection (research, 2026-09-29).
- 2026-09-29 — Maceió's edition uses Copeve/Ufal as its board (owner, follow-up). The research says the Fundepes organizes the concurso and the Copeve applies the papers; Fundepes is not seeded.
- 2026-09-29 — Rows carry fixed GUIDs and literal normalized values, with a test tying them to the domain (BR3, BR4) — a migration must not depend on code that evolves — Claude.
- 2026-09-29 — Authority acronyms are `PM-<CITY>` — the field is required and unique-friendly, and the notice does not give one — Claude.
- 2026-09-29 — No new package, no new permission, no new screen — Claude.
- 2026-09-29 — The research runs again at build time in the built-in browser (BR12): the first pass could not open the Curitiba, Salvador and Maceió notices (403, 404, HTML instead of PDF), so their data is provisional until then — Claude.

## Sources
Research of 2026-09-29, first pass; every value below is provisional until BR12 is done at build. "Primary" is the notice, the prefeitura or the board's site; anything else is not used for a value.

| City | Board | Notice year and reference | Position | Application date | Link | Edition state |
|---|---|---|---|---|---|---|
| Curitiba | Instituto AOCP | year and reference not verified (secondary sources say Edital 01/2025); the paper was applied 2026-01-18 | `Guarda Municipal` (AOCP news) | 2026-01-18 (AOCP news) | https://www.institutoaocp.org.br/concursos/669 (403 to the tool, to be opened in the browser) | `Draft`, or not seeded if the year stays unconfirmed |
| Manaus | Instituto Consulplan | 2026, `Edital nº 01, de 23 de março de 2026` (Diário Oficial de Manaus, extra edition 6276, 2026-03-23; also read in F-35, 2026-09-26) | `Técnico Municipal I - Guarda Municipal` | 2026-05-24 (notice item 6.1; a rectification was not checked) | https://dhg1h5j42swfq.cloudfront.net/2026/03/24001950/edital-gcm-manaus-2026.pdf | `Published` |
| Salvador | Fundação Getulio Vargas (FGV) | 2026, `Edital nº 02/2026` (FGV page, rectified 2026-09-28) | `Guarda Civil Municipal` | empty: only secondary sources give 2027-01-17 | https://conhecimento.fgv.br/concursos/pmsguarda2026 | `Published` |
| Maceió | Copeve/Ufal | 2026, `Edital nº 01/2026`, 2026-03-04 (Copeve; rectified 2026-04-29 and 2026-08-06, not read) | `Guarda Civil Municipal` | 2026-07-12 only if the rectifications keep it, else empty | https://maceio.al.gov.br/noticias/semsc/prefeitura-de-maceio-publica-edital-de-concurso-para-a-guarda-civil-municipal | `Published` if the reference and link hold, else `Draft` |
| Recife | none | no notice: the prefeitura created the commission on 2026-09-12 | none | none | none | no edition |
| Goiânia | none | no notice; board not defined | none | none | none | no edition |
| Rio de Janeiro | not applicable | internal selection, not seeded | not applicable | not applicable | https://www.rio.rj.gov.br/web/portaldeconcursos/exibeconteudo?id=16886382 | not seeded |

Not verified in the first pass: the Curitiba, Salvador and Maceió notices themselves, any rectification of a date, and the exact name of the Curitiba position.

## Out of scope
- The subjects and questions of those exams: epics 692 and 693.
- The subject taxonomy of Simulae's seed (BR13), registered as an idea.
- Recife, Goiânia and Rio editions, and any other city or exam type: the back office already adds them.
- Notice documents, vacancies, salaries and the later phases of a concurso (physical test, medical exam): not modeled.
- Re-seeding, updating or removing seeded rows after the migration.

## Open questions
- (none)

## Change notes

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: feature/F-37
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
