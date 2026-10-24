---
feature: F-134
epic: Foundation and identity
status: validating
board: 185
version: 1
---
# Home, error and diagnostics pages in the new composition

Technical terms: [glossary](../glossary.md)

## Summary
The home page, the error and not-found pages and the dev page `Dev/AiDiagnostics.razor` take the composition of the Control Alt Systems identity (D-4). The gallery `/dev/ui` is part of F-125. The colours and controls come from F-125; this item decides each page's composition and its single orange action, if any.

## Start
- Depends on: F-125
- Palette note (2026-10-22): the identity moved to palette v2 (design system handoff H-04); F-148 (done) took it to the theme, so this item uses the new values and the amber action.
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, in both themes, at desktop and phone width, signed out and signed in; Docker Desktop running for the app host — owner
- Model: sonnet (no signal: no door, no public contract, no schema, size M)
- Suggested path: `/agile:build F-134`
- Parallel with: F-130, F-131, F-132, F-133 (different pages)

## Goal
Give the pages every visitor meets first or in trouble (home, error, not found) and the dev diagnostics page a real composition in the Control Alt Systems identity, with one shared shape for the error and not-found states, and decide where the single amber action lives.

## Users and use cases
- UC1 A visitor who is not signed in opens `/` and sees a welcome, the notice that Simulab is being built, and the actions "Sign in" and "Create account".
- UC2 A signed-in user opens `/` and sees a welcome and one card per menu section with the links to the areas the user may open.
- UC3 A user hits a server error and sees the error page with "Try again" (the amber action), "Go to home" and the request ID to quote to support.
- UC4 A user opens an address that does not exist, or a page the user has no permission for, and sees the same not-found page with "Go to home".
- UC5 A developer opens `/dev/ai` and sees the question and the answer as two cards, the answer with model, tokens, cost and duration.

## Business rules
- BR1 Home stays open to everyone (no `[Authorize]`). It reads the authentication state: signed out shows BR2, signed in shows BR3. Both show the "being built" notice (`AppAlert`, info) and no amber action.
- BR2 Signed out: "Sign in" (primary, navy) goes to `/sign-in`; "Create account" (secondary) goes to `/sign-up`.
- BR3 Signed in: a title "Welcome back" without the user's name, then one `AppSectionCard` per menu section that has at least one link for this user, in the order of `NavigationSection`, each link with the icon and label the menu uses. The links come from `NavigationItems.Visible` (Home left out) with the same permission claims as `NavMenu`, so the Development section shows only in Development. A user with no section sees only the title and the notice. The cards are comfort, not security: the endpoints still enforce the permissions.
- BR4 The error page (`/Error`, raised by `UseExceptionHandler`) shows the error icon, the title, the message, the request ID when there is one (in the mono font), and two actions: "Try again" is the amber action (`AppActionButton`) and reloads the address the user was on; "Go to home" is secondary and goes to `/`. At 360px of free width the amber action takes its compact variant.
- BR5 The not-found page (`/not-found`) and the view of a route the user is not authorized for are the same component (`NotFoundContent`) and look the same (confirmed from F-6 BR7): an icon, "Page not found", the message and one primary action "Go to home". No amber action, no "404" number, and no text that says permission.
- BR6 The error and not-found pages share one kit component, `AppPageState` (icon, title, message, an actions slot), defined once in the kit and shown in the gallery `/dev/ui` section `gallery-states`. The page picks the icon, the texts and the actions.
- BR7 `/dev/ai` keeps its behavior (Development only, a call charged to a user, errors from `ErrorText`) and is composed as two `AppSectionCard`: "Question" (the field and "Send", primary navy, no amber) and, after an answer, "Answer" with the text and a grid of model, tokens, cost and duration, the numbers in the mono font.
- BR8 At most one amber action per screen: only the error page has one. No other page of this item uses `AppActionButton`.
- BR9 No route, status code, exception handler, permission, API call or layout changes. The banner `#blazor-error-ui` and the shell stay as they are.
- BR10 Only kit components and `AppIcons` are used: no new package, image or illustration.
- BR11 Every text is a resource key, in every locale of `UI locales:`.

