---
feature: F-185
epic: Data type controls
status: done
Autopilot: stop 2
board: 255
version: 1
---
# Date range and relative time

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: done (2026-10-30)
- Stopped at: Shipped: merge 60692c6d on main, board 255 closed
- Next action: Nothing: the item is done
- Waiting on: nothing

## Summary
The kit gains `AppDateRangeField` (presets "last 7, 30, 90 days" and "this year", plus a custom range of two dates), `AppRelativeTime` ("3 days ago", the exact date in the tooltip) and `AppDateText`, in the gallery. The period filters of `AccountEvents` and `RoleHistory`, a select of days today, move to the range field in its presets-only mode (the Api still takes a number of days); the audit lists show the relative time beside the exact time. Catalog: `docs/design/data-type-controls.md`, section 2.3.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (lane jussara; the kit and two admin lists change).
- Risk: high (door: src/Hosts/Simulab.Web/Components/Pages/Admin/AccountEvents.razor)
- Suggested path: `/agile:autopilot F-185 --assume --worktree`.
- Parallel with: F-181, F-182.

## Goal
A period is picked in one named control and a moment in the past reads the way a person says it, so the audit lists stop repeating a select of days and a bare timestamp, and a future screen can filter by a real date range.

## What already exists
- `AccountEvents` (`account-events-period-filter`) and `RoleHistory` (`history-period-filter`): `AppSelectField<int?>` with "All time" and "Last 7, 30, 90 days" (`AccountEventListQuery.Periods`, `RoleChangeListQuery.Periods`); the choice is `DaysFilter`, in the address as `days`, sent as `&days=`. The Api refuses any other number (`account_event.period_invalid`, `role_change.period_invalid`) and computes the window from the server clock.
- The "When" column of both lists is an `AppDateColumn<DateTimeOffset>` with `Format="g"` on the time converted to the user's time zone (`UserTimeZone.ToLocal`).
- `AppDateField` (`DateOnly?`, `Min`, `Max`, typing and calendar) and `AppDateColumn` (default format `d`). No relative-time or date-text helper exists. The pages have no `TimeProvider`; the kit test context has a fake one.
- The Api takes a number of days only: there is no `from`/`to` in the contracts.

## Users and use cases
- UC1 An admin filters the account events or the role history by "last 7, 30 or 90 days" or all time, in one control, with the choice kept in the address as before.
- UC2 An admin reads, in each row of both lists, the exact time and how long ago it was ("3 days ago"), with the exact date in the tooltip.
- UC3 A developer finds the range field (presets and custom range), the relative time and the date text in every state in the gallery `/dev/ui`.

