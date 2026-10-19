---
feature: F-78
epic: Subject taxonomy
status: building
board: 122
version: 1
---
# Notice subjects of the municipal guard editions

Technical terms: [glossary](../glossary.md)

## Summary
Seed the `NoticeSubject` rows and their mapping for the municipal guard editions F-37 brought in, so those editions show a real syllabus. Simulae has no notice subjects; the source is the official notices.

## Start
- Depends on: F-74 (the `NoticeSubject` model, done), F-51 (the canonical subjects and topics, done), F-75 (the mapping model, done). All three are on `main` (checked 2026-10-19).
- Waits on (to start): nothing.
- Needed to validate: the local app host started from this item's worktree with its own database, showing the four seeded editions on the F-74/F-75 edition page and on the F-76 student syllabus — Claude starts it; the owner signs in.
- Model: sonnet (no signal: data migration only, no door, no contract, no schema change, size S)
- Suggested path: `/agile:build F-78`.
- Parallel with: F-89 (different tables; it touches the edition form, this item touches no screen). F-86 copies notice subjects between editions: no file in common, but its validation is easier after this seed.

## Goal
The four municipal guard editions show the syllabus of their own notice, mapped to the canonical subjects and topics, from day one in every environment — so the student syllabus (F-76) and the question work (F-95 onwards) have real data.

## What exists
Verified on 2026-10-19 in this worktree (branch from `main` at `7808cea`, which holds F-74, F-75, F-51, F-76, F-77, F-79):
- The F-37 seed (`20260929142114_SeedMunicipalGuardCatalog.cs`) has four editions with fixed ids `0198f370-0004-7000-8000-00000000000<n>`: 1 Curitiba 2025 (Instituto AOCP, "Edital nº 02/2025"), 2 Manaus 2026 (Instituto Consulplan, "Edital nº 01, de 23 de março de 2026"), 3 Salvador 2026 (FGV, "Edital nº 02/2026"), 4 Maceió 2026 (Copeve/Ufal, "Edital nº 01/2026"). Recife and Goiânia have an exam and no edition.
- `NoticeSubject` (F-74, `Simulab.Catalog.Domain/Entities/NoticeSubject.cs`), table `catalog.notice_subjects` (migration `20261004210926_AddNoticeSubjects`): edition (fixed), optional group (≤ 100), label (2 to 200), optional number of questions (1 to 500), `DisplayOrder`, normalized group and label, soft delete, global rows.
- `NoticeSubjectMapping` (F-75), table `catalog.notice_subject_mappings` (migration `20261008193457_AddNoticeSubjectMappings`): each entry a whole subject or one topic, 0 to 50 per notice subject, no topic together with its own whole subject in one row (`NoticeSubjectMappingRules`), the same subject allowed under several rows (F-75 BR6).
- F-51 seed (`20261005135846_SeedGuardTaxonomy`): 23 subjects (`0198f370-0006-7000-8000-<n>`) and 70 topics (`0198f370-0007-7000-8000-<n>`); a name already taken was reused (F-51 BR6), so on some databases a canonical subject has another id than the seed's.
- Neither Simulab nor Simulae (read-only) holds the text of these notices. The notices were read on 2026-10-19 (sources in `## Approved list`).

## Users and use cases
- UC1 A curator opens one of the four guard editions on a database where the seed ran and finds its notice subjects, grouped and ordered as the notice's exam table, each with its number of questions and its mapping.
- UC2 A student opens a published guard exam and sees each edition's syllabus (F-76) built from those rows.
- UC3 A curator edits, moves, remaps or deletes a seeded notice subject; the change stays.