## Screens and API
- `/` (`Home.razor`), `/Error` (`Error.razor`), `/not-found` (`NotFound.razor` and `NotFoundContent.razor`), `/dev/ai` (`Dev/AiDiagnostics.razor`) change composition; `Ui/AppPageState.razor` is new; `/dev/ui` gains the `AppPageState` example. No endpoint and no error code change.
- Files: the four pages above, `Ui/AppPageState.razor`, `Pages/Dev/UiGallery.razor`, `wwwroot/app.css` (the shape of `AppPageState` and the card grid of Home, from theme variables only), the resource files `SharedResources*.resx`.

### Wireframe: Home, signed out (`/`)
```text
+----------------------------------------------------------+
| Practice like it is exam day                    [changed]|
| Realistic practice exams, and a coach that tells you     |
| what to study next.                                      |
|                                                          |
| [ Sign in ]  [ Create account ]                    [new] |
|                                                          |
| (i) Simulab is being built. Features arrive here one at  |
|     a time.                                              |
+----------------------------------------------------------+
```

### Wireframe: Home, signed in (`/`)
```text
+----------------------------------------------------------+
| Welcome back                                    [changed]|
| Realistic practice exams, and a coach that tells you     |
| what to study next.                                      |
|                                                          |
| (i) Simulab is being built. Features arrive here one at  |
|     a time.                                              |
|                                                          |
| +-----------------------+ +-----------------------+      |
| | [] Study              | | [] Content            | [new]|
| |  (o) Catalog          | |  (o) Exams            |      |
| |                       | |  (o) Questions        |      |
| +-----------------------+ +-----------------------+      |
| +-----------------------+  a card per section the user   |
| | [] Administration     |  may open, in menu order       |
| |  (o) Users            |                                |
| +-----------------------+                                |
+----------------------------------------------------------+
```

### Wireframe: Error (`/Error`)
```text
+----------------------------------------------------------+
|                    (!)                          [changed]|
|            Something went wrong                          |
|  An error occurred while processing your request.        |
|                                                          |
|  [Ctrl]+[Alt]+[ (R) Try again ]   [ Go to home ]   [new] |
|                                                          |
|  Request ID: 00-4bf92f3577b34da6a3ce929d0e0e4736-...     |
|  (mono font)                                             |
+----------------------------------------------------------+
```

### Wireframe: Not found (`/not-found`, also a route without permission)
```text
+----------------------------------------------------------+
|                    (?)                          [changed]|
|                Page not found                            |
|     The page you are looking for does not exist.         |
|                                                          |
|                  [ Go to home ]                    [new] |
+----------------------------------------------------------+
```

### Wireframe: AI diagnostics (`/dev/ai`, Development only)
```text
+----------------------------------------------------------+
| Dev > AI diagnostics                                     |
| +------------------------------------------------------+ |
| | [] Question                                 [changed]| |
| |  Prompt ______________________________________       | |
| |  [ Send ]                                            | |
| +------------------------------------------------------+ |
| +------------------------------------------------------+ |
| | [] Answer (after a call)                       [new] | |
| |  The text of the answer ...                          | |
| |  Model        Tokens in/out     Cost       Duration  | |
| |  claude-...   12 / 40           0.000123   850 ms    | |
| +------------------------------------------------------+ |
+----------------------------------------------------------+
```

## Docs
- flow: new docs/flows/home-and-error-pages.md — the flow of the four pages: who sees what on Home, the error and not-found states, and the `AppPageState` shape.

