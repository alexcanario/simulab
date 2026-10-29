---
feature: F-181
epic: Data type controls
status: validating
Autopilot: stop 2
board: 251
version: 1
---
# Number controls: integer, decimal, score and year

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: validating (2026-10-29)
- Stopped at: Built; waiting for the owner's validation on screen
- Next action: Follow the validation script and say if it passes (Owner)
- Waiting on: nothing

## Summary
The kit gains `AppNumberFormat` (parse and format in the user's culture, only the culture's decimal separator), `AppIntegerField`, `AppDecimalField` (`Decimals` required), `AppScoreField`, `AppYearField`, the formatters `AppNumberText` and `AppScoreText` and the column `AppScoreColumn`, all in the gallery. The screens move to them: question count, points per question, objective and essay cut-offs and essay max score (F-168 types them as `string` today) and the notice year in `ExamEditionForm`. Number inputs use `type="text"` with `inputmode`, never `type="number"`. Catalog: `docs/design/data-type-controls.md`, section 2.2.

## Start
- Depends on: F-168 (done; owns `ExamEditionForm` and `NoticeSubjectDialog`).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (lane jussara; the kit and two catalog forms change).
- Risk: medium (no signal; code or rules changed (.claude/agile/sync.json))
- Suggested path: `/agile:autopilot F-181 --assume --worktree`.
- Parallel with: F-185.

## Goal
Each kind of number has one named control, one formatter and one column in the kit, so a page stops holding a number as `string` and repeating its parse, its format and its error, and the build of F-186 can refuse `type="number"`.

## What already exists
- `AppTextField<TValue>` (`src/Hosts/Simulab.Web/Components/Ui/AppTextField.razor`): label above, required mark, hint or error, `InputMode`, `Pattern`, `InputAttributes`, `EndContent` (F-183). No numeric field exists in the kit and no `MudNumericField` is used anywhere.
- `ScoreText` (`Components/Pages/Catalog/ScoreText.cs`): the page-level helper that formats a score with two decimals and parses it in the current culture (`NumberStyles.Number`, which also accepts the group separator).
- `NoticeSubjectDialog`: question count and points per question are `AppTextField<string>` with `InputType.Number`, parsed by the page (`TryParseCount`, `ScoreText.TryParse`), errors `QuestionCountError` and `PointsError`.
- `ExamEditionForm`: objective cut-off, essay max score and essay cut-off are `AppTextField<string>` parsed by `ParsedScore`; the notice year is an `AppTextField<int?>` with `InputType.Number`; duration hours and minutes are number fields that F-182 replaces.
- `CatalogSearch`: the notice year filter is an `AppSelectField<int?>` of the years that exist in the data, not a free number (see Decisions).
- `AppNumberColumn<TItem,TProperty>` (right-aligned `PropertyColumn`) and `FileSizeText`; `CatalogLimits` holds the bounds (question count 1..500, points 0.01..100, score 0.01..1000, `ScoreDecimalPlaces` 2, notice year from 1990, 4 digits).

## Users and use cases
- UC1 A curator types the question count of a notice subject in a field that takes whole numbers only and shows the unit.
- UC2 A curator types points per question, the objective and essay cut-offs and the essay maximum score with the decimal separator of their language (`,` in pt-BR and pt-PT, `.` in en) and sees the value formatted with the exam's decimal places when leaving the field.
- UC3 A curator types the notice year of an edition in a 4-digit field with no thousands separator.
- UC4 A developer finds every number control in every state in the gallery `/dev/ui`.

