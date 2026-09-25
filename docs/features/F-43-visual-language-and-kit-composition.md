---
feature: F-43
epic: Foundation and identity
status: refining
board: 765
version: 1
---
# Visual language and kit composition

## Summary
The Simulab screens read as a single column of plain fields on an empty card, while Simulae's back office — whose
palette Simulab already copied — reads as sectioned cards, multi-column form grids, radio cards, status chips and a
summary aside. Give the UI kit the page-composition patterns it lacks, write down the visual language (typography,
spacing, radius and where the accent colour appears in content), and migrate every existing screen to it. Decided in
D-2 (owner, 2026-09-25) after F-34 was rejected on screen for its look. Absorbs B-16 and B-17.

## Start
- Depends on: nothing left. F-34 merged as `3386b40` on 2026-09-25, so `AppLookupField`, the exam and
  issuing-authority screens and B-16's file are all on `main`.
- Waits on: nothing. The direction is decided in D-2; the reference is Simulae's approved back office.
- Suggested path: `/agile:screen` on the exam form during this refinement (the `ux-designer` agent), then
  `/agile:build` — D-2 requires the owner to approve that mockup before any code.
- Parallel with: none — it rewrites the theme and the kit that every other screen uses. F-41 is in `building`
  in its own worktree and touches no screen; it merges first or this one rebases on it.

## What exists
- Kit: 30 files in `src/Hosts/Simulab.Web/Components/Ui/`. All of them are field, table, row-action, dialog,
  alert or empty/loading/error components. **None composes a page**, which is why every form is one column of
  plain fields on one card.
- Gallery `/dev/ui`: 11 sections (`GallerySections.All`) — table, row actions, states, confirm, form, fields,
  lookups, alerts, errors, feedback, icons. None of them is a layout.
- `src/Hosts/Simulab.Web/wwwroot/app.css`: 733 lines, 80 `.app-*` classes. `.app-chip`, `.app-status` and
  `.app-badge` exist; there is no section card, form grid or radio card.
- Theme: `SimulabTheme` already carries Simulae's palette (ADR-0001 #30) — primary `#2478C5` darkened to
  `#216DB5` by B-8, secondary `#1A9E8C`, tertiary `#F5A020`, background `#F2F5FB`, drawer `#0F3266`. Nine
  contrast passes (B-8, B-9, B-10, F-10, F-17) are pinned by `ThemeContrastTests` and `ThemePaletteTests`.
- 31 `.razor` files under `Components/Pages/`: 12 back-office screens and dialogs, 15 identity pages on
  `AuthLayout`, plus the gallery, `Home`, `Error` and `NotFound`.
- Reference: `simulae/docs/4. UI UX Design/mockups/[US #351] Cadastro e Edicao de Concursos.html` (read-only
  source). The nine patterns it uses and the kit lacks are listed in D-2.

## Goal
Every Simulab screen reads as one designed system — grouped in titled sections, multi-column where the fields
are short, with colour carrying meaning — instead of a single column of plain fields on an empty card.

## Users and use cases
- UC1 An Admin opens a form and sees its fields grouped in titled sections, two or three to a row where they
  are short, so the form is read in blocks instead of scrolled past empty space.
- UC2 An Admin picks a choice that changes the rest of the form (the exam scope) from cards that carry a
  one-line explanation each, instead of a bare select.
- UC3 An Admin who saves an incomplete form sees one panel at the top of the card listing every field that is
  missing, besides the message beside each field.
- UC4 An Admin scanning a list reads a record's state as a coloured chip, the same chip on every screen.
- UC5 An Admin filling a long form sees a read-only aside with what is already filled and what is still
  missing.
- UC6 A developer opens `/dev/ui` and finds every new pattern with its states, so a screen mockup can only use
  what is there (rule `ui`).

## Business rules
- BR1 Nine patterns enter the kit as components in `Components/Ui/`, each with its own gallery section:
  `AppSectionCard` (title, optional icon, optional subtitle), `AppFormGrid` (1, 2 or 3 columns, collapsing to
  one at phone width), `AppRadioCards` (option, description, selected state), `AppConditionalField` (shown by a
  choice, clears its value when hidden), a leading-icon parameter on `AppTextField`, `AppSelectField` and
  `AppLookupField`, `AppStatusChip`, `AppErrorSummary` (the offending fields listed at the top of the card),
  `AppFormAside` (read-only summary beside the form) and `AppItemRows` (child items with per-row actions).
- BR2 No page reproduces any of these in local markup: the architecture test that already forbids raw
  `MudTable`/`MudDataGrid` outside the kit gains the same check for the new patterns' MudBlazor building blocks.
- BR3 The accent colour appears in content in exactly four places: the section card's icon and title, a status
  chip, a field's leading icon, and the focus ring. Nowhere else — no tinted card or alert surfaces. Every pair
  keeps coming from `SimulabTheme`, so no new colour token is introduced and no new contrast pair is needed
  beyond BR6 and BR7.
- BR4 The form's primary action stays `AppFormActions` at the bottom right of the card (rule `ui`); the aside
  of BR1 is read-only and carries no button. Simulae puts the actions in the aside; Simulab does not.
- BR5 The visual language is written as CSS custom properties on `:root` in `app.css` — a type scale, a spacing
  scale and a radius scale — and every new kit rule reads them instead of hard-coded values. A test reads
  `app.css` and fails on a length literal inside an `.app-*` rule that a scale token covers.
- BR6 (from B-16) The field hint reads at 4.5:1 or better on every surface it sits on, in both themes, and the
  pair enters `ThemeContrastTests`. Today it is 1.90:1 dark and 2.64:1 light.
- BR7 (from B-17) The selected navigation item reads at 4.5:1 or better in both themes, and the pair enters
  `ThemeContrastTests`. Today it is 4.37:1 dark, dimmer than the unselected items around it.