## Acceptance criteria
- AC1 Given a visitor who is not signed in, when `/` renders, then the page shows the title, the tagline, the "being built" notice, a "Sign in" link to `/sign-in` and a "Create account" link to `/sign-up`, and no section card and no amber action (BR1, BR2, BR8).
- AC2 Given a user signed in with the permission to browse the catalog and no other, when `/` renders, then one card "Study" holds one link to `/catalog` with its icon, there is no "Content" or "Administration" card, and there is no "Sign in" link (BR1, BR3).
- AC3 Given a user signed in with administration permissions, when `/` renders, then the cards follow the section order of the menu and every link also appears in the menu for that user, and in Production no link of the Development section appears (BR3).
- AC4 Given a user signed in with no permission at all, when `/` renders, then the page shows only the title, the tagline and the notice (BR3).
- AC5 Given a failed request handled by `UseExceptionHandler`, when `/Error` renders, then it shows the error icon, "Something went wrong", the message, the request ID in the mono font when there is one, "Try again" as the amber action and a secondary link "Go to home" to `/` (BR4, BR6).
- AC6 Given the error page, when "Try again" is pressed, then the browser reloads the address the user was on; and when no request ID exists, then no request ID line renders (BR4).
- AC7 Given a free width under 360px, when the error page renders, then the amber action is the compact variant and the page has no horizontal scroll (BR4).
- AC8 Given an address that does not exist, when the router answers, then `/not-found` shows "Page not found", the message and one primary "Go to home", and no amber action and no "404" (BR5, BR8).
- AC9 Given a route the signed-in user has no permission for, when the router answers, then the markup is the same as AC8's, so the two cases cannot be told apart (BR5).
- AC10 Given the gallery `/dev/ui`, when it opens, then `gallery-states` shows an `AppPageState` with an icon, a title, a message and two actions (BR6).
- AC11 Given `/dev/ai` in Development and a successful call, when the answer arrives, then a "Question" card and an "Answer" card render, the answer holds the text, model, tokens, cost and duration, and the numbers use the mono font; given a failed call, then the error alert shows and no "Answer" card renders (BR7).
- AC12 Given `/dev/ai` outside Development, when it is requested, then the response is the not-found page as before (BR9).
- AC13 Given the pages of this item, when they are scanned, then only `Error.razor` uses `AppActionButton`, and no page or stylesheet carries a literal colour (BR8, BR10).
- AC14 Given the pages of this item in the light and dark themes, when text and non-text pairs are measured, then they keep 4.5:1 and 3:1 (inherited from the theme; the page adds no colour) (BR10).
- AC15 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en) and the missing-key test is green (BR11).

## Criterion → test

