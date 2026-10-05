---
feature: F-51
epic: Subject taxonomy
status: building
board: 88
version: 1
---
# Import the municipal guard taxonomy

Technical terms: [glossary](../glossary.md)

## Summary
Bring the subject taxonomy of Simulae's `GuardaMunicipalContentSeed` into the two-level model of F-79 (ADR-0001 #43) as a one-time data migration of the `catalog` schema, the F-37 precedent. Simulae's 23 knowledge domains become `Subject` rows, its 9 top-level subjects map onto the fixed areas of F-79, its topics stay `Topic` rows, and the city-specific content moves to a new "Conhecimentos locais" subject with one topic per city (ADR-0001 consequences). Result: 23 subjects and 70 topics, listed whole in `## Approved list`. The source of truth for the names is Simulae's decision record `docs/1. Product Owner/[TK #662] Decisoes confirmadas - Taxonomia Guarda Municipal.md`.

## Start
- Depends on: F-79 (the `Subject`, `Topic` and `Area` model, its migration and `CatalogText.Normalize` use) — `refining` on 2026-10-04, worktree `D:\wt\simulab\f-79-subjects-topics`; the build of F-51 starts after F-79 is merged. F-37 (the data migration precedent, done).
- Waits on (to start): the merge of F-79 — Claude and owner, through F-79's own cycle.
- Needed to validate: the local app host with the F-79 screens (`/admin/subjects`) — Claude.
- Suggested path: `/agile:refine` → `/agile:build` (after F-79 ships).
- Parallel with: F-74, F-77.

