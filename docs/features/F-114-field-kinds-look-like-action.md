---
feature: F-114
epic: Foundation and identity
status: approved
board: 162
version: 2
---
# Field kinds look like what they do

Technical terms: [glossary](../glossary.md)

## Summary
The kit's pickers must be told apart from a text box by their look, because the look says what the user can do. A list that opens before the user types (fixed or read from the database and held whole by the page) shows an expand arrow and also filters by typing, in memory. A list that needs typing first (a large catalog, searched on the server) keeps the magnifier. Free text stays plain. Today `AppSelectField` renders a read-only `MudSelect` with `Adornment.None` (`AppSelectField.razor:21`), so the "Area" field of the subject dialog looks like a text box and cannot be filtered. 18 uses of `AppSelectField` on 10 pages, plus 4 in the gallery. Source: D-3 (`docs/discussions/D-3-field-affordance-combobox-lookup.md`).

## Start
- Depends on: nothing (the kit components exist; F-42 and F-43 are done).
- Waits on (to start): nothing. The two reads D-3 left open are settled in `## Decisions` (account event types are a fixed list in code, `AccountEventTypes.All`; notice years come from the Api's filters endpoint).
- Needed to validate: the local app host, light and dark theme, keyboard only — owner.
- Suggested path: `/agile:screen` (the three field kinds, with their states, in the gallery) → `/agile:build`.
- Parallel with: F-76, F-77, F-78, F-86, F-89 and F-95 are in other worktrees and some touch the same pages (`SubjectDialog`, `Subjects`, `CatalogSearch`); this item changes only the field component and the call sites' options, so expect small conflicts in those pages. Build it alone, after the merge of any item that edits the same page.

## Goal
Make the look of a field say what it does, once, in the kit, so the pages follow without changes.

## What already exists (verified 2026-10-11)
- `AppSelectField` (`MudSelect`, read-only input, no arrow either with or without a leading icon: rendered markup checked in a scratch bUnit test). 18 call sites on 10 pages: account language; account events type and period; role history role, author and period; users role; catalog search type, scope, organizer and notice year; exams type and scope filters; exam form type and language; organizer kind; subject dialog area; subjects area filter. Gallery: 4 more.
- `AppLookupField` (`MudAutocomplete`): magnifier at the start, `OpenOnFocus`, `MinChars`, `MaxItems`, clear button; used for the issuing authority, organizer on the edition form, the states, the topic's subject and the exams filter.
- `MudAutocomplete` and `MudSelect` (MudBlazor 9.9.0) each have one adornment slot (`Adornment` Start or End) plus `OpenIcon`, `CloseIcon` and `ClearIcon`; `MudAutocomplete` with `Adornment.End` renders the arrow as a button in the end slot (rendered markup checked).
- `AppMultiPickField` (F-75) and `AppSuggestField` (F-74) are separate fields with their own anatomy; this item does not change them.
- From a fixed list (11): account language; account events type and period; role history period; exams type and scope; exam form type and language; catalog search type and scope; organizer kind. From the database, loaded whole by the page (7): users role; role history role and author; catalog search organizer and notice year; subject dialog area; subjects area filter. None of the seven has a search-by-term endpoint.
- Leading icons (F-43 BR1) are set on 7 select fields.

## Users and use cases
- UC1 A back-office user sees that a picker opens a list: an arrow at the end of the field, and opens it by clicking, focusing or the keyboard.
- UC2 A back-office user narrows a list by typing, in a picker that shows the arrow (a closed list of some tens of items).
- UC3 A student or back-office user keeps the magnifier on a field that needs typing before it lists anything (a catalog search on the server).
- UC4 A user of any picker keeps the "all" choice of a filter exactly where it is today: the first item of the list.

## Business rules
- BR1 The look says the action: an arrow at the end of the field means "a list that opens before you type"; a magnifier at the start means "type first, the server answers"; a plain field means free text.
- BR2 A list the page already holds whole (a fixed list in code, or a list read from the database in one call, at most 100 items by convention, the Api page cap) uses the arrow, opens on focus, shows every item and filters by typing in memory. A list that can pass that or that the page does not hold whole uses the lookup with the magnifier and a server search (`AppLookupField`, unchanged).
- BR3 `AppSelectField` keeps its name and parameters (`Options`, `Value`, `ValueChanged`, `Required`, `Hint`, `Error`, `Disabled`, `LeadingIcon`); the 18 call sites do not change. Its inside changes to a filterable combobox.
- BR4 The "all" choice of a filter stays as the first item of the list (null or empty value), shown as the field's text. Focusing the field opens the whole list, not the list narrowed by the text "All".
- BR5 The leading icon (F-43 BR1) and the arrow are both kept when the kit can draw them without losing contrast, focus or the clear button. If the prototype in the gallery cannot, the arrow wins and the leading icon is dropped on that field.
- BR6 The arrow and the list are reachable by keyboard alone (arrow keys, Enter, Esc), announced as a combobox (`role="combobox"`, `aria-expanded`), and have a visible focus ring (rule `ui`).
- BR7 Colours of the arrow come from the theme tokens; its contrast pair is asserted over the theme (`ThemeContrastTests`) in both themes.
- BR8 The pattern is written once: `ui-project.md` gains the rule "what each field kind looks like" and the gallery shows the three kinds with their states.

## Screens and API
- No route and no endpoint changes; no new error code.
- Kit: `AppSelectField` (inside), `AppLookupField` (an optional arrow for a short list it holds, only if the screen design needs it; its default stays the magnifier), the CSS of the arrow, the gallery page `/dev/ui` (three field kinds, each with empty, with value, disabled, error, long text and loading states), `ui-project.md`.
- The seven database-sourced lists keep their `AppSelectField` call sites (BR3): they become arrow comboboxes that filter in memory, with no new endpoint.
- The app manual: the pages that have such fields describe the field in the three languages only if the text of a manual page names the control; checked at build.

### Screen: Field kinds gallery section (`/dev/ui`)
Mockup: `docs/features/mockups/F-114-field-kinds-look-like-action.html` (state, language and theme switchers; every colour is an identity token).

#### Placement and layout
- A new section `gallery-field-kinds` ("Field kinds") on `/dev/ui`, right after "Form fields" and before "Lookup field", so the rule is read before the lookup's own states. The existing sections keep their `AppSelectField` instances: they gain the arrow with no change to the page.
- Anatomy: `MudText h2` title, `MudText body2` hint (the rule in one sentence, `Gallery.FieldKinds.Hint`), then one `MudPaper Outlined` card (`gallery-form`) with an `AppFormGrid Columns="3"`: one column per kind, in the order arrow combobox, magnifier lookup, free text. Each column has a small heading (`Gallery.FieldKinds.Select` / `.Lookup` / `.Text`) and the kind's states stacked, each state preceded by a caption (`Gallery.FieldKinds.State.*`). A state a kind does not have shows the caption and `Gallery.FieldKinds.State.NotApplicable` instead of a field.
- Below 960 px the grid stacks to one column (what `AppFormGrid` already does).
- Nothing is saved: the fields hold their value in the page, like every other gallery field.

#### Anatomy of each kind (BR1)
| Kind | Component | Start of the field | End of the field | Opens | Narrows by typing |
| --- | --- | --- | --- | --- | --- |
| Arrow combobox | `AppSelectField` (inside: `MudAutocomplete`, `Variant.Outlined`, `Margin.Dense`, `OpenOnFocus`, `MinCharacters=0`, `MaxItems` = the options count, `CoerceText`, `Clearable=false`) | optional decorative `LeadingIcon` (F-43) | arrow `AppIcons.Expand` (closed) / `AppIcons.Collapse` (open), a button | on focus, click or Arrow-down, whole list | in memory, case and accent insensitive |
| Magnifier lookup | `AppLookupField` (unchanged: `MudAutocomplete`, magnifier `Adornment.Start`, `MinChars`, clear button) | magnifier `AppIcons.Search` | clear button when a value is set | after `MinChars` typed (or on focus when `OpenOnFocus`, F-42) | on the server |
| Free text | `AppTextField` (unchanged) | optional decorative `LeadingIcon` | nothing (or the page's own adornment, e.g. the password reveal) | never | n/a |

- `AppIcons.Expand` = `Icons.Material.Outlined.ArrowDropDown`, `AppIcons.Collapse` = `Icons.Material.Outlined.ArrowDropUp`: the library's defaults are the Filled family, which `ui-project.md` forbids. Two new constants in `AppIcons`, listed in the gallery's icon section automatically.
- The combobox has no clear button: every select of the app reaches null through an explicit first option ("All" on a filter, "No area" on the subject dialog; verified in `Subjects.razor:94` and `SubjectDialog.razor:65`), and a second way to the same value would be a second pattern (rule `ui`). The lookup keeps its clear button (BR2, unchanged).
- The arrow is 24 px, inside a 32 px round hit target (rule `ui`: at least 24 px), `tabindex="-1"`: the keyboard reaches the list through the input (Arrow-down), so a field stays one Tab stop. Clicking the arrow toggles the list and keeps focus in the input.
- Text of the input while closed: the selected option's text (`TextOf`), as today. While the user types, the text is the typed term; on blur with no pick, the text goes back to the selected option (`CoerceText`) and `ValueChanged` does not fire (AC5).
- BR4: the term that equals the selected option's text does not filter. Focusing a field that shows "All" lists every option; filtering starts when the typed text differs from the selected option's text.

#### States (each shown in the gallery for each kind that has it)
| State | Arrow combobox | Magnifier lookup | Free text |
| --- | --- | --- | --- |
| Empty | no value, placeholder-free (the first option is the empty choice), arrow at the end, hint below | placeholder, magnifier, default hint `Lookup.Hint.MinChars` | placeholder, hint |
| With value | option text in the input, arrow | chosen text, magnifier at the start, clear button at the end | the text |
| Disabled | `Disabled`: text and arrow in the library's disabled ink (no token; an inactive control is exempt from the contrast minimum, WCAG 1.4.3 and 1.4.11), `cursor: not-allowed`, the hint stays (AC8) | same, magnifier muted | same |
| Error | border and text `error`, error text replaces the hint, `aria-invalid`, arrow keeps `actionDefault` (AC8) | same | same |
| Long text | an option wider than the field wraps in the list (several lines, no `nowrap`); in the closed input it scrolls as native text; a long error text wraps below | same for an option and its secondary text | a long error text wraps below |
| Loading | the page is still fetching the options: a 16 px `MudProgressCircular` (`primary`) replaces the arrow, the input accepts focus, the list shows `SelectField.Loading`; see open point 2 | the server search running: the library's progress indicator inside the list (as today) | n/a: a free text field has no loading state of its own; the form's `AppLoadingState` covers it |
| Open list | `MudPopover` under the field, same width, max 300 px with scroll, `surface` background, `divider` border, radius 10 px; every option, the first being the empty choice; the selected option has `aria-selected="true"` and a `primaryLighten` background; the keyboard-focused option has the focus ring; hover `primaryLighten`; arrow turns to `AppIcons.Collapse` | the results after `MinChars`, each with text and secondary text | n/a |
| No results | the typed term matches nothing: one non-interactive line `SelectField.NoResults` with the term; Enter does nothing; Esc or blur restores the selected option's text | `Lookup.NoResults` (today) | n/a |
| Leading icon + arrow (BR5) | `LeadingIcon` at the start, text, arrow at the end: both drawn; see "BR5 prototype" | the `LeadingIcon` of F-43 replaces the magnifier today; see open point 3 | `LeadingIcon` at the start (F-43, unchanged) |

#### BR5 prototype: leading icon and arrow together
- The mockup shows both: icon (20 px, `actionDefault`, `aria-hidden`) at the start, arrow (24 px button, `actionDefault`) at the end, the input text between them with 8 px gaps, the focus ring around the whole outline, the error border unchanged. Nothing is lost: there is no clear button on the combobox (above), the focus ring is the outline's, and both icons keep the `actionDefault` contrast.
- The library has one adornment slot per field (`## What already exists`). Drawing both therefore needs the kit to draw one of the two itself: the arrow stays in the library's slot (`Adornment.End`, `OpenIcon`/`CloseIcon`, a real button that toggles the list) and the leading icon is drawn by `AppSelectField` as a `MudIcon` inside the field's wrapper, placed over the input's left padding. This is open point 1: if the build cannot keep the three conditions of BR5 with it, the arrow wins and the icon is dropped on the 7 select fields with a `LeadingIcon`, and AC6 records it.

#### Colours (all existing tokens; no new colour)
- Arrow, magnifier, leading icon: `actionDefault` (`#4A5A7A` light, `#7A8EB0` dark). The field is transparent, so the icon sits on `surface` (cards, dialogs, the table toolbar) or on `background` (a field straight on the page). Contrast: light 6.92:1 on `surface`, 6.34:1 on `background`; dark 4.89:1 on `surface`, 5.36:1 on `background`. All above 3:1 for a UI part; `ThemeContrastTests` adds the pair `actionDefault` over `surface` and `background` under the name of the arrow (BR7, AC9), if it is not already asserted there.
- Border `linesInputs`; focus ring `primary` (2 px); error `error`; option hover and selected `primaryLighten` with `textPrimary` (already measured on this surface, DESIGN.md); list `surface` with `divider` border; hint `textSecondary`; progress `primary`.
- Disabled: the library's disabled ink (translucent, no token, as every disabled field of the app today).

#### Keyboard (AC10)
- Tab: lands on the input (one stop per field; the arrow and the leading icon are not stops).
- Arrow-down on a closed combobox: opens the whole list and moves to the first option; Arrow-up/down move; Home/End first/last option (library behaviour, unverified (gallery DOM)); Enter picks and closes; Esc closes the list only, the dialog around it stays open; Tab from an open list closes it without picking (text restored).
- Typing with the list open narrows it; the focused option follows to the first match.
- Lookup and text: unchanged.

#### Accessibility
- The input of the combobox: `role="combobox"`, `aria-expanded` (true while the list is open), `aria-autocomplete="list"`, `aria-haspopup="listbox"`, `aria-controls` = the list's id, `aria-labelledby` the label, `aria-describedby` the hint/error, `aria-invalid`, `aria-required`, `autocomplete="off"`: the same set `AppLookupField` already writes through `UserAttributes` (it found that `MudAutocomplete` 9.9 renders a plain text input; see unverified claims below).
- The arrow: a `button` with `aria-label` `SelectField.Expand` while closed and `SelectField.Collapse` while open, `tabindex="-1"`. The leading icon and the magnifier: `aria-hidden="true"` (decorative, F-43). The clear button of the lookup: unchanged (`Lookup.Clear`).
- The list: `role="listbox"` with `role="option"` items and `aria-selected` on the chosen one (library markup, unverified (gallery DOM)).
- Announcements, through the same `aria-live="polite"` span `AppLookupField` has: on open or filter `SelectField.Results` with the count; on no match `SelectField.NoResults`; on loading `SelectField.Loading`.
- A screen reader on Tab: "Area, combobox, collapsed, All, required, Optional. Groups the subject with similar ones." (label, role, state, value, required, description). On Arrow-down: "expanded, list, All, 1 of 9". On typing "mat": "Options: 1", then "Mathematics, 1 of 1".
- Focus ring: the field's outline turns `primary`, 2 px, in both themes; the option in focus has the same ring inside the list.

#### UI texts (SharedResources; new keys)
| Key | en | pt-BR | pt-PT |
| --- | --- | --- | --- |
| `SelectField.Expand` | Show the options | Mostrar as opções | Mostrar as opções |
| `SelectField.Collapse` | Hide the options | Ocultar as opções | Ocultar as opções |
| `SelectField.NoResults` | No option matches "{0}". | Nenhuma opção corresponde a "{0}". | Nenhuma opção corresponde a "{0}". |
| `SelectField.Results` | Options: {0} | Opções: {0} | Opções: {0} |
| `SelectField.Loading` | Loading the options... | Carregando as opções... | A carregar as opções... |
| `Gallery.Section.FieldKinds` | Field kinds | Tipos de campo | Tipos de campo |
| `Gallery.FieldKinds.Hint` | The look says the action: an arrow at the end opens the list before you type and narrows it as you type; a magnifier at the start needs typing first, and the server answers; a plain field is free text. | A aparência diz a ação: a seta no fim abre a lista antes de digitar e a estreita conforme se digita; a lupa no início pede que se digite primeiro, e o servidor responde; um campo simples é texto livre. | O aspeto diz a ação: a seta no fim abre a lista antes de escrever e estreita-a à medida que se escreve; a lupa no início pede que se escreva primeiro, e o servidor responde; um campo simples é texto livre. |
| `Gallery.FieldKinds.Select` | Arrow combobox (AppSelectField) | Caixa de combinação com seta (AppSelectField) | Caixa de combinação com seta (AppSelectField) |
| `Gallery.FieldKinds.Lookup` | Magnifier lookup (AppLookupField) | Busca com lupa (AppLookupField) | Pesquisa com lupa (AppLookupField) |
| `Gallery.FieldKinds.Text` | Free text (AppTextField) | Texto livre (AppTextField) | Texto livre (AppTextField) |
| `Gallery.FieldKinds.State.Empty` | Empty | Vazio | Vazio |
| `Gallery.FieldKinds.State.WithValue` | With value | Com valor | Com valor |
| `Gallery.FieldKinds.State.Disabled` | Disabled | Desabilitado | Desativado |
| `Gallery.FieldKinds.State.Error` | Error | Erro | Erro |
| `Gallery.FieldKinds.State.LongText` | Long text | Texto longo | Texto longo |
| `Gallery.FieldKinds.State.Loading` | Loading | Carregando | A carregar |
| `Gallery.FieldKinds.State.Open` | Open list | Lista aberta | Lista aberta |
| `Gallery.FieldKinds.State.NoResults` | No results | Sem resultados | Sem resultados |
| `Gallery.FieldKinds.State.LeadingIcon` | Leading icon | Ícone no campo | Ícone no campo |
| `Gallery.FieldKinds.State.NotApplicable` | This kind has no such state. | Este tipo não tem esse estado. | Este tipo não tem esse estado. |

Existing keys reused by the section: `Subjects.Field.Area`, `Subjects.Field.Area.Help`, `Subjects.NoArea`, `Common.Filter.All`, `Area.*`, `Exams.Field.IssuingAuthority`, `Exams.Field.IssuingAuthority.Placeholder`, `Exams.Field.IssuingAuthority.Hint`, `Exams.Field.Name`, `Exams.Field.Name.Placeholder`, `Exams.Field.Name.Hint`, `Exams.Field.ContentLanguage`, `Lookup.Hint.MinChars`, `Lookup.NoResults`, `Lookup.Results`, `Lookup.Clear`, `Common.Search`, `exam.issuing_authority_required`, `exam.name_required` (if absent, the build picks the existing required-name code of the exam form).

#### Permissions
- `/dev/ui` is Development-only, as today; no permission and no role difference. The field kinds behave the same for every role on every page.

## Acceptance criteria
- AC1 Given any `AppSelectField`, when it is rendered, then an expand arrow is visible at the end of the field and the input accepts typing (it is not read-only).
- AC2 Given an `AppSelectField` with options, when the user focuses it, then the whole list opens, with every option, before any typing.
- AC3 Given the list is open, when the user types, then the list narrows in memory, ignoring case and accents, and a text with no match shows the localized "no results" message.
- AC4 Given a filter with an "all" first item, when the field already shows "All" and is focused, then the list shows every option, not only "All".
- AC5 Given the user picks an option, when the list closes, then `ValueChanged` fires once with that option's value, and picking "all" gives the null (or empty) value as today.
- AC6 Given a field with `LeadingIcon` and the arrow (BR5), when it is rendered, then both are shown, or the arrow is shown alone and the screen design records that the icon was dropped; in both cases the arrow is present.
- AC7 Given `AppLookupField` with the default parameters, when it is rendered, then it keeps the magnifier at the start and the minimum characters hint (no regression); given it also receives `LeadingIcon`, then it still shows the magnifier and the parameter is ignored (v2).
- AC8 Given the field in error or disabled, when it is rendered, then the arrow is muted when disabled and the field keeps its hint and error text.
- AC9 Given the arrow and the field, when the theme is light or dark, then the contrast ratio of the arrow against the field background passes the threshold asserted over the theme.
- AC10 Given the keyboard only, when the user tabs to the field, then arrow-down opens the list, arrow keys move, Enter picks and Esc closes without closing a dialog around the field.
- AC11 Given the 10 pages with an `AppSelectField`, when their existing page tests run, then they pass without changes to the call sites (the pages' behaviour is unchanged).
- AC12 Given `/dev/ui`, when it is opened, then the three field kinds appear with their states, in light and dark; the open-list and no-results states appear on the live field, with a caption saying what to type (v2).
- AC13 All new texts appear in pt-BR, pt-PT and en: `SelectField.Expand`, `.Collapse`, `.NoResults`, `.Results` and `.Loading`, plus the gallery keys of the screen section (v2).
- AC14 Given `AppSelectField` with `Loading=true`, when it is rendered, then a 16 px progress replaces the arrow and the list shows `SelectField.Loading`; with `Loading=false` (the default) nothing changes and no call site is edited (v2).

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

## Decisions
- 2026-10-11 — Rule of the field kind: the arrow is for any list the page holds whole, whatever its source; the magnifier is for what needs typing and the server — the owner chose the recommended option ("seta se a lista abre antes de digitar") — reading of 2026-10-11: none of the seven database lists has a search endpoint, so the D-3 literal "database = lookup with server search" would cost new endpoints for lists of some tens of rows. The caps of the roles, authors and organizers lists are not measured; the 100-item ceiling is the Api page cap, a convention.
- 2026-10-11 — "Update the kit controls" (owner, same answer) read as: the kit components, the gallery `/dev/ui` and the rule in `ui-project.md` carry the new pattern (BR8). `AppMultiPickField` and `AppSuggestField` are not changed; the owner corrects this at approval if the phrase meant them too.
- 2026-10-11 — Component: keep `AppSelectField`, replace its inside — the owner chose the recommended option. The call sites stay; the glossary term "combobox" already exists (F-75).
- 2026-10-11 — Seta x ícone — owner chose "try both in the kit" over the recommended "the arrow wins and the icon goes" (average 6.5 vs 7.3); kept. Falls back to the arrow alone if the prototype fails (BR5). Recorded as an override event.
- 2026-10-11 — The "all" choice stays as the first item of the list — the owner chose the recommended option; the list opens whole on focus (BR4).
- 2026-10-11 — The account event types are a fixed list in code (`AccountEventTypes.All`) and the notice years come from the Api's filters (database): resolved from the code, not asked.
- 2026-10-11 — No new package: `MudAutocomplete` is already used by `AppLookupField`; its `Adornment.End` arrow is verified in the rendered markup.
- 2026-10-11 — Screen, open point 1 (icon + arrow): try both in the gallery, fall back to the arrow alone — the owner chose the recommended option (BR5 unchanged).
- 2026-10-11 — Screen, open point 2: `AppSelectField` gets an optional `Loading` bool (default false); the 7 database lists may opt in later — the owner chose the recommended option (AC14).
- 2026-10-11 — Screen, open point 3: `AppLookupField` ignores `LeadingIcon` and always shows the magnifier; the parameter stays so call sites compile — the owner chose the recommended option (AC7). Call sites that pass `LeadingIcon` to a lookup lose the icon.
- 2026-10-11 — Screen, open points 4 and 5: `SelectField.Results` and `.Loading` belong to AC13; the gallery shows the live field with a caption for the open and no-results states — the owner chose the recommended option. Point 6 (content is not translated): no change.

## Out of scope
- `AppMultiPickField` (F-75) and `AppSuggestField` (F-74): own anatomy, unchanged.
- Server-side search endpoints for the seven database lists (only if a list grows past the ceiling: a new item).
- Replacing the leading icons of F-43 on fields that are not pickers.
- New field kinds (date, number).

## Open questions
- (none)

## Change notes

### v2 — 2026-10-11
- What: the screen design (section "Screen: Field kinds gallery section" and the mockup `docs/features/mockups/F-114-field-kinds-look-like-action.html`) is added and approved with the item; the owner answered its four open points.
- Why: `/agile:screen` ran after the approval of v1.
- Affected: AC6 (re-approved, text unchanged: the mockup shows icon and arrow together, the gallery proves it or the arrow-alone fallback applies), AC7 (lookup ignores `LeadingIcon`), AC12 (open-list and no-results states on the live field), AC13 (five keys), AC14 (new: `Loading` parameter of `AppSelectField`).
- Not affected: AC1-AC5, AC8-AC11, BR1-BR8, the 18 call sites (BR3 holds, `Loading` is optional).
- Re-approved: 2026-10-11 (owner, "Aprovo").

## Validation script

## Events

- 2026-10-11 question — asked 4, from context 2
- 2026-10-11 override — Arrow x leading icon — chose "try both in the kit" over "arrow wins"
- 2026-10-11 change — v2: screen design approved; AC7, AC12, AC13 changed, AC14 added

## Delivery