- BR8 Twelve back-office screens migrate to the patterns: `Roles`, `RoleDialog`, `Users`, `UserRolesDialog`,
  `RoleHistory`, `AccountEvents`, `Organizers`, `OrganizerDialog`, `IssuingAuthorities`,
  `IssuingAuthorityDialog`, `Exams`, `ExamForm`. The 15 identity pages on `AuthLayout` (a declared exception in
  `ui-project.md`, with its own approved design), `Home`, `Error`, `NotFound` and the gallery keep their layout.
- BR9 Density changes inside the cards only — the section card's padding and the grid's gaps. The global
  MudBlazor spacing, the app bar and the drawer are untouched.
- BR10 Every new UI text exists in pt-BR, pt-PT and en in `SharedResources`; the patterns carry no hard-coded
  string, and a parameter whose right value depends on what the page means (a section's title, a chip's label,
  an icon) is `[EditorRequired]` (rule `ui`).

## Screens and API
No endpoint, no route and no schema change: this item rewrites how the existing screens are composed. The
routes of BR8 stay exactly as they are, and `/dev/ui` gains nine sections.

The detailed screen section and the mockup come from `/agile:screen` on `/admin/exams/{id}` (the exam form),
the reference screen: it is the only page that uses a section card, a form grid, radio cards, a conditional
field, a leading icon and the aside at once.

## Acceptance criteria
- AC1 Given the gallery, when it is opened in Development, then it renders a section for each of the nine
  patterns of BR1, with every state of each.
- AC2 Given a page that reproduces one of the nine patterns in local markup, when the architecture tests run,
  then they fail naming that page (BR2).
- AC3 Given the exam form, when it is rendered, then its fields are inside titled section cards and the short
  ones share a row, and at phone width every row collapses to one column (BR1).
- AC4 Given the exam form with the scope not chosen, when the Admin picks `State` from the radio cards, then
  the conditional field appears labelled `State`, and going back to `National` hides it and clears its value
  (BR1) — the behaviour F-34 already has, now through the kit component.
- AC5 Given an incomplete form, when the Admin saves, then one panel at the top of the card lists every
  missing field and each field also shows its own message (BR1, UC3).
- AC6 Given a list with a state column, when it is rendered, then the state is an `AppStatusChip` and the same
  state uses the same chip on every screen (BR1).
- AC7 Given the exam form, when it is rendered on a wide viewport, then the read-only aside shows what is
  filled and what is missing, and carries no button (BR4).
- AC8 Given any `.app-*` rule in `app.css`, when the CSS test runs, then it fails on a length literal that a
  scale token of BR5 covers.
- AC9 Given the theme, when `ThemeContrastTests` runs, then the field hint reads at least 4.5:1 on the surface
  and on the background, in both themes (BR6) — seen failing on today's token first.
- AC10 Given the theme, when `ThemeContrastTests` runs, then the selected navigation item reads at least 4.5:1
  in both themes (BR7) — seen failing on today's colour first.
- AC11 Given each of the twelve screens of BR8, when its bUnit tests run, then it renders through the kit
  patterns and no test of its behaviour changed (BR8).
- AC12 Given the accent colour, when the screens are read, then it appears only in a section card's icon and
  title, a status chip, a field's leading icon and the focus ring (BR3).
- AC13 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green (BR10).

## Decisions
- 2026-09-25 — Keep Simulae's base palette; add composition patterns and a written visual language instead of new
  hues — D-2 option B.
- 2026-09-25 — Copy Simulae's composition closely, not as loose inspiration — the owner already validated those
  screens.
- 2026-09-25 — Migrate the kit, the gallery and every existing screen in this item — `ui.md` requires one pattern
  defined once.
- 2026-09-25 — Absorb B-16 (field hint contrast) — same theme file, same kit fields.
- 2026-09-25 — Absorb B-17 (selected navigation item, 4.37:1 dark) as well — the project rule says a contrast
  fix on a theme token covers the whole family in the same item, and B-16, B-17 and this item all rewrite the
  same tokens; three passes over the same decision is what that rule exists to prevent (owner, refinement).
- 2026-09-25 — All nine patterns of D-2 enter at once (BR1) — the set was measured against the reference
  mockup, and F-35's editions section needs the child-item rows immediately after (owner, question 1).
- 2026-09-25 — The form's actions stay at the bottom right of the card and the aside is read-only (BR4) —
  Simulae puts them in the aside, but `ui.md` says one primary button per page at the bottom right, and an
  exception written into `ui-project.md` would buy nothing here (owner, question 2).
- 2026-09-25 — The accent appears in four places only, with no tinted surfaces (BR3) — none of the four is
  running text, so the tokens already covered by `ThemeContrastTests` still hold and no new pair is needed
  (owner, question 3).
- 2026-09-25 — Only the twelve back-office screens migrate (BR8) — the identity pages have a declared
  exception and a design the owner already validated; migrating them would triple the surface for no complaint
  (owner, question 4).
- 2026-09-25 — No new package: the patterns are CSS and composition over the MudBlazor already in
  `Directory.Packages.props`, and the tests use the libraries the Web test project already has (rule
  `build-config`).
- 2026-09-25 — Density changes inside the cards only (BR9) — a global density change would move every screen,
  including the ones this item does not touch.

## Out of scope
- A new brand: palette, logo and identity of Simulab's own (D-2 option C, parked).
- The exam session screens' distraction-free exception (parked in D-2 until the first such screen exists).
- The 15 identity pages on `AuthLayout`, `Home`, `Error`, `NotFound` and the gallery's own chrome (BR8).
- Any behaviour change on the twelve screens: this item changes how they are composed, not what they do.

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-43
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