## Business rules
- BR1 `AppDateRangeField` holds an `AppDateRange(DateOnly? From, DateOnly? To, int? Days)`: `Days` is set when a "last n days" preset is chosen, and the range is then the last n days up to today (`TimeProvider`, the user's time zone); "this year" is 1 January to today; a custom range is two `AppDateField`s.
- BR2 The buttons are `DayPresets` (default 7, 30 and 90), "This year" (`ThisYear`, default on), "Custom range" (`AllowCustom`, default on) and an optional first button with the page's word for no range (`AllText`, "All time"); the chosen one is `aria-pressed` and its text is bold: colour never speaks alone.
- BR3 A custom range checks `From` ≤ `To` and shows a localized error; either end may be left empty (open-ended). Typing is in the user's date format, as `AppDateField`.
- BR4 A page that can filter only by days (the Api's contract) turns off `ThisYear` and `AllowCustom`: the field is then the presets and the "all" button, and the page reads `Days`. Both audit lists do.
- BR5 `AppRelativeTime` (`DateTimeOffset`): "Just now" under a minute, then minutes, hours, days, months and years, each in the singular or plural of the language, from `TimeProvider`; a moment in the future reads "Just now"; it renders a `<time datetime>` with the exact date and time (`g`, the value as given) in the tooltip.
- BR6 `AppDateText` (`DateOnly?`) shows the date in the user's culture (`d`), a dash for none; `AppDateRange.Format` writes a range as "01/10/2026 – 27/10/2026" (both dates whole; an open end is left open).
- BR7 The audit lists keep the exact time in the "When" cell and add the relative time under it, so an audit record is never only "3 days ago".
- BR8 Every control keeps the shared contract (catalog section 3). The Api and its period check do not change.

## Screens and API
- No route or endpoint changes; no API contract changes (the custom range and "this year" wait for an Api that takes `from` and `to`, an idea).
- Kit: `AppDateRangeField`, `AppDateRange`, `AppRelativeTime`, `AppDateText` in `src/Hosts/Simulab.Web/Components/Ui/`.
- Pages: `AccountEvents` and `RoleHistory` (the period filter and the "When" column).
- Gallery `/dev/ui`: a section "Dates" with the controls in every state.

### Wireframe: Account events, period and when
```text
+----------------------------------------------+
| Period                              [changed]|
| [All time] [Last 7 days] [Last 30 days] [..90]|
|                                              |
| When                          Event  [changed]|
| 10/29/2026, 3:04 PM           Sign in        |
| 3 days ago                                   |
+----------------------------------------------+
```

### Wireframe: Gallery, Dates
```text
+----------------------------------------------+
| Dates                                   [new]|
| Range: presets | this year | custom | error  |
| Relative time: now | minutes | days | years  |
| Date text: date | none | range               |
+----------------------------------------------+
```

## Risks
- The period filter loses its address state — the page keeps `DaysFilter`, the `days` address and the request; only the control changes, with the filter tests kept — proven — low impact — two-way door.
- A relative time read as the record's only time — the exact time stays in the cell and in the tooltip — proven — low impact — two-way door.
- The tests that find the period as a select by id break — the id stays on the group and the page tests are rewritten with the control — not proven — low impact — two-way door.

## Docs
- flow: new docs/flows/date-range-relative-time.md — the three controls, where each is used and the wireframes above.

## Acceptance criteria
- AC1 Given `AppDateRangeField` with the presets 7, 30 and 90, when the user chooses "Last 30 days", then the value is the last 30 days up to today with `Days` 30, the button is `aria-pressed`, and the "all" button gives an empty range.
- AC2 Given `ThisYear` on, when the user chooses "This year", then the range is 1 January to today with `Days` null.
- AC3 Given `AllowCustom` on, when the user chooses "Custom range", then two date fields appear; a `From` after `To` shows the localized error and does not change the value; either end may be empty.
- AC4 Given `ThisYear` and `AllowCustom` off, when it renders, then only the "all" and the day presets are offered.
- AC5 Given `AppRelativeTime`, when it shows 30 s, 5 min, 1 h, 3 days, 2 months and 2 years ago, then it reads "Just now", "5 minutes ago", "1 hour ago", "3 days ago", "2 months ago" and "2 years ago"; the tooltip has the exact date and time and the element is a `time` with its `datetime`.
- AC6 Given `AppDateText` and `AppDateRange.Format`, when they show a date, none and a range, then they read the date in the culture, "—" and "01/10 – 27/10/2026" style text.
- AC7 Given the gallery `/dev/ui`, when it opens, then a section "Dates" shows the range field in its states, the relative time and the date text.
- AC8 Given `AccountEvents` and `RoleHistory`, when the admin picks a period, then the period is the range field in presets-only mode, the address has `days`, the request has `&days=`, and the list answers as before.
- AC9 Given the audit lists, when they show a row, then the "When" cell has the exact time and the relative time under it.
- AC10 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en).

## Criterion → test

Tests locked: bc0eb75e2bba2987c0191229b1f588c9197f59a2 (5 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `AppDateRangeFieldTests.Click_LastThirtyDays_IsTheLastThirtyDaysUpToToday`, `Render_TheChosenPreset_IsPressed_AndTheOthersAreNot`, `Click_All_IsTheEmptyRange`, `Render_OffersAllTheDayPresetsThisYearAndCustom_InThatOrder` |
| AC2 | `AppDateRangeFieldTests.Click_ThisYear_IsTheFirstOfJanuaryUpToToday_WithNoDays` |
| AC3 | `AppDateRangeFieldTests.Click_Custom_ShowsTwoDateFields`, `Custom_AStartAfterTheEnd_ShowsTheErrorAndDoesNotChangeTheValue`, `Custom_AValidRange_IsReported_AndAnEndMayBeLeftEmpty` |
| AC4 | `AppDateRangeFieldTests.PresetsOnly_NoThisYearAndNoCustom_OffersJustTheDaysAndAll` |
| AC5 | `AppRelativeTimeTests` (all) |
| AC6 | `DateTextTests` (all) |
| AC7 | `UiGalleryDatesTests` (all) |
| AC8 | `AccountEventsPageTests.FiltersInTheAddress_AreSentAndShownOnTheFilters`, `FilterChanged_OnALaterPage_ReloadsFromTheFirstPageWithTheFilter`; `RoleHistoryPageTests` (same two); `DatePageFieldsTests.Period_IsTheRangeFieldInPresetsOnlyMode`; `PeriodButtonClickTests` (a real click) |
| AC9 | `AccountEventsPageTests.Load_TheWhenCell_HasTheExactTimeAndTheRelativeTime`, `RoleHistoryPageTests.Load_TheWhenCell_HasTheExactTimeAndTheRelativeTime`, `DatePageFieldsTests.WhenColumn_ShowsTheRelativeTime` |
| AC10 | `DateResourcesTests.Key_HasATextInTheLanguage` |

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-29 — The period filters keep the Api's days: the contracts of both lists take 7, 30 or 90 days and refuse anything else, so the custom range and "this year" cannot filter them without a change in the Identity contract (a contract change is the owner's); the range field has a presets-only mode and the two pages use it; the custom range and "this year" are built in the kit, shown in the gallery and left for an Api that takes `from` and `to` (named at stop 2 for the owner to capture as an idea: it needs a contract change in the Identity module) — verified in the code — assumed: premise-correction
- 2026-10-29 — The audit lists keep the exact time and add the relative time under it: an audit record needs its exact moment — assumed: technical
- 2026-10-29 — `AppDateRange` carries `Days` so a page that filters by days reads the preset it was given, without computing days back from dates — assumed: technical
- 2026-10-29 — Screen design: no new mockup — the range field is a row of toggle buttons like the level filter chips of F-184 and two existing date fields; the text wireframes show the screens — assumed: screen-design
- 2026-10-29 — No new package: everything uses MudBlazor and the framework already in the solution — assumed: packages
- 2026-10-29 — This run is on Sonnet 5.5, not on the lane's opus: a session cannot switch its own model — lane jussara — assumed: model-choice
- 2026-10-29 — Two defects of the locked tests, worked around in the code instead of asking for an unlock: (1) `Custom_*` call a date field's callback off the renderer's thread, which throws for a callback with a receiver; the range field's date callbacks have no receiver and re-render with `InvokeAsync(StateHasChanged)` (safe in production: the callback runs on the circuit's dispatcher); (2) the page-source scan cannot match a tag that holds `=>`, so both pages pass a method (`OnPeriodChangedAsync`) instead of a lambda — assumed: technical
- 2026-10-29 — Independent review (agile:reviewer, risk high: `AccountEvents.razor` is a door): 8 findings, none a blocker; the receiver-less callbacks were judged safe. Fixed: "today" is the user's day (`Zone`, the pages pass `TimeZone.Current`); the range error reaches the two date fields and the wrapper's error class; the pressed state resets with a page-side reset; the gallery's relative-time samples use the injected `TimeProvider`; `BR6` amended to whole dates; click tests added in a new file. Accepted: no `Required`/`ReadOnly` on the range field (a filter is optional; disabled stands for read-only) and no long-label state in the gallery; the 6 resource keys `*.Filter.Period.Days7/30/90` stay, since a resource test still reads them (a cleanup, not this item); the relative text is computed per render (no live refresh) and months and years count 30 and 365 days (BR5) — assumed: technical
- 2026-10-29 — Screen check in the app host not done in the build: other lanes of the run use the same ports and the same Docker at the same time, and a screen check needs a signed-in session; it is in the validation script (`Only you`) — assumed: technical
- 2026-10-29 — Reuse: `AppDateField` for the custom range, `AppDateColumn` for the column, the toggle-button look of the level chips of F-184, `UserTimeZone` for the zone; nothing new outside the kit.
- 2026-10-29 — Shortcuts: none
- 2026-10-30 — Unlock of `GallerySections.cs` (locked by the tests of this item) to merge `main`, which brought the `gallery-numbers` and `gallery-choices` lines at the same place as `gallery-dates`; all three lines kept, tests locked again — owner, in chat: "autorizo o merge de F-185" (the first attempt to unlock was denied; the owner typed the words again directly)
- 2026-10-30 — Merge authorized by the owner's words "autorizo o merge de F-185" while the item is not `Risk: low` and the validation script was not run on screen; the owner repeated the words after being told that rule. Screen steps: `not tested`.
- 2026-10-29 — plan — 1 kit tests and skeletons, locked; 2 AppDateRange and AppDateRangeField → range tests green; 3 AppRelativeTime and AppDateText → text tests green; 4 resx in three locales, gallery "Dates", styles → gallery and resource tests green; 5 AccountEvents and RoleHistory (period field, When cell) → page tests green; 6 flow page, manual pages, gate, scan, review, validation script — assumed: technical

