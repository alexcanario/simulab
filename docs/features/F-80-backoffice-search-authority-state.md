---
feature: F-80
epic: Assessment catalog
status: validating
board: 123
version: 1
---
# Back-office exam search matches authority and state

Technical terms: [glossary](../glossary.md)

## Summary
The back-office exam list search matches only the exam name (`ExamQueries.cs:24-28`), while the student catalog search also matches the issuing authority and the state (`NormalizedScopeDetail`, `PublishedExamQueries.cs:23-30`). Make the back-office search match the same fields. Raised while refining F-57 (owner, 2026-10-04): kept out so F-57 stays one filter.

## Start
- Depends on: nothing (F-42, done, already fills `NormalizedScopeDetail`; F-57, done, was the item this waited behind).
- Waits on (to start): nothing.
- Needed to validate: the app host started from this worktree (the owner starts it), the seeded admin (F-52) and three exams the validation script creates through the back office: an exam of an authority with a distinctive name, a `State` exam (SP) and a `Municipal` exam.
- Model: sonnet (no signal: one query, one page text, no door, no schema change, two modules at most)
- Suggested path: `/agile:build F-80` (small and clear; `/agile:autopilot F-80` also fits).
- Parallel with: any item outside the Catalog exam list (`ExamQueries`, `Exams.razor`). F-57, which used to block this, is done.

## Goal
An editor types what they remember of an exam (a word of its name, of its authority or of the place) in the back-office list's search box and finds it, exactly as a student does in the catalog.

