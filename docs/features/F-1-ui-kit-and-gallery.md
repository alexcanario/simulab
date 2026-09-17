---
feature: F-1
epic: Foundation and identity
status: approved
board: 705
version: 1
---
# UI kit and gallery

## Summary
The owner wants one frontend behavior everywhere: hover on every control, one way to edit an item, one icon style. Simulae's screens drifted because each page assembled MudBlazor on its own (8 raw `MudTable`, 316 raw `Icons.Material` uses, no shared kit).
Build the UI kit in `Simulab.Web/Components/Ui/`, `AppIcons` with semantic names over Material Outlined, a dev-only gallery page `/dev/ui`, and architecture tests that forbid raw `Icons.Material` and raw `MudTable` / `MudDataGrid` outside the kit. Standards: `.claude/rules/agile/ui.md` and `ui-project.md`.
It comes before any screen is imported from Simulae, so imported screens enter already in the standard.

## Goal
Every future screen is assembled from one set of tested patterns, so behavior, icons and vocabulary are the same on every page and the build rejects drift.

## What exists
- `Simulab.Web`: `MainLayout` (app bar, language switch), `SimulabTheme` (light and dark palettes), `Home`, `Error`, `NotFound`, `SharedResources` in en, pt-BR and pt-PT.
- Tests: `ResourceParityTests` (missing-key test), `HomePageTests` (bUnit), architecture tests for forbidden references and vocabulary.
- Not yet: `Components/Ui/`, `AppIcons`, `Common.*` keys, snackbar configuration, any table or dialog.

## Users and use cases
- UC1 A developer (Claude) builds a list screen from the kit table, row actions, page header and list states, without touching raw MudBlazor table components.
- UC2 A developer builds a create/edit form with the kit form actions and gets the saving state and the unsaved-changes warning for free.
- UC3 A developer asks for confirmation of a destructive action with the kit confirmation dialog.
- UC4 A developer turns an API error code into text with one helper.
- UC5 The owner opens `/dev/ui` in Development and checks every kit pattern in light and dark mode, in the three languages and by keyboard.

## Business rules
- BR1 Kit components: `AppDataTable`, `AppRowActions`, `AppPageHeader`, `AppConfirmDialog` (shown through a kit service), `AppLoadingState`, `AppEmptyState`, `AppErrorState`, `AppFormActions`, the `ErrorText` helper and the snackbar defaults. Exam session components are out of scope (Exam Simulator epic).
- BR2 `AppDataTable` loads data from the server page by page. Page sizes are 10, 25 and 50; the default is 25. Search sits in the table toolbar, top left. Rows have hover. Numbers are right-aligned. Dates and numbers use the user's culture. Long text is truncated with a tooltip. Sortable columns show a sort indicator. While loading, on empty and on error it shows the kit states.
- BR3 `AppRowActions` sits in the last column: edit, then delete. With more than three actions, the extra ones go into an overflow menu. Every action is an icon button with a tooltip and an accessible name from resources.
- BR4 `AppPageHeader` shows the title, the breadcrumb and at most one primary action at the top right.
- BR5 `AppConfirmDialog` for a destructive action: the confirm button uses the error color and its text names the action and the object (for example "Delete exam board ABC"). The dialog body says what happens to related data. Esc and Cancel close it without acting.
- BR6 `AppEmptyState` shows a message and, when given, the list's primary action. `AppErrorState` shows a message and a "Try again" button that reloads.
- BR7 `AppFormActions`: primary "Save" at the bottom right, then "Cancel" as a text button. While saving, Save is disabled and shows progress. With unsaved changes, leaving asks for confirmation, both for navigation inside the app and for closing or reloading the browser tab.
- BR8 `ErrorText` turns an API error code into text using the code as the resource key. An unknown code shows a generic message; a raw code, status number or exception message is never shown.
- BR9 Snackbar: bottom right, 4 seconds, with a close button. Configured once in `Program.cs`.
- BR10 `AppIcons` exposes semantic names (`Add`, `Edit`, `Delete`, `Search`, `More`, `Retry`, `Close`, `Back`, `Save`, `Warning`, `Error`, `Empty`, `LightMode`, `DarkMode`) over Material Outlined. Same action, same icon everywhere.
- BR11 Action vocabulary lives in shared keys `Common.Add`, `Common.Edit`, `Common.Delete`, `Common.Save`, `Common.Cancel`, `Common.Search`, plus `Common.TryAgain`, `Common.Close` and `Common.More`, in the three languages.
- BR12 `/dev/ui` exists only in the Development environment; in any other environment the route returns 404. It needs no permission. It shows every kit pattern with sample data and has its own light/dark switch. The app-wide theme switch belongs to F-2.
- BR13 Outside `Components/Ui/`, no `.razor` or `.cs` file uses `<MudTable` or `<MudDataGrid`; outside `AppIcons`, none uses `Icons.Material`. The build checks it.
- BR14 Every clickable kit element has a pointer cursor, a visible focus ring and a target of at least 24 px, set in the kit or the theme, never per page.