Tests locked: bb041fe1dd945465edd48df969e3240314ad9b8e (6 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `HomeCompositionTests.Visitor_SeesTheWelcomeTheNoticeAndTheTwoWaysIn_AndNoCardAndNoAmberAction`, `Visitor_SignInIsThePrimaryActionAndCreateAccountIsSecondary` |
| AC2 | `HomeCompositionTests.SignedInWithOnlyTheCatalogPermission_SeesOneStudyCardWithOneLink_AndNoSignInLink` |
| AC3 | `HomeCompositionTests.SignedInAsAdministrator_SeesTheCardsInMenuOrder_WithTheLinksTheMenuShows`, `SignedInAsAdministratorInProduction_SeesNoDevelopmentLink` |
| AC4 | `HomeCompositionTests.SignedInWithNoPermission_SeesOnlyTheTitleTheTaglineAndTheNotice` |
| AC5 | `ErrorPageTests.Error_ShowsTheIconTheTitleTheMessageAndTheRequestId`, `Error_HasTheAmberTryAgainActionAndASecondaryGoToHomeLink` |
| AC6 | `ErrorPageTests.TryAgain_ReloadsTheAddressTheUserWasOn`, `Error_WithoutARequestId_ShowsNoRequestIdLine` |
| AC7 | `ErrorPageTests.Error_OnAViewportUnder360_TakesTheCompactActionAndWrapsItsActions`, `Error_OnAWiderViewport_TakesTheFullAction` |
| AC8 | `NotFoundPageTests.NotFound_ShowsTheIconTheTitleTheMessageAndOnlyGoToHome` |
| AC9 | `NotFoundPageTests.RouteWithoutPermission_RendersTheSameMarkupAsAPageThatDoesNotExist`, `Router_AnswersARouteTheUserIsNotAuthorizedForWithTheNotFoundContent` |
| AC10 | `AppPageStateTests` (4 tests), `UiGalleryPageStateTests.Gallery_StatesSection_ShowsAPageStateWithIconTitleMessageAndTwoActions` |
| AC11 | `AiDiagnosticsCardsTests` (3 tests) |
| AC12 | existing `AiDiagnosticsPageTests.Page_OutsideDevelopment_RendersNothing`, `DevPagesHostTests.Get_AiDiagnosticsOutsideDevelopment_Returns404` |
| AC13 | `HomeAndErrorPagesScanTests.OnlyTheErrorPage_UsesTheAmberAction`, `PagesAndTheirStylesheetRules_CarryNoLiteralColour`, existing `ActionColourScopeTests.RoutedPages_PutNoTertiaryColourOnAButtonAndNoActionShortcut` |
| AC14 | `HomeAndErrorPagesScanTests.PagesAndTheirStylesheetRules_CarryNoLiteralColour` (the pages add no colour) with the existing `ThemeContrastTests` |
| AC15 | `HomeAndErrorPagesScanTests.TheNewTexts_ExistInEveryLocale`, `TheNewTexts_AreTranslatedNotCopiedIntoPortuguese`, existing `ResourceParityTests.Every_key_exists_in_every_language` |

## Decisions
- 2026-10-23 — Unauthorized and not found look the same, with no "404" number — F-6 BR7 (the `Routes.razor` comment), still true — confirmed: src/Hosts/Simulab.Web/Components/Routes.razor
- 2026-10-23 — One amber action at most per screen, only for a "restart" appeal; each screen of this item decides — F-148 BR7 and the button README of the design system, still true — confirmed: docs/features/F-148-control-alt-palette-v2.md
- 2026-10-23 — Kit components and `AppIcons` only: no new package, image or illustration — ui-project.md, still true — confirmed: .claude/rules/agile/ui-project.md
- 2026-10-23 — Home shows a welcome plus actions: "Sign in" and "Create account" for a visitor, section cards of the areas a user may open for a signed-in user, the notice kept — it uses only what exists, and the page stops being a dead end — asked
- 2026-10-23 — No amber action on Home — signing in restarts nothing, and the restart icon would mislead — asked
- 2026-10-23 — The error page has the amber "Try again" plus a secondary "Go to home" — the one real restart appeal of the item — asked
- 2026-10-23 — The not-found page has only "Go to home" (primary), no amber, no 404 — nothing restarts, and the page also serves the forbidden case — asked
- 2026-10-23 — `/dev/ai` becomes two cards with the metrics in a mono-font grid, "Send" stays primary — the design system's amber is for a restart, which Send is not — asked
- 2026-10-23 — The banner `#blazor-error-ui` stays in the layouts, outside this item — it belongs to the shell, not to a page — asked
- 2026-10-23 — Home does not print the user's name in its title ("Welcome back") — the menu already shows the account, and a name or e-mail in a heading adds nothing — Claude
- 2026-10-23 — A new kit component `AppPageState` serves the error and not-found pages — ui.md says a pattern is defined once, and the two pages have the same shape — Claude
- 2026-10-23 — Try again reloads the address (`NavigationManager.Refresh(forceReload: true)`) — the handler re-executes `/Error` and the original URL stays in the bar — Claude
- 2026-10-23 — Home cards reuse the registry `NavigationItems` and `AppSectionCard`, so Home and the menu cannot disagree — Claude
- 2026-10-23 — No wireframe-only mockup file: the screens use kit components only, so the text wireframes decide them; `/agile:screen F-134` is still available before approval — Claude

- 2026-10-23: plan — 1 `AppPageState` kit component + gallery example + css → AC10; 2 not-found (`NotFoundContent`) on it → AC8, AC9; 3 error page on it: amber "Try again", "Go to home", request ID, compact under 360px → AC5, AC6, AC7; 4 Home signed out (welcome, notice, sign-in and create-account) → AC1; 5 Home signed in (section cards from `NavigationItems`) → AC2, AC3, AC4; 6 `/dev/ai` as two cards → AC11, AC12; 7 scans (one amber action, no literal colour, contrast inherited) and the three locales → AC13, AC14, AC15; 8 flow page `docs/flows/home-and-error-pages.md` → `techdocs.js check`
- 2026-10-23: the pre-existing `ActionColourScopeTests.AllowedPages` and the F-126 scan `QuestionFormActionScopeTests.OtherRoutedPages_...` get `Error.razor` (their messages say each screen item adds its own) — Claude
- 2026-10-23: tests unlocked (the AC2 and AC4 tests ran in Development, where the menu shows its Development section to everyone, and the AC9 test compared markup with per-render Blazor event ids; the code was right, the tests changed) — owner chose "Destravar e corrigir"

- 2026-10-23: Reuse: `AppSectionCard`, `AppAlert`, `AppActionButton`, `AppLink`, `AppIcons` and `NavigationItems.Visible/Sections` (Home and the menu share one registry); the viewport subscription of `MainLayout` (`IBrowserViewportService`) for the compact amber key; `ActionColourScopeTests`' scan helpers; no new package
- 2026-10-23: Shortcuts: none
- 2026-10-24: change from the owner's validation — the Development section (`/dev/ui`, `/dev/ai`) is for the administrator only: both registry items now require `IdentityPermissions.RolesManage` besides `DevelopmentOnly`, so a student or a visitor sees it neither on Home nor in the menu (before, any account saw it in Development). The menu changes with Home because both read `NavigationItems` (AC3). The page routes are not guarded by this (a student who types `/dev/ui` in Development still opens it). New tests `DevelopmentSectionAdminOnlyTests` (6); `NavMenuTests`, `MainLayoutTests`, `ShellHostTests` and `NavigationItemsTests` updated to sign an administrator in; `Simulab.Web.Tests` 2525 passed in 39 s, gate GREEN, architecture tests 301 of 301
- 2026-10-23: Build checks — gate GREEN (build of `Simulab.Web` and `Simulab.Web.Tests`, no new warning), `Simulab.Web.Tests` 2518 passed in 1 m 11 s, `agile scan GREEN` (two medium findings in `tools/Simulab.DocGen`, older than this item), `techdocs.js check F-134` GREEN; screen check of `/`, `/Error` (desktop and 320px), `/nothing-here` and `/dev/ai` signed out in the app host (own database `simulab_f134`); the signed-in flows are in the validation script. Not run: `/agile:review` (no auth, data, contract or migration change)
- 2026-10-23: `Simulab.ArchitectureTests` failed once (`AspirePackagesTests.MessagePack_ResolvesToPatchedVersion_InEveryProject`, no resolved package) while only the Web projects were built in this worktree; after `dotnet build Simulab.slnx` (0 warnings, 0 errors) it passes: 301 of 301

## Out of scope
- The gallery `/dev/ui` beyond the `AppPageState` example (F-125).
- The shell: app bar, menu, `#blazor-error-ui`, `ReconnectModal`.
- The sign-in, sign-up and legal pages (F-133).
- A student dashboard, study recommendations or "start a practice exam" on Home — no such feature exists yet; each arrives with its own item.
- Copying the request ID, an error report form, or a status code on the not-found page.

## Open questions
- (none)

## Validation script
### English
Needed to validate: you, on screen, in both themes, at desktop and phone width, signed out and signed in; Docker Desktop running for the app host; one account with administration permissions (Admin) and one new account with no extra role. Docker Desktop was running during this build; the accounts are yours to provide (a new one is made in step 4).

1. Stop any app host you have open (a running one locks `bin/`), then start this item's app host from the worktree folder. It runs on its own database, `simulab_f134`, on the local PostgreSQL server; nothing lands in the shared `simulab` database. Wait until the Aspire dashboard address is printed, then open https://localhost:7125.

```powershell
cd D:\wt\simulab\f-134-home-error-pages
$env:Database__Name = "simulab_f134"; dotnet run --project src/Hosts/Simulab.AppHost
```

```bash
cd /d/wt/simulab/f-134-home-error-pages
Database__Name=simulab_f134 dotnet run --project src/Hosts/Simulab.AppHost
```

   Expected: a line `Dashboard: https://localhost:<port>/login?t=<token>`; https://localhost:7125 answers with the home page.
2. Home, signed out. Open https://localhost:7125 with no one signed in.
   - [ ] 2.1 Where: the page body. The title "Practice like it is exam day", the line under it, then two buttons: "Sign in" (filled navy) and "Create account" (outlined), then the blue notice "Simulab is being built...". No amber button and no cards.
   - [ ] 2.2 Where: the theme switch (the moon at the top right of the app bar, the bar at the top of the page). Click it: the title, the buttons and the notice stay readable in light and in dark. Leave it in the theme you prefer.
   - [ ] 2.3 Where: the language switch (the globe, top right of the app bar). Choose Português (Brasil), then Português (Portugal): the title, "Entrar" / "Iniciar sessão" and "Criar conta" change. Switch back to English.
3. Home, signed in as the Admin. Click "Sign in" and sign in with the Admin account.
   - [ ] 3.1 Where: the page body. The title is "Welcome back" (no name), the line and the notice stay, and the "Sign in" and "Create account" buttons are gone.
   - [ ] 3.2 Where: the cards under the notice, one per menu section in the order of the sidebar (the left menu): Study, Content, Administration and, because the app runs in Development, Development. Each link shows the same icon and label as the sidebar. Click one link of each card: it opens the page the sidebar item opens.
   - [ ] 3.3 Where: the cards at 1280px wide or narrower (press F12, Ctrl+Shift+M and pick 1024, then 375): the cards go from a grid to a single column and nothing scrolls sideways.
4. Home, signed in with a new account. Sign out (avatar at the top right, then "Sign out"), click "Create account", make a new account and confirm its email (the message arrives in Mailpit, whose address is on the Aspire dashboard), then sign in with it.
   - [ ] 4.1 Where: the cards on Home. Only the cards for what this account may open appear (for example only Study) and every link on them is also in its sidebar; the cards of the Admin are not here.
5. Not found, and a page you may not open. Signed in with the new account from step 4:
   - [ ] 5.1 Where: the address bar. Open https://localhost:7125/nothing-here: an icon, "Page not found", "The page you are looking for does not exist." and one filled navy button "Go to home". No amber button, no "404".
   - [ ] 5.2 Where: the address bar. Open https://localhost:7125/admin/roles (a page this account may not open): the page looks exactly like the one in 5.1, with no word about permission. Click "Go to home": Home opens.
6. The error page. Open https://localhost:7125/Error (the page every server error ends on; opened by hand it shows the same page).
   - [ ] 6.1 Where: the page body. The red error icon, "Something went wrong", the message, the two buttons and, under them, "Request ID: ..." in the mono font. "Try again" is the amber key with Ctrl and Alt before it; "Go to home" is outlined.
   - [ ] 6.2 Where: the amber key "Try again". Hold the mouse button down on it: it sinks and its icon turns. Click it: the page reloads and stays on the same address with a new request ID.
   - [ ] 6.3 Where: the window itself, made 320px wide (F12, Ctrl+Shift+M, width 320, then reload): the amber key is alone (no Ctrl and Alt), the two buttons sit one under the other, the long request ID breaks into lines and the page does not scroll sideways.
7. The AI diagnostics page and the gallery. In the sidebar under Development click "AI gateway".
   - [ ] 7.1 Where: the page body. One card "Question" with the prompt field and the filled navy "Send" button (not amber), and no "Answer" card. If your API key is configured, write a short prompt and click Send: an "Answer" card appears with the text and four figures (Model, Tokens in / out, Cost, Duration) in the mono font. Without a key the red alert above the cards explains it and no "Answer" card appears.
   - [ ] 7.2 Where: the sidebar, under Development, "UI kit", then the section "List states", under the three small states. A "Page state" with an icon, a title, a message and two buttons shows.
8. Keyboard and both themes, on Home signed in as the Admin.
   - [ ] 8.1 Where: the whole page, from the top with the Tab key. Focus goes through the app bar, the sidebar, the first card's links and then every other card's links, in reading order; each shows a blue focus ring and Enter opens the link.
   - [ ] 8.2 Where: Home, the error page and the not-found page, each in the light and in the dark theme: no text is hard to read and no colour looks out of the palette.

### pt-BR
Necessário para validar: você, na tela, nos dois temas, em largura de desktop e de celular, sem e com login; Docker Desktop em execução para o app host; uma conta com permissões de administração (Admin) e uma conta nova sem papel extra. O Docker Desktop estava em execução durante este build; as contas são suas (a nova é criada no passo 4).

1. Pare qualquer app host que esteja aberto (um em execução trava o `bin/`) e inicie o app host deste item pela pasta do worktree. Ele usa o próprio banco, `simulab_f134`, no servidor PostgreSQL local; nada vai para o banco compartilhado `simulab`. Espere o endereço do painel do Aspire aparecer e abra https://localhost:7125.

```powershell
cd D:\wt\simulab\f-134-home-error-pages
$env:Database__Name = "simulab_f134"; dotnet run --project src/Hosts/Simulab.AppHost
```

```bash
cd /d/wt/simulab/f-134-home-error-pages
Database__Name=simulab_f134 dotnet run --project src/Hosts/Simulab.AppHost
```

   Esperado: uma linha `Dashboard: https://localhost:<porta>/login?t=<token>`; https://localhost:7125 responde com a página inicial.
2. Home, sem login. Abra https://localhost:7125 sem ninguém logado.
   - [ ] 2.1 Onde: o corpo da página. O título "Practice like it is exam day", a linha abaixo, depois dois botões: "Sign in" (cheio, azul-marinho) e "Create account" (com contorno), depois o aviso azul "Simulab is being built...". Nenhum botão âmbar e nenhum cartão.
   - [ ] 2.2 Onde: o botão de tema (a lua no canto superior direito da barra superior, a faixa no alto da página). Clique: o título, os botões e o aviso continuam legíveis no claro e no escuro. Deixe no tema que preferir.
   - [ ] 2.3 Onde: o botão de idioma (o globo, canto superior direito da barra superior). Escolha Português (Brasil) e depois Português (Portugal): o título, "Entrar" / "Iniciar sessão" e "Criar conta" mudam. Volte para English.
3. Home, logado como Admin. Clique em "Sign in" e entre com a conta Admin.
   - [ ] 3.1 Onde: o corpo da página. O título é "Welcome back" (sem nome), a linha e o aviso continuam e os botões "Sign in" e "Create account" sumiram.
   - [ ] 3.2 Onde: os cartões abaixo do aviso, um por seção do menu na ordem da sidebar (o menu lateral esquerdo): Study, Content, Administration e, porque o app roda em Development, Development. Cada link mostra o mesmo ícone e o mesmo rótulo da sidebar. Clique em um link de cada cartão: abre a página que o item da sidebar abre.
   - [ ] 3.3 Onde: os cartões em 1280px de largura ou menos (F12, Ctrl+Shift+M, escolha 1024 e depois 375): os cartões passam de uma grade para uma coluna só e nada rola para o lado.
4. Home, logado com uma conta nova. Saia (avatar no canto superior direito, depois "Sign out"), clique em "Create account", crie uma conta nova e confirme o e-mail (a mensagem chega no Mailpit, cujo endereço está no painel do Aspire) e entre com ela.
   - [ ] 4.1 Onde: os cartões da Home. Só aparecem os cartões do que essa conta pode abrir (por exemplo só Study) e todo link neles também está na sidebar dela; os cartões do Admin não estão aqui.
5. Não encontrada, e uma página que você não pode abrir. Logado com a conta nova do passo 4:
   - [ ] 5.1 Onde: a barra de endereço. Abra https://localhost:7125/nothing-here: um ícone, "Page not found", "The page you are looking for does not exist." e um botão cheio azul-marinho "Go to home". Nenhum botão âmbar e nenhum "404".
   - [ ] 5.2 Onde: a barra de endereço. Abra https://localhost:7125/admin/roles (uma página que essa conta não pode abrir): a página é idêntica à de 5.1, sem nenhuma palavra sobre permissão. Clique em "Go to home": a Home abre.
6. A página de erro. Abra https://localhost:7125/Error (a página em que todo erro do servidor termina; aberta à mão mostra a mesma página).
   - [ ] 6.1 Onde: o corpo da página. O ícone vermelho de erro, "Something went wrong", a mensagem, os dois botões e, abaixo, "Request ID: ..." na fonte mono. "Try again" é a tecla âmbar com Ctrl e Alt antes dela; "Go to home" tem contorno.
   - [ ] 6.2 Onde: a tecla âmbar "Try again". Segure o botão do mouse sobre ela: afunda e o ícone gira. Clique: a página recarrega e continua no mesmo endereço, com um novo request ID.
   - [ ] 6.3 Onde: a própria janela, com 320px de largura (F12, Ctrl+Shift+M, largura 320 e recarregue): a tecla âmbar fica sozinha (sem Ctrl e Alt), os dois botões ficam um sob o outro, o request ID longo quebra em linhas e a página não rola para o lado.
7. A página de diagnóstico de IA e a galeria. Na sidebar, em Development, clique em "AI gateway".
   - [ ] 7.1 Onde: o corpo da página. Um cartão "Question" com o campo do prompt e o botão cheio azul-marinho "Send" (não âmbar), e nenhum cartão "Answer". Se sua chave de API estiver configurada, escreva um prompt curto e clique em Send: aparece um cartão "Answer" com o texto e quatro números (Model, Tokens in / out, Cost, Duration) na fonte mono. Sem chave, o alerta vermelho acima dos cartões explica e nenhum cartão "Answer" aparece.
   - [ ] 7.2 Onde: a sidebar, em Development, "UI kit", depois a seção "List states", abaixo dos três estados pequenos. Aparece um "Page state" com ícone, título, mensagem e dois botões.
8. Teclado e os dois temas, na Home logado como Admin.
   - [ ] 8.1 Onde: a página inteira, do topo com a tecla Tab. O foco passa pela barra superior, pela sidebar, pelos links do primeiro cartão e depois pelos de cada outro cartão, na ordem de leitura; cada um mostra um anel de foco azul e Enter abre o link.
   - [ ] 8.2 Onde: a Home, a página de erro e a de não encontrada, cada uma no tema claro e no escuro: nenhum texto fica difícil de ler e nenhuma cor parece fora da paleta.

## Events

- 2026-10-23 question — asked 6, from context 3
- 2026-10-23 blocked — test wrong: AC2 and AC4 tests ran in Development, where the Development section shows for everyone; AC9 compared markup with per-render Blazor event ids
- 2026-10-24 test-fail — 1 new warning(s): MSB3061 in C:/Program Files/dotnet/sdk/10.0.402/Microsoft.Common.CurrentVersion.targets, 1 red