## What already exists (verified 2026-10-21)
- Nothing in `ExamQueries.cs` or `PublishedExamQueries.cs` changed since F-57 (`60da647`, the last commit on `ExamQueries.cs`).
- Back-office list: `Exams.razor` (`/admin/exams`, `catalog.manage`) uses `AppDataTable` with `Searchable="true"` and the placeholder `Exams.Search.Placeholder` ("Pesquisar pelo nome" / "Search by name"); the term travels as `search` to `GET /api/v1/catalog/exams` (`ExamListQuery.Search`).
- `ExamQueries.ListAsync`: `search` is normalized (`CatalogText.Normalize`) and tested as one contiguous substring of `Exam.NormalizedName` (`ExamQueries.cs:24-28`). The other filters (authority, type, scope, state) combine with AND; the sort and the paging run after.
- `PublishedExamQueries.ListAsync` (student): the normalized term is split on whitespace and every word must appear in at least one of `Exam.NormalizedName`, the authority's `NormalizedName` or `Exam.NormalizedScopeDetail` (`PublishedExamQueries.cs:23-30`, F-36 BR3).
- `Exam.NormalizedScopeDetail` is filled for every exam: by `Exam.Apply` (`Exam.cs:127-138`, for a State exam it holds the state's name and acronym, `BrazilianStates.SearchText`), backfilled by the migrations `AddExamNormalizedScopeDetail` and `NormalizeExamStates`. No new migration is needed.
- `IssuingAuthority.NormalizedName` exists and is already read by the sort (`ExamQueries.cs:94-97`). The authority's stored acronym is not searched anywhere since F-44 BR5.
- The `Exams.Empty.Search` message already names the term; it does not change.
- Tests today: `ExamEndpointTests.List_SearchWithoutAccents_FindsAccentedNames` (name only) and `ExamTests` (`Create_StateWithAnAcronymOfTheList_StoresItInUpperCaseAndSearchesByNameAndAcronym`); the student side is covered by `PublishedExamEndpointTests.List_SearchWords_MatchAcrossExamAuthorityAndScopeDetail`.
- Manual: `docs/manual/pt-BR/exams.md` step 3 says the search box filters "por nome".

## Users and use cases
- UC1 A catalog editor opens `/admin/exams`, types a word of an exam's authority and finds the exams of that authority.
- UC2 The editor types a state's name or acronym (`sao paulo`, `sp`) and finds the exams of that state.
- UC3 The editor types a municipality as typed on a `Municipal` exam (`guarulhos`) and finds it.
- UC4 The editor types words from different fields (`guarda sao paulo`) and finds the exam that has each word somewhere.
- UC5 An API client lists exams with `search=<text>` and gets the same matches as the screen.

## Business rules
- BR1 The back-office search follows the student search's rule (F-36 BR3): the text is normalized with `CatalogText.Normalize` and split on whitespace; an exam matches when every word appears in at least one of the exam's normalized name, its issuing authority's normalized name, its normalized scope detail. Blank or whitespace-only text is no filter.
- BR2 The authority's stored acronym is not searched (F-44 BR5); the organizer (board) is not searched either, since an exam has none (the board belongs to the edition).
- BR3 For a `State` exam the scope detail the search reads holds the state's name and acronym, so `sp` and `sao paulo` find it, with or without accents or capitals (F-42 BR5, unchanged). A `National` exam has no scope detail and is found by name and authority only.
- BR4 The search combines with the existing filters (authority, type, scope, state) by AND, and with the sort and the paging: `total` counts the exams the search and the filters matched.
- BR5 A word written in several parts of the name no longer has to be contiguous: `judicial tecnico` finds "Tecnico em Avaliação Judicial". This is the one behavior that changes for a text that already worked.
- BR6 Nothing else changes: no field, route, parameter, error code or migration. `search` stays optional on `GET /api/v1/catalog/exams`.
- BR7 The search box's placeholder says what it searches (D3).

## Screens and API
- `/admin/exams` (`catalog.manage`): only the search placeholder changes.
- `GET /api/v1/catalog/exams?search=` — same route and parameters; the meaning of `search` widens (BR1). No error code is added.
- Texts, the key `Exams.Search.Placeholder` (the key stays, three values change):

| Key | en | pt-BR | pt-PT |
| --- | --- | --- | --- |
| `Exams.Search.Placeholder` | Search by exam, authority or place | Pesquisar por exame, órgão ou local | Pesquisar por exame, entidade ou local |

### Wireframe: Exams (back office)
```text
+----------------------------------------------------------+
| Exams                                           [ + Add ] |
| [changed] [ Search by exam, authority or place ______ ]   |
| Authority [__] Type [All v] Scope [All v] State [____]    |
| Name              | Authority        | Type   | Scope      |
| ...                                                      |
+----------------------------------------------------------+
```

## Docs
- flow: new docs/flows/exam-list-search.md — how the editor finds an exam in the back-office list (name, authority, state or place; word by word), the one behavior that changed (BR5).

## Acceptance criteria
- AC1 Given an exam "Tecnico Judiciario" of the authority "Tribunal de Justica Alfa", when the editor searches `alfa` (any case, with or without accents), then the exam is listed; when the search is the authority's accented form, the result is the same. (UC1, BR1)
- AC2 Given a `State` exam whose detail is `SP`, when the search is `sp` or `sao paulo` or `SÃO PAULO`, then it is listed; when the search is `rio`, then it is not. (UC2, BR3)
- AC3 Given a `Municipal` exam whose detail is `Guarulhos`, when the search is `guarulhos`, then it is listed; a `National` exam is not found by `guarulhos` or `sp`. (UC3, BR3)
- AC4 Given the exam "Guarda Municipal" of "Prefeitura de Sao Paulo" with detail `SP`, when the search is `guarda sao`, `GUARDA  paulo` (two spaces) or `guarda sp`, then it is listed; when it is `guarda rio`, then it is not. (UC4, BR1)
- AC5 Given the exam "Tecnico em Avaliação Judicial", when the search is `avaliacao` or `judicial tecnico`, then it is listed. (BR1, BR5)
- AC6 Given an authority whose stored acronym is `XYZW` (a column the screens no longer show), when the search is `xyzw`, then its exams are not listed. (BR2)
- AC7 Given a search that matches exams of two authorities, when the authority filter, the type filter and a page size of 1 are applied, then `items` and `total` count only the exams the search and the filters matched, and the second page holds the remaining one. (UC5, BR4)
- AC8 Given text with only spaces, or no text, when the list is requested, then every exam is returned as before. (BR1)
- AC9 Given the exam list through the real HTTP pipeline (`GET /api/v1/catalog/exams?search=...`) as an admin, then the same exams as the query returns; a student without `catalog.manage` still gets 403. (UC5, BR6)
- AC10 Given the exam list on screen, then the search box shows the new placeholder; the new texts exist in en, pt-BR and pt-PT and the missing-key test is green. (BR7)

## Criterion → test

Tests locked: d87c1785509c0ce0b0b45c5c357a57f0e8d0dc35 (2 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | ExamSearchEndpointTests.List_SearchByAnAuthorityWord_FindsTheExamsOfThatAuthority |
| AC2 | ExamSearchEndpointTests.List_SearchByTheStateAcronymOrName_FindsTheStateExam |
| AC3 | ExamSearchEndpointTests.List_SearchByTheMunicipality_FindsTheMunicipalExamAndNeverANationalOne |
| AC4 | ExamSearchEndpointTests.List_SearchWithWordsOfDifferentFields_FindsTheExamThatHasEachWordSomewhere |
| AC5 | ExamSearchEndpointTests.List_SearchWithNonContiguousWordsOfTheName_FindsTheExam |
| AC6 | ExamSearchEndpointTests.List_SearchByTheStoredAcronymOfTheAuthority_FindsNothing |
| AC7 | ExamSearchEndpointTests.List_SearchWithFiltersAndPaging_CountsOnlyWhatTheSearchAndTheFiltersMatched |
| AC8 | ExamSearchEndpointTests.List_SearchBlankOrAbsent_ReturnsEveryExam |
| AC9 | ExamSearchEndpointTests.List_SearchThroughTheHttpPipeline_ServesTheAdminAndClosesTheStudent |
| AC10 | ExamsSearchPlaceholderTests.SearchBox_SaysItSearchesExamAuthorityOrPlace; ResourceParityTests (missing-key) |

## Decisions
- 2026-10-21 — The back-office search uses the student search's word-by-word rule over exam name, authority name and scope detail (BR1) — one meaning of "search" across the two lists; the cost is that non-contiguous words of a name now match (BR5) — confirmed: docs/features/F-36-catalog-browsing-for-students.md (BR3)
- 2026-10-21 — The authority's stored acronym stays out of the search (BR2) — it is a hidden value since F-44, a hit on it would have no explanation on screen — confirmed: docs/features/F-44-issuing-authority-acronym-hidden.md (BR5)
- 2026-10-21 — Placeholder: "Pesquisar por exame, órgão ou local" (pt-PT: entidade; en: "Search by exam, authority or place") — same three words as the student search (`Catalog.Search.Placeholder`) with the verb the list already used — asked
- 2026-10-21 — Technical, no question: the match on the authority is an `Any` over `IssuingAuthorities` inside the `Where` (it keeps the soft-delete filter and the existing `Select` join); the word-splitting rule goes to one helper in `CatalogText` used by both queries, so the two lists cannot drift again; the student query's behavior is unchanged and its tests prove it. The build checks where `CatalogText` lives before adding the helper.
- 2026-10-21 — Technical, no question: no index. Query cost was not measured: the shape equals the student search that already ships, over a catalog of hundreds of exams, and the item makes no claim about performance.
- 2026-10-21 — Technical, no question: no security section — same route, same permission, no new input beyond the existing `search`, which is already normalized and bound as a parameter by EF.
- 2026-10-21 — Technical, no question: no new package, no migration, no OpenAPI change (`search` already exists), no `docs/architecture/` regeneration.
- 2026-10-21 — The `## Start` note "not parallel with F-57" is dropped: F-57 is `done` (merge on `main`).
- 2026-10-21 — plan — 1 tests first: `ExamSearchEndpointTests` (AC1-AC9 over HTTP) and `ExamsSearchPlaceholderTests` (AC10, bUnit), seen failing, committed, locked → the new tests are red for a missing behavior; 2 `CatalogText.SearchWords` helper + `ExamQueries` word-by-word match over name, authority name and scope detail, `PublishedExamQueries` switched to the helper → Catalog tests green, student tests unchanged; 3 placeholder in the three resx files → Web tests and missing-key test green; 4 manual (pt-BR), flow page `docs/flows/exam-list-search.md`, glossary check → `techdocs.js check F-80` green; 5 app-host check of `/admin/exams`, coverage table, scan, validation script → status `validating`.
- 2026-10-21 — Reuse: `ComparableText.Normalize` via `CatalogText`, the student query's word rule (now `CatalogText.SearchWords`, shared), `Exams.Search.Placeholder` key kept. Shortcuts: none. Not done in the build: the app-host check of `/admin/exams` (the page sits behind sign-in and the app host needs the owner's containers); the screen is covered by bUnit and the validation script.
- 2026-10-21 — The GitHub board was rate limited when the build started: the `building` mirror is sent when the quota returns.

## Out of scope
- Searching the labels of the scope or the type ("Estadual", "Concurso"): they are translated by the caller, so the Api does not know them.
- Searching the authority's acronym, or the organizer of an edition.
- Keeping the back-office filters and the search in the URL (the student search does it, the back office does not).
- Highlighting the matched words in the rows.
- The search of the other back-office lists.

## Open questions
- (none)

## Change notes

## Validation script
### English
Needed to validate: Docker running (the app host starts its containers) and the seeded admin `admin@simulab.local` (F-52; in place now: yes). The three exams are created in step 2 through the back office.

1. Start the app (the app host, which starts the database and the other services in containers; stop it before any build). This worktree uses its own database, `simulab_f80`, so the shared local one is untouched.
   - [ ] 1.1 In a terminal, from the item's worktree, run the command for your shell below and wait for the Aspire dashboard address.
   ```powershell
   cd D:\wt\simulab\f-80-backoffice-search; $env:Database__Name = "simulab_f80"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   ```bash
   cd /d/wt/simulab/f-80-backoffice-search && Database__Name=simulab_f80 dotnet run --project src/Hosts/Simulab.AppHost
   ```
   - [ ] 1.2 Open https://localhost:7125/sign-in and sign in as `admin@simulab.local` (its password is the local development one in the project settings).
2. Create the three exams the search will look for.
   - [ ] 2.1 In the sidebar (left menu) click Content, then Issuing authorities, then Add; name it `Instituto Zefir` and save.
   - [ ] 2.2 In the sidebar click Exams, then Add; pick the authority `Instituto Zefir`, name the exam `Analista Quimera`, scope National, save.
   - [ ] 2.3 Add a second exam of the same authority named `Escrevente Quimera`, scope State (the picker for the state appears), state São Paulo (SP), save.
   - [ ] 2.4 Add a third exam of the same authority named `Guarda Quimera`, scope Municipal, detail `Guarulhos`, save.
3. The search box says what it searches.
   - [ ] 3.1 In the sidebar click Exams → the search box above the table reads "Search by exam, authority or place".
4. Find the exams by authority and by place.
   - [ ] 4.1 Type `zefir` in the search box (any case) → the three exams of Instituto Zefir are listed, and the table's total says 3.
   - [ ] 4.2 Replace it with `sao paulo quimera` → only `Escrevente Quimera` is listed; `SP QUIMERA` gives the same.
   - [ ] 4.3 Replace it with `guarulhos` → only `Guarda Quimera` is listed.
   - [ ] 4.4 Replace it with `quimera analista` (words out of order) → only `Analista Quimera` is listed; `zefir rio` → the empty-list message names the term.
5. The search works with the filters.
   - [ ] 5.1 With `zefir` typed, pick the Scope filter State → only `Escrevente Quimera` remains and the total says 1; clear the filter → three again.
   - [ ] 5.2 Clear the search box (or type only spaces) → the whole list comes back.
6. The texts follow the language.
   - [ ] 6.1 Switch the app language with the globe in the app bar (the top strip) to Portuguese (Brazil) → the placeholder reads "Pesquisar por exame, órgão ou local"; to Portuguese (Portugal) → "Pesquisar por exame, entidade ou local"; back to English.
7. Who may use it, and the keyboard.
   - [ ] 7.1 Sign out, sign up a student at `/sign-up` (the verification mail is in the mail catcher) and open `/admin/exams` → the page is refused; sign back in as the admin.
   - [ ] 7.2 With the keyboard only: Tab into the search box, type `guarulhos`, wait for the list to filter → `Guarda Quimera` is the only row.

### pt-BR
Necessário para validar: Docker rodando (o app host sobe seus containers) e o admin semeado `admin@simulab.local` (F-52; no lugar agora: sim). Os três exames são criados no passo 2 pelo back office.

1. Inicie o app (o app host, que sobe o banco e os demais serviços em containers; pare-o antes de qualquer build). Este worktree usa um banco próprio, `simulab_f80`, então o banco local compartilhado não é tocado.
   - [ ] 1.1 Em um terminal, na pasta do worktree do item, rode o comando do seu shell abaixo e espere o endereço do painel do Aspire.
   ```powershell
   cd D:\wt\simulab\f-80-backoffice-search; $env:Database__Name = "simulab_f80"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   ```bash
   cd /d/wt/simulab/f-80-backoffice-search && Database__Name=simulab_f80 dotnet run --project src/Hosts/Simulab.AppHost
   ```
   - [ ] 1.2 Abra https://localhost:7125/sign-in e entre como `admin@simulab.local` (a senha é a de desenvolvimento local, nas configurações do projeto).
2. Crie os três exames que a busca vai procurar.
   - [ ] 2.1 No menu lateral clique em Content, depois Issuing authorities, depois Add; dê o nome `Instituto Zefir` e salve.
   - [ ] 2.2 No menu lateral clique em Exams, depois Add; escolha o órgão `Instituto Zefir`, nome `Analista Quimera`, abrangência National (nacional), salve.
   - [ ] 2.3 Adicione um segundo exame do mesmo órgão chamado `Escrevente Quimera`, abrangência State (estadual; aparece o seletor de estado), estado São Paulo (SP), salve.
   - [ ] 2.4 Adicione um terceiro exame do mesmo órgão chamado `Guarda Quimera`, abrangência Municipal, detalhe `Guarulhos`, salve.
3. A caixa de busca diz o que procura.
   - [ ] 3.1 No menu lateral clique em Exams → a caixa de busca acima da tabela mostra "Pesquisar por exame, órgão ou local" (no idioma em uso; em inglês, "Search by exam, authority or place").
4. Encontre os exames pelo órgão e pelo local.
   - [ ] 4.1 Digite `zefir` na caixa de busca (maiúscula ou minúscula) → os três exames do Instituto Zefir aparecem, e o total da tabela diz 3.
   - [ ] 4.2 Troque por `sao paulo quimera` → só `Escrevente Quimera` aparece; `SP QUIMERA` dá o mesmo resultado.
   - [ ] 4.3 Troque por `guarulhos` → só `Guarda Quimera` aparece.
   - [ ] 4.4 Troque por `quimera analista` (palavras fora de ordem) → só `Analista Quimera` aparece; `zefir rio` → a mensagem de lista vazia cita o termo.
5. A busca funciona junto com os filtros.
   - [ ] 5.1 Com `zefir` digitado, escolha o filtro de abrangência State → só `Escrevente Quimera` fica e o total diz 1; limpe o filtro → voltam os três.
   - [ ] 5.2 Limpe a caixa de busca (ou digite só espaços) → a lista inteira volta.
6. Os textos seguem o idioma.
   - [ ] 6.1 Troque o idioma do app pelo globo na barra superior (a faixa no alto) para português (Brasil) → o texto da caixa é "Pesquisar por exame, órgão ou local"; para português (Portugal) → "Pesquisar por exame, entidade ou local"; volte para inglês.
7. Quem pode usar, e o teclado.
   - [ ] 7.1 Saia, cadastre um aluno em `/sign-up` (o e-mail de verificação está no capturador de e-mail) e abra `/admin/exams` → a página é recusada; entre de novo como admin.
   - [ ] 7.2 Só com o teclado: Tab até a caixa de busca, digite `guarulhos`, espere a lista filtrar → `Guarda Quimera` é a única linha.

## Events

- 2026-10-21 question — asked 1, from context 2

## Delivery