## Out of scope
- An Api that takes `from` and `to` for the audit lists (not captured here: the owner decides); the time field and the date-time field (F-187).

## Open questions
- (none)

## Change notes

## Validation script

### pt-BR
Necessário para validar: o app host local com o administrador da carga inicial `admin@simulab.local` (senha em `src/Hosts/Simulab.Api/appsettings.Development.json`, `Identity:SeedAdmin:Password`) — você fornece, está no lugar. Entrar com essa conta cria o primeiro evento da lista de eventos de conta.

#### Verificado pelo Claude
- Os 3 controles novos, os textos e as duas listas: 3067 testes do projeto Web verdes na suíte inteira, entre eles os testes novos dos botões de período, do tempo relativo, do texto de data, da galeria e das duas listas, com cliques de verdade nos botões (em c9417f07).
- Os testes de arquitetura (vocabulário, camadas, recursos): 310 verdes, com a solução inteira compilada sem erro (em a9a31217).
- O gate das mudanças: `agile gate GREEN`, sem aviso novo; a documentação técnica: `agile techdocs GREEN: 1 page(s)` (em c9417f07).
- O scan de segurança: `agile scan GREEN` (em c9417f07).
- Os textos novos existem em pt-BR, pt-PT e en: 84 verificações de `DateResourcesTests` verdes (em c9417f07).
- A revisão independente (risco alto): 8 achados, nenhum bloqueio; 5 corrigidos, 3 aceitos com decisão em `## Decisions` (em c9417f07).
- Não conferido em tela pelo Claude: o app host não foi iniciado neste trabalho (a mesma porta e o mesmo Docker são usados por outras frentes em paralelo); a aparência fica nos passos abaixo (em c9417f07).

