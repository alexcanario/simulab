---
feature: F-43
epic: Foundation and identity
status: approved
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
routes of BR8 stay exactly as they are, and `/dev/ui` gains nine sections. The exam form keeps the API it
already calls (`GET/POST/PUT /api/v1/catalog/exams`, `GET /api/v1/catalog/issuing-authorities`) and its
error codes; no request or response field changes.

Mockup: `docs/features/mockups/F-43-exam-form.html` (state, language and theme switchers; sample data of a
municipal exam of Prefeitura Municipal de Fortaleza, PMF).

### Reference screen — exam form (`/admin/exams/new`, `/admin/exams/{id:guid}`)

Permission: `catalog.manage`, checked by the route attribute that is already there
(`[Authorize(Policy = PermissionPolicy.Prefix + CatalogPermissions.Manage)]`), by the menu item and by the
`Add` / `Edit` actions on `/admin/exams`. A visitor without it gets `NotFoundContent`, as F-2 BR7 decided:
a page whose permission is missing looks exactly like one that does not exist. Only the Admin role carries
the permission today; the check is by permission, never by role name.

#### Layout

Page header (`AppPageHeader`, unchanged): breadcrumb `Content / Exams / <name or Add exam>`, `h1` with
`Exams.Form.AddTitle` or `Exams.Form.EditTitle`, no primary action at the top right (the form owns the only
primary button).

Below it, `.app-form-layout`: a two-column grid, `minmax(0, 1fr)` for the form and `20rem` for the aside,
gap `--app-space-4`, `align-items: start`. At `max-width: 1279.98px` it becomes one column and the aside
stacks **below** the form (never hidden: it is the only place that lists what is still missing). At
`max-width: 599.98px` every form grid row also collapses to one column (AC3).

Column 1 — the form card (`MudPaper.app-form-card`, `Elevation="0"`, the surface the page already uses),
in this order:

| Order | Block | Contents |
|---|---|---|
| 1 | `AppErrorSummary` | Only after a failed save. Lists every offending field as a link to it (UC3, AC5). |
| 2 | `AppAlert Severity="Error"` | Only for a server/business refusal that belongs to no single field. |
| 3 | `AppSectionCard` **Identification** | `AppFormGrid Columns="2"`: issuing authority, name. |
| 4 | `AppSectionCard` **Classification** | `AppFormGrid Columns="2"`: assessment type, content language. |
| 5 | `AppSectionCard` **Where the exam applies** | `AppRadioCards` for the scope (3 cards, one row), then `AppConditionalField` with the state/municipality field. |
| 6 | `AppFormActions` | Bottom right of the form card: `Cancel` (text) then `Save` (filled primary). BR4. |

A section card is a bordered region **inside** the form card — the same shape `.app-permission-group`
already has on `/admin/roles` (1 px `divider` border, radius, own padding), not a second elevated paper.
That keeps one surface per page, keeps `AppFormActions` at the bottom right of the card the rule asks for,
and still reads as the reference's sectioned blocks.

Column 2 — `AppFormAside` (`MudPaper.app-form-aside`, `position: sticky; top: var(--app-space-4)`), read-only,
no button (BR4, AC7): summary rows for the six values, then a checklist of what is still missing. It comes
after the form in the DOM, so the keyboard reaches the actions before it.

#### Fields

