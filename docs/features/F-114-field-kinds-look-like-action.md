---
feature: F-114
epic: Foundation and identity
status: refining
board: 162
version: 1
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

## Acceptance criteria
- AC1 Given any `AppSelectField`, when it is rendered, then an expand arrow is visible at the end of the field and the input accepts typing (it is not read-only).
- AC2 Given an `AppSelectField` with options, when the user focuses it, then the whole list opens, with every option, before any typing.
- AC3 Given the list is open, when the user types, then the list narrows in memory, ignoring case and accents, and a text with no match shows the localized "no results" message.
- AC4 Given a filter with an "all" first item, when the field already shows "All" and is focused, then the list shows every option, not only "All".
- AC5 Given the user picks an option, when the list closes, then `ValueChanged` fires once with that option's value, and picking "all" gives the null (or empty) value as today.
- AC6 Given a field with `LeadingIcon` and the arrow (BR5), when it is rendered, then both are shown, or the arrow is shown alone and the screen design records that the icon was dropped; in both cases the arrow is present.
- AC7 Given `AppLookupField` with the default parameters, when it is rendered, then it keeps the magnifier at the start and the minimum characters hint (no regression).
- AC8 Given the field in error or disabled, when it is rendered, then the arrow is muted when disabled and the field keeps its hint and error text.
- AC9 Given the arrow and the field, when the theme is light or dark, then the contrast ratio of the arrow against the field background passes the threshold asserted over the theme.
- AC10 Given the keyboard only, when the user tabs to the field, then arrow-down opens the list, arrow keys move, Enter picks and Esc closes without closing a dialog around the field.
- AC11 Given the 10 pages with an `AppSelectField`, when their existing page tests run, then they pass without changes to the call sites (the pages' behaviour is unchanged).
- AC12 Given `/dev/ui`, when it is opened, then the three field kinds appear with their states, in light and dark.
- AC13 All new texts appear in pt-BR, pt-PT and en (the "no results" text of the combobox and the accessible name of the arrow).

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

## Decisions
- 2026-10-11 — Rule of the field kind: the arrow is for any list the page holds whole, whatever its source; the magnifier is for what needs typing and the server — the owner chose the recommended option ("seta se a lista abre antes de digitar") — reading of 2026-10-11: none of the seven database lists has a search endpoint, so the D-3 literal "database = lookup with server search" would cost new endpoints for lists of some tens of rows. The caps of the roles, authors and organizers lists are not measured; the 100-item ceiling is the Api page cap, a convention.
- 2026-10-11 — "Update the kit controls" (owner, same answer) read as: the kit components, the gallery `/dev/ui` and the rule in `ui-project.md` carry the new pattern (BR8). `AppMultiPickField` and `AppSuggestField` are not changed; the owner corrects this at approval if the phrase meant them too.
- 2026-10-11 — Component: keep `AppSelectField`, replace its inside — the owner chose the recommended option. The call sites stay; the glossary term "combobox" already exists (F-75).
- 2026-10-11 — Seta x ícone — owner chose "try both in the kit" over the recommended "the arrow wins and the icon goes" (average 6.5 vs 7.3); kept. Falls back to the arrow alone if the prototype fails (BR5). Recorded as an override event.
- 2026-10-11 — The "all" choice stays as the first item of the list — the owner chose the recommended option; the list opens whole on focus (BR4).
- 2026-10-11 — The account event types are a fixed list in code (`AccountEventTypes.All`) and the notice years come from the Api's filters (database): resolved from the code, not asked.
- 2026-10-11 — No new package: `MudAutocomplete` is already used by `AppLookupField`; its `Adornment.End` arrow is verified in the rendered markup.

## Out of scope
- `AppMultiPickField` (F-75) and `AppSuggestField` (F-74): own anatomy, unchanged.
- Server-side search endpoints for the seven database lists (only if a list grows past the ceiling: a new item).
- Replacing the leading icons of F-43 on fields that are not pickers.
- New field kinds (date, number).

## Open questions
- (none)

## Change notes

## Validation script

## Events

- 2026-10-11 question — asked 4, from context 2
- 2026-10-11 override — Arrow x leading icon — chose "try both in the kit" over "arrow wins"

## Delivery