#### Só você
1. Passo 1 de 4 (cerca de 4 min): suba o app no banco próprio do item. Feche antes o Visual Studio ou qualquer app host rodando de outra pasta.
Git Bash:
```bash
cd /d/wt/simulab/f-185-date-range-relative && Database__Name=simulab_f185 dotnet run --project src/Hosts/Simulab.AppHost
```
PowerShell 7:
```powershell
cd D:\wt\simulab\f-185-date-range-relative; $env:Database__Name = 'simulab_f185'; dotnet run --project src/Hosts/Simulab.AppHost
```
Esperado: o endereço do painel do Aspire aparece e o Web responde em https://localhost:7125.

2. Passo 2 de 4 (cerca de 5 min): confira os controles de data na galeria.
   - [ ] 2.1 Entre com `admin@simulab.local`, abra https://localhost:7125/dev/ui (menu lateral esquerdo, seção Development, "UI kit") e role até a seção "Datas".
   Esperado: duas colunas: o período em vazio, preenchido, erro e desabilitado; e o tempo relativo ("há 20 segundos" aparece como "Agora mesmo", "há 5 minutos", "há 3 horas", "há 3 dias", "há 2 meses", "há 2 anos") com o texto de data embaixo.
   - [ ] 2.2 No período em vazio clique em "Últimos 30 dias", depois em "Este ano", depois em "Todo o período".
   Esperado: o botão clicado fica com a borda mais grossa e o texto em negrito; só um fica assim por vez.
   - [ ] 2.3 Clique em "Período personalizado": digite em "De" uma data depois da data de "Até".
   Esperado: aparece "A data inicial não pode ser depois da data final." e os dois campos ficam com o aviso; corrigindo, o aviso some.
   - [ ] 2.4 Passe o mouse sobre um tempo relativo ("há 3 dias").
   Esperado: a dica mostra a data e a hora exatas.