| Field | Component | Type / limits | Required | Leading icon | Placeholder |
|---|---|---|---|---|---|
| Issuing authority | `AppLookupField` `Id="exam-authority"` | server search, min 2 chars, max 20 candidates | yes | `AppIcons.IssuingAuthorities` | `Exams.Field.IssuingAuthority.Placeholder` (already exists, reused unchanged) |
| Name | `AppTextField TValue="string"` `Id="exam-name"` | text, 2–200 (`CatalogLimits.ExamNameMaxLength`), `Autocomplete="off"` | yes | `AppIcons.Exams` | `Exams.Field.Name.Placeholder` (new) |
| Assessment type | `AppSelectField TValue="AssessmentType?"` `Id="exam-assessment-type"` | 4 options in `ExamText.AssessmentTypeOrder` | yes | `AppIcons.AssessmentType` (new) | `Exams.Field.AssessmentType.Placeholder` (new) — the text of the empty option, as the reference does for its own select |
| Content language | `AppSelectField TValue="string"` `Id="exam-content-language"` | `SupportedCultures.All`, each in its own name with `lang`; default `pt-BR` | yes | `AppIcons.Language` | none: the field always carries a value (F-34's `DefaultContentLanguage`), so it has no empty option to hold one |
| Scope | `AppRadioCards TValue="ExamScope?"` `Id="exam-scope"` | National / State / Municipal, each with a one-line description | yes | card icons: `AppIcons.Scope` (new), `AppIcons.Place` (new), `AppIcons.Place` | none: radio cards have no input; the three cards and their descriptions are the example |
| State / Municipality | `AppTextField` `Id="exam-scope-detail"` inside `AppConditionalField` | text, max 120 (`CatalogLimits.ExamScopeDetailMaxLength`) | yes, only while visible | `AppIcons.Place` (new) | `Exams.Field.State.Placeholder` or `Exams.Field.Municipality.Placeholder` (new), following the label |

A placeholder is a worked example (`Ex.: ...`), in the style of the Simulae reference: it never replaces the
label, which stays above the field, and it never carries a rule — the limits and the format live in the hint,
so no required format exists only inside a placeholder that disappears as soon as the Admin types.

Behaviour is unchanged from F-34: leaving `State` or `Municipal` hides the detail and clears its value and
its message (AC4); the label is `Exams.Field.State` or `Exams.Field.Municipality`; validation runs on blur
and on submit; the API stays the authority and its refusals land on the field that caused them.

#### The nine kit components used here

| Component | Parameters (`*` = `[EditorRequired]`) | On this screen |
|---|---|---|
| `AppSectionCard` | `Title*`, `Icon*`, `Subtitle`, `Id`, `ChildContent*` | three of them; `Icon` is an `AppIcons` constant, the only accent icon in content (BR3) |
| `AppFormGrid` | `Columns*` (1, 2 or 3), `ChildContent*` | two of them, `Columns="2"` |
| `AppRadioCards<TValue>` | `Id*`, `Label*`, `Options*` (`AppRadioCardOption<TValue>(Value, Text, Description, Icon)`), `Value`, `ValueChanged`, `Columns`, `Required`, `Error` | the scope (UC2) |
| `AppConditionalField` | `Visible*`, `ChildContent*`, `OnHidden` | the state/municipality field; `OnHidden` is what clears the value |
| leading icon on fields | `LeadingIcon`, on `AppTextField`, `AppSelectField`, `AppLookupField` | five fields; the icon is decorative (`aria-hidden="true"`), never the only carrier of meaning |
| `AppStatusChip` | `Text*`, `Status*` (`Neutral`/`Success`/`Warning`/`Error`/`Info`) | **not used here** — the exam has no status field. See open question 1 |
| `AppErrorSummary` | `Title*`, `Items*` (`AppErrorSummaryItem(FieldId, Label)`), `Visible` | after a failed save |
| `AppFormAside` | `Title*`, `Rows*` (`AppAsideRow(Label, Value)`), `ChecklistTitle*`, `Checklist*` (`AppChecklistItem(Label, Done)`) | the summary column; no action slot at all |
| `AppItemRows` | `Items*`, `RowTemplate*`, `Actions`, `EmptyMessage*` | **not used here** — the editions arrive with F-35. See open question 1 |

`AppFormActions`, `AppPageHeader`, `AppAlert`, `AppLoadingState` and the three field components are used as
they are today; only the leading-icon parameter is added to the fields.

#### States

| # | State | What the screen shows |
|---|---|---|
| S1 | Loading (edit route only) | `AppLoadingState` replaces the whole two-column layout; `role="status"`, `Common.Loading`. |
| S2 | Empty / new | Every field blank except content language (`pt-BR`) and no scope chosen; the conditional field is absent; the aside shows `Common.Summary.NotFilled` on every row and five pending checklist items. This is the form's empty state. |
| S3 | Ready (edit) | Values loaded, breadcrumb carries the exam name, `Save` disabled until something changes (`HasChanges`), aside filled, checklist all done. |
| S4 | Saving | `Save` disabled, spinner plus `Common.Saving` inside it, `Cancel` disabled, fields stay enabled but the form is not resubmittable. |
| S5 | Success | Snackbar `Exams.Saved` (kit position and duration); on add, the route is replaced by `/admin/exams/{id}` and the title becomes `Exams.Form.EditTitle`; the page stays open. |
| S6 | Validation error | `AppErrorSummary` at the top of the card with one link per offending field, each field also shows its own message, focus moves to the summary, nothing is sent. |
| S7 | Server / business error | Field-owned refusals (`exam.name_taken`, `exam.scope_detail_too_long`, `exam.issuing_authority_required`) go to the field; anything else is `AppAlert Severity="Error"` at the top of the card with the text of the code through `ErrorText`. |
| S8 | Not found | The card holds `AppAlert` with `exam.not_found` and the `Exams.BackToList` link — today's behaviour, now inside the form card. |
| S9 | Permission denied | `NotFoundContent` (`NotFound.Title`, `NotFound.Message`): the route is indistinguishable from one that does not exist (F-2 BR7). |
| S10 | Conditional hidden | Scope `National`: no detail field, its value and message cleared, the aside row reads `Common.Summary.NotFilled` and the checklist item for it disappears. |
| S11 | Conditional shown — state | Scope `State`: the field appears labelled `Exams.Field.State`, required, empty. |
| S12 | Conditional shown — municipal | Scope `Municipal`: the same field labelled `Exams.Field.Municipality`. |
| S13 | Unsaved changes | Leaving the page asks `Common.Discard.*` through `AppFormActions`'s `NavigationLock` — unchanged. |

#### Accessibility (WCAG 2.2 AA)

- `h1` is the page title and takes focus on navigation (`FocusOnNavigate`). Each `AppSectionCard` is a
  `<section aria-labelledby="...-title">` whose title is an `h2`; its subtitle is inside the section, read
  after the heading. The card's icon is `aria-hidden="true"`.
- `AppFormGrid` only sets `grid-template-columns`, so the DOM order is the reading and tab order:
  issuing authority → name → assessment type → content language → scope group → detail field (when
  visible) → Cancel → Save → aside. The aside is last and contains no focusable element.
- `AppRadioCards` is a `role="radiogroup"` labelled by its own label, with roving `tabindex`: one tab stop,
  arrow keys move and select, Space selects. Each card is `role="radio"` with `aria-checked`, and its
  description is bound with `aria-describedby`, so the reader hears "National, valid across the country,
  radio button, 1 of 3, selected". Cards are at least 44 px tall.
- `AppConditionalField` is a `<div role="group">`. When it appears, a polite live region announces
  `Exams.ScopeDetail.Shown` with the label ("State is now required."); focus is not moved, so the keyboard
  does not lose its place inside the radio group.
- `AppErrorSummary` is `role="alert"`, `tabindex="-1"` and receives focus when a save fails; each item is a
  real link to `#<field-id>`. In-page links need the `wwwroot/js/shell.js` handling the skip link already
  has (rule `ui-project`), otherwise `<base href="/">` navigates away.
- Fields keep the kit's contract: label above with `for`, `aria-describedby` pointing at the hint or the
  error, `aria-invalid`, `aria-required`; the `*` marker is `aria-hidden`. The hint is a hint until the
  field errs, then it is the message — never both.
- The aside is a `<aside aria-labelledby="exam-summary-title">` with no live region: it changes on every
  keystroke and would flood the reader. Each checklist item carries its state as text
  (`Common.Checklist.Done` / `Common.Checklist.Pending`) in an `app-visually-hidden` span, so the tick is
  never the only carrier.
- `Save` announces its progress with `role="status"` and `Common.Saving`; the snackbar is the kit's, polite.
- Every clickable target is at least 24 px, has a pointer cursor and the focus ring already defined in
  `app.css` (2 px `--mud-palette-primary`, offset 2 px) — the fourth and last place the accent appears (BR3).

#### Colour, and where the accent appears (BR3, BR6)

Only `SimulabTheme` tokens; no new hue. Ratios not already pinned by `ThemeContrastTests`, computed from the
tokens in both themes:

| Pair | Light | Dark | Needs |
|---|---|---|---|
| Field hint `text-secondary` on `surface` (BR6 fix: today `text-disabled`) | 6.92:1 | 4.89:1 | 4.5:1 |
| Field hint `text-secondary` on `background` | 6.34:1 | 5.36:1 | 4.5:1 |
| Selected radio card: `text-primary` on `primary-lighten` | 13.75:1 | 12.44:1 | 4.5:1 |
| Selected radio card description: `text-secondary` on `primary-lighten` | 6.33:1 | 4.60:1 | 4.5:1 |
| Selected radio card border: `primary` on `primary-lighten` | 4.45:1 | 5.69:1 | 3:1 |
| Checklist done mark: `success` on `surface` | 3.32:1 | 4.89:1 | 3:1 |
| Section card icon: `primary` on `surface` (already pinned by B-8) | 5.36:1 | 6.04:1 | 3:1 |

The section card's 1 px `divider` border is decorative (1.43:1 light): the section is also named by its
heading and separated by spacing, so no information depends on that line. The error summary is the kit's
filled `AppAlert` (its ink is pinned by F-10 and B-9); no card, panel or alert gets a tinted background of
its own (BR3, AC12).

#### Resource keys

Reused, unchanged: `Exams.Form.AddTitle`, `Exams.Form.EditTitle`, `Exams.Field.IssuingAuthority(.Hint,
.Placeholder)`, `Exams.Field.Name(.Hint)`, `Exams.Field.AssessmentType`, `Exams.Field.Scope`,
`Exams.Field.State`, `Exams.Field.Municipality`, `Exams.Field.ScopeDetail.Hint`,
`Exams.Field.ContentLanguage(.Hint)`, `Exams.Scope.*`, `Exams.AssessmentType.*`, `Exams.Saved`,
`Exams.BackToList`, `Exams.Title`, `Nav.Section.Content`, `Common.Save`, `Common.Saving`, `Common.Cancel`,
`Common.Loading`, `Common.TryAgain`, `Common.Discard.*`, `NotFound.Title`, `NotFound.Message`, and the
`exam.*` error codes.

New (BR10, AC13):

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Exams.Section.Identification` | Identification | Identificação | Identificação |
| `Exams.Section.Identification.Subtitle` | Who publishes the exam and how it is named in the catalog. | Quem publica o exame e como ele é chamado no catálogo. | Quem publica o exame e como é designado no catálogo. |
| `Exams.Section.Classification` | Classification | Classificação | Classificação |
| `Exams.Section.Classification.Subtitle` | The kind of assessment and the language its content is written in. | O tipo de avaliação e o idioma em que o conteúdo é escrito. | O tipo de avaliação e o idioma em que o conteúdo está escrito. |
| `Exams.Section.Scope` | Where the exam applies | Onde o exame se aplica | Onde o exame se aplica |
| `Exams.Section.Scope.Subtitle` | The territory the exam covers. A state or municipal exam also names the place. | O território que o exame cobre. Exame estadual ou municipal também informa o local. | O território que o exame abrange. Exame estadual ou municipal também indica o local. |
| `Exams.Scope.National.Description` | Valid across the country. | Vale em todo o país. | Válido em todo o país. |
| `Exams.Scope.State.Description` | Asks for the state. | Pede o estado. | Pede o estado. |
| `Exams.Scope.Municipal.Description` | Asks for the municipality. | Pede o município. | Pede o município. |
| `Exams.ScopeDetail.Shown` | {0} is now required. | {0} agora é obrigatório. | {0} passou a ser obrigatório. |
| `Exams.Field.Name.Placeholder` | E.g.: PMF 2026 Municipal Guard | Ex.: Concurso PMF 2026 — Guarda Municipal | Ex.: Concurso CM Lisboa 2026 — Polícia Municipal |
| `Exams.Field.AssessmentType.Placeholder` | Select the assessment type | Selecione o tipo de avaliação | Selecione o tipo de avaliação |
| `Exams.Field.State.Placeholder` | E.g.: Ceará | Ex.: Ceará | Ex.: Ceará |
| `Exams.Field.Municipality.Placeholder` | E.g.: Fortaleza/CE | Ex.: Fortaleza/CE | Ex.: Fortaleza/CE |
| `Exams.Form.Aside.Title` | Summary | Resumo | Resumo |
| `Exams.Form.Aside.Checklist` | Before saving | Antes de salvar | Antes de guardar |
| `Common.Summary.NotFilled` | Not filled | Não preenchido | Por preencher |
| `Common.Checklist.Done` | Done | Concluído | Concluído |
| `Common.Checklist.Pending` | Pending | Pendente | Pendente |
| `Common.ErrorSummary.Title` | This form cannot be saved yet | Ainda não é possível salvar este formulário | Ainda não é possível guardar este formulário |
| `Common.ErrorSummary.Intro` | Fill in these fields: | Preencha estes campos: | Preencha estes campos: |
| `Gallery.Section.SectionCard` | Section card | Cartão de seção | Cartão de secção |
| `Gallery.Section.FormGrid` | Form grid | Grade do formulário | Grelha do formulário |
| `Gallery.Section.RadioCards` | Radio cards | Cartões de opção | Cartões de opção |
| `Gallery.Section.ConditionalField` | Conditional field | Campo condicional | Campo condicional |
| `Gallery.Section.FieldIcon` | Field with a leading icon | Campo com ícone inicial | Campo com ícone inicial |
| `Gallery.Section.StatusChip` | Status chip | Etiqueta de estado | Etiqueta de estado |
| `Gallery.Section.ErrorSummary` | Error summary | Resumo de erros | Resumo de erros |
| `Gallery.Section.FormAside` | Form aside | Resumo lateral | Resumo lateral |
| `Gallery.Section.ItemRows` | Item rows | Linhas de itens | Linhas de itens |

New `AppIcons` constants (Material Outlined, rule `ui-project`): `AssessmentType = Category`,
`Scope = Public`, `Place = Place`, `Pending = RadioButtonUnchecked`. `Verified` (CheckCircle) is reused for
the checklist's done mark.

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
- 2026-09-26 — `AppStatusChip` gets its first real use on the `Users` list, which already paints
  `AccountStatus.Active` / `AccountStatus.Pending` through `.app-status`, and `AppItemRows` lives only in the
  gallery until F-35 brings the editions — the exam has no status field, so AC6 needs another screen to be
  checked on (owner, screen question 1).
- 2026-09-26 — Below 1280 px the form aside stacks under the form and is never hidden — it is the only place
  that lists what is still missing, and a phone is where that matters most (owner, screen question 2).
- 2026-09-25 — The mockup's drawer is chrome copied from the app as it is today, selected item included
  (`rgba(255,255,255,.14)` behind unchanged text). It does **not** show BR7's fix, so the owner will not see
  B-17 resolved in the mockup; the fix and its `ThemeContrastTests` pair are still part of this item. Checked
  in the mockup file, not reported by the design pass.

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
