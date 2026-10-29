---
feature: F-183
epic: Data type controls
status: done
Autopilot: shipping
board: 253
version: 1
---
# Text kind controls: text area, e-mail, URL, code, one-time code, search

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: done (2026-10-29)
- Stopped at: Shipped: merge a2b47e40 on main, board 253 closed; worktree folder held by a running app host
- Next action: Close the app host that runs from the F-183 worktree, then rerun worktree remove (Owner)
- Waiting on: nothing

## Summary
The kit gains `AppTextAreaField` (auto-grow, counter), `AppEmailField` (trim, lower-case, `inputmode=email`, `Autocomplete` required), `AppUrlField` (http(s) check, open-link adornment), `AppCodeField` (no spaces, monospace), `AppOtpField` (6 digits, paste fills all) and `AppSearchField`, in the gallery. The screens move to them: the e-mail fields of `SignIn`, `SignUp`, `ForgotPassword`, `ResetPassword`, `CheckEmail` and `VerifyEmail`, the one-time code fields of `SignIn` and `Security` (3), the notice URL and the multi-line fields of `IssuingAuthorityDialog`, `OrganizerDialog` and `AiDiagnostics`. Catalog: `docs/design/data-type-controls.md`, section 2.1.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: an account with two-factor sign-in on, in the worktree's app host (created during the validation; the validation script says how).
- Model: opus (a door: the sign-in and two-factor screens change their fields).
- Risk: high (door: src/Hosts/Simulab.Web/Components/Pages/Identity/CheckEmail.razor; reserved: docs/features/F-183-text-kind-controls.md: ## Security)
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-184.

## Goal
Each kind of text has one named control in the kit, so a page stops repeating the setup of an e-mail, a code, a URL or a long text, and the build of F-186 can refuse the old patterns.

## What already exists
- `AppTextField<TValue>` (`src/Hosts/Simulab.Web/Components/Ui/AppTextField.razor`): label above, hint or error, `Counter`, `Lines` (above 1 it already grows with its content, F-33), `Autocomplete`, `LeadingIcon`, adornment.
- E-mail: 6 `AppTextField` with `InputType.Email` and `Autocomplete="username"` (`SignIn`, `SignUp`, `ForgotPassword`, `ResetPassword`, `CheckEmail`, `VerifyEmail`), `MaxLength="AccountLimits.EmailMaxLength"` (254).
- Codes: 4 `AppTextField` with `Autocomplete="one-time-code"` and `MaxLength="AccountLimits.TotpCodeMaxLength"` (32). Only `security-enrol-code` takes a TOTP code alone; `sign-in-code` takes a TOTP code or, after "use a recovery code", a recovery code; `security-regenerate-code` and `security-disable-code` take either in the same field ("code or recovery code" labels).
- URL: `edition-notice-url` (`ExamEditionForm`, `InputType.Url`, its own `NoticeUrlError`), `issuing-authority-website` and `organizer-website` (`Autocomplete="url"`).
- Multi-line: `issuing-authority-description`, `organizer-description`, `ai-prompt` (`AiDiagnostics`, `Lines="5"`, no limit), the two change-reason fields of `QuestionForm` (`PublishReasonId`, `ChangeReasonId`) and `question-status-reason` (`QuestionStatusReasonDialog`, already with `Counter`).
- Search: only inside `AppDataTable` (a `MudTextField` with `AppIcons.Search`, debounce and clear); no page has a search box outside a table.

## Users and use cases
- UC1 A visitor or student types an e-mail on the sign-in, sign-up, password and verification pages and the field keeps it clean (no spaces, lower case) without each page doing it.
- UC2 A student with two-factor sign-in types or pastes the 6-digit code from the authenticator app in one field built for digits.
- UC3 A student types a recovery code, or a code or recovery code, in a code field that ignores spaces and shows the characters in monospace.
- UC4 A curator types a long description or reason in a text area that grows and always shows how much is left.
- UC5 A curator types a notice URL or a website, gets `https://` added when missing and can open the link in a new tab from the field.
- UC6 A developer finds every text kind, in every state, in the gallery `/dev/ui`.

## Business rules
- BR1 `AppEmailField`: on leave, trims and lower-cases the value; `type=email`, `inputmode=email`, `spellcheck=false`; `Autocomplete` is `[EditorRequired]` (`username` or `email`).
- BR2 `AppOtpField`: digits only, exactly 6 (`TotpLength`), `inputmode=numeric`, `autocomplete=one-time-code`; pasting a code (spaces and hyphens ignored) fills it whole; the form is sent only by its button, never on the 6th digit.
- BR3 `AppCodeField`: removes spaces as typed, monospace text, `spellcheck=false`, `autocapitalize=off`; optional upper-case.
- BR4 Fields that take a recovery code, or a code or recovery code, use `AppCodeField`; `AppOtpField` is used only where a TOTP code is the one answer (`Approved list`, list 1).
- BR5 `AppTextAreaField`: grows from `MinLines` (default 3) to `MaxLines` (default 10), then scrolls; `MaxLength` is `[EditorRequired]` and the counter is always on.
- BR6 `AppUrlField`: on leave, trims and adds `https://` when the value has no scheme; only `http` and `https` are valid (localized error); a valid value shows an "open in a new tab" adornment (`rel="noopener noreferrer"`); `inputmode=url`, `spellcheck=false`.
- BR7 `AppSearchField`: magnifier, debounce, clear button; `AppDataTable` renders its search with it, so every table and any later search box look the same.
- BR8 Every new control keeps the shared contract of `docs/design/data-type-controls.md` section 3 (label above, required mark, hint, error, description announced, value never cleared on an error).
- BR9 The server rules do not change: the Api stays the authority for every value (e-mail, code, URL, length).

## Screens and API
- No route or endpoint changes; no API contract changes.
- Kit: `AppTextAreaField`, `AppEmailField`, `AppUrlField`, `AppCodeField`, `AppOtpField`, `AppSearchField` in `src/Hosts/Simulab.Web/Components/Ui/`.
- Pages: every field of `Approved list` changes control; their layout stays where it is. Only the screens whose look changes have a wireframe.
- Gallery `/dev/ui`: a section "Text kinds" with the six controls in every state.

### Wireframe: Sign in, second step (code)
```text
+----------------------------------------------+
| Sign in                                      |
| Authentication code *                [changed]|
| [1][2][3][4][5][6]   one box, 6 digits       |
| Type the 6-digit code from your app          |
| ( Use a recovery code )                      |
|   -> Recovery code *                [changed]|
|      [ XXXX-XXXX ________ ]  monospace       |
| [ Sign in ]                     [ Cancel ]   |
+----------------------------------------------+
```

### Wireframe: Security, two-factor
```text
+----------------------------------------------+
| Security                                     |
| Turn on two-factor sign-in                   |
| 1. Scan the QR code  2. Confirm              |
| Code *                               [changed]|
| [1][2][3][4][5][6]   AppOtpField             |
| [ Turn on ]                                  |
|----------------------------------------------|
| Code or recovery code *              [changed]|
| [ ________________ ]  AppCodeField           |
| [ Regenerate codes ] / [ Turn off ]          |
+----------------------------------------------+
```

### Wireframe: Exam edition form, notice section
```text
+----------------------------------------------+
| Notice                                       |
| Notice reference        Notice URL [changed] |
| [ Edital 01/2026 ]      [ https://... ][open]|
+----------------------------------------------+
```

### Wireframe: Gallery, Text kinds
```text
+----------------------------------------------+
| Text kinds                              [new]|
| Text area: empty | filled | error | disabled |
| E-mail:    empty | filled | error | disabled |
| URL:       empty | valid+open | error        |
| Code:      empty | filled | upper-case       |
| One-time:  empty | filled | error | disabled |
| Search:    empty | typed + clear             |
+----------------------------------------------+
```

## Security
### Surfaces
- S1 Sign-in second step (`sign-in-code`) — anyone with a valid password challenge.
- S2 Security page code fields (`security-enrol-code`, `security-regenerate-code`, `security-disable-code`) — the signed-in account owner.
- S3 E-mail fields of sign-in, sign-up, password reset and verification pages — anonymous visitors.
### Threats
- T1 (S1, I) A one-time or recovery code leaks outside the field: logged, put in the URL or kept in browser storage by the new control → AC8 [sec]
- T2 (S1, S) The new code field weakens the second factor: a partial or malformed code is accepted, or the lockout after wrong codes stops counting → AC9 [sec]
- T3 (S3, S) Lower-casing the e-mail on the page signs a user into, or sends a reset link for, a different account than the one typed → AC10 [sec]
- T4 (S2, E) Swapping the fields lets regenerate or turn off run without a valid code → AC9 [sec]

## Docs
- flow: new docs/flows/text-kind-controls.md — the six controls, where each is used and the wireframes above.

## Acceptance criteria
- AC1 Given an `AppEmailField`, when the user types ` Ana@Exemplo.COM ` and leaves the field, then the value is `ana@exemplo.com`, and the input carries `type=email`, `inputmode=email` and the given `autocomplete`.
- AC2 Given an `AppOtpField`, when the user types letters or a 7th digit, then they are not taken; when the user pastes `123 456`, then the value is `123456`; the form is not sent until the button is pressed.
- AC3 Given an `AppCodeField`, when the user types `abcd efgh`, then the value is `abcdefgh`, shown in monospace with `spellcheck=false`.
- AC4 Given an `AppTextAreaField` with `MaxLength`, when the user types past `MinLines`, then it grows up to `MaxLines` and the counter shows the length and the limit.
- AC5 Given an `AppUrlField`, when the user types `exemplo.gov.br` and leaves, then the value is `https://exemplo.gov.br` and the open-link adornment appears with `target=_blank` and `rel="noopener noreferrer"`; given `ftp://x`, then the field shows the localized error and keeps the text.
- AC6 Given `AppDataTable`, when it renders its search, then it uses `AppSearchField` and the search, debounce and clear behave as before.
- AC7 Given each field of `Approved list` (lists 1 to 3), when its page renders, then it uses the control named there and the page still signs in, signs up, resets a password, verifies an e-mail, turns two-factor on and off, regenerates recovery codes and saves the catalog item and the question reason as before.
- AC8 [sec] (T1) Given a code typed or pasted in `AppOtpField` or `AppCodeField`, when the page sends it, then the value goes only in the request body of the existing call; the control writes nothing to logs, the URL or browser storage.
- AC9 [sec] (T2, T4) Given the sign-in second step and the Security page with the new fields, when a wrong, partial or malformed code is sent, then the Api refuses it as before and the lockout and the existing two-factor tests stay green.
- AC10 [sec] (T3) Given an account created as `Ana@Exemplo.com`, when the user signs in or asks a reset with `ANA@exemplo.com `, then it reaches that same account (Identity normalizes the e-mail; the page only trims and lower-cases).
- AC11 Given the gallery `/dev/ui`, when the section "Text kinds" opens, then each control shows empty, filled, focus, error, disabled, read-only and long-value states, in light and dark.
- AC12 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en) and the missing-key test is green.

