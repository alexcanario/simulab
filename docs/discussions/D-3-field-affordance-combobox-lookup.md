---
discussion: D-3
status: decided
date: 2026-10-11
---
# Field affordance: combobox, lookup and text box

Technical terms: [glossary](../glossary.md)

## Idea
A control's look must say what the user can do with it. A combobox both searches its items and lets the user pick
one by expanding it; a text box only searches as the user types. Today the "Area" combobox of the subject dialog
looks like a text box.

## What we already know
- `AppSelectField` renders a `MudSelect` with `Adornment.None` when no leading icon is set
  (`Components/Ui/AppSelectField.razor:21`). The expand arrow of a `MudSelect` is its adornment, so the arrow is
  most likely hidden. Not yet seen on screen: to be confirmed on `/dev/ui` at refinement.
- A `MudSelect` cannot filter: it only opens and picks.
- `AppLookupField` (`MudAutocomplete`) filters by typing and shows a magnifier at the start; it has no arrow.
  `OpenOnFocus` plus `MinChars` 0 plus `MaxItems` already make it work for a short fixed list (F-42).
- `AppTextField`, `AppSelectField`, `AppLookupField` and `AppSuggestField` share the same outline, height and
  label, so nothing tells typing from picking.
- 20 uses of `AppSelectField` across 10 pages (counted 2026-10-11).
  - From the database (ids, `Guid?`): users role filter, role history role and author filters, catalog search
    organizer, subject dialog area, subjects area filter.
  - From a fixed list (enum, language, period): account language, account events type and period, role history
    period, exams type and scope filters, exam form type and language, catalog search type and scope, organizer
    kind.
  - Unclear: catalog search notice year (`NoticeYearOptions`), account events type (event codes).
- `ui.md`: one behaviour everywhere, defined once in the kit; so the change is in the kit and the pages follow.
- D-2 and F-43 set the visual language; this adds a rule about what each field kind looks like.

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — Arrow only | `AppSelectField` and `AppLookupField` get an arrow; the text box stays plain | S | Select and lookup still look alike |
| B — Arrow, magnifier, plain | Arrow = pick from a list that opens before typing; magnifier = search that needs typing; plain = type | M | Changes `ui-project.md` and the `/dev/ui` gallery |
| C — B plus own fill and border for pickers | Pickers get a distinct background | L | Departs from the Simulae composition decided in D-2 |

Recommendation: B — the arrow answers the "Area" case and the magnifier stays honest for catalog searches that
list only after two letters.

## Decisions
- 2026-10-11 — Take option B: arrow, magnifier, plain — the look says the action, as the owner asked.
- 2026-10-11 — A closed list may also be filtered by typing (owner's definition of a combobox: it both searches
  and picks by expanding) — so the "Area" field is not a plain select.
- 2026-10-11 — The kind of field follows where the data comes from: data read from the database uses the lookup
  (`AppLookupField`, filtering on the server); a fixed list (an enum, a language, a period) uses the combobox
  (filtering in memory, arrow). Owner's rule, in place of "convert every select to one component".
- 2026-10-11 — Reading to confirm at refinement: a lookup over a short database list opens before typing and shows
  the arrow; a lookup over a large catalog needs typing and keeps the magnifier.
- 2026-10-11 — The owner chose against the recommended "all selects become one filterable combobox"; scored 6.5
  against 6.5, kept.

## Parked
- Which component the combobox is: keep `AppSelectField` with a filter, or a new `AppComboboxField` — decided at
  `/agile:screen`, with the glossary term.
- Where `NoticeYearOptions` and the account event types come from (database or fixed) — read at refinement.
- How the "all" choice (null value) of a filter looks in a lookup — settled in the item's screen design.

## Outcome
- Epics: none (one feature)
- Features: F-114
- Bugs: none
