---
bug: B-27
feature: F-34
status: idea
board: 164
severity: low
epic: Foundation and identity
---
# AppLookupField live region may keep the previous count after a search

Technical terms: [glossary](../glossary.md)

## What happens
`AppLookupField` sets `_announcement` inside `SearchOptionsAsync`, the callback MudBlazor calls for the search (`src/Hosts/Simulab.Web/Components/Ui/AppLookupField.razor`). The library re-renders its own subtree when the search returns, not this wrapper, so the visually hidden `aria-live="polite"` span may still hold the text of the previous search ("Results: 12", "Nothing found for ...") until something else re-renders the field.

Found by the F-114 retro (2026-10-11), on the sibling field `AppSelectField`: typing "MATEMA" left its live region at `Options: 12`, and after `InvokeAsync(StateHasChanged)` was added it read `Options: 1`. bUnit did not show it, because a test re-renders on assertion.

What F-114 already fixed: only `AppSelectField` (the new combobox). What is left: `AppLookupField`, same pattern, not verified there.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: a browser on `/dev/ui` and, ideally, a screen reader or the DOM read of the `aria-live` span — you.
- Suggested path: `/agile:refine B-27` to confirm the cause on screen first (type in a lookup of the gallery and read the `aria-live` span), then `/agile:build B-27`.
- Parallel with: any item that does not edit `AppLookupField.razor`.
- Cause not verified: measure it at `/agile:refine` with the symptom seen on `AppSelectField`.

## Expected
After every search the live region says the count (or "nothing found") of that search, never the previous one.

## Cause
Cause not verified for `AppLookupField`. The mechanism was confirmed on `AppSelectField` (F-114, 2026-10-11): the announcement is set in the library callback and the wrapper is not re-rendered; the fix there is `_ = InvokeAsync(StateHasChanged)` after setting it.

## Fix
To be settled at refinement (the same call after setting `_announcement`, and a browser check).

## Regression test
- (to be written at build; bUnit cannot see the stale text, so the check is on the gallery's rendered DOM)

## Open questions
- (none)

## Validation script
1. (written at build)

## Events

## Delivery
