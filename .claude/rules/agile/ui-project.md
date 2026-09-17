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
- A screen imported from Simulae is converted to the kit on the way in; it never enters with its original table, icons or edit pattern.
