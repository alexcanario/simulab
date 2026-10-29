---
title: Date range and relative time
summary: The kit controls for a period and for a moment in the past (date range field, relative time, date text), what each does to the value and which screens use them
items: [F-185]
---
# Date range and relative time

Technical terms: [glossary](../glossary.md)

## Trigger
A user picks the period of a list, or reads when something happened. The page picks the control by the kind of date (catalog: `docs/design/data-type-controls.md`, 2.3). The two audit lists, account events and role history, are the first users.

## Steps
1. `AppDateRangeField` holds an `AppDateRange(From, To, Days)`: a row of toggle buttons (`aria-pressed`, the chosen one bold with a heavier border) for the optional "all" button (`AllText`), the "last n days" presets (`DayPresets`, 7, 30 and 90 by default), "This year" (`ThisYear`) and "Custom range" (`AllowCustom`). Choosing a preset reports the range up to today (`TimeProvider`) and, for a days preset, `Days`; "all" reports the empty range.
2. "Custom range" shows two `AppDateField`s. A start after the end shows `DateRange.Invalid` and reports nothing; either end may stay empty (open-ended).
3. A page whose Api takes a number of days (account events and role history: 7, 30 or 90, anything else is refused) turns off `ThisYear` and `AllowCustom` and reads `Days`; it gives the field `AppDateRange.OfDays(DaysFilter)`.
4. `AppRelativeTime` writes a moment in the past in the biggest unit ("Just now", "5 minutes ago", "3 days ago", "2 months ago", "2 years ago", each in the language) from `TimeProvider`, in a `<time datetime>` whose tooltip is the exact date and time. A moment in the future reads "Just now".
5. The "When" cell of both audit lists keeps the exact time (`g`, in the user's time zone) and adds the relative time under it: an audit record is never only "3 days ago".
6. `AppDateText` shows a calendar day in the culture (`d`), a dash for none; `AppDateRange.Format` writes a range as "01/10/2026 – 27/10/2026".

## Rules
- The Api, its period check and its error codes did not change. "This year" and the custom range exist in the kit but filter nothing yet: they need an Api that takes `from` and `to`.
- The period is kept in the address as `days`, as before.

## Wireframe
```text
+----------------------------------------------+
| Period                              [changed]|
| [All time] [Last 7 days] [Last 30 days] [..90]|
|                                              |
| When                          Event  [changed]|
| 10/29/2026, 3:04 PM           Sign in        |
| 3 days ago                                   |
+----------------------------------------------+
```

```text
+----------------------------------------------+
| Dates                                   [new]|
| Range: presets | this year | custom | error  |
| Relative time: now | minutes | days | years  |
| Date text: date | none | range               |
+----------------------------------------------+
```

## Errors
- `DateRange.Invalid` — the custom range starts after it ends.