## Business rules
- BR1 The seed is one data migration of the `catalog` schema, after F-75's migration. It runs once per database, like any migration (F-37 BR1, F-51 BR1).
- BR2 Each of the four editions gets exactly the notice subjects of `## Approved list`, in that order (`DisplayOrder` 0, 1, 2, ...), with the group, label and number of questions written there. All rows are global (`TenantId` null), with fixed ids and frozen literals including the normalized group and label; a test proves every literal equals what `NoticeSubject.Create` and `CatalogText.Normalize` compute today (F-51 BR2).
- BR3 An edition that is deleted, or missing, or already has at least one notice subject (live or deleted) when the migration runs is skipped whole: none of its rows and none of their mapping entries are written (decision of 2026-10-06, question 6).
- BR4 Each seeded notice subject gets the mapping entries of `## Approved list`. A target is found by its seeded id first, then by normalized name (a subject by name; a topic by name within that subject), so a subject F-51 reused under another id is still found. A target that is not found, or is deleted, is skipped; the row is seeded with the entries left, possibly none (an admin's delete is final, F-51 BR6).
- BR5 A notice subject with no canonical equivalent stays unmapped, or partly mapped, and is listed under "Not covered by the taxonomy" in `## Approved list` (decision of 2026-10-06, question 5).
- BR6 The seed never updates a row: an admin's later edit, move, remap or delete stays (F-37 BR5, F-51 BR7). `Down` removes the seeded mapping entries and notice subjects by id.
- BR7 Nothing else is seeded: no edition, no canonical subject or topic, no alias.

## Screens and API
No new route, screen or endpoint. The seeded rows appear in F-74's section "Notice subjects" of `/admin/exams/{examId}/editions/{id}` with F-75's chips, and in F-76's student syllabus. Error codes: none.

## Acceptance criteria
- AC1 Given an empty database, when the `catalog` migrations are applied, then each of the four guard editions holds exactly the notice subjects of `## Approved list`, in that order, with that group, label and number of questions, all with a null `TenantId`. (UC1, BR1, BR2)
- AC2 Given the seeded database, then each notice subject has exactly the mapping entries of `## Approved list` (whole subject or topic), and the rows listed under "Not covered by the taxonomy" have only the entries the list gives them. (BR4, BR5)
- AC3 Given the seed literals, then every notice subject rebuilt through `NoticeSubject.Create` returns success, its stored normalized group and label equal `CatalogText.Normalize`, each edition's numbers of questions add up to the total in `## Approved list`, and every row's mapping passes `NoticeSubjectMappingRules.Normalize` and `CheckOverlap`. (BR2)
- AC4 Given a database where, before the seed migration, the Salvador edition has one notice subject typed by an admin, when the migration runs, then Salvador keeps only that row and no mapping is added to it, and the other three editions are seeded. (BR3)
- AC5 Given a database where the Maceió edition is soft-deleted before the seed migration, when it runs, then Maceió gets no notice subject and the migration succeeds. (BR3)
- AC6 Given a database where the canonical subject "Informática" exists under another id than F-51's (F-51 BR6), when the seed migration runs, then the rows that map "Informática" point at that existing subject. (BR4)
- AC7 Given a database where the topic "Curitiba (PR)" is soft-deleted before the seed migration, when it runs, then "História e Geografia de Curitiba" is seeded with no mapping entry and the migration succeeds. (BR4)
- AC8 Given the seeded database, when an admin edits a seeded notice subject and the migrations are applied again, then the edit stays and no duplicate appears; when the migration is rolled back, then the seeded notice subjects and their mapping entries are gone and the admin's own rows stay. (UC3, BR6)
- AC9 Given the Api host, when the catalog migrations ran, then the OpenAPI document still returns 200, EF reports no pending model change, and the architecture tests pass. (BR1)
- AC10 The seed adds no UI text; the three UI locales and the missing-key test are unchanged and green.
- On screen only (validation script): a seeded edition shows its rows and chips on the edition page, and the student syllabus of a published guard exam lists them (UC1, UC2).

## Criterion → test

Tests locked: 5aaf27504eb85a84d5b74e33d6e14b8e9df4386a (1 file); unlocks: 0

| Criterion | Test |
|---|---|
| AC1 | `SeedGuardNoticeSubjectsTests.Migrate_EmptyDatabase_EachEditionHoldsExactlyItsListedNoticeSubjectsInOrderWithNoTenant`; `SeedGuardNoticeSubjectsTests.Migrate_EmptyDatabase_TheNoticeSubjectsHaveTheFixedIdsOfTheSeed` |
| AC2 | `SeedGuardNoticeSubjectsTests.Migrate_EmptyDatabase_EachNoticeSubjectHasExactlyItsListedMappingEntriesInOrder`; `SeedGuardNoticeSubjectsTests.Migrate_EmptyDatabase_TheMappingEntriesHaveTheFixedIdsOfTheSeed`; `SeedGuardNoticeSubjectsTests.Migrate_EmptyDatabase_TheUncoveredNoticeSubjectHasNoEntryAndEveryOtherOneHasAtLeastOne` |
| AC3 | `SeedGuardNoticeSubjectsTests.Migrate_EmptyDatabase_EverySeededRowEqualsWhatTheDomainComputes`; `SeedGuardNoticeSubjectsTests.Migrate_EmptyDatabase_EachEditionsQuestionsAddUpToTheNoticeTotal` |
| AC4 | `SeedGuardNoticeSubjectsTests.Migrate_ASalvadorNoticeSubjectTypedByAnAdmin_KeepsOnlyThatRowAndSeedsTheOtherThreeEditions`; `SeedGuardNoticeSubjectsTests.Migrate_AManausNoticeSubjectAlreadyDeleted_SkipsTheWholeEditionToo` |
| AC5 | `SeedGuardNoticeSubjectsTests.Migrate_TheMaceioEditionSoftDeletedBeforehand_GetsNoNoticeSubjectAndTheMigrationSucceeds` |
| AC6 | `SeedGuardNoticeSubjectsTests.Migrate_ACanonicalSubjectUnderAnotherId_IsStillMappedByItsName` |
| AC7 | `SeedGuardNoticeSubjectsTests.Migrate_ASoftDeletedTopic_IsSkippedAndTheRowIsSeededWithTheEntriesLeft`; `SeedGuardNoticeSubjectsTests.Migrate_ASoftDeletedSubject_IsSkippedWholeAndItsTopicsToo` |
| AC8 | `SeedGuardNoticeSubjectsTests.Migrate_AgainAfterAnAdminEditedASeededRow_KeepsTheEditAndAddsNoDuplicate`; `SeedGuardNoticeSubjectsTests.Down_RemovesTheSeededRowsAndKeepsTheAdminsOwnNoticeSubject`; `SeedGuardNoticeSubjectsTests.Down_KeepsASeededNoticeSubjectThatHoldsAnEntryOfTheAdmin` |
| AC9 | `SeedGuardNoticeSubjectsTests.Migrate_EmptyDatabase_EachEditionHoldsExactlyItsListedNoticeSubjectsInOrderWithNoTenant` (no pending model change); `ApiHostTests.OpenApi_document_is_served`; the architecture tests (full suite) |
| AC10 | no resource file changes; the existing Catalog resource tests stay green (full suite) |
| On screen only | pending |

## Approved list
Checked by the owner on 2026-10-19. Per edition: the source read, then the rows in order as `<n>. [group] Label — questions → mapping`. Mapping: a subject name alone is the whole subject; `Subject › Topic` is one topic. Subject numbers in brackets follow F-51's `## Approved list`.

### Edition 1 — Curitiba 2025 (40 questions)
Source: AOCP, Edital nº 2/2025 – Abertura, consolidated with Retificação nº 1/2025, item 10.1 and Anexo II — https://arquivos-site.institutoaocp.org.br/publicacoes/61d6e126-7d7a-464f-bddf-d31525423205.pdf. No block heading: no group.
1. Língua Portuguesa — 5 → Leitura e Interpretação; Morfologia; Sintaxe; Ortografia e Pontuação; Semântica e Léxico
2. Raciocínio Lógico e Matemático — 5 → Aritmética e Proporcionalidade; Álgebra e Conjuntos; Geometria; Estatística e Tratamento de Informações; Lógica Proposicional; Raciocínio Analítico
3. Noções de Informática — 5 → Informática
4. História e Geografia de Curitiba — 5 → Conhecimentos locais › Curitiba (PR)
5. Noções de Direito (Constitucional, Administrativo, Penal e Processual Penal) — 10 → Direito Administrativo; Direito Constitucional; Direito Penal
6. Legislação — 10 → Legislação Municipal; Estatuto das Guardas Municipais; Estatuto do Desarmamento; Legislação Federal; Direito Administrativo › Legislação administrativa; Direito Penal › Crimes específicos; Direito Constitucional › Direitos fundamentais; Procedimentos Policiais › Progressividade da força

### Edition 2 — Manaus 2026 (60 questions)
Source: Consulplan, Edital nº 01, de 23 de março de 2026, item 5.1.1 (Quadro I) and Anexo I. The official link stored on the edition is a scanned PDF; the text was read from a copy of the same notice (https://cdn.direcaoconcursos.com.br/uploads/2026/03/1_701057.pdf), and the counts are checked by the points column (15 × 2,00 = 30,00; total 100). No retification found. No block heading: no group.
1. Língua Portuguesa — 10 → Leitura e Interpretação; Produção Textual › Comunicação escrita; Morfologia; Sintaxe; Ortografia e Pontuação; Semântica e Léxico
2. Informática — 5 → Informática
3. Geografia e História de Manaus — 5 → Conhecimentos locais › Manaus (AM)
4. Ética e Direitos Humanos — 5 → Direitos Humanos
5. Direito Constitucional — 5 → Direito Constitucional
6. Direito Penal — 5 → Direito Penal
7. Direito Processual Penal — 5 → Direito Penal › Procedimentos processuais; Direito Penal › Processo penal
8. Legislação de trânsito — 5 → Legislação Federal › Código de Trânsito
9. Legislação específica — 15 → Estatuto das Guardas Municipais; Estatuto do Desarmamento; Legislação Municipal; Conhecimentos locais › Manaus (AM); Legislação Federal › ECA (Lei 8.069/1990); Legislação Federal › Lei Maria da Penha (Lei 11.340/2006); Direito Administrativo › Legislação administrativa; Direito Penal › Crimes específicos

### Edition 3 — Salvador 2026 (70 questions)
Source: FGV, Edital nº 02, de 17 de setembro de 2026, consolidated after the 1ª retificação (28/09/2026), item 9.4 and Anexo I — https://conhecimento.fgv.br/sites/default/files/concursos/edital-prefeitura-de-salvador-2026-guarda-retificado-1_1.pdf. Two blocks; the notice's "01." numbering is not part of the label.
1. [Módulo I - Conhecimentos Básicos] Língua Portuguesa — 10 → Leitura e Interpretação; Produção Textual; Morfologia; Sintaxe; Ortografia e Pontuação; Semântica e Léxico
2. [Módulo I - Conhecimentos Básicos] Raciocínio Lógico-Matemático — 10 → Aritmética e Proporcionalidade; Álgebra e Conjuntos; Geometria; Estatística e Tratamento de Informações; Lógica Proposicional; Raciocínio Analítico
3. [Módulo I - Conhecimentos Básicos] Informática — 8 → Informática
4. [Módulo II - Conhecimentos Específicos] Noções de Direito Constitucional e Direito Civil — 10 → Direito Constitucional
5. [Módulo II - Conhecimentos Específicos] Noções de Direito Penal e Direito Processual Penal — 7 → Direito Penal
6. [Módulo II - Conhecimentos Específicos] Noções de Administração e Políticas Públicas — 5 → (none)
7. [Módulo II - Conhecimentos Específicos] Conhecimentos na Área de Atuação — 10 → Procedimentos Policiais
8. [Módulo II - Conhecimentos Específicos] Legislação — 10 → Legislação Federal; Estatuto do Desarmamento; Estatuto das Guardas Municipais; Legislação Municipal; Conhecimentos locais › Salvador (BA); Direito Penal › Crimes específicos

### Edition 4 — Maceió 2026 (60 questions)
Source: Copeve/Ufal, Edital nº 01/2026, consolidated "Retificado em 24.09.2026", item 11.2 and Anexo I — https://copeve.ufal.br/sistema/anexos/Concurso%20Publico%20Guarda%20Civil%20Municipal%20de%20Maceio%20-%202026/Edital%2001.2026%20-%20Retificado%20em%2024.09.2026.pdf. Língua Portuguesa has 10 questions after a retification (15 struck). No block heading: no group.
1. Língua Portuguesa — 10 → Leitura e Interpretação; Morfologia; Sintaxe; Ortografia e Pontuação; Semântica e Léxico
2. Raciocínio Lógico — 5 → Aritmética e Proporcionalidade; Álgebra e Conjuntos; Geometria; Estatística e Tratamento de Informações; Lógica Proposicional; Raciocínio Analítico
3. Noções de Informática — 5 → Informática
4. Legislação Municipal Específica — 10 → Legislação Municipal; Conhecimentos locais › Maceió (AL)
5. Conhecimentos Específicos — 30 → Procedimentos Policiais; Direito Administrativo; Direito Constitucional; Direito Penal; Legislação Federal; Estatuto das Guardas Municipais

### Not covered by the taxonomy
Parts of a notice subject with no canonical subject or topic in F-51 (BR5); a curator may create them in F-79 and map them with F-75.
- Salvador 4: Direito Civil (LINDB, pessoas, bens, fatos jurídicos).
- Salvador 6: the whole row (Administração; Políticas Públicas) — unmapped.
- Curitiba 6: Constituição do Estado do Paraná; Lei 13.675/2018 (SUSP).
- Manaus 9: Lei 13.675/2018 (SUSP); Lei 13.709/2018 (LGPD).
- Maceió 5: mecânica básica, direção defensiva, segurança do trabalho, primeiros socorros; Lei 13.675/2018 (SUSP).

## Decisions
- 2026-10-06 — Source: Claude reads the four notices through the links stored on the editions and transcribes them; the owner sends any notice the link does not give; the owner checks the transcribed list before approval (owner, question 1, "1 e 2").
- 2026-10-06 — Scope: the four existing editions; Recife and Goiânia have no edition and stay out (owner, question 2).
- 2026-10-06 — One `NoticeSubject` per line of the notice's exam table (subject and number of questions); the group is the notice's block ("Conhecimentos Básicos", ...); the number of questions is blank when the notice does not state it (F-74 BR4) (owner, question 3).
- 2026-10-06 — The refinement waits for F-75 to be approved, then maps subjects and topics in one pass — no rework if F-75 picks a different model (owner, question 4).
- 2026-10-06 — A notice subject with no canonical equivalent in F-51 stays unmapped and is listed in this file; a curator creates the canonical subject in F-79's back office and maps it with F-75 (owner, question 5).
- 2026-10-06 — An edition that already has any notice subject when the seed runs is skipped whole — no mixing of the admin's order and labels with the seed's (owner, question 6).
- 2026-10-06 — Delivery is one data migration of the `catalog` schema with fixed GUIDs and frozen literals, never updating a row — the F-37 and F-51 precedent (Claude, technical).
- 2026-10-06 — No new package expected — xunit, AwesomeAssertions and Testcontainers.PostgreSql are already in `Directory.Packages.props` (Claude, verified in F-51).
- 2026-10-06 — Board: GitHub issue #122 set to Ready (Claude).
- 2026-10-19 — Refinement resumed: F-74, F-75 and F-51 are `done` on `main` (Claude, verified).
- 2026-10-19 — The four notices read through their official hosts, except Manaus, whose official PDF is a scan: read from a copy of the same notice, counts checked by the points column; the latest consolidated text is used where retifications exist (Claude, sources in `## Approved list`).
- 2026-10-19 — Fixed ids: notice subjects `0198f370-0008-7000-8000-<n>`, mapping entries `0198f370-0009-7000-8000-<n>`, `<n>` frozen in the migration — the next free prefixes after F-51's `0006` and `0007` (Claude, technical).
- 2026-10-19 — The skip and lookup logic of BR3 and BR4 is SQL inside the migration (insert where the edition qualifies; targets resolved by id, then by normalized name), not `InsertData` — the F-51 precedent (Claude, technical).
- 2026-10-19 — The test keeps its own copy of `## Approved list`, so a slip in the migration's literals fails a test (F-51 precedent) (Claude, technical).
- 2026-10-19 — No system-design or architect pass: no new project, contract, message or schema change (Claude, technical).
- 2026-10-19 — `## Approved list` (rows, counts and mapping) checked and accepted as written — the sums match each notice's total (owner; asked).
- 2026-10-19 — Manaus counts taken from the text copy of the notice, its source recorded — the points column confirms them (owner; asked).
- 2026-10-19 — Salvador's group keeps the notice's block name in normal case, "Módulo I - Conhecimentos Básicos" / "Módulo II - Conhecimentos Específicos" — closest to the notice, as decided on 2026-10-06 (owner; asked).
- 2026-10-19 — A canonical target deleted by an admin is skipped and the row is seeded with the entries left (BR4) — an admin's delete is final (owner; confirmed: F-51 BR6).

- 2026-10-19 — Approved by the owner ("aprovo f-78") (owner).
- 2026-10-19 — Build started; the branch was brought up to date with `main` first (docs only). The newest catalog migration is `20261014160521_AddSubjectTopicAliases` (F-77), so the seed goes after it (Claude, verified).
- 2026-10-19 — plan — 1 tests first: `SeedGuardNoticeSubjectsTests` keeps its own copy of the approved list and covers AC1-AC8 on real PostgreSQL → seen failing (no rows), committed and locked; 2 migration `SeedGuardNoticeSubjects` (EF scaffold for the Designer, body replaced): one SQL insert per edition guarded by "edition live and no notice subject at all", then one SQL insert per mapping entry resolving the target by seeded id, then by normalized name → AC1-AC8 green; 3 `Down` by id, keeping a notice subject that still holds an entry of an admin → AC8; 4 AC9 (OpenAPI, pending model changes, architecture tests) and AC10 (missing-key test) through the affected suites; 5 coverage table, scan, validation script → `validating`.
- 2026-10-19 — Mapping entry ids are `0198f370-0009-7000-8000-<notice subject number * 100 + position in its list>`, a frozen rule over a frozen list; the test asserts them (Claude, technical).

## Out of scope
- Editions of Recife and Goiânia.
- Creating canonical subjects or topics missing from F-51 (the parts under "Not covered by the taxonomy").
- Weight, points per question and minimum score per notice subject — epic E-6.
- The discursive, physical and other phases of the notices.

## Docs
- none — a data migration only: no new flow, route, contract or screen.

## Open questions
- (none)

## Events

- 2026-10-19 question — asked 3, from context 1
