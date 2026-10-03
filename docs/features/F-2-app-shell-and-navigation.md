---
feature: F-2
epic: Foundation and identity
status: done
board: 706
version: 1
---
# App shell and navigation

Technical terms: [glossary](../glossary.md)

## Summary
Side navigation menu, app-wide light/dark switch (remembered per user later), a slot for the user menu and a skip-to-content link. Uses the UI kit from F-1 and the `ThemeState` it introduced.

## Goal
Every page lives in one shell that works the same on desktop and phone, in light and dark mode, by keyboard, and that later features only extend with menu items.

## What exists
- `MainLayout`: app bar with the app name and `LanguageSwitch`; no drawer, no menu, no skip link.
- `ThemeState` (F-1) drives `MudThemeProvider.IsDarkMode`; it resets to light on every full page load (language switch).
- Pages: `/` (Home) and `/dev/ui` (Development only). No authentication or permissions yet (F-5, F-6).
- Simulae (reference only): 3 menu sections and 30 hardcoded links, Filled icons, checks by role name, dark mode not stored. None of it is imported as is.

## Users and use cases
- UC1 A visitor on a desktop sees the app bar and an open side menu, and collapses the menu to icons to gain space; the choice is kept on the next visit.
- UC2 A visitor on a phone opens the menu from the app bar, picks an item, and the menu closes.
- UC3 A visitor switches between light and dark mode; the choice survives a reload and a language switch.
- UC4 A keyboard user jumps straight to the page content with the skip link.
- UC5 A developer (Claude) adds a menu item for a new screen by adding one entry to the navigation registry.

## Business rules
- BR1 App bar, left to right: menu button, "Simulab" (link to `/`), spacer, theme switch, language switch, user menu slot. The user menu slot is empty until F-5.
- BR2 Menu items come from one registry (`NavigationItems`): section, route, icon (`AppIcons`), resource key, match mode and an optional required permission.
- BR3 Items now: "Home" (`/`, exact match, no section). In the Development environment only, a "Development" section with "UI kit" (`/dev/ui`).
- BR4 Sections, in order: Study, Content, Administration, Development. A section with no visible items is not shown.
- BR5 An item with a required permission is hidden while there is no permission check (until F-6). Items never check role names.
- BR6 The current page's item is highlighted and marked `aria-current="page"`. Home matches only `/`; other items match their route prefix.
- BR7 Desktop (width ≥ `Md`): the menu is open by default and can be collapsed to icons only (mini). Collapsed items show their label as a tooltip. The state is kept in the cookie `simulab.nav` (`expanded` / `collapsed`, 1 year).
- BR8 Phone and tablet (width < `Md`): the menu is closed by default, opens over the content, and closes after navigating or on Esc / backdrop click.
- BR9 Theme: without a stored choice the app follows the system preference. The theme switch stores the choice in the cookie `simulab.theme` (`light` / `dark`, 1 year), without reloading the page, and the choice survives a language switch. F-8 moves it to the user profile.
- BR10 The theme switch shows the icon of the mode it switches to (moon in light mode, sun in dark mode), with a tooltip and accessible name.
- BR11 Skip link "Skip to main content" is the first focusable element, visible only when focused, and moves focus to the main content.
- BR12 The UI kit gallery drops its own theme switch; it uses the app bar switch (F-1 BR12 said the app-wide switch belongs to F-2).

## Screens and API
### Shell (every page except the future exam session layout)
- Layout (`MainLayout`):
  1. Skip link (`a.app-skip-link`, href `#main-content`).
  2. `MudAppBar`: `MudIconButton` menu (`AppIcons.Menu`), `MudLink` "Simulab" to `/`, `MudSpacer`, theme switch (`MudIconButton` in `MudTooltip`), `LanguageSwitch`, user menu slot (empty `div`).
  3. `MudDrawer` (`DrawerVariant.Mini` on desktop, `DrawerVariant.Temporary` below `Md`; `ClipMode.Always`) with `NavMenu`: `MudNavMenu` → Home `MudNavLink`, then per section a header (`MudText` overline) and its `MudNavLink` items; in mini mode section headers are hidden and a divider separates sections.
  4. `MudMainContent` → `main#main-content` (`tabindex="-1"`) → `MudContainer` → `@Body`.
- Components: `MudLayout`, `MudAppBar`, `MudDrawer`, `MudNavMenu`, `MudNavLink`, `MudTooltip`, `MudIconButton`; icons only through `AppIcons` (new: `Menu`, `Home`, `ChevronLeft`, `ChevronRight`, `Build`).
- Actions: open/close menu (menu button; on desktop it toggles expanded/mini), navigate, switch theme, switch language.
- Permissions: none yet (BR5).
- API: none. Error codes: none.