## Screens and API
- `/dev/ui` — dev-only gallery, one section per kit pattern, with a light/dark switch. Uses the app layout (language switch available).
- No API endpoints. No error codes of its own; `ErrorText` falls back to `Common.Error.Unexpected`.

## Acceptance criteria
- AC1 Given a table with 60 items from a fake server source, when the gallery table loads, then it shows 25 rows, offers page sizes 10, 25 and 50, and requests the next page from the source when paging. (BR2)
- AC2 Given the table source is loading, empty or fails, then the table shows the loading state, the empty state with its primary action, or the error state; clicking "Try again" calls the source again. (BR2, BR6)
- AC3 Given a search term typed in the table toolbar, then the source receives the term and the first page is requested. (BR2)
- AC4 Given a row with two actions, then edit and delete appear in that order; given five actions, then three appear and the rest are in the overflow menu; each icon button has a tooltip and an accessible name. (BR3)
- AC5 Given a page header with title, breadcrumb and primary action, then all three render and the action is the only filled button. (BR4)
- AC6 Given a destructive confirmation for "exam board ABC", then the confirm button uses the error color and names the action and object; confirming returns true; Cancel or Esc returns false. (BR5)
- AC7 Given a form with changes, when the owner navigates away inside the app, then a confirmation appears and navigation happens only if confirmed; the browser-tab guard is registered while there are changes and removed after saving. (BR7)
- AC8 Given a save in progress, then Save is disabled and shows progress, and Cancel is a text button to its left. (BR7)
- AC9 Given a known error code, `ErrorText` returns the resource text; given an unknown code, it returns the generic message and never the code. (BR8)
- AC10 Given the app starts, then snackbars appear at the bottom right, last 4 seconds and can be closed. (BR9)
- AC11 Given the environment is not Development, when `/dev/ui` is requested, then the response is 404; in Development it renders every kit section. (BR12)
- AC12 Given a `.razor` or `.cs` file outside the kit with `<MudTable`, `<MudDataGrid` or `Icons.Material`, then the architecture test fails and names the file. (BR13)
- AC13 Given `AppIcons`, then every icon is from the Material Outlined set. (BR10)
- AC14 All new texts (`Common.*`, kit and gallery keys) exist in pt-BR, pt-PT and en, and the missing-key test is green. (BR11)
- AC15 On screen: the gallery in light and dark mode shows hover on rows, pointer cursor and focus ring on every control, and is fully operable by keyboard. (BR2, BR14) — validation script.

## Decisions
- 2026-09-17 — Kit scope, page sizes 10/25/50 (default 25), snackbar bottom right 4 s with close, gallery Development-only with 404 elsewhere, gallery-local theme switch, unsaved-changes guard for in-app navigation and browser tab — owner answers to refinement questions 1-6.
- 2026-09-17 — `AppDataTable` wraps `MudDataGrid` with server data — paging, sorting and hover are built in; only the kit references it.
- 2026-09-17 — `AppIcons` is a static class of string constants over `Icons.Material.Outlined` — semantic names with no runtime cost.
- 2026-09-17 — Confirmation is shown through a kit service (`IConfirmService`) over `IDialogService` — one call site pattern, testable with bUnit.
- 2026-09-17 — The architecture test for BR13 scans source text of `src/Simulab.Web` — Razor markup is not visible through reflection.
- 2026-09-17 — Unsaved-changes guard uses `NavigationLock` (in-app navigation and `beforeunload`) — built into Blazor, no JS of our own.
- 2026-09-17 — Exam session components are out of scope — they belong to the Exam Simulator epic and have no screen design yet.

## Out of scope
- App-wide theme switch, navigation menu and user menu (F-2).
- Exam session components (Exam Simulator epic).
- Converting Simulae screens (each import feature).
- Permission checks on the gallery (RBAC arrives in F-6).
- App manual pages: the gallery is dev-only and changes no user-visible behavior.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
<!-- Filled by /agile:ship. -->
