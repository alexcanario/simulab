---
paths:
  - "**/*.razor"
  - "**/*.razor.cs"
  - "**/*.razor.css"
  - "**/Simulab.Web/**/*.cs"
---
# UI standards — Simulab choices

The core rule `ui.md` holds the standard. This file holds only what Simulab decided on top of it (owner, 2026-09-17).

- The UI kit lives in `Simulab.Web/Components/Ui/`; the gallery is the dev-only page `/dev/ui`. Both come with feature F-1, before any screen is imported from Simulae.
- Library: MudBlazor. `<MudTable` and `<MudDataGrid` appear only inside the kit; an architecture test enforces it.
- Icon family: Material **Outlined**. `Icons.Material.*` appears only inside `AppIcons`; an architecture test enforces it.
- Hover, focus and pressed states come from the kit and `SimulabTheme`. Snackbar position and duration are set once, in `Program.cs`.
- Complex entities, edited on their own page: question, exam, exam edition, import draft. Everything else starts as simple (dialog) until refinement says otherwise.
- Deleting is soft delete; the confirmation text says what happens to related data.
- Action vocabulary keys: `Common.Add`, `Common.Edit`, `Common.Delete`, `Common.Save`, `Common.Cancel`, `Common.Search`, in the shared resources, in the three languages.
- Long text in a table cell is truncated with a tooltip; sortable columns say so.
- Accessibility target is WCAG 2.2 AA; the exam timer is exposed accessibly without interrupting the candidate.
- Declared exception — exam session screens: distraction-free layout, no app menu, no breadcrumb; focus on the question, the timer and the answer sheet. Their patterns are kit components and are in the gallery; icons, feedback, vocabulary and accessibility rules still apply.
- Declared exception — sign-up, verification and legal pages use `AuthLayout`: no app menu, no breadcrumb, language and theme switches kept. Its field, alert and strength patterns are kit components in the gallery (F-4).
- A screen imported from Simulae is converted to the kit on the way in; it never enters with its original table, icons or edit pattern.
- In-page links (`href="#id"`) leave the page because of `<base href="/">`; handle them in `wwwroot/js/shell.js`, as the skip link does.
- A kit parameter whose right value depends on what the page means (autocomplete, input purpose, a destructive action's wording) has no default: it is `[EditorRequired]`, so a page that forgets it fails the build (B-5).
- What each field kind looks like (F-114): the look says the action, and the three kinds are in the gallery section "Field kinds". A list the page holds whole (a fixed list in code, or one database call of at most 100 items) is `AppSelectField`: arrow at the end, opens on focus, narrows by typing in memory, no clear button (its "all" or "none" is the first option). A list that needs typing first and is searched on the server is `AppLookupField`: magnifier at the start, minimum of characters, clear button. Free text is `AppTextField`: no arrow, no magnifier. `LeadingIcon` is decorative on a select and a text field and ignored on a lookup. Never put an arrow on a field that does not open before typing, or a magnifier on one that does.
- A `MudMenu`'s icon activator uses `MudMenu`'s own `Icon`/`AriaLabel` parameters, never a custom `MudIconButton` in `ActivatorContent` — that button never gets the click-handling class `MudMenu` wires to (found B-2: the account menu never opened).
- A CSS rule on a library class is written from the package's own CSS (`~/.nuget/packages/<lib>/<version>/staticwebassets/*.min.css`), never from memory of the name: the rule that widened the question list named `mud-container-maxwidth-large` and matched nothing, the class is `mud-container-maxwidth-lg`. (F-100, 2026-10-13)
- An AppSelectField filter over string uses TValue="string" with an empty value for "all", as AccountEvents.razor does; TValue="string?" produces CS8669 in the generated Razor code and turns the gate red. (F-102, 2026-10-15)
