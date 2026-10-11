---
feature: F-114
epic: Foundation and identity
status: idea
board: 162
version: 1
---
# Field kinds look like what they do

Technical terms: [glossary](../glossary.md)

## Summary
The kit's pickers must be told apart from a text box by their look, because the look says what the user can do. A fixed list (an enum, a language, a period) uses a combobox: expand arrow, picks by expanding and filters by typing in memory. Data read from the database uses the lookup (`AppLookupField`, search on the server): the arrow when the list opens before typing, the magnifier when typing is needed. Free text stays plain. Today `AppSelectField` hides the expand arrow (`Adornment.None`, `AppSelectField.razor:21`), so the "Area" field of the subject dialog looks like a text box. 20 uses of `AppSelectField` across 10 pages. Source: D-3 (`docs/discussions/D-3-field-affordance-combobox-lookup.md`).

## Start
- Depends on: nothing (the kit components exist; F-42 and F-43 are done).
- Waits on (to start): unknown — settled at `/agile:refine` (where `NoticeYearOptions` and the account event types come from, database or fixed).
- Needed to validate: the local app host, light and dark theme, keyboard only — owner.
- Suggested path: `/agile:refine` → `/agile:screen` (the three field kinds in the gallery) → `/agile:build`.
- Parallel with: unknown — settled at `/agile:refine`.
