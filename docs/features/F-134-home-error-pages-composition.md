---
feature: F-134
epic: Foundation and identity
status: refining
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
| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC2 | pending |
| AC3 | pending |
| AC4 | pending |
| AC5 | pending |
| AC6 | pending |
| AC7 | pending |
| AC8 | pending |
| AC9 | pending |
| AC10 | pending |
| AC11 | pending |
| AC12 | pending |
| AC13 | pending |
| AC14 | pending |
| AC15 | pending |

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

## Out of scope
- The gallery `/dev/ui` beyond the `AppPageState` example (F-125).
- The shell: app bar, menu, `#blazor-error-ui`, `ReconnectModal`.
- The sign-in, sign-up and legal pages (F-133).
- A student dashboard, study recommendations or "start a practice exam" on Home — no such feature exists yet; each arrives with its own item.
- Copying the request ID, an error report form, or a status code on the not-found page.

## Open questions
- (none)

## Events

- 2026-10-23 question — asked 6, from context 3
