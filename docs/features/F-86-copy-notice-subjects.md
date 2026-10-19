---
feature: F-86
epic: Subject taxonomy
status: done
board: 129
version: 1
---
# Copy notice subjects from another edition

Technical terms: [glossary](../glossary.md)

## Summary
On an exam edition, the admin copies the `NoticeSubject` rows (group, label, number of questions, order) of another edition of the same exam, to save typing when a notice repeats from year to year. Captured from the F-74 refinement (owner, 2026-10-04).

## Start
- Depends on: F-74 (done, merged `a2a3dd5`) and F-75 (done, merged `58ab5a7`). The item branch is behind `main` and does not contain F-75 yet: the build merges `main` into it first.
- Waits on (to start): nothing.
- Needed to validate: one exam with two editions in the local database, one of them with notice subjects (mapped) and the other with none — the F-37 seed gives the editions; Claude adds the notice subjects and their mapping through the F-74 and F-75 screens (Claude).
- Suggested path: `/agile:build F-86`.
- Parallel with: F-77 (different tables and screens); not F-89 at build if it also changes the edition page (not verified).

## Goal
A notice that repeats from year to year is typed once: the next edition starts from a copy and the admin only fixes what changed.

## What exists (verified 2026-10-06)
- `ExamEdition` (F-35) in the `Catalog` module (`src/Modules/Catalog/Simulab.Catalog.Domain/Entities/ExamEdition.cs`): exam fixed at creation, organizer, notice year, position, `Draft`/`Published`, soft delete.
- Edition endpoints under `/api/v1/catalog/exams/{examId}/editions` (`ExamEndpoints.cs`, group `/exams` under the `catalog` prefix — the `Created` location is `/api/v1/catalog/exams/{examId}/editions/{id}`), guarded by `catalog.manage`.
- Edition page `ExamEditionForm.razor`; editions list on the exam page `ExamEditionsSection.razor`.
- `NoticeSubject` (F-74, done, verified on `main` 2026-10-11): table `catalog.notice_subjects`, endpoints in `NoticeSubjectEndpoints.cs` (group `/exams/{examId}/editions/{editionId}/notice-subjects` under the `catalog` prefix: list, create, update, `/{id}/move`, delete), section `NoticeSubjectsSection.razor` with the empty message `NoticeSubjects.Empty` and the Add button; uniqueness of label per edition and group (F-74 BR5), position per edition (F-74 BR6).
- Mapping (F-75, done, verified on `main` 2026-10-11): entity `NoticeSubjectMapping : TenantEntity` (soft delete, F-75 change note 1), table `catalog.notice_subject_mappings`; 0 to 50 entries per notice subject, each a whole subject or one topic (F-75 BR1).
- `AppIcons.Copy` (`ContentCopy`) already exists.
- Premise note: F-74's file writes its routes as `/api/v1/exams/...`; the code puts them under `/api/v1/catalog/exams/...`. This item uses the real prefix.

## Users and use cases
- UC1 An admin with `catalog.manage` opens an edition with no notice subjects and copies, in one step, the notice subjects of another edition of the same exam.
- UC2 The admin then fixes what changed in the new notice with the F-74 actions (edit, move, delete, add).

## Business rules
- BR1 The source is another edition of the same exam, not deleted, Draft or Published, with at least one live notice subject. The target itself is never a source.
- BR2 The copy is allowed only when the target edition has no live notice subject; otherwise 409 `notice_subject.copy_target_not_empty` and nothing is written.
- BR3 Every live notice subject of the source is copied, in the source's order: group, label, number of questions and position. Deleted rows of the source are not copied.
- BR4 The copies are new rows of the target edition (new ids, `TenantId` null like F-74 BR1); the source is not changed. Source and copies are independent afterwards.
- BR5 The copy is one save: either every row (and its mapping) is written or none is.
- BR6 The mapping of each source row to the canonical taxonomy (F-75) is copied with it, entry by entry, as it is in the source at the moment of the copy. An overlap the source holds (F-75 BR4, created by a later topic move) is copied as is, not refused.
- BR7 A source edition that does not exist, is deleted, belongs to another exam or is the target → 404 `exam_edition.copy_source_not_found`. A source with no live notice subject → 400 `notice_subject.copy_source_empty`.
- BR8 The copy works on a Draft or a Published target edition, like the other notice subject actions (F-74 BR9).
- BR9 The endpoint requires `catalog.manage`; the action appears only in the section of F-74, which is shown only to who has it (F-74 BR11).

