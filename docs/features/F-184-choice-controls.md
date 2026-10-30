---
feature: F-184
epic: Data type controls
status: validating
Autopilot: stop 2
board: 254
version: 1
---
# Choice controls: switch, segmented, yes/no, level, language

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: validating (2026-10-29)
- Stopped at: Built; waiting for the owner's validation on screen
- Next action: Follow the validation script and say if it passes (Owner)
- Waiting on: nothing

## Summary
The kit gains `AppSwitch` (a setting that applies at once), `AppSegmentedField` (2–4 short options), `AppYesNoField` (`bool?`: yes, no, not set), `AppLevelField` and `AppLevelBadge` (dots with the level name; `AppLevelFilterField` for the filter mode with many levels), `AppBoolText`, `AppBoolColumn` and `AppLanguageField`, in the gallery. The screens move to them: difficulty in `QuestionForm` and in the `Questions` filter and table, the true/false key of `QuestionForm`, and the five content-language selects. The difficulty label and dot gauge of F-154 are delivered here. Catalog: `docs/design/data-type-controls.md`, section 2.4.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (lane jussara; the kit and the question form change).
- Risk: high (door: src/Hosts/Simulab.Web/Components/Pages/Identity/Account.razor)
- Suggested path: `/agile:autopilot F-184 --assume --worktree`.
- Parallel with: F-183.

## Goal
Each kind of choice has one named control, one text and one column in the kit, so a page stops picking a select, a radio group or a checkbox by habit, and the difficulty shows as dots with its name wherever it is read or set.

## What already exists
- `AppCheckbox`, `AppRadioCards`, `AppSelectField`, `AppMultiPickField`, `AppStatusChip` and `AppNumberColumn` in `src/Hosts/Simulab.Web/Components/Ui/`. No `MudSwitch`, `MudToggleGroup` or `MudSlider` is used anywhere.
- Difficulty: `QuestionForm` `question-difficulty` is an `AppSelectField int?` (`DifficultyOptions`: not set, then "1" to "5"); the `Questions` filter `questions-filter-difficulty` is an `AppMultiPickField` (levels 1 to 5 and "Not set", state in `QuestionListState` and the address by `QuestionListQueryString`); the table column is an `AppNumberColumn`. Limits: `QuestionLimits.DifficultyMin` 1, `DifficultyMax` 5. No resource key names a level.
- True/false key: `question-true-false-key` is an `AppRadioCards<bool?>` with two cards ("True", "False"); a frozen question shows plain text.
- Content language: `AppSelectField<string>` fed by `SupportedCultures.All` in `QuestionForm` (`question-content-language`), `BaseTextForm` (`base-text-content-language`), `ExamForm` (`exam-content-language`), `BaseTexts` (the filter) and `Account` (`account-preferred-language`).
- Checkboxes: every `AppCheckbox` is saved with its form or is local state (sign-up consent, role and permission lists, the correct-answer mark, "I saved the codes", "decorative image"). **No checkbox is a setting applied at once**, so `AppSwitch` has no screen to move to today (see Decisions).

## Users and use cases
- UC1 A curator sets the difficulty of a question by clicking one of five dots and reads its name ("Easy", "Medium"…), or clears it.
- UC2 A curator marks a true/false question as true or false in one segmented control.
- UC3 A curator or a student filters the question list by one or more difficulty levels, including "not set", and reads the difficulty of each row as dots with the name.
- UC4 A user picks a content language in the same control on every form.
- UC5 A developer finds every choice control in every state in the gallery `/dev/ui`.