### States
| State | What shows |
|---|---|
| Desktop, menu expanded | Drawer 240 px with section headers and labels; menu button tooltip "Collapse menu" |
| Desktop, menu collapsed | Drawer 56 px with icons only; tooltips with labels; menu button tooltip "Expand menu" |
| Phone, menu closed | No drawer; content full width |
| Phone, menu open | Drawer over the content with backdrop; closes after navigating |
| Active item | Highlighted background, `aria-current="page"` |
| Light / dark | Theme from the cookie, otherwise from the system |
| Production | No "Development" section |
| Skip link focused | Visible at top left, above the app bar |
Loading, empty, saving, validation, permission-denied and server-error states do not apply: the shell has no data of its own.

### Texts (resource keys)
| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Layout.SkipToContent` | Skip to main content | Pular para o conteúdo | Saltar para o conteúdo |
| `Layout.HomeLink` | Simulab, home page | Simulab, página inicial | Simulab, página inicial |
| `Layout.Menu.Label` | Main menu | Menu principal | Menu principal |
| `Layout.Menu.Open` | Open menu | Abrir menu | Abrir menu |
| `Layout.Menu.Close` | Close menu | Fechar menu | Fechar menu |
| `Layout.Menu.Collapse` | Collapse menu | Recolher menu | Recolher menu |
| `Layout.Menu.Expand` | Expand menu | Expandir menu | Expandir menu |
| `Nav.Home` | Home | Início | Início |
| `Nav.Section.Study` | Study | Estudo | Estudo |
| `Nav.Section.Content` | Content | Conteúdo | Conteúdo |
| `Nav.Section.Administration` | Administration | Administração | Administração |
| `Nav.Section.Development` | Development | Desenvolvimento | Desenvolvimento |
| `Nav.Dev.UiKit` | UI kit | Kit de interface | Kit de interface |
Existing keys reused: `Common.ThemeSwitch.ToDark`, `Common.ThemeSwitch.ToLight`, `Layout.LanguageSwitch.Label`, `App.Name`.

### Accessibility
- Tab order: skip link, menu button, Simulab link, theme switch, language switch, user menu slot, menu items, page content.
- The drawer is a `nav` landmark labelled "Main menu"; the content is the `main` landmark.
- Icon-only buttons have tooltips and accessible names from resources; the menu button exposes `aria-expanded`.
- Mockup: `docs/features/mockups/F-2-app-shell-and-navigation.html`.

## Acceptance criteria
- AC1 Given any page, then the app bar shows, in order, the menu button, the "Simulab" link to `/`, the theme switch, the language switch and an empty user menu slot. (BR1)
- AC2 Given the Development environment, then the menu shows "Home" and a "Development" section with "UI kit"; given Production, then only "Home" and no "Development" section. (BR3, BR4)
- AC3 Given a registry section with no visible items, then its header is not rendered; given an item that requires a permission, then it is not rendered. (BR2, BR4, BR5)
- AC4 Given the current route is `/dev/ui`, then "UI kit" is highlighted with `aria-current="page"` and "Home" is not; given `/`, then only "Home" is. (BR6)
- AC5 Given a desktop width, when the menu button is pressed, then the menu switches between expanded and icons-only, the button's label changes between "Collapse menu" and "Expand menu", and the cookie `simulab.nav` stores the state; given the cookie `collapsed`, then the page renders the menu collapsed. (BR7)
- AC6 Given a width below `Md`, then the menu is closed; when opened and an item is chosen, then it closes. (BR8)
- AC7 Given no `simulab.theme` cookie, then the theme follows the system preference; given the cookie `dark`, then the page renders dark from the first request; when the switch is pressed, then the theme changes without a reload and the cookie is written. (BR9)
- AC8 Given dark mode, then the switch shows the sun icon with the name "Switch to light mode"; given light mode, the moon icon with "Switch to dark mode". (BR10)
- AC9 Given the page loads, then the first focusable element is the skip link pointing to `#main-content`, and `main#main-content` exists with `tabindex="-1"`. (BR11)
- AC10 Given the UI kit gallery, then it has no theme switch of its own. (BR12)
- AC11 All new texts exist in pt-BR, pt-PT and en, and the missing-key test is green.
- AC12 On screen: desktop and phone widths, light and dark, one language switch keeping the theme, and keyboard only (skip link, menu, switches) — validation script. (BR7, BR8, BR9, BR11)