## What exists
Verified on 2026-10-04:
- In Simulab `src/`: no `Subject`, `Topic` or `Area` yet; they arrive with F-79 (file in its worktree, `refining`): `catalog.subjects` (name unique by normalized form over `TenantId`, deleted rows included), `catalog.topics` (name unique within its subject, `SubjectId` restricted), `catalog.areas` seeded with nine codes (`Languages`, `Mathematics`, `LogicalReasoning`, `NaturalSciences`, `HumanSciences`, `Law`, `InformationTechnology`, `Administration`, `SpecificKnowledge`).
- The F-37 seed `20260929142114_SeedMunicipalGuardCatalog.cs` is the pattern: fixed GUIDs, frozen literal values including the normalized columns, a test tying the literals to the domain rules, never updating a row.
- The six cities of the F-37 catalog: Curitiba, Manaus, Salvador, Recife, Goiânia, Maceió (F-37 BR7).
- Simulae (`repo/src/Modules/ContentCatalog/.../Seeding/GuardaMunicipalContentSeed.cs`): 9 subjects → 23 domains → 70 topics, identical to the TK #662 record; a start-up seeder (idempotent by name), with five tests in `GuardaMunicipalContentSeedTests.cs` (counts, accents, idempotence, coexistence with the minimal taxonomy).
- Premises corrected from the idea's summary:
  - "Simulae's top-level subject becomes the `Area`" is a mapping, not a copy: F-79 fixed its own nine areas, so the nine Simulae subjects map onto six of them (BR3).
  - "One topic per city" collapses detail: `Topic` has no children (ADR-0001 #46), so Curitiba's four history and geography topics and the Recife and Maceió legislation topics become one topic per city (BR5).
  - The 9/23/70 count does not survive the remap: the result is 23 subjects and 70 topics, not the same 70.

## Goal
Real taxonomy data in every environment early, so the notice-subject screens (F-74, F-75) and the guard editions (F-78) are refined and validated against real subjects and topics.

## Users and use cases
- UC1 A curator opens `/admin/subjects` on a fresh database and finds the 23 subjects of `## Approved list` with their areas and topic counts.
- UC2 A curator opens a seeded subject and finds its topics.
- UC3 A curator edits, moves or deletes a seeded subject or topic in the back office; the change stays.
- UC4 A developer whose database already has a subject or topic with a seeded name applies the migration without error.

## Business rules
- BR1 The seed is one data migration of the `catalog` schema, after F-79's migration. It runs once per database, from the pipeline or on start, like any migration; nothing seeds at application start (F-37 BR1).
- BR2 All rows are global (`TenantId` null), carry fixed GUIDs written in the migration, and hold frozen literal values including the normalized names. A test proves every literal equals what the domain computes today (`Subject.Create`, `Topic.Create`, `CatalogText.Normalize`), so a later change of the rules cannot silently drift the seed (F-37 BR3, BR4).
- BR3 Each subject gets the area of `## Approved list`: Simulae's LINGUAGEM and GRAMÁTICA → `Languages`; MATEMÁTICA → `Mathematics`; LÓGICA E RACIOCÍNIO → `LogicalReasoning`; TECNOLOGIA → `InformationTechnology`; DIREITO and LEGISLAÇÃO → `Law`; TÉCNICAS OPERACIONAIS → `SpecificKnowledge`; "Conhecimentos locais" → `SpecificKnowledge`.
- BR4 Subject and topic names are the TK #662 names as written (pt-BR content, accents kept, not translated), except for the changes of BR5.
- BR5 Local content: a new subject "Conhecimentos locais" holds one topic per city of the F-37 catalog, named `<City> (<UF>)`. Simulae's domain "História e Geografia de Curitiba" is not seeded; its four topics are folded into "Curitiba (PR)". The topics "Legislação municipal de Recife" and "Legislação de Maceió" are not seeded; they are folded into "Recife (PE)" and "Maceió (AL)". The rest of "Legislação Municipal" stays a subject of its own.
- BR6 A name already taken is reused, never duplicated and never an error: a seeded subject whose normalized name already exists (deleted rows included) is not inserted, and its seeded topics go under the existing subject when it is not deleted; when the existing subject is deleted, its seeded topics are skipped too (an admin's delete is final). A seeded topic whose normalized name already exists in its target subject is not inserted.
- BR7 The seed never updates an existing row: an existing subject keeps its own area and name, and an admin's later edit, move or delete stays (F-37 BR5). Re-running is impossible by design; a second seed is a second migration.
- BR8 Nothing else is seeded: no area (F-79 owns them), no board (F-37), no alias (F-77), no notice subject (F-78).

## Screens and API
No new route, screen or endpoint. The seeded rows appear in F-79's `/admin/subjects` pages and `/api/v1/catalog/subjects` endpoints. Error codes: none.

## Acceptance criteria
- AC1 Given an empty database, when the `catalog` migrations are applied, then it holds exactly the 23 subjects and 70 topics of `## Approved list`, each topic under its listed subject, all with a null `TenantId`. (UC1, UC2, BR1, BR4)
- AC2 Given the seeded database, then each subject has the area of `## Approved list` (BR3), and "Conhecimentos locais" has the six city topics of BR5 and none of the four Curitiba detail topics or the two city legislation topics. (BR3, BR5)
- AC3 Given the seed literals, then every subject and topic rebuilt through its domain `Create` returns success, and its stored normalized name equals what `CatalogText.Normalize` computes (BR2).
- AC4 Given a database where, before the seed migration, a subject "direito constitucional" exists with no area and a topic "Crase" under it, when the seed migration runs, then no second "Direito Constitucional" appears, the existing subject keeps its name and no area, and the four seeded topics of "Direito Constitucional" are added under it. (UC4, BR6, BR7)
- AC5 Given a database where a subject "Geometria" exists soft-deleted before the seed migration, when it runs, then no subject "Geometria" is inserted, the topic "Geometria" is not inserted, and the migration succeeds. (BR6)
- AC6 Given a database where the subject "Informática" exists with a topic "LibreOffice", when the seed migration runs, then "Informática" holds one "LibreOffice" and the other four seeded topics. (BR6)
- AC7 Given a seeded topic soft-deleted by an admin, when the migrations are applied again, then the topic stays deleted and no duplicate appears. (UC3, BR7)
- AC8 Given the Api host, when the catalog migrations ran, then the OpenAPI document still returns 200, EF reports no pending model change, and the architecture tests pass. (BR1)
- AC9 The seed adds no UI text; the three UI locales and the missing-key test are unchanged and green.

## Approved list
Subjects and their topics, in seed order (subject — area code: topics). Simulae's top-level subject in brackets for reference only.

1. Leitura e Interpretação — `Languages` [LINGUAGEM]: Interpretação de textos
2. Produção Textual — `Languages` [LINGUAGEM]: Redação (nível superior); Comunicação escrita
3. Morfologia — `Languages` [GRAMÁTICA]: Morfologia
4. Sintaxe — `Languages` [GRAMÁTICA]: Sintaxe, Concordância e Regência
5. Ortografia e Pontuação — `Languages` [GRAMÁTICA]: Ortografia; Pontuação
6. Semântica e Léxico — `Languages` [GRAMÁTICA]: Semântica; Vocabulário
7. Aritmética e Proporcionalidade — `Mathematics` [MATEMÁTICA]: Operações básicas; Percentuais; Regra de três; Problemas práticos
8. Álgebra e Conjuntos — `Mathematics` [MATEMÁTICA]: Equações algébricas; Teoria dos conjuntos; Análise combinatória
9. Geometria — `Mathematics` [MATEMÁTICA]: Geometria
10. Estatística e Tratamento de Informações — `Mathematics` [MATEMÁTICA]: Tratamento de informações e estatística
11. Lógica Proposicional — `LogicalReasoning` [LÓGICA E RACIOCÍNIO]: Lógica proposicional; Lógica formal
12. Raciocínio Analítico — `LogicalReasoning` [LÓGICA E RACIOCÍNIO]: Sequências lógicas; Análise de dados; Análise crítica
13. Informática — `InformationTechnology` [TECNOLOGIA]: Sistemas operacionais Windows; Pacotes Microsoft Office; LibreOffice; Segurança na internet; Conceitos de hardware
14. Direito Administrativo — `Law` [DIREITO]: Princípios da administração pública; Hierarquia administrativa; Legislação administrativa; Conceitos fundamentais; Atos administrativos; Contratos administrativos
15. Direito Constitucional — `Law` [DIREITO]: Direitos fundamentais; Constituição Federal (art. 144); Legislação de proteção; Princípios constitucionais
16. Direito Penal — `Law` [DIREITO]: Tipificação de crimes; Tipos penais; Penas e sanções; Códigos penais; Crimes específicos; Procedimentos processuais; Processo penal
17. Direitos Humanos — `Law` [DIREITO]: Proteção de direitos fundamentais; Legislação de direitos humanos; Direitos humanos e cidadania; Cidadania
18. Estatuto do Desarmamento — `Law` [LEGISLAÇÃO]: Lei 10.826/2003 (Estatuto do Desarmamento)
19. Estatuto das Guardas Municipais — `Law` [LEGISLAÇÃO]: Lei 13.022/2014 (Estatuto das Guardas)
20. Legislação Municipal — `Law` [LEGISLAÇÃO]: Estatuto dos Funcionários Públicos Municipais; Lei complementar municipal; Estatuto de servidores municipais; Legislação municipal específica; Organização municipal
21. Legislação Federal — `Law` [LEGISLAÇÃO]: Código de Trânsito; ECA (Lei 8.069/1990); Lei Maria da Penha (Lei 11.340/2006)
22. Procedimentos Policiais — `SpecificKnowledge` [TÉCNICAS OPERACIONAIS]: Técnicas de abordagem; Progressividade da força; Controle de distúrbios; Manejo de armamento; Câmeras corporais
23. Conhecimentos locais — `SpecificKnowledge` [new, BR5]: Curitiba (PR); Manaus (AM); Salvador (BA); Recife (PE); Goiânia (GO); Maceió (AL)

Count check: 23 subjects; topics 1+2+1+1+2+2+4+3+1+1+2+3+5+6+4+7+4+1+1+5+3+5+6 = 70.

Not seeded (folded into "Conhecimentos locais", BR5): the subject "História e Geografia de Curitiba" with Povos originários; Patrimônio histórico (material e imaterial); Relevo, clima, hidrografia; Urbanização — and the topics Legislação municipal de Recife; Legislação de Maceió.

## Decisions
- 2026-10-04 — Simulae's nine subjects map onto F-79's fixed areas as in BR3, "Conhecimentos locais" under `SpecificKnowledge` — uses only areas that exist, so the area filter works on day one (owner, question 1).
- 2026-10-04 — "Conhecimentos locais" gets one topic for each of the six F-37 cities, named `<City> (<UF>)` — every city with an exam in the catalog has its topic; the UF tells same-name cities apart (owner, question 2).
- 2026-10-04 — Curitiba's four history and geography topics are folded into "Curitiba (PR)", and the Recife and Maceió legislation topics into their city topics — follows ADR-0001 (one topic per city); the detail can return with parent topics (ADR-0001 #46) (owner, question 3).
- 2026-10-04 — A name already taken is reused, never an error (BR6) — a soft delete keeps the name taken, so a failing migration would block a local database with no fix through the screens; F-79's own validation creates "Direito Constitucional" (owner, question 4).
- 2026-10-04 — One data migration with fixed GUIDs and frozen literals, and a test tying the literals to the domain — the F-37 precedent: a migration must not depend on code that evolves, and an admin's delete must not come back (Claude, technical).
- 2026-10-04 — The skip logic of BR6 is SQL inside the migration (insert where the normalized name is free, topics resolved by the subject's normalized name), not `InsertData` — `InsertData` cannot skip a conflict (Claude, technical).
- 2026-10-04 — Simulae's seed tests come over as their Simulab equivalents (counts, accents through the normalized literals, coexistence through BR6); its idempotence test becomes AC7 because a migration runs once — "no code without its tests" (Claude, technical).
- 2026-10-04 — No new package: xunit, AwesomeAssertions and Testcontainers.PostgreSql are already in `Directory.Packages.props` (Claude, verified).
- 2026-10-04 — Board: GitHub issue #88 set to Ready (Claude).
- 2026-10-04 — Approved by the owner ("aprovo F-51"); the build waits for the merge of F-79 (owner).

## Out of scope
- Simulae's seven boards — done in F-37.
- Simulae's `MinimalTaxonomySeed` fixture — a bootstrap fixture, not real data.
- Aliases for the seeded names — F-77.
- Notice subjects of the guard editions and their mapping — F-78.
- Detailed local topics per city (history, geography, municipal law) — needs parent topics, ADR-0001 #46.
- Any change to F-79's model or screens.

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