## Criterion → test

Tests locked: 785b1b7d63a379e25de3bc296656b49a4206554f (4 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `AppEmailFieldTests.Leave_TrimsAndLowerCasesTheValue`, `Input_CarriesTheEmailAttributes`; `TextKindSignInTests.Email_IsTheEmailField` |
| AC2 | `AppOtpFieldTests.LettersAndASeventhDigit_AreNotTaken`, `PastedCode_FillsTheFieldWhole`, `Input_AsksForDigitsAndTheOneTimeCode`; `TextKindSignInTests.Code_Typed_IsNotSentUntilTheButton`, `Code_PastedWithASpace_IsSentWhole` |
| AC3 | `AppCodeFieldTests.Spaces_AreRemoved`, `UpperCase_On_RaisesTheLetters`, `Input_IsMonospaceWithNoSpellCheck` |
| AC4 | `AppTextAreaFieldTests.Renders_ATextAreaOfTheMinimumLines`, `Grows_UpToTheMaximumLines`, `Counter_IsAlwaysOn` |
| AC5 | `AppUrlFieldTests.Leave_TrimsAndAddsTheSchemeWhenMissing`, `ValidValue_ShowsTheOpenLinkInANewTab`, `OtherScheme_ShowsTheErrorAndKeepsTheText`, `PageError_WinsOverTheFieldsOwn`, `Input_AsksForAWebAddress`, `IsValid_TakesOnlyHttpAndHttps` |
| AC6 | `AppSearchFieldTests.Renders_AMagnifierAndAClearButton`, `DataTable_SearchesWithTheSearchField`; the existing `AppDataTable*Tests` |
| AC7 | `TextKindPageFieldsTests.Field_UsesTheControlOfItsKind` (18 fields), `SignInCode_IsAOneTimeCodeOrARecoveryCode`; `TextKindSignInTests.CodeStep_UsesTheOneTimeCodeField`, `RecoveryCodeMode_UsesTheCodeField`; `TextKindSecurityPageTests.*`; the existing page tests of each screen (Identity, Catalog, QuestionBank, Dev) |
| AC8 | `TextKindSignInTests.Code_StaysOutOfTheAddressAndTheBrowserStorage`, `RecoveryCode_StaysOutOfTheAddressAndTheBrowserStorage` |
| AC9 | existing: `SignInTotpTests.Code_Wrong_GoesBackToThePasswordWithTheReason`, `SecurityPageTests.Confirm_WrongCode_ShowsItOnTheField`, `TotpTests.CodeStep_FiveWrongCodes_LockTheAccountAndRefuseTheRightCodeWithTheSecondsLeft`, `TotpTests.Confirm_WrongCode_RefusesKeepsItOffAndCountsAFailure` (the Api does not change) |
| AC10 | `TextKindSignInTests.Email_MixedCaseWithSpaces_IsKeptTrimmedAndLowerCase`; the lookup normalizes the e-mail (`UserDirectory`, `normalizer.NormalizeEmail`) |
| AC11 | `UiGalleryTextKindsTests.*` |
| AC12 | existing `ResourceParityTests` |

## Approved list
### 1. Code fields
1. `sign-in-code` (`SignIn`, code mode) → `AppOtpField`
2. `sign-in-code` (`SignIn`, recovery code mode) → `AppCodeField`
3. `security-enrol-code` (`Security`) → `AppOtpField`
4. `security-regenerate-code` (`Security`, code or recovery code) → `AppCodeField`
5. `security-disable-code` (`Security`, code or recovery code) → `AppCodeField`

### 2. E-mail and URL fields
1. `SignIn`, `SignUp`, `ForgotPassword`, `ResetPassword`, `CheckEmail`, `VerifyEmail` e-mail fields → `AppEmailField` (`Autocomplete="username"`)
2. `edition-notice-url` (`ExamEditionForm`) → `AppUrlField`
3. `issuing-authority-website` (`IssuingAuthorityDialog`) → `AppUrlField`
4. `organizer-website` (`OrganizerDialog`) → `AppUrlField`

### 3. Multi-line fields
1. `issuing-authority-description` (`IssuingAuthorityDialog`) → `AppTextAreaField`
2. `organizer-description` (`OrganizerDialog`) → `AppTextAreaField`
3. `ai-prompt` (`AiDiagnostics`) → `AppTextAreaField`
4. Publish reason (`QuestionForm`, `PublishReasonId`) → `AppTextAreaField`
5. Change reason (`QuestionForm`, `ChangeReasonId`) → `AppTextAreaField`
6. `question-status-reason` (`QuestionStatusReasonDialog`) → `AppTextAreaField`

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-28 — No similar item (229 items read); F-133 (identity screens composition) takes the new controls as they come — epic decision of 2026-10-27 — confirmed: docs/epics/data-type-controls.md
- 2026-10-28: Size L — owner chose "Keep whole" over the recommended "Split as proposed" (average 6.3 vs 6.8); kept. Size L judged, kept whole — asked
- 2026-10-28 — Fields that take a recovery code, or a code or recovery code, use `AppCodeField`; `AppOtpField` only where a TOTP code is the one answer — a recovery code is not 6 digits; the flows do not change — asked
- 2026-10-28 — The three multi-line fields outside the summary (`QuestionForm` ×2, `QuestionStatusReasonDialog`) migrate too — one control per data type on every screen — asked
- 2026-10-28 — The two website fields (`IssuingAuthorityDialog`, `OrganizerDialog`) migrate to `AppUrlField` — same principle as the answer above (one control per data type); found after the round — confirmed: answer on the multi-line fields
- 2026-10-28 — E-mail trimmed and lower-cased on leave; `Autocomplete` required — confirmed: docs/design/data-type-controls.md (2.1, 3)
- 2026-10-28 — One-time code is sent only by the button, no auto-submit — keeps today's behavior and avoids a double send — assumed: technical (docs/design/data-type-controls.md 2.1, "auto-submit optional")
- 2026-10-28 — `AppSearchField` is rendered by `AppDataTable`; no page has a search outside a table — one look for every search — assumed: layout-in-kit (code read)
- 2026-10-28 — The notice reference stays `AppTextField`: it holds spaces ("Edital 01/2026"), which `AppCodeField` removes — scope unchanged — assumed: scope (summary)
- 2026-10-28 — `AppTextAreaField` wraps the auto-grow `AppTextField` already has (F-33) rather than a new input — one place for the field look — technical
- 2026-10-28 — Screens whose look does not change (a field swap with the same look) get no wireframe; the four that change are drawn — technical
- 2026-10-28 — No new package — the controls are built on MudBlazor already referenced — technical
- 2026-10-28 — Two tests of the test phase dropped before the lock (`Autocomplete_HasNoDefault`, `MaxLength_HasNoDefault`): they passed on the skeleton; the compiler's RZ2012 warning, which the gate refuses, already enforces `[EditorRequired]` — technical
- 2026-10-28 — The `ai-prompt` text area counts to 4000 characters (`PromptMaxLength` on the page); the Api sets no limit on this development page — `AppTextAreaField` needs a limit for its counter — technical
- 2026-10-28 — Existing tests changed for the approved behavior: `ExamEditionFormTests`, `OrganizersPageTests` and `IssuingAuthoritiesPageTests` now type an address with another scheme, since one with no scheme is completed with `https://` (BR6); the AI diagnostics tests type with `oninput`, since the text area's counter follows the typing (BR5); `UsersPageTests` finds the table box by one of its classes — technical
- 2026-10-28 — `AppTextField` gains `Class`, `InputMode`, `Pattern`, `InputAttributes`, `MaxLines` and `EndContent`; MudBlazor writes `inputmode` and `pattern` itself, so they are its parameters, not raw attributes — technical
- 2026-10-28: plan — 1 bUnit tests of the six controls and of the page fields → fail, locked; 2 `AppTextField` takes extra input attributes and end content, `AppTextAreaField`, `AppEmailField`, `AppCodeField` → their tests green; 3 `AppOtpField`, `AppUrlField`, `AppSearchField` inside `AppDataTable` → their tests and the table tests green; 4 identity pages (6 e-mail, 5 code fields) → Identity page tests green; 5 catalog, question and AI pages (3 URL, 6 multi-line) → their page tests green; 6 gallery "Text kinds", resources in three locales, flow page → parity and techdocs green; 7 app host screen check, gate, scan, validation script → gate GREEN
- 2026-10-29: review #1 (gallery states) — confirmed, fixed: `AppOtpField` gained `ReadOnly`; the gallery shows read-only and long value for it, and long value for the search box. Error, disabled and read-only do not apply to the search box (it has no label, hint or error by BR7) — autopilot
- 2026-10-29: review #2 (search box and contract item 1) — confirmed, accepted: `AppSearchField` keeps its `aria-label` and has no visible label, hint or error; BR7 asks for the table's look, and a search is not a form field. Exempt from item 1 of `docs/design/data-type-controls.md` section 3 — assumed by autopilot
- 2026-10-29: review #3 and #4 (AC10 and AC9 tests) — confirmed, fixed: `TextKindApiReachTests` (Identity.Tests, new file, the lock stays untouched) signs in and asks a reset with the lower-cased address of an account created with capitals, and sends 3, 5, 7-digit and lettered codes to the code step: refused as `TotpCodeInvalid`, one failure counted (6 tests green)
- 2026-10-29: review #5 (normalize on leave, not on each key) — confirmed, kept as built: normalizing on each key would rewrite the value under the caret on every key in Blazor Server; the field cleans on leave and `MaxLength` limits the paste. Listed at stop 2 for the owner — assumed by autopilot
- 2026-10-29: review #6 (gallery state test is loose) and #7 (four copies of the http(s) check) — minor, not applied: #6 touches a locked file, #7 changes three pages beyond the item; both become ideas — autopilot
- 2026-10-29: review coverage — checked and fine: security (code kept in page state and request body, no logger, JS interop or storage in the new controls, `javascript:` refused, `rel="noopener noreferrer"`, lockout untouched), data (no migration), contracts (no route or API change), localization (22 keys in three locales), locks (diff of the 4 locked paths empty), rules (icons, `[EditorRequired]`, no Portuguese identifiers). Read: 40 changed files
- 2026-10-29: traps — T1 each wrapper re-declares the `AppTextField` parameters: a later parameter is copied five times; T2 `TotpLength` of the kit repeats `TotpDigits` of Identity: a change on one side cuts the code silently; T3 new accounts keep a lower-case e-mail and old ones the typed case: an exact match on `Email` (not `NormalizedEmail`) would split them

## Out of scope
- Screen composition of the identity and catalog forms: F-133, F-128, F-130.
- Numbers, choices, dates and reserve types: F-181, F-182, F-184, F-185, F-187, F-188.
- Phone and tax id: F-188.
- The build refusing `InputType.Email` / `InputType.Url` outside the kit: F-186.
- Any server-side change to e-mail, code or URL validation.

## Open questions
- (none)

## Change notes

## Validation script

### pt-BR
Necessário para validar: o app host local com o administrador da carga inicial `admin@simulab.local` (senha em `src/Hosts/Simulab.Api/appsettings.Development.json`, `Identity:SeedAdmin:Password`) e um app autenticador no seu celular (Google Authenticator, Authy ou similar) — você fornece. A conta com verificação em dois fatores é criada no passo 2.

#### Verificado pelo Claude
- Os 6 controles novos, os 18 campos das telas e a galeria: 2946 testes do projeto Web verdes na suíte completa, 79 deles nos controles, nas telas de campos e na galeria (em ca819b9).
- AC9 e AC10 na Api: código de 3, 5 e 7 dígitos ou com letra é recusado como `TotpCodeInvalid` e conta uma falha; entrar e pedir redefinição com o e-mail em minúsculas chega à conta criada com maiúsculas; 6 testes de `TextKindApiReachTests` verdes (em ca819b9).
- O gate completo: `agile gate GREEN`, nenhum aviso novo, baseline com 0 entradas; Identity 589, Catalog 706, QuestionBank 857 e ExamEngine 99 testes verdes (em ca819b9).
- O scan de segurança: `agile scan GREEN` (16 falsos positivos, 13 corrigidos, 1 fechado pelo Claude) (em ca819b9).
- A revisão independente: 7 achados, 3 corrigidos, 1 aceito com decisão, 1 mantido (normaliza ao sair do campo) e 2 viram ideias; veja `## Decisions` (em ca819b9).
- A conferência de tela da galeria e das páginas de entrada feita na construção, que achou e corrigiu a fonte monoespaçada dos campos de código (em 6b14a4e).

#### Só você
1. Passo 1 de 6 (cerca de 4 min): suba o app no banco próprio do item. Feche antes o Visual Studio ou qualquer app host rodando desta pasta.
Git Bash:
```bash
cd /d/wt/simulab/f-183-text-kind-controls && Database__Name=simulab_f183 dotnet run --project src/Hosts/Simulab.AppHost
```
PowerShell 7:
```powershell
cd D:\wt\simulab\f-183-text-kind-controls; $env:Database__Name = 'simulab_f183'; dotnet run --project src/Hosts/Simulab.AppHost
```
Esperado: o endereço do painel do Aspire aparece e o Web responde em https://localhost:7125.

2. Passo 2 de 6 (cerca de 6 min): crie a conta e confira o e-mail limpo.
   - [ ] 2.1 Abra https://localhost:7125/sign-up e, no campo de e-mail, digite ` Ana.F183@Exemplo.COM ` (com espaços e maiúsculas) e saia do campo.
   Esperado: o campo mostra `ana.f183@exemplo.com`, sem espaços.
   - [ ] 2.2 Conclua o cadastro, abra o Mailpit pelo painel do Aspire e clique no link de verificação.
   Esperado: a página de verificação confirma o e-mail.
   - [ ] 2.3 Em https://localhost:7125/sign-in entre com `ANA.F183@exemplo.com ` (maiúsculas e um espaço no fim) e a senha.
   Esperado: o campo limpa o e-mail e a entrada funciona, na mesma conta.

3. Passo 3 de 6 (cerca de 6 min): ligue a verificação em dois fatores com o campo de código.
   - [ ] 3.1 No menu do usuário (canto superior direito da barra superior) abra Conta e clique no link "Segurança".
   Esperado: aparecem o QR code e o campo "Código" de seis dígitos.
   - [ ] 3.2 Leia o QR code no app autenticador, digite no campo `12a3456` e saia do campo.
   Esperado: ficam só dígitos, no máximo seis (`123456`), nunca letras; nada é enviado sozinho ao chegar no sexto dígito.
   - [ ] 3.3 Apague, digite o código real do app e clique em "Ativar".
   Esperado: os dois fatores ligam e a lista de códigos de recuperação aparece; guarde um deles.

4. Passo 4 de 6 (cerca de 6 min): entre com os dois fatores.
   - [ ] 4.1 Saia da conta e entre de novo no /sign-in com e-mail e senha.
   Esperado: aparece o segundo passo com o campo "Código de autenticação".
   - [ ] 4.2 Cole `123 456` (com espaço) no campo.
   Esperado: o campo fica com `123456`; o formulário só é enviado pelo botão Entrar, e um código errado volta ao passo da senha com o aviso.
   - [ ] 4.3 Clique em "Usar um código de recuperação" e digite o código guardado com um espaço no meio.
   Esperado: o campo está em letra monoespaçada, o espaço some ao sair do campo e a entrada funciona.
   - [ ] 4.4 Em Segurança, no campo "Código ou código de recuperação", digite um código inválido e clique em "Gerar novos códigos".
   Esperado: o aviso aparece no campo e o texto digitado continua lá.

5. Passo 5 de 6 (cerca de 6 min): endereço e texto longo no catálogo, como `admin@simulab.local`.
   - [ ] 5.1 No menu lateral esquerdo abra Conteúdo > Órgãos emissores > Adicionar; em "Site oficial" digite `exemplo.gov.br` e saia do campo.
   Esperado: o campo mostra `https://exemplo.gov.br` e um botão de abrir o link aparece na ponta direita; clicar abre uma nova aba.
   - [ ] 5.2 Troque o endereço por `ftp://x`.
   Esperado: o erro de endereço aparece e o texto continua no campo.
   - [ ] 5.3 No campo "Descrição" digite várias linhas.
   Esperado: o campo cresce até 10 linhas e depois rola; o contador abaixo mostra o tamanho e o limite.
   - [ ] 5.4 Repita 5.1 em Conteúdo > Organizadores > Adicionar e, no formulário de uma edição, no campo do endereço do edital (veja o mapa em `docs/agile/ui-map.md`).
   Esperado: o mesmo comportamento.

6. Passo 6 de 6 (cerca de 5 min): galeria, idiomas e teclado.
   - [ ] 6.1 No menu lateral esquerdo abra Desenvolvimento > UI kit e role até "Text kinds".
   Esperado: cada controle aparece em vazio, preenchido, erro, desabilitado, somente leitura e valor longo (a busca só em vazio, preenchido e valor longo), nos temas claro e escuro (botão de tema da barra superior).
   - [ ] 6.2 Troque o idioma pelo globo da barra superior para English e Português (Portugal).
   Esperado: rótulos, dicas e erros dos controles mudam, sem chave crua.
   - [ ] 6.3 Só com o teclado, refaça a entrada do passo 4 (Tab, digitar, Enter no botão).
   Esperado: o foco anda na ordem da tela e fica visível.

## Events

- 2026-10-28 question — asked 3, from context 5
- 2026-10-28 override — Size: kept whole over split (6.3 vs 6.8)
- 2026-10-28 test-fail — 1 new warning(s): MSB3061 in C:/Program Files/dotnet/sdk/10.0.403/Microsoft.Common.CurrentVersion.targets, 1 red
- 2026-10-28 stop — fresh context before review
- 2026-10-29 rework — review #1: gallery states of OTP and search
- 2026-10-29 rework — review #3/#4: AC10 and AC9 API tests
- 2026-10-29 stop — stop 2 (validation)

## Delivery
- Branch: feature/F-183
- Merge: a2b47e40 (--no-ff, pushed to origin/main, board #253)
- Tests: full suite GREEN (ship --fresh): Web 2946, Identity 589 (6 new), Catalog 706, QuestionBank 857, ExamEngine 99, Api 83, AppHost 122; scan GREEN; app version 1.28.0
- Manual pages: docs/manual/pt-BR/two-factor.md, sign-in-and-sign-out.md, issuing-authorities.md, organizers.md (pt-PT and en are frozen)
- Validation evidence: the owner answered "Validado: merge F-183" in chat; the local tick store was empty (0 of 17 recorded) and the file had boxes 2.1 to 4.2 ticked by hand (reset here, since ticks live outside the file); steps 4.3 to 6.3 not ticked, recorded as not tested; the owner's chat answer is the validation.
- Review follow-ups not built (ideas to register): the gallery state test is loose and its file is locked (review #6); four copies of the http(s) address check (review #7).
Gate time: 11 passes, 34 min 53 s — turn 6 (3 min 18 s), ship 2 (21 min 41 s), scan 3 (9 min 54 s); 1 red
Usage: 1455.3k weighted (estimate) — ship 31% · other 69% — agents 24% — context per turn 233k — 1 session, 72 turns