3. Passo 3 de 4 (cerca de 5 min): confira a lista de eventos de conta.
   - [ ] 3.1 No menu lateral esquerdo, na seção Administração, escolha Eventos de conta.
   Esperado: a coluna Quando mostra a data e a hora exatas e, embaixo, "Agora mesmo" ou "há N minutos"; acima da tabela há a fila de botões Todo o período (apertado), Últimos 7, 30 e 90 dias.
   - [ ] 3.2 Clique em "Últimos 30 dias".
   Esperado: o botão fica apertado (borda grossa e negrito), o endereço da página ganha `days=30` e a lista recarrega na primeira página.
   - [ ] 3.3 Recarregue a página.
   Esperado: "Últimos 30 dias" continua apertado. Clique em "Todo o período": `days` some do endereço.

4. Passo 4 de 4 (cerca de 4 min): confira o histórico de papéis e o teclado.
   - [ ] 4.1 Na seção Administração escolha Histórico de papéis e repita 3.1 e 3.2 (o período fica no endereço como `days`).
   Esperado: o mesmo comportamento da lista de eventos.
   - [ ] 4.2 Só com o teclado: use Tab até a fila de botões do período e Enter ou Espaço em "Últimos 90 dias".
   Esperado: cada botão recebe o foco visível, uma parada de Tab por botão, e Enter ou Espaço escolhe o período.

## Delivery

Gate time: ship run about 7 min (fresh run); 0 red on the ship run
- Branch: feature/F-185
- Merge: 60692c6d (`Merge feature/F-185`, no fast-forward, on main after F-184)
- Tests: full suite green on the merged tree (Web, Catalog 706, Identity 589, QuestionBank 872, ExamEngine 99 and the rest); architecture tests green; `agile gate GREEN`, `agile scan GREEN`, `agile docs GREEN`, `techdocs check F-185` GREEN
- Manual pages: docs/manual/pt-BR/account-events.md, docs/manual/pt-BR/role-history.md
- Technical pages: docs/flows/date-range-relative-time.md (new)
- App version: 1.32.0 -> 1.33.0
- Validation evidence: the owner typed "autorizo o merge de F-185" in chat (twice, the second time directly to this session after the first unlock attempt was denied), after being told the rule (feature-ship precondition 1) asks for the validation words on an item that is not `Risk: low`; it was taken as the validation and the merge authorization. Screen steps of the validation script: `not tested`.

## Events

- 2026-10-29 assumed — 6 answers
- 2026-10-30 blocked — test wrong: the owner, in chat: "autorizo o merge de F-185" (unlock of GallerySections.cs only, to keep gallery-numbers, gallery-choices and gallery-dates)
