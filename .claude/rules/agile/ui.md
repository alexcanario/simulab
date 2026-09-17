---
paths:
  - "**/*.razor"
  - "**/*.razor.cs"
  - "**/*.razor.css"
  - "**/Simulab.Web/**/*.cs"
---
# UI standards

One behavior everywhere. A pattern is defined once, in the UI kit (`Simulab.Web/Components/Ui/`) or in `SimulabTheme`, never page by page. If a page needs something the kit does not have, extend the kit; do not improvise on the page.

## Kit first
- Pages use the kit, not raw MudBlazor, for: data tables, row actions, page header, confirmation dialog, empty / loading / error states, form actions.
- `<MudTable` and `<MudDataGrid` appear only inside the kit. An architecture test enforces it.
- Every kit pattern is shown on the dev-only gallery page `/dev/ui`. Screen mockups (`/agile:screen`) use only patterns from the gallery.

## Hover and focus
- Every table row has hover. Every clickable element has a pointer cursor and a visible focus ring (WCAG 2.2 AA).
- Hover, focus and pressed states come from the kit and the theme. Never set or remove them on a single page.

## Icons
- One family in the whole app: Material **Outlined**.
- Pages never write `Icons.Material.*`. They use semantic names from `AppIcons` (`AppIcons.Edit`, `AppIcons.Delete`, `AppIcons.Add`). An architecture test enforces it.
- The same action has the same icon everywhere. A new action adds a name to `AppIcons`, once.
- An icon-only button always has a tooltip and an `aria-label`, both from resources.

## Editing and row actions
- Row actions live in the last column: edit (pencil), then delete (bin). More than three actions go into an overflow menu.
- The item name is a link only when a read-only detail page exists. A link never opens the edit form.
- Simple entity (up to about six fields, no nested lists, no rich text): edit in a dialog.
- Complex entity (question, exam, edition, import draft): edit on its own page, with a route.
- The same entity is always edited the same way, from every screen that lists it.

## Destructive actions
- Always the kit confirmation dialog. The confirm button is the error color and names the action and the object ("Delete exam board"), never "Yes" / "No".
- Deleting is soft delete; the text says what happens to related data.

## Feedback
- Success: snackbar, same position and duration everywhere (set once in `Program.cs`).
- Validation errors: next to the field. Business and server errors: an alert at the top of the form or dialog.
- API error codes become text through one helper (resource key = error code). Never show a raw code, a status number or an exception message.

## List states
- Every list has three states besides data, all from the kit: loading (skeleton), empty (message and the primary action), error (message and "try again").

## Forms
- Label above the field; required fields marked; help text under the field.
- Validate when leaving the field and on submit.
- Actions at the bottom right: primary "Save" (filled), then "Cancel" (text). While saving, the primary button is disabled and shows progress.
- Leaving a form with unsaved changes asks for confirmation.
- Enter submits a single-line form; Esc closes a dialog.

## Page anatomy
- `PageHeader` from the kit: title, breadcrumb, and the primary action at the top right.
- One primary (filled) button per page. Secondary actions are outlined, tertiary are text. Colors are semantic (primary, error, success), never picked by taste.

## Tables
- Server-side paging; the same page sizes everywhere; search box in the same place; sortable columns say so.
- Numbers right-aligned; dates and numbers formatted with the user's culture; long text truncated with a tooltip.

## Action vocabulary
- One term per action per language, from `Common.*` keys in the shared resources (`Common.Add`, `Common.Edit`, `Common.Delete`, `Common.Save`, `Common.Cancel`, `Common.Search`). Never "Add" on one screen and "New" or "Create" on another.

## Keyboard and targets
- Logical tab order; everything reachable and operable by keyboard; click targets at least 24 px.

## Exam session screens (declared exception)
- Distraction-free layout: no app menu, no breadcrumb. Focus on the question, the timer and the answer sheet.
- Their patterns are also kit components and are also in the gallery. Icons, feedback, vocabulary and accessibility rules still apply.

## Done for a screen
- Uses only kit patterns; no raw icon or raw table.
- Hover, focus, empty, loading and error states checked.
- Checked in light and dark mode, and with one language switch.
- Operable by keyboard alone.
