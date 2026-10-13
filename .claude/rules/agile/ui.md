---
paths:
  - "**/*.razor"
  - "**/*.razor.cs"
  - "**/*.razor.css"
  - "**/*.xaml"
---
# UI standards

- One behavior everywhere: a pattern is defined once, in the UI kit or the theme, never page by page. If the kit lacks something, extend the kit. A kit parameter whose right value depends on what the page means (autocomplete, input purpose, a destructive action's wording) has no default: it is required (Blazor `[EditorRequired]`, whose `RZ2012` warning the gate refuses), so a page that forgets it fails the build.
- Pages use the kit, not raw library components, for data tables, row actions, page header, confirmation dialog, form actions and the empty, loading and error states.
- Every kit pattern is shown on a dev-only gallery page. Screen mockups use only patterns from the gallery.
- Every table row has hover; every clickable element has a pointer cursor and a visible focus ring. Never set or remove these on a single page.
- One icon family in the whole app (recorded in ADR-0001), and one visual identity (`docs/design/identity.tokens.json`): colors, fonts and radius come from its tokens, never from a page or a mockup. Pages use semantic names (`AppIcons.Edit`), never the library's icon constants. The same action has the same icon everywhere.
- An icon-only button always has a tooltip and an accessible name, both from resources.
- An item is edited one way everywhere: row actions in the last column (edit, then delete; more than three go into an overflow menu). The name is a link only to a read-only detail page, never to the edit form.
- Simple entity (about six fields, no nested lists): edit in a dialog. Complex entity: edit on its own page, with a route. The same entity is always edited the same way.
- A destructive action uses the kit confirmation dialog; the confirm button is the error color and names the action and the object, never "Yes" / "No".
- Success is a snackbar, same position and duration everywhere. Validation errors sit next to the field; business and server errors are an alert at the top of the form.
- API error codes become text through one helper (resource key = code). Never show a raw code, a status number or an exception message.
- Every list has loading, empty (with the primary action) and error (with "try again") states. Every component that shows user or Core text also has a long-text state (a message wider than the component), in the gallery and in a wrap test.
- Forms: label above the field, required fields marked, validate on leaving the field and on submit, primary "Save" at the bottom right, then "Cancel" as text.
- While saving, the primary button is disabled and shows progress. Leaving with unsaved changes asks for confirmation. Esc closes a dialog.
- Page anatomy: a page header with title, breadcrumb and the primary action at the top right. One primary (filled) button per page; secondary outlined, tertiary text.
- Tables: server-side paging with the same page sizes everywhere, search in the same place, numbers right-aligned, dates and numbers in the user's culture.
- One term per action per language, from shared `Common.*` resource keys. Never "Add" on one screen and "New" on another.
- Everything is operable by keyboard, in a logical tab order; click targets are at least 24 px.
- A declared exception (for example a distraction-free exam or checkout flow) is written in the project rules, and its patterns are also kit components.
- What the build can check goes into the build: architecture tests forbid raw icon constants and raw table components outside the kit, and a test over the theme tokens checks the contrast ratio of every foreground/background pair, in both themes — a screen check only sees the pairs that screen happens to render.
- A screen is done when it uses only kit patterns and was checked in light and dark mode, with one language switch, and by keyboard alone. A colour or contrast bug is checked in both themes and on every surface the component sits on (card, page background, app bar): the ratio is computed from the theme tokens, then measured on screen; a premise such as "dark theme only" is verified, not assumed.