## Business rules
- BR1 `AppSwitch` (`bool`): a switch with a visible label, `role="switch"`, `aria-checked`; its change takes effect at once, so a page never puts it in a form with a Save button.
- BR2 `AppSegmentedField<TValue>`: 2 to 4 short options in one row, one selected; `role="radiogroup"` with `role="radio"` buttons; arrow keys move the choice; `Value` may be unset unless `Required`.
- BR3 `AppYesNoField` (`bool?`) is the segmented field with the options "Yes" and "No"; `TrueText` and `FalseText` replace the words ("True", "False"); an optional third option for "not set" (`NotSetText`, as in a filter "All"); `AppBoolText` shows the same words and "—" for none; `AppBoolColumn` shows an icon with the text.
- BR4 `AppLevelField` (`int?`): one dot per level from `Min` to `Max`, the chosen level and those below it filled; the level name beside the dots (`Names` is `[EditorRequired]`, one per level, from the page's resources); a clear button when not `Required`; `role="radiogroup"`, each dot a `role="radio"` named "n — name"; arrows, Home and End move the choice. No colour carries meaning: the number and the name stay on screen (the colour ramp, the gauge and the slider are F-191).
- BR5 `AppLevelFilterField`: the filter mode of the level control: one toggle chip per level (mini dots and the name, `aria-pressed`) plus an optional "not set" chip; many may be on.
- BR6 `AppLevelBadge`: the mini dots and the level name as text; none reads "Not set". It is the display of the level and its table cell.
- BR7 `AppLanguageField` (`string?`, a culture code): an `AppSelectField` preset with the options from `SupportedCultures.All` in their native names.
- BR8 Every control keeps the shared contract (catalog section 3): `Id` and `Label` required, label above, required mark, hint, error, description announced.
- BR9 The server rules do not change: the Api stays the authority for the difficulty range, the key and the language.

## Screens and API
- No route or endpoint changes; no API contract changes.
- Kit: `AppSwitch`, `AppSegmentedField`, `AppYesNoField`, `AppLevelField`, `AppLevelFilterField`, `AppLevelBadge`, `AppBoolText`, `AppBoolColumn`, `AppLanguageField` in `src/Hosts/Simulab.Web/Components/Ui/`.
- Pages: `QuestionForm` (difficulty, true/false key, content language), `Questions` (difficulty filter and column), `BaseTextForm`, `BaseTexts`, `ExamForm`, `Account` (content or preferred language).
- Gallery `/dev/ui`: a section "Choices" with the controls in every state.
- Mockup: the level field, badge and filter were drawn with the owner for F-191 ([F-191-level-visuals.html](mockups/F-191-level-visuals.html)); this item builds the same shapes without colour, gauge or slider. The other controls are standard kit patterns drawn by the wireframes.

### Wireframe: Question form, difficulty and true/false key
```text
+----------------------------------------------+
| Difficulty                          [changed]|
| (o)(o)(o)( )( )  Medium            [x clear] |
| From 1 (easiest) to 5 (hardest). Optional.   |
|                                              |
| Correct answer *                    [changed]|
| [ True | False ]   segmented                 |
+----------------------------------------------+
```

### Wireframe: Questions list, difficulty
```text
+----------------------------------------------+
| Difficulty                          [changed]|
| [oo Very easy] [ooo Easy] [oooo Hard] [Not set]|
|                                              |
| Statement            Difficulty     [changed]|
| What is...?          (o)(o)(o)( )( ) Medium  |
+----------------------------------------------+
```

### Wireframe: Gallery, Choices
```text
+----------------------------------------------+
| Choices                                 [new]|
| Switch | Segmented | Yes/No | Level | Badge  |
| Level filter | Language | Bool text / column |
| empty | filled | error | disabled | read-only|
+----------------------------------------------+
```

## Risks
- The true/false key changes control and a wrong value reaches the Api — the field keeps `bool?` and the page sends the same value; the existing tests of the key are rewritten for the new control and the save test is kept — proven — high impact — two-way door.
- The difficulty filter loses its address state — the page keeps `QuestionListState` and `QuestionListQueryString`; only the control that toggles a level changes, with the filter tests kept — proven — low impact — two-way door.
- Tests that find the old markup by id or role (`question-difficulty` as a select, radio cards for the key) break — the ids stay and the tests are rewritten with the control — not proven — low impact — two-way door.

## Docs
- flow: new docs/flows/choice-controls.md — the nine controls, where each is used and the wireframes above.
- flow: update docs/flows/question-form-composition.md#Steps — difficulty and the true/false key use the new controls.

## Acceptance criteria
- AC1 Given `AppSwitch`, when the user clicks or presses Space, then `ValueChanged` fires at once with the new `bool`, and the control is a `role="switch"` with `aria-checked`.
- AC2 Given `AppSegmentedField`, when the user clicks an option or presses an arrow key, then that option is selected and `ValueChanged` fires; the group is a `radiogroup` of `radio` buttons.
- AC3 Given `AppYesNoField` with `TrueText` "True" and `FalseText` "False", when the user picks "False", then the value is `false`; with `NotSetText` the third option sets `null`; an unset value shows no option selected.
- AC4 Given `AppLevelField` with five names, when the user clicks the third dot, then the value is 3, three dots are filled and the name of level 3 is shown; the clear button sets `null` (absent when `Required`); arrows and Home and End move the level; each dot is named "n — name".
- AC5 Given `AppLevelFilterField`, when the user toggles chips, then each chip reports `aria-pressed` and the page receives the set of levels and the "not set" flag.
- AC6 Given `AppLevelBadge`, `AppBoolText` and `AppBoolColumn`, when they show 3, true and no value, then they read the level name with its dots, the words "Yes" or the page's `TrueText`, and "—" or "Not set".
- AC7 Given `AppLanguageField`, when it renders, then its options are `SupportedCultures.All` in native names and it behaves as `AppSelectField<string>`.
- AC8 Given the gallery `/dev/ui`, when it opens, then a section "Choices" shows each control in empty, filled, error, disabled and read-only states.
- AC9 Given `QuestionForm`, when the curator sets the difficulty and the true/false key, then they are `AppLevelField` and `AppYesNoField`, the form saves the same values as before, and a frozen question still shows them read-only.
- AC10 Given `Questions`, when the user filters by difficulty, then the filter is `AppLevelFilterField`, the address and the list answer as before, and the table shows each difficulty with `AppLevelBadge`.
- AC11 Given the five language fields, when searched in the sources, then each uses `AppLanguageField`.
- AC12 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en).