## Screens and API
Screens follow the rules `ui` and `ui-project`. No mockup: one button and one picking dialog built from kit components already designed (owner, question 5).

### Screen 1 — Empty state of the "Notice subjects" section (F-74)
- When the list is empty, next to the Add button, a second outlined button `NoticeSubjects.Copy.Open` (icon `AppIcons.Copy`, Material Outlined `ContentCopy`, existing) opens the dialog of Screen 2.
- The button is shown only when the list is empty (BR2). When the exam has no other edition with notice subjects, the button stays visible, disabled, with the tooltip `NoticeSubjects.Copy.NoSource`.
- On success: snackbar `NoticeSubjects.Copy.Done` with the number of rows copied, the list reloaded, focus on the first row's Edit button.

### Screen 2 — Copy dialog (`CopyNoticeSubjectsDialog.razor`)
- `MudDialog` like `NoticeSubjectDialog.razor`: title `NoticeSubjects.Copy.Title`, text `NoticeSubjects.Copy.Intro`, `AppAlert` at the top for errors, `AppFormActions` at the bottom (primary `NoticeSubjects.Copy.Confirm`, Cancel text).
- One required choice: the source edition, as a radio list (`AppRadioCards` or the kit's single-choice list), one option per source of BR1, newest notice year first; each option shows notice year, position (or nothing), board name and `NoticeSubjects.Copy.SourceCount.One` / `.Many` with its number of live notice subjects.
- The confirm button is disabled until a source is picked; while copying it shows progress and the dialog cannot be closed.
- Errors: `notice_subject.copy_target_not_empty` (another admin added rows meanwhile), `exam_edition.copy_source_not_found`, `notice_subject.copy_source_empty` and server errors go to the dialog's alert; the dialog stays open; on `copy_target_not_empty` the section reloads behind it.
- Cancel or Esc closes at once (nothing typed to lose).

### API
- GET `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/copy-sources` — the editions that BR1 allows for this target: `[{ "editionId", "noticeYear", "position", "organizerName", "noticeSubjectCount" }]`, newest notice year first.
- POST `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/copy` — body `{ "sourceEditionId": guid }`; 200 with `{ "copiedCount": int }`.
- Error codes: `exam.not_found`, `exam_edition.not_found` (existing, for the target); `exam_edition.copy_source_not_found` (404), `notice_subject.copy_source_empty` (400), `notice_subject.copy_target_not_empty` (409).
- No new table or column: the copies are rows of `catalog.notice_subjects` and `catalog.notice_subject_mappings`.

### UI texts
| Key | pt-BR | pt-PT | en |
|---|---|---|---|
| `NoticeSubjects.Copy.Open` | Copiar de outra edição | Copiar de outra edição | Copy from another edition |
| `NoticeSubjects.Copy.NoSource` | Nenhuma outra edição deste exame tem disciplinas do edital. | Nenhuma outra edição deste exame tem disciplinas do aviso. | No other edition of this exam has notice subjects. |
| `NoticeSubjects.Copy.Title` | Copiar disciplinas de outra edição | Copiar disciplinas de outra edição | Copy notice subjects from another edition |
| `NoticeSubjects.Copy.Intro` | As disciplinas da edição escolhida, com grupos, número de questões, ordem e mapeamento, são copiadas para esta edição. Depois, ajuste o que mudou no edital. | As disciplinas da edição escolhida, com grupos, número de questões, ordem e mapeamento, são copiadas para esta edição. Depois, ajuste o que mudou no aviso. | The subjects of the chosen edition, with their groups, numbers of questions, order and mapping, are copied to this edition. Then adjust what changed in the notice. |
| `NoticeSubjects.Copy.SourceCount.One` | 1 disciplina | 1 disciplina | 1 subject |
| `NoticeSubjects.Copy.SourceCount.Many` | {0} disciplinas | {0} disciplinas | {0} subjects |
| `NoticeSubjects.Copy.Confirm` | Copiar | Copiar | Copy |
| `NoticeSubjects.Copy.Done` | {0} disciplinas copiadas. | {0} disciplinas copiadas. | {0} subjects copied. |
| `NoticeSubjects.Copy.Done.One` | 1 disciplina copiada. | 1 disciplina copiada. | 1 subject copied. |
| `exam_edition.copy_source_not_found` | A edição escolhida não está mais disponível. Escolha outra. | A edição escolhida já não está disponível. Escolha outra. | The chosen edition is no longer available. Pick another one. |
| `notice_subject.copy_source_empty` | A edição escolhida não tem disciplinas do edital para copiar. | A edição escolhida não tem disciplinas do aviso para copiar. | The chosen edition has no notice subjects to copy. |
| `notice_subject.copy_target_not_empty` | Esta edição já tem disciplinas do edital; a cópia só é feita numa lista vazia. A lista foi recarregada. | Esta edição já tem disciplinas do aviso; a cópia só é feita numa lista vazia. A lista foi recarregada. | This edition already has notice subjects; a copy is only made into an empty list. The list was reloaded. |

## Acceptance criteria
- AC1 Given an exam with editions 2023 (3 live notice subjects in two groups, one deleted) and 2025 (none), when the admin copies 2023 into 2025, then 2025 has 3 notice subjects with the same groups, labels, numbers and order, new ids, and 2023 is unchanged (UC1, BR3, BR4).
- AC2 Given the source rows are mapped to canonical subjects and topics, when copied, then each copy has the same mapping entries as its source row (BR6).
- AC3 Given the target already has a live notice subject, when the copy is requested, then 409 `notice_subject.copy_target_not_empty` and nothing is written (BR2).
- AC4 Given the target only has deleted notice subjects, when the copy is requested, then it succeeds (BR2).
- AC5 Given a source of another exam, a deleted source, a source that does not exist or the target itself, when the copy is requested, then 404 `exam_edition.copy_source_not_found` (BR1, BR7).
- AC6 Given a source with no live notice subject, when the copy is requested, then 400 `notice_subject.copy_source_empty` (BR7).
- AC7 Given a failure while writing the copies, when the save fails, then no notice subject and no mapping entry is left in the target (BR5).
- AC8 Given an exam with editions A (with subjects), B (empty, deleted later) and the target, when the copy sources are listed for the target, then only A appears, with its subject count, newest year first (BR1).
- AC9 Given a Published target edition, when the copy is requested, then it succeeds (BR8).
- AC10 Given a user without `catalog.manage`, when either endpoint is called, then 403 (BR9).
- AC11 Given the section is empty and a source exists, when the page shows, then "Copy from another edition" is enabled; given no source exists, then it is disabled with its reason; given the list is not empty, then it is not shown (Screen 1).
- AC12 Given the admin picks a source and confirms, when the copy succeeds, then the snackbar says how many were copied and the list shows the copies (Screen 2).
- AC13 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Criterion → test

Tests locked: bd437bea711c9d6374dbc16d73e28ace874f109f (1 file); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `CopyNoticeSubjectsEndpointTests.Copy_IntoAnEmptyEdition_CopiesTheLiveRowsInOrderAndLeavesTheSourceAlone` |
| AC2 | `CopyNoticeSubjectsEndpointTests.Copy_CarriesTheMappingOfEachRow` |
| AC3 | `CopyNoticeSubjectsEndpointTests.Copy_IntoAnEditionWithALiveRow_IsRefusedAndWritesNothing` |
| AC4 | `CopyNoticeSubjectsEndpointTests.Copy_IntoAnEditionWithOnlyDeletedRows_Succeeds` |
| AC5 | `CopyNoticeSubjectsEndpointTests.Copy_FromASourceThatCannotBeUsed_IsNotFound` |
| AC6 | `CopyNoticeSubjectsEndpointTests.Copy_FromASourceWithNoLiveRow_IsRefused` |
| AC7 | `CopyNoticeSubjectsEndpointTests.Copy_WhenTheSaveFails_LeavesNeitherRowsNorMappingEntries` |
| AC8 | `CopyNoticeSubjectsEndpointTests.CopySources_ListsOnlyUsableEditionsNewestYearFirst` |
| AC9 | `CopyNoticeSubjectsEndpointTests.Copy_IntoAPublishedEdition_Succeeds` |
| AC10 | `CopyNoticeSubjectsEndpointTests.BothRoutes_WithoutTheManagePermission_AreForbidden` |
| AC11 | `CopyNoticeSubjectsTests.Section_EmptyListWithASource_OffersTheCopyButtonEnabled`, `CopyNoticeSubjectsTests.Section_EmptyListWithNoSource_ShowsTheCopyButtonDisabledWithItsReason`, `CopyNoticeSubjectsTests.Section_ListWithRows_DoesNotShowTheCopyButton` |
| AC12 | `CopyNoticeSubjectsTests.Copy_PickingASourceAndConfirming_SendsItSaysHowManyAndShowsTheCopies`, `CopyNoticeSubjectsTests.Copy_OneRow_UsesTheSingularMessage`, `CopyNoticeSubjectsTests.Dialog_ListsOneCardPerSourceAndEnablesCopyOnlyAfterAPick` |
| AC13 | `CopyNoticeSubjectsTests.EveryNewText_ExistsInTheThreeLanguagesWithTheSamePlaceholders`, `CopyNoticeSubjectsTests.Intro_PtPtSaysAvisoWherePtBrSaysEdital` |

## Decisions
- 2026-10-06 — Sources are the other editions of the same exam only — the real case is the notice that repeats year to year; the list stays short (owner, question 1).
- 2026-10-06 — The copy is allowed only into an empty list — no conflict or duplicate rule to define; whoever started typing adjusts by hand (owner, question 2).
- 2026-10-06 — The copy carries the F-75 mapping; F-86 is built after F-75 — the mapping is part of the row, copying without it means remapping everything (owner, question 3).
- 2026-10-06 — Entry point: a button in the section's empty state plus a picking dialog — works for editions already created; one click copies (owner, question 4).
- 2026-10-06 — No mockup; the screen is described here — one button and one dialog from kit components already designed in F-74 (owner, question 5).
- 2026-10-06 — Out of scope: the edition's other fields, copying across exams, undoing a copy; no new idea captured (owner, question 6).
- 2026-10-06 — No new packages, production or tests — the kit, EF Core and the existing test hosts cover it (Claude).
- 2026-10-06 — One POST `.../notice-subjects/copy` with the source id, plus a GET of eligible sources — the server checks BR1/BR2 and writes in one save; the dialog never assembles rows itself (Claude).
- 2026-10-06 — A source with an overlapping mapping (F-75 BR4) is copied as is — the copy reproduces the source; F-75 checks overlap only when a row is saved by the dialog (Claude).
- 2026-10-11 — The list of copy sources is not paged — at most a few editions per exam; the F-74 list of notice subjects is not paged either (Claude).
- 2026-10-11 — Approved by the owner ("aprovo f-86"); `## Start` and `## What exists` re-checked against `main` after the F-74 and F-75 merges (owner, Claude).
- 2026-10-06 — Routes use the real prefix `/api/v1/catalog/exams/...`; F-74's file says `/api/v1/exams/...` — a premise to correct in F-74 with `/agile:change F-74` (Claude, reported to the owner).
- 2026-10-19 — Build started in the item worktree; `main` was already merged into the branch (F-74 and F-75 present). The design passes (`system-design`, `architect`) are skipped: no new project, table, column or message between modules — two endpoints and their DTOs in the existing Catalog slice (Claude).
- 2026-10-19 — plan — 1 sources query + `GET .../copy-sources` (AC8, AC10) → locked endpoint tests; 2 copy handler happy path with order, mapping, deleted-target and Published target (AC1, AC2, AC4, AC9) → AC tests green; 3 refusals 404/400/409 (AC3, AC5, AC6) → AC tests green; 4 one-save atomicity (AC7) → failure test green; 5 Web client + `CopyNoticeSubjectsDialog` + section button (AC11, AC12) → bUnit green; 6 UI texts in three locales (AC13) → missing-key test green; gate, scan, validation script.
- 2026-10-19 — The screen tests of AC11 to AC13 (bUnit) were written with the screen in step 5, not locked: they need the dialog component, which the lock phase of the API tests did not create (Claude).
- 2026-10-19 — One text key beyond the refined table: `NoticeSubjects.Copy.Source` ("Edition to copy from" / "Edição de origem"), the legend of the radio group, which the kit's `AppRadioCards` requires (Claude).
- 2026-10-19 — A save refused by the unique index of the label inside the target (another admin wrote meanwhile) answers `notice_subject.copy_target_not_empty`, not `notice_subject.duplicate`: for the caller it is the same BR2 case (Claude).
- 2026-10-19 — The dialog gets the sources the section already read and a callback to read them again after a stale-choice refusal (`copy_source_not_found`, `copy_source_empty`); no second read on open (Claude).
- 2026-10-19 — Reuse: nothing found to reuse for the copy itself; reused `NoticeSubjectStore`/`INoticeSubjectQueries` ports, `AppRadioCards`, `AppFormActions`, `AppAlert`, `ErrorText`, the F-74 dialog options and focus pattern. Shortcuts: none (Claude).

## Out of scope
- Copying the edition's other fields (year, position, board, notice reference).
- Copying from an edition of another exam.
- Appending to or replacing a list that already has notice subjects.
- Undoing a copy (rows are deleted one by one with the F-74 action).

## Open questions
- (none)

## Change notes

## Validation script
### English
Needed to validate: one exam with two editions, one of them with mapped notice subjects and one with none. The seeded exam **Guarda Municipal** has editions 2025 (6 subjects) and 2026 (9 subjects), all mapped; Claude also left two editions in the database `simulab_f86` that it made while checking: **2024** (empty, the one step 1 uses) and **2027** (a copy of 2025). All of it is in place now. The app host starts the local PostgreSQL, Redis and Mailpit containers; close any app host running from another checkout first (two hosts fight for the same ports).

Git Bash, from `D:/wt/simulab/f-86-copy-notice-subjects`:

```bash
Database__Name=simulab_f86 dotnet run --project src/Hosts/Simulab.AppHost
```

PowerShell 7, from the same folder:

```powershell
$env:Database__Name = "simulab_f86"; dotnet run --project src/Hosts/Simulab.AppHost
```

Expected: the Aspire dashboard URL is printed and the Web is at https://localhost:7125, with no `fail` line. To repeat, stop it (Ctrl+C) and run it again; stop it before you build (a running host locks `bin/`).

1. Open the empty edition.
   - [ ] 1.1 Sign in as `admin@simulab.local` (password in `src/Hosts/Simulab.Api/appsettings.Development.json`, `Identity:SeedAdmin:Password`). In the sidebar (left menu) under Content click **Exams**; the list appears.
   - [ ] 1.2 Click **Guarda Municipal**; in its **Editions** section click the **2024** edition. The page "Edit edition" opens.
   - [ ] 1.3 Scroll to the **Notice subjects** card (the notice subjects of the edition: each subject as the notice names and groups it, with its number of questions). It says "This edition has no notice subject yet." and, next to **Add notice subject**, there is an outlined button **Copy from another edition** (enabled).
2. Copy from another edition.
   - [ ] 2.1 Click **Copy from another edition**. A dialog "Copy notice subjects from another edition" opens with the explanation, the group "Edition to copy from" and one card per edition, newest first: **2027** (6 subjects), **2026 — Técnico Municipal I - Guarda Municipal** (9 subjects), **2025 — Guarda Municipal** (6 subjects), each with its board. **Copy** is disabled.
   - [ ] 2.2 Click the **2026** card; **Copy** is enabled.
   - [ ] 2.3 Click **Copy**. The dialog closes, the snackbar (the message that appears and fades) says "9 subjects copied." and the card lists 9 rows under their group headings, each with its mapping chips.
   - [ ] 2.4 Open the **2026** edition in the same way and compare: same groups, labels, numbers of questions, order and chips. It still has its 9 rows (the source is not changed).
3. After the copy.
   - [ ] 3.1 Back in **2024**: the **Copy from another edition** button is gone (a list with rows is not copied into); the total of questions is at the right end of the card.
   - [ ] 3.2 Edit one row of **2024** (change its number of questions) and Save; open **2026**: that row is unchanged (copy and source are independent). Delete one row of **2024**: the 2026 rows stay.
4. The button with no source.
   - [ ] 4.1 In **Exams** click **Add exam**, fill the required fields and Save; in the new exam add one edition (any board, year 2024) and Save. In that edition's **Notice subjects** card the **Copy from another edition** button is disabled and its tooltip (the hint when you point at it) says "No other edition of this exam has notice subjects."
5. Keyboard only, on a new empty edition.
   - [ ] 5.1 In **Guarda Municipal** add an edition (board Instituto AOCP, year 2023) and Save; in its **Notice subjects** card press Tab until **Copy from another edition** has focus and press Enter: the dialog opens.
   - [ ] 5.2 Press Arrow down to move between the edition cards and Space or Enter to pick one; Tab to **Copy** and press Enter: the rows appear and the focus is on the first row's Edit button.
   - [ ] 5.3 Repeat on another empty edition and press Esc in the dialog: it closes at once, nothing is copied and the focus returns to **Copy from another edition**.
6. Language and theme.
   - [ ] 6.1 Switch the language (globe in the top bar) to **pt-PT** and to **pt-BR**: the button, the dialog, the snackbar and the tooltip are in that language (pt-BR: "Copiar de outra edição", "disciplinas copiadas"; pt-PT says "aviso" where pt-BR says "edital").
   - [ ] 6.2 In the light and in the dark theme the button, the cards, the selected card and the disabled button stay readable.
7. [ ] Permission check: sign in as a Student. `/admin/exams` and the edition page show Page not found, so the button and the dialog are not reachable.

### pt-BR
Necessário para validar: um exame com duas edições, uma com disciplinas do edital mapeadas e outra sem nenhuma. O exame **Guarda Municipal** do seed tem as edições 2025 (6 disciplinas) e 2026 (9 disciplinas), todas mapeadas; o Claude também deixou duas edições no banco `simulab_f86`, feitas durante a conferência: **2024** (vazia, a que o passo 1 usa) e **2027** (cópia da 2025). Tudo isso já está no lugar. O app host sobe os contêineres locais de PostgreSQL, Redis e Mailpit; feche antes qualquer app host de outro checkout (dois hosts brigam pelas mesmas portas).

Git Bash, a partir de `D:/wt/simulab/f-86-copy-notice-subjects`:

```bash
Database__Name=simulab_f86 dotnet run --project src/Hosts/Simulab.AppHost
```

PowerShell 7, a partir da mesma pasta:

```powershell
$env:Database__Name = "simulab_f86"; dotnet run --project src/Hosts/Simulab.AppHost
```

Esperado: a URL do painel do Aspire é impressa e a Web fica em https://localhost:7125, sem nenhuma linha `fail`. Para repetir, pare (Ctrl+C) e rode de novo; pare antes de compilar (um host em execução trava `bin/`).

1. Abra a edição vazia.
   - [ ] 1.1 Entre como `admin@simulab.local` (senha em `src/Hosts/Simulab.Api/appsettings.Development.json`, `Identity:SeedAdmin:Password`). No menu lateral esquerdo, em Content, clique em **Exams**; a lista aparece.
   - [ ] 1.2 Clique em **Guarda Municipal**; na seção **Editions** clique na edição **2024**. A página "Edit edition" abre.
   - [ ] 1.3 Role até o cartão **Notice subjects** (as disciplinas do edital da edição: cada matéria como o edital a nomeia e agrupa, com o número de questões). Ele diz "This edition has no notice subject yet." e, ao lado de **Add notice subject**, há um botão com contorno **Copy from another edition** (habilitado).
2. Copie de outra edição.
   - [ ] 2.1 Clique em **Copy from another edition**. Abre o diálogo "Copy notice subjects from another edition" com a explicação, o grupo "Edition to copy from" e um cartão por edição, a mais recente primeiro: **2027** (6 subjects), **2026 — Técnico Municipal I - Guarda Municipal** (9 subjects), **2025 — Guarda Municipal** (6 subjects), cada um com a banca. **Copy** está desabilitado.
   - [ ] 2.2 Clique no cartão **2026**; **Copy** fica habilitado.
   - [ ] 2.3 Clique em **Copy**. O diálogo fecha, o snackbar (a mensagem que aparece e some) diz "9 subjects copied." e o cartão lista 9 linhas sob os títulos de grupo, cada uma com seus chips de mapeamento.
   - [ ] 2.4 Abra a edição **2026** do mesmo jeito e compare: mesmos grupos, nomes, números de questões, ordem e chips. Ela continua com suas 9 linhas (a origem não muda).
3. Depois da cópia.
   - [ ] 3.1 De volta à **2024**: o botão **Copy from another edition** sumiu (lista com linhas não recebe cópia); o total de questões está na ponta direita do cartão.
   - [ ] 3.2 Edite uma linha da **2024** (mude o número de questões) e salve; abra a **2026**: essa linha não mudou (cópia e origem são independentes). Exclua uma linha da **2024**: as linhas da 2026 continuam.
4. O botão sem origem.
   - [ ] 4.1 Em **Exams** clique em **Add exam**, preencha os campos obrigatórios e salve; no exame novo adicione uma edição (qualquer banca, ano 2024) e salve. No cartão **Notice subjects** dessa edição o botão **Copy from another edition** está desabilitado e a dica (o texto que aparece ao apontar) diz "No other edition of this exam has notice subjects."
5. Só com o teclado, numa edição vazia nova.
   - [ ] 5.1 Em **Guarda Municipal** adicione uma edição (banca Instituto AOCP, ano 2023) e salve; no cartão **Notice subjects** dela, aperte Tab até o foco chegar em **Copy from another edition** e aperte Enter: o diálogo abre.
   - [ ] 5.2 Use a seta para baixo para mover entre os cartões de edição e Espaço ou Enter para escolher um; Tab até **Copy** e Enter: as linhas aparecem e o foco fica no botão Edit da primeira linha.
   - [ ] 5.3 Repita em outra edição vazia e aperte Esc no diálogo: ele fecha na hora, nada é copiado e o foco volta para **Copy from another edition**.
6. Idioma e tema.
   - [ ] 6.1 Troque o idioma (globo na barra superior) para **pt-PT** e para **pt-BR**: o botão, o diálogo, o snackbar e a dica estão nesse idioma (o app abre em inglês; em pt-BR: "Copiar de outra edição", "9 disciplinas copiadas."; o pt-PT diz "aviso" onde o pt-BR diz "edital").
   - [ ] 6.2 No tema claro e no escuro, o botão, os cartões, o cartão selecionado e o botão desabilitado continuam legíveis.
7. [ ] Verificação de permissão: entre como Student. `/admin/exams` e a página da edição mostram Page not found, então o botão e o diálogo não são alcançáveis.

## Events

- 2026-10-19 test-fail — build blocked by a running process (Simulab.slnx), 1 red

## Delivery
- Branch: `feature/F-86`, merged into `main` by `dfc5fd0` (`--no-ff`), board #129. App version 1.6.0.
- Full check (`gate.js ship`): build 25 s, 0 new warnings; suite 4,3 min wall (294 s): Catalog 608 in 4 m 48 s, Identity 508 in 4 m 44 s, QuestionBank 456 in 4 m 5 s, Web 2166 in 29 s, Architecture 296, AppHost 80, Api 58, Jobs 25, Persistence 25, Email 19, Ai 10, ApiResults 13, SharedKernel 12; 0 failed. The first run was refused (RED) only because the owner's validation app host still held the build output; it passed after that host was stopped. Scan: `agile scan GREEN` (open medium findings in `tools/Simulab.DocGen` and MessagePack, not from this item). Test lock: unchanged (`bd437be`, 1 file).
- Budget: the full suite is at the edge of the 5 min budget and each integration project (Catalog, Identity, QuestionBank) is above the 2 min budget; a retro finding, not caused by this item alone.
- Criterion → test: 13 rows filled (AC1 to AC10 in `CopyNoticeSubjectsEndpointTests`, AC11 to AC13 in `CopyNoticeSubjectsTests`).
- Manual: `exam-editions.md` (new "Copiar as disciplinas de outra edição" and three messages) in pt-BR only; pt-PT and en are frozen (F-124).
- Technical docs: `docs/architecture/Catalog/routes.md` and `docs/api/Simulab.Api.json` regenerated (two routes). Glossary: nothing missing. Technical pages: none.
- Validated on screen by the owner on 2026-10-19 ("validado"). Validation evidence: 0 of 16 boxes were ticked in the widget, so each path is recorded as not tested; the owner's own statement is the evidence.
- Worktree: `D:/wt/simulab/f-86-copy-notice-subjects` could not be removed at ship time (`worktree.js`: HELD, a `.vs` folder, Visual Studio may hold it); branch `feature/F-86` is merged and stays until the worktree is removed.
Gate time: 6 passes, 19 min 17 s — turn 2 (3 min 12 s), ship 2 (9 min 5 s), scan 2 (7 min); 1 red
Usage: 3379.3k weighted (estimate) — build 85% · ship 15% — context per turn 281k → 429k — 1 session, 145 turns
