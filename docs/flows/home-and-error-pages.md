---
title: Home, error and not-found pages
summary: What a visitor and a signed-in user see on Home, how the error and not-found pages share one kit shape (AppPageState), where the one amber action lives, and the two-card layout of the AI diagnostics page.
items: [F-134]
---
# Home, error and not-found pages

Technical terms: [glossary](../glossary.md)

## Trigger
A user opens `/`, an address that does not exist or a route the user may not open (`/not-found`), a page that raised an exception (`/Error`, re-executed by `UseExceptionHandler`), or `/dev/ai` in Development.

## Steps
1. `Home.razor` reads the authentication state. Signed out, it shows the title, the tagline, "Sign in" (primary, `/sign-in`), "Create account" (secondary, `/sign-up`) and the "being built" notice (`AppAlert`, info).
2. Signed in, it shows "Welcome back", the tagline, the notice and one `AppSectionCard` per section of `NavigationItems.Sections(...)`. The links come from `NavigationItems.Visible(All, isDevelopment, hasPermission)` with the permission claims `NavMenu` reads, so Home and the menu cannot disagree. Home itself is left out of the cards; a section with no visible link has no card; a user with no section sees the title, the tagline and the notice.
3. A server error is handled by `UseExceptionHandler("/Error")`. `Error.razor` renders `AppPageState` with the error icon, the title, the message, the request ID (`app-page-state-request-id`, mono font; only when `Activity.Current` or the `HttpContext` has one) and two actions: "Try again" (`AppActionButton`, the only amber action of the app's pages besides the question form) and "Go to home".
4. "Try again" calls `NavigationManager.Refresh(forceReload: true)`: the browser asks the server for the address the user was on. Under 360px of free width (`IBrowserViewportService`, measured once the circuit is connected) the button takes its `Compact` variant.
5. An address that matches nothing is answered by the router's `NotFoundPage` (`NotFound.razor`), and a route the user may not open by `AuthorizeRouteView`'s `NotAuthorized` fragment; both render `NotFoundContent`, so they are the same markup: an `AppPageState` with an icon, "Page not found", the message and one primary "Go to home". No amber action, no status number, no text about permission.
6. `/dev/ai` (Development only) shows two `AppSectionCard`: "Question" (the prompt and "Send", primary navy) and, after an answer, "Answer" (the text and a grid of model, tokens, cost and duration, the figures in the mono font). A failed call shows the error alert and no "Answer" card.

## Rules
- At most one amber action per screen, and only for a restart appeal: the error page has it; Home, not found and `/dev/ai` have none (`HomeAndErrorPagesScanTests`, `ActionColourScopeTests`).
- The not-found page serves the missing and the forbidden case alike (F-6 BR7); the cards on Home are comfort, the endpoints still enforce the permissions.
- `AppPageState` is a kit component shown in the gallery (`/dev/ui`, section `gallery-states`); a page picks the icon, the texts and the actions. The title is an `h1`, or an `h2` where the page already has its own (`TitleLevel`).
- The pages carry no literal colour; the rules written for them in `wwwroot/app.css` use theme variables only.

## Wireframe
Mockup: none (the wireframes below are the screen design of F-134).

### Home, signed out (`/`)
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

### Home, signed in (`/`)
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

### Error (`/Error`)
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

### Not found (`/not-found`, also a route without permission)
```text
+----------------------------------------------------------+
|                    (?)                          [changed]|
|                Page not found                            |
|     The page you are looking for does not exist.         |
|                                                          |
|                  [ Go to home ]                    [new] |
+----------------------------------------------------------+
```

### AI diagnostics (`/dev/ai`, Development only)
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

## Errors
- Unhandled exception — `UseExceptionHandler` re-executes `/Error`; the user sees the error page and quotes the request ID.
- Unknown address or route without permission — the same not-found page; the status code is the router's and does not change.
- `ai.*` codes on `/dev/ai` — shown as text through `ErrorText` in the alert above the cards (for example `ai.quota_exceeded`).