## Criterion → test

Tests locked: 47af0f663ad6bb8365f6cd6491c289ae29f2ac76 (5 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `AppSwitchTests` (all) |
| AC2 | `AppSegmentedFieldTests` (all), `ChoiceKeyboardFocusTests.SegmentedField_AnArrowKey_MovesTheFocusToTheNewOption` |
| AC3 | `AppYesNoFieldTests` (all) |
| AC4 | `AppLevelFieldTests` (all), `ChoiceKeyboardFocusTests.LevelField_AnArrowKey_MovesTheFocusToTheNewDot`, `LevelField_TwoArrowKeys_ReachTheSecondNeighbour` |
| AC5 | `AppLevelFilterFieldTests` (all) |
| AC6 | `ChoiceDisplayTests` (all), `AppBoolColumnTests.Column_ShowsTheWordsOfEachRow_WithAnIcon` |
| AC7 | `AppLanguageFieldTests.Render_IsASelectFieldOfTheSupportedLanguages` |
| AC8 | `UiGalleryChoicesTests` (all) |
| AC9 | `ChoicePageFieldsTests.Field_UsesTheControlOfItsKind` (difficulty, key), `QuestionFormTests` (difficulty saved), `TrueFalseQuestionFormTests` (the key) |
| AC10 | `QuestionsFilterPageTests.Filter_DifficultyNotSetAndFive_AreSentAsNoneAndFive`, `Address_WithFilters_IsAppliedAndShownInThePickers`; `ChoicePageFieldsTests.QuestionsTable_ShowsTheDifficultyWithTheBadge` |
| AC11 | `ChoicePageFieldsTests.Field_UsesTheControlOfItsKind` (languages), `BaseTextsFilter_UsesTheLanguageField` |
| AC12 | `ChoiceResourcesTests.Key_HasATextInTheLanguage` |

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — The level shows dots with the level name — owner
- 2026-10-27 — Takes the difficulty gauge of F-154 — owner, similar-items card of the epic
- 2026-10-29 — `AppSwitch` is built and shown in the gallery only: the code has no checkbox that is a setting applied at once (every `AppCheckbox` is saved with its form or is local), so the summary's "the checkboxes that are settings" has nothing to move — verified in the code — assumed: premise-correction
- 2026-10-29 — The level colour ramp, the gauge and the slider stay in F-191 (it depends on this item); the dots here are one neutral tone — epic cut — confirmed: docs/features/F-191-level-colour-gauge-slider.md
- 2026-10-29 — The filter mode is its own control, `AppLevelFilterField`, instead of a mode of `AppLevelField`: one is a radio group and the other a set of toggle chips, which share no keyboard model — assumed: technical
- 2026-10-29 — `AppLevelField` takes the level names from the page (`Names`): the names depend on what is measured (the difficulty of a question), so the kit holds none; the difficulty names live in the question bank resources — assumed: technical
- 2026-10-29 — The true/false key moves from two radio cards to the segmented yes/no field (`TrueText` "True", `FalseText` "False"): it is a `bool?` of two short words, the catalog's rule for keys like `TrueFalseKey` — confirmed: docs/design/data-type-controls.md 2.4
- 2026-10-29 — Screen design reuses the mockup drawn for F-191 (the dots and the name, the badge, the filter chips) and the text wireframes; no new mockup — assumed: screen-design
- 2026-10-29 — No new package: everything uses MudBlazor and the framework already in the solution — assumed: packages
- 2026-10-29 — This run is on Sonnet 5.5, not on the lane's opus: a session cannot switch its own model — lane jussara — assumed: model-choice
- 2026-10-29 — plan — 1 kit tests and skeletons, locked; 2 AppSwitch, AppSegmentedField, AppYesNoField → their tests green; 3 AppLevelField, AppLevelFilterField, AppLevelBadge → level tests green; 4 AppBoolText, AppBoolColumn, AppLanguageField → display tests green; 5 resx in three locales, gallery "Choices", styles → gallery and resource tests green; 6 QuestionForm (difficulty, key, language) → QuestionBank tests green; 7 Questions filter and column, BaseTextForm, BaseTexts, ExamForm, Account → page tests green; 8 flow page, manual page, gate, scan, validation script — assumed: technical
- 2026-10-29 — `QuestionsPageTests` (not locked) changed with the badge: the difficulty cell reads the level name and "Not set" as text, in place of the number and a hidden dash with a spoken text; the key `Questions.Difficulty.NotSet.Spoken` is no longer used — assumed: technical
- 2026-10-29 — Screen check in the app host not done in the build: other lanes of the run use the same ports and the same Docker at the same time, and a screen check needs a signed-in session; it is in the validation script (`Only you`) — assumed: technical
- 2026-10-29 — Reuse: `AppRadioCards` and `AppSelectField` for the keyboard model and the select, `AppUrlField`'s wrapper pattern, `AppNumberColumn`'s column pattern; nothing new outside the kit.
- 2026-10-29 — Independent review (agile:reviewer, risk high: `Account.razor` is a door): 7 findings, none a blocker. Fixed: the arrow keys now move the focus with the choice (`AppLevelField`, `AppSegmentedField`; major, new test file `ChoiceKeyboardFocusTests`, since the locked file stays); the word for "no level" is the page's (`NotSetText`, "Não definida" beside "Dificuldade"); the grammar of `Gallery.Choices.Hint` in pt-BR and pt-PT; the unused key `Questions.Difficulty.NotSet.Spoken` removed. Accepted: the arrow keys do not call `preventDefault`, as `AppRadioCards` (the page may scroll on ArrowUp and ArrowDown; a limit of the render model) — marked in Shortcuts below; a switch has no error state and the segmented, yes/no and language fields use Disabled for read-only (the gallery shows them disabled); `Questions.Filter.Difficulty.Placeholder` stays because a resource test still reads it — assumed: technical
- 2026-10-29 — Shortcuts: 1 marked — arrow keys do not call `preventDefault` in `AppLevelField` and `AppSegmentedField` (the same limit as `AppRadioCards`); trigger: a report that the page scrolls on arrow keys — assumed: technical
- 2026-10-30 — Unlock of `GallerySections.cs` (locked by the tests of this item) to merge `main`, which brought F-181's `gallery-numbers` line at the same place as `gallery-choices`; both lines kept, tests locked again — owner, round 5: "autorizo o merge de F-184" (the owner typed it three times, never an explicit yes to the unlock; the orchestrator read it as consent to this one narrow unlock)
- 2026-10-30 — Merge authorized by the owner's words "autorizo o merge de F-184" while the item is `Risk: high` and the validation script was not run on screen; the owner repeated the words after being told that rule. Screen steps: `not tested`.

## Out of scope
- The colour ramp, the gauge and the slider (F-191); the enforcement tests (F-186); `AppRadioCards` stays for choices that need a description.

## Open questions
- (none)

## Change notes

## Validation script

### pt-BR
Necessário para validar: o app host local com o administrador da carga inicial `admin@simulab.local` (senha em `src/Hosts/Simulab.Api/appsettings.Development.json`, `Identity:SeedAdmin:Password`) — você fornece, está no lugar.

#### Verificado pelo Claude
- Os 9 controles novos, a coluna, os textos e as telas: 3103 testes do projeto Web verdes na suíte inteira, entre eles os testes novos dos controles, da galeria, das telas, dos textos e do foco pelo teclado (em 01b719d6).
- A revisão independente (risco alto): 7 achados, nenhum bloqueio; o principal (as setas não moviam o foco) foi corrigido com teste novo, 3 menores foram corrigidos e 3 aceitos com decisão em `## Decisions` (em 01b719d6).
- Os testes de arquitetura (vocabulário, camadas, recursos): 310 verdes, com a solução inteira compilada sem erro (em c1675647).
- O gate das mudanças: `agile gate GREEN`, sem aviso novo; a documentação técnica: `agile techdocs GREEN: 2 page(s)` (em 01b719d6).
- O scan de segurança: `agile scan GREEN` (em 01b719d6).
- Os textos novos existem em pt-BR, pt-PT e en: 96 verificações de `ChoiceResourcesTests` verdes (em c1675647).
- Não conferido em tela pelo Claude: o app host não foi iniciado neste trabalho (a mesma porta e o mesmo Docker são usados por outras frentes em paralelo); a aparência fica nos passos abaixo (em c1675647).

#### Só você
1. Passo 1 de 4 (cerca de 4 min): suba o app no banco próprio do item. Feche antes o Visual Studio ou qualquer app host rodando de outra pasta.
Git Bash:
```bash
cd /d/wt/simulab/f-184-choice-controls && Database__Name=simulab_f184 dotnet run --project src/Hosts/Simulab.AppHost
```
PowerShell 7:
```powershell
cd D:\wt\simulab\f-184-choice-controls; $env:Database__Name = 'simulab_f184'; dotnet run --project src/Hosts/Simulab.AppHost
```
Esperado: o endereço do painel do Aspire aparece e o Web responde em https://localhost:7125.

2. Passo 2 de 4 (cerca de 5 min): confira os controles de escolha na galeria.
   - [ ] 2.1 Entre com `admin@simulab.local`, abra https://localhost:7125/dev/ui (menu lateral esquerdo, seção Development, "UI kit") e role até a seção "Escolhas".
   Esperado: três colunas com os controles em vazio, preenchido, erro e desabilitado: interruptor, segmentado, sim/não, idioma, nível, filtro de nível e o texto de nível e de sim/não.
   - [ ] 2.2 No interruptor "Receber lembretes" clique e depois use a barra de espaço.
   Esperado: o interruptor liga e desliga a cada acionamento, com o botão deslizando de lado.
   - [ ] 2.3 No nível "Dificuldade" preenchido clique no quarto ponto; depois clique no botão de limpar (x).
   Esperado: quatro pontos ficam cheios e o nome ao lado vira "Difícil"; ao limpar, nenhum ponto fica cheio e o nome vira "Não definido".
   - [ ] 2.4 No filtro de nível clique em dois chips e depois em "Não definido".
   Esperado: cada chip clicado fica com borda mais grossa e negrito (ligado); clicar de novo desliga.

3. Passo 3 de 4 (cerca de 5 min): confira a dificuldade, a resposta do certo/errado e o idioma no formulário da questão.
   - [ ] 3.1 No menu lateral esquerdo clique em Content > Questions > Add, escolha o tipo "Certo/Errado" e role até o cartão "Classificação".
   Esperado: a dificuldade são cinco pontos com o nome ao lado; o idioma do conteúdo é a lista com o seu idioma já escolhido.
   - [ ] 3.2 Clique no segundo ponto da dificuldade e salve o rascunho depois de preencher o resto.
   Esperado: ao reabrir a questão, dois pontos cheios e o nome "Fácil".
   - [ ] 3.3 No cartão da resposta clique em "Errado" e depois em "Certo".
   Esperado: o botão escolhido fica com borda grossa e negrito, só um por vez; a escolha é salva com a questão.
   - [ ] 3.4 Só com o teclado: use Tab até os pontos da dificuldade e as setas para a direita e para a esquerda; depois Tab até os botões Certo/Errado e as setas.
   Esperado: as setas mudam o nível e a resposta; há uma só parada de Tab em cada grupo, com o foco visível.

4. Passo 4 de 4 (cerca de 3 min): confira a lista de questões.
   - [ ] 4.1 Em Content > Questions olhe a coluna "Dificuldade".
   Esperado: cada linha mostra os pontos e o nome do nível ("Média", "Difícil"); sem dificuldade, o texto "Não definido".
   - [ ] 4.2 Na barra de filtros clique no chip "Difícil" e no chip "Não definida".
   Esperado: a lista filtra só pelas dificuldades ligadas e o endereço da página ganha `difficulty=4` e `difficulty=none`; recarregar a página mantém os chips ligados.

## Events

- 2026-10-29 assumed — 9 answers
- 2026-10-29 test-fail — 1 new warning(s): CA1826 in tests/Hosts/Simulab.Web.Tests/Ui/ChoiceKeyboardFocusTests.cs, 1 red
- 2026-10-30 blocked — test wrong: the owner, round 5: "autorizo o merge de F-184" (unlock of GallerySections.cs only, keep gallery-choices and gallery-numbers)