## Decisions
- 2026-09-17 — Menu items: Home, plus a Development section in Development; sections Study, Content, Administration prepared and hidden while empty; desktop drawer collapsible to icons with the state in a cookie; phone drawer temporary; theme follows the system until chosen, stored in a cookie that survives the language switch; app bar order as in BR1 with an empty user slot; skip link; exam session layout out of scope; mockup before approval — owner answers to refinement questions 1-8.
- 2026-09-17 — Mockup approved: drawer in the theme's dark blue (same as the app bar), 240 px expanded / 56 px icons-only, section headers become dividers when collapsed, active item with highlighted background plus a left bar — owner, screen questions 1-2.
- 2026-09-17 — Drawer built with `MudDrawer` + `MudNavMenu`, responsive at MudBlazor's `Md` breakpoint — library components, no custom layout code.
- 2026-09-17 — One navigation registry (`NavigationItems`) with section, route, icon, resource key, match and optional permission — features add items without touching the layout; F-6 plugs the permission check in one place.
- 2026-09-17 — Cookies are read on the server at the first request (theme and drawer state render correctly without a flash) and written from the browser through a small JS interop call — no page reload when switching.
- 2026-09-17 — The app manual gets a short "Getting around" page (menu, theme, language) in the three languages when this feature ships — first end-user visible behavior.
- 2026-09-17 — Without a theme cookie, the server renders light and `MudThemeProvider` switches to the system preference after the first render — a short flash on the first visit only is accepted.
- 2026-09-17 — Build: one `MudDrawer` with `DrawerVariant.Mini`, which MudBlazor already turns into a temporary drawer below `Md`; the layout learns the breakpoint from `IBrowserViewportService`. Until it is known (first render), CSS hides the drawer on small screens so phones do not flash an open menu.
- 2026-09-17 — Build: `MudNavLink` renders a `div` instead of a link when it has `OnClick`, so the phone menu closes on `LocationChanged` in the layout instead.
- 2026-09-17 — Build: the skip link is handled by `wwwroot/js/shell.js` (capture-phase click, focus on `main`): with `<base href="/">` a plain `#main-content` link would navigate to `/`, and the script also works before the circuit starts. The same file writes the preference cookies.
- 2026-09-17 — Build: the drawer overrides MudBlazor's active link colour (primary blue on dark blue was unreadable) with the mockup's light background, white text and left bar.

## Out of scope
- User menu content, sign-in and sign-out (F-5).
- Permission checks on menu items (F-6).
- Storing the theme in the user profile (F-8).
- The distraction-free exam session layout (Exam Simulator epic).
- Menu items for screens that do not exist yet.

## Open questions
- (none)

## Change notes

## Validation script
Close any running app host first (it locks the build output).
1. Start `dotnet run --project src/Simulab.AppHost` and open the Web at https://localhost:7125 in a desktop-width window. No sign-in (none exists yet). Check the app bar order: menu button, "Simulab", theme switch, language switch; the menu shows "Home" and a "Development" section with "UI kit"; "Home" is highlighted.
2. Click "UI kit": the gallery opens, "UI kit" is highlighted, "Home" is not, and the gallery has no theme switch of its own.
3. Press the menu button: the menu collapses to icons (hover an icon: tooltip with its name; the button tooltip reads "Expand menu"). Reload the page (F5): it stays collapsed. Press it again to expand.
4. Press the theme switch (moon icon, "Switch to dark mode"): the app turns dark without reloading and the icon becomes a sun. Reload: still dark.
5. Switch the language to Português (Brasil): the page reloads in Portuguese, still dark, and the menu reads "Início", "Desenvolvimento", "Kit de interface".
6. Narrow the window below 960 px (or use the browser's phone view): the menu is hidden; open it with the menu button, choose "Início": the menu closes.
7. Keyboard only, after a reload at desktop width: the first Tab shows "Pular para o conteúdo" at the top left; Enter moves focus to the page content (the next Tab lands inside the page). Tab through the menu button, "Simulab", theme and language switches; Enter/Space work on each.
8. Clear the site cookies `simulab.theme` and `simulab.nav` (or use a private window) and set the OS to dark mode: the app follows it after loading. There is no permission check in this feature (menu items with permissions arrive with F-6).

## Delivery
- Branch: `feature/F-2` (deleted after merge)
- Merge: the `--no-ff` merge commit "Merge feature/F-2: app shell and navigation (AB#706)" on `main`, 2026-09-17
- Validated on screen by the owner: 2026-09-17
- Tests: full suite 124 passed, 0 failed (SharedKernel 8, Architecture 8, Api 4, Web 104), 4 s; full build 5 s, 0 warnings
- Manual pages: `getting-around` (new) and `index` in en, pt-BR and pt-PT