## Business rules
- BR1 `AppNumberFormat` parses and formats in the culture of the user (`CultureInfo.CurrentCulture`). The decimal separator is the only separator accepted: the group separator and the other language's decimal separator are a parse error. A leading sign is accepted; spaces around the text are trimmed; blank means no value (`null`).
- BR2 `AppIntegerField` (`int?`): whole numbers only; optional `Min`, `Max`, `Unit` (suffix) and `Stepper` (minus and plus buttons); `type="text"`, `inputmode="numeric"`.
- BR3 `AppDecimalField` (`decimal?`): `Decimals` is `[EditorRequired]` (the meaning decides it); `inputmode="decimal"`; on leave the value is rounded to `Decimals` and shown with them (`60,00`), never while typing; a text with the wrong separator or letters is a parse error.
- BR4 `AppScoreField` is `AppDecimalField` with the unit "points" (`pts`); `Decimals` stays required and comes from the exam rule (`CatalogLimits.ScoreDecimalPlaces` today).
- BR5 `AppYearField` (`int?`): 4 digits at most, shown plain ("2026", never "2.026"); default range 1950 to the current year + 2 (`TimeProvider`); `Min` and `Max` override it.
- BR6 A value that does not parse keeps the text the user typed, shows a localized error (the page's own `Error` wins) and reports it through `InvalidChanged`; the bound value is not changed, so a page never saves a stale number: it treats `Invalid` as blocking.
- BR7 `AppNumberText` shows an `int?` or `decimal?` with the culture and a fixed number of decimals, "—" for none; `AppScoreText` shows it with the unit ("2,5 pts"); `AppScoreColumn` is `AppNumberColumn` with `Decimals` required.
- BR8 Every control keeps the shared contract of the catalog (section 3): `Id` and `Label` required, label above, required mark, hint, error, description announced, value never cleared on an error.
- BR9 The server rules do not change: the Api stays the authority for every value (range, decimals); the page keeps its range messages and codes, taken from `CatalogLimits`.

## Screens and API
- No route or endpoint changes; no API contract changes.
- Kit: `AppNumberFormat`, `AppIntegerField`, `AppDecimalField`, `AppScoreField`, `AppYearField`, `AppNumberText`, `AppScoreText`, `AppScoreColumn` in `src/Hosts/Simulab.Web/Components/Ui/`.
- Pages: `NoticeSubjectDialog` (question count, points per question), `ExamEditionForm` (objective cut-off, essay max score, essay cut-off, notice year); their layout stays where it is.
- Gallery `/dev/ui`: a section "Numbers" with the controls in every state.

### Wireframe: Notice subject dialog
```text
+----------------------------------------------+
| Notice subject                               |
| Questions *             Points per question *|
| [ 20 ______ ] questions [ 1,50 ___ ] pts     |
|                              [changed]       |
+----------------------------------------------+
```

### Wireframe: Exam edition form, scores
```text
+----------------------------------------------+
| Notice year *   Objective cut-off  Essay max |
| [ 2026 ]        [ 60,00 ] pts      [ 10,00 ] |
| Essay cut-off                                |
| [ 5,00 ] pts                                 |
|                                   [changed]  |
+----------------------------------------------+
```

### Wireframe: Gallery, Numbers
```text
+----------------------------------------------+
| Numbers                                 [new]|
| Integer / Decimal / Score / Year             |
| empty | filled | error | disabled | read-only|
| long label | stepper | unit | table column  |
+----------------------------------------------+
```

## Risks
- A page saves a stale number after a parse error — the control does not call `ValueChanged` on invalid text and reports `InvalidChanged`; both forms block the save on it, with a test — proven — high impact — two-way door.
- A value typed in one culture and re-read in another — the field is parsed and formatted by the current culture only and nothing is stored as text — proven — low impact — two-way door.
- Typed fields change the markup of inputs that tests find by id — ids stay as they are (`notice-subject-question-count`, `notice-subject-points`, ...) — not proven — low impact — two-way door.

## Docs
- flow: new docs/flows/number-controls.md — the controls, their rules, where each is used and the wireframes above.
- flow: update docs/flows/edition-exam-rules.md#Steps — the score fields are typed number controls.

## Acceptance criteria
- AC1 Given the culture pt-BR or en, when `AppNumberFormat` parses and formats, then only that culture's decimal separator is accepted ("1,5" in pt-BR, "1.5" in en), the group separator and the other separator are refused, blank is `null` and a value formats with the asked decimals.
- AC2 Given `AppIntegerField`, when the user types "20", "20,5", "abc" or blank, then the value is 20, a parse error (text kept), a parse error (text kept) and `null`; the input is `type="text"` with `inputmode="numeric"` and shows the `Unit`.
- AC3 Given `AppDecimalField` with `Decimals` 2, when the user types "1,5" and leaves the field, then the value is 1.5 and the text becomes "1,50"; typing "1.5" in pt-BR is a parse error; the input has `inputmode="decimal"` and `type="text"`.
- AC4 Given `AppScoreField`, when it renders, then it behaves as `AppDecimalField` and shows the unit "pts" in the user's language.
- AC5 Given `AppYearField`, when the user types "2026", then the value is 2026 and shows "2026"; "2.026", "26" and a year out of range are errors; the default range is 1950 to the current year + 2.
- AC6 Given a field with a parse error, when it renders, then the typed text is kept, the error is the description of the input (`aria-invalid`, `aria-describedby`), `InvalidChanged` reported true, and `ValueChanged` was not called; the page's own `Error` wins over the field's.
- AC7 Given `AppNumberText` and `AppScoreText`, when they show 1234.5 with 1 decimal, 2.5 as a score and no value, then they read "1.234,5" in pt-BR, "2,5 pts" and "—".
- AC8 Given `AppScoreColumn` in an `AppDataTable`, when a row has 2.5, then the cell is right-aligned and formatted with the column's `Decimals`.
- AC9 Given the gallery `/dev/ui`, when it opens, then a section "Numbers" shows each control in empty, filled, error, disabled, read-only, long label and stepper states.
- AC10 Given `NoticeSubjectDialog`, when the curator types the question count and the points per question, then both are typed controls bound to `int?` and `decimal?`, a parse error blocks the save, and a valid save sends the same request as before.
- AC11 Given `ExamEditionForm`, when the curator types the objective cut-off, the essay max score, the essay cut-off and the notice year, then they are typed controls, the existing checks (cut-off above the objective total, essay cut-off above the maximum, year range) answer the same, and a parse error blocks the save.
- AC12 Given the sources of those two forms, when searched, then none of those fields uses `InputType.Number` or `AppTextField<string>` for a number.
- AC13 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en).

## Criterion → test

Tests locked: 89e801c3f3158154fffc70fa45a27318519f59b2 (10 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `AppNumberFormatTests` (all) |
| AC2 | `AppIntegerFieldTests.Input_IsTextWithTheNumericKeyboard_AndShowsTheUnit`, `Leave_AWholeNumber_IsTheValue`, `Leave_Blank_IsNoValue`, `Leave_NotAWholeNumber_KeepsTheTextShowsTheErrorAndDoesNotChangeTheValue`, `Stepper_PlusAndMinus_MoveTheValue` |
| AC3 | `AppDecimalFieldTests` (all) |
| AC4 | `AppScoreFieldTests.Score_BehavesAsTheDecimalFieldAndShowsThePointsUnit` |
| AC5 | `AppYearFieldTests` (all) |
| AC6 | `AppIntegerFieldTests.Leave_NotAWholeNumber_KeepsTheTextShowsTheErrorAndDoesNotChangeTheValue`, `Error_ThePagesOwnMessage_WinsOverTheFields`, `Leave_AfterAnError_ValidTextClearsIt`, `Description_IsWhatTheInputPointsAt`; `AppDecimalFieldTests.Leave_TheOtherSeparator_IsAParseErrorAndKeepsTheText` |
| AC7 | `NumberTextTests` (all) |
| AC8 | `AppScoreColumnTests.Column_RightAlignsTheCellAndFormatsWithItsDecimals` |
| AC9 | `UiGalleryNumbersTests` (all) |
| AC10 | `NoticeSubjectPointsTests.Dialog_TheTwoFields_AreTypedNumberControls`, `Dialog_ATextThatIsNotANumber_IsRefusedAndNothingIsSent`, `Dialog_Add_SendsThePointsPerQuestion`; `NoticeSubjectsSectionTests` (dialog fields) |
| AC11 | `ExamRulesFormTests.Edit_TheScoreFieldsAndTheYear_AreTypedNumberControls`, `Edit_ATextThatIsNotANumber_BlocksTheSave`, `Save_Add_SendsTheDurationInMinutesAndTheThreeScores`; `ExamEditionFormTests` (year) |
| AC12 | `NumberPageFieldsTests.Field_UsesTheControlOfItsKind`, `Form_HasNoNumberInputAndNoTextFieldHoldingANumber` |
| AC13 | `NumberResourcesTests.Key_HasATextInTheLanguage` |

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — Decimal input accepts only the culture separator — owner
- 2026-10-29 — This run is on Sonnet 5.5, not on the lane's opus: a session cannot switch its own model — lane jussara — assumed: model-choice
- 2026-10-29 — `CatalogSearch` keeps its notice year filter as `AppSelectField`: it offers the years that exist in the data (`filters.NoticeYears`), so a free year field would let the user pick a year with no result; the summary's "the filter moves" came from the epic text and is not true of the code — verified in the code — assumed: premise-correction
- 2026-10-29 — Duration hours and minutes of `ExamEditionForm` stay as they are: F-182 replaces them with `AppDurationField` — epic cut — confirmed: docs/design/data-type-controls.md section 5
- 2026-10-29 — A control reports a parse error through `InvalidChanged` and does not call `ValueChanged`; the page treats it as blocking — a number must never be saved stale — assumed: technical
- 2026-10-29 — Range messages stay on the pages (their Api codes and `CatalogLimits`); the controls only take `Min` and `Max` for the stepper and the year range — fewer moving parts, no behaviour change — assumed: technical
- 2026-10-29 — Typed fields are built on `AppTextField<string>` and keep the raw text; they do not use `MudNumericField` — the catalog refuses it (section 4) and it breaks the pt-BR comma — confirmed: docs/design/data-type-controls.md
- 2026-10-29 — `AppNumberText` and `AppScoreText` are components (the unit "pts" is localized) with static `Format` helpers in `AppNumberFormat` for code; `AppScoreColumn` extends `AppNumberColumn` and takes `Decimals` — assumed: technical
- 2026-10-29 — No new package: everything uses MudBlazor and the framework already in the solution — assumed: packages
- 2026-10-29 — plan — 1 AppNumberFormat + tests → format tests green; 2 AppIntegerField, AppDecimalField, AppScoreField, AppYearField → control tests green; 3 AppNumberText, AppScoreText, AppScoreColumn → display tests green; 4 resx in three locales + gallery section "Numbers" + styles → gallery and resource tests green; 5 NoticeSubjectDialog moves to the controls → NoticeSubjectPointsTests green; 6 ExamEditionForm moves to the controls → ExamRulesFormTests and ExamEditionFormTests green; 7 flow page, catalog update and manual page (pt-BR) → techdocs check; 8 gate, review, scan, validation script — assumed: technical
- 2026-10-29 — Screen check in the app host not done in the build: other lanes of the run use the same ports and the same Docker at the same time, and a screen check needs a signed-in session; it is in the validation script (`Only you`) — assumed: technical
- 2026-10-29 — A control with text that is not a number shows the Api's message in the two forms (the same text as before F-181) and its own message when the page gives none; the dialog test of the earlier item stays as it was — assumed: technical
- 2026-10-29 — `docs/design/data-type-controls.md` is not edited: it is a proposal kept by the epic (F-186 marks what is built) — assumed: technical
- 2026-10-29 — Reuse: `AppTextField` (+ `EndContent`, `InputMode`, F-183) for the input, `AppNumberColumn` for the column, `AppUrlField`'s wrapper pattern for the controls, `TimeProvider` for the year range; nothing new outside the kit.
- 2026-10-29 — Shortcuts: none
- 2026-10-29 — Independent review not run: risk medium, no door and no change-review trigger (no auth, schema, contract or data); the full web suite, the architecture tests, the gate and the scan are green — assumed: technical

## Out of scope
- Duration and range controls (F-182); percent and money (F-187); the slider (F-191); the enforcement architecture tests (F-186).
- A free year field in the `CatalogSearch` filter.

## Open questions
- (none)

## Change notes

## Validation script

### pt-BR
Necessário para validar: o app host local com o administrador da carga inicial `admin@simulab.local` (senha em `src/Hosts/Simulab.Api/appsettings.Development.json`, `Identity:SeedAdmin:Password`) — você fornece, está no lugar.

#### Verificado pelo Claude
- Os 4 controles, os formatadores, a coluna de nota e as duas telas: 3092 testes do projeto Web verdes na suíte inteira, entre eles os testes novos dos controles, da galeria, das duas telas e dos textos (em d0edccf8).
- Os testes de arquitetura (vocabulário, camadas, recursos): 310 verdes (em d0edccf8).
- O gate das mudanças: `agile gate GREEN`, sem aviso novo; a documentação técnica: `agile techdocs GREEN: 2 page(s)` (em d0edccf8).
- Os textos novos existem em pt-BR, pt-PT e en: 63 verificações de `NumberResourcesTests` verdes (em d0edccf8).
- O scan de segurança: `agile scan GREEN` (nenhum achado novo; os 2 abertos são de `tools/Simulab.DocGen`, fora desta mudança) (em f1d15536).
- Não conferido em tela pelo Claude: o app host não foi iniciado neste trabalho (a mesma porta e o mesmo Docker são usados por outras frentes em paralelo); a aparência fica nos passos abaixo (em d0edccf8).

#### Só você
1. Passo 1 de 4 (cerca de 4 min): suba o app no banco próprio do item. Feche antes o Visual Studio ou qualquer app host rodando de outra pasta.
Git Bash:
```bash
cd /d/wt/simulab/f-181-number-controls && Database__Name=simulab_f181 dotnet run --project src/Hosts/Simulab.AppHost
```
PowerShell 7:
```powershell
cd D:\wt\simulab\f-181-number-controls; $env:Database__Name = 'simulab_f181'; dotnet run --project src/Hosts/Simulab.AppHost
```
Esperado: o endereço do painel do Aspire aparece e o Web responde em https://localhost:7125.

2. Passo 2 de 4 (cerca de 5 min): confira os controles de número na galeria.
   - [ ] 2.1 Entre com `admin@simulab.local`, abra https://localhost:7125/dev/ui (menu lateral esquerdo, seção Development, "UI kit") e role até a seção "Números".
   Esperado: três colunas (inteiro, decimal, nota e ano), cada uma com os estados vazio, preenchido, erro, desabilitado e somente leitura.
   - [ ] 2.2 No campo inteiro "Número de questões" digite `20,5` e saia do campo.
   Esperado: o aviso "Informe um número inteiro, sem separadores." aparece embaixo, o texto `20,5` continua no campo.
   - [ ] 2.3 No campo decimal "Peso da matéria" digite `2.5` (com ponto) e saia; depois digite `2,5` e saia.
   Esperado: com o ponto, o aviso pede o separador `,`; com a vírgula, o aviso some e o campo mostra `2,5`.
   - [ ] 2.4 No campo de nota "Pontos por questão" digite `1,5` e saia; no campo de ano digite `2.026` e saia.
   Esperado: a nota vira `1,50` com "pts" no fim do campo; o ano com ponto mostra o aviso de intervalo (de 1950 ao ano atual + 2).
   - [ ] 2.5 No campo "Com botões de passo" clique em `+` e em `−`.
   Esperado: o número sobe e desce de um em um, os botões ficam desabilitados em 1 e em 500.

3. Passo 3 de 4 (cerca de 6 min): confira as notas e o ano no formulário da edição.
   - [ ] 3.1 No menu lateral esquerdo clique em Content > Exams, abra o exame de guarda municipal e, em "Editions", clique em Editar na edição Manaus 2026.
   Esperado: o cartão "Regras da prova" mostra a nota de corte `60,00 pts`, a redação `20,00 pts` e o corte `10,00 pts`.
   - [ ] 3.2 Em "Nota de corte da objetiva" digite `60.5` (com ponto) e clique em Salvar.
   Esperado: o campo mostra o aviso da nota de corte, o painel de erros no topo lista o campo e nada é salvo.
   - [ ] 3.3 Corrija para `60,5`, saia do campo e clique em Salvar.
   Esperado: o campo mostra `60,50`, a edição é salva e o resumo à direita mostra a nota de corte `60,5`.
   - [ ] 3.4 Em "Ano do edital" digite `20x6` e clique em Salvar.
   Esperado: o aviso do ano aparece, o texto continua no campo e nada é salvo; com `2026` salva normalmente.

4. Passo 4 de 4 (cerca de 4 min): confira o diálogo da disciplina do edital e o teclado.
   - [ ] 4.1 Na mesma edição, no cartão "Disciplinas do edital", clique em "Adicionar disciplina", digite um nome, `abc` em "Número de questões" e `1,5` em "Pontos por questão" e clique em Salvar.
   Esperado: o número de questões mostra o aviso de 1 a 500, nada é enviado, e os pontos viram `1,50` com "pts".
   - [ ] 4.2 Corrija o número para `10`, e clique em Salvar.
   Esperado: a disciplina entra na lista com "1,50 pt" ao lado das 10 questões.
   - [ ] 4.3 Só com o teclado: abra o diálogo de novo, use Tab para passar pelos campos e digite os números.
   Esperado: o foco passa pelos dois campos de número na ordem do formulário, sem armadilha, e o teclado do celular (se testar) é o numérico.

## Events

- 2026-10-29 assumed — 9 answers
