---
bug: B-27
feature: F-34
status: done
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
- Suggested path: `/agile:build B-27` (cause read in the code at refinement; the on-screen reading is the first build step).
- Parallel with: any item that does not edit `AppLookupField.razor` or `AppSuggestField.razor`.

## Expected
After every search the live region says the count (or "nothing found") of that search, never the previous one; a failed search shows its "try again" message at once.

Acceptance criteria:
1. Given an `AppLookupField` that has announced "Results: N" for a term, when the reader types another term and the search returns, then the `aria-live` span holds the count of that second search (or "Nothing found for ..."), without any other render.
2. Given an `AppLookupField` whose search throws, when the search returns, then the description shows "The search failed." with the "try again" button, without any other render.
3. Given an `AppSuggestField` that has announced "<label>: N" for a text, when the reader types another text, then the `aria-live` span holds the count of that second text, without any other render.
4. Given the gallery `/dev/ui` open through the app host, when a lookup is searched twice with different terms, then the DOM of the `aria-live` span read after each search shows that search's text (checked on screen, validation script).

## Cause
Verified in today's code (2026-10-12; the files were last touched by F-114, F-74, F-42 and F-43):
- `src/Hosts/Simulab.Web/Components/Ui/AppLookupField.razor:166-187`: `SearchOptionsAsync` is the `SearchFunc` of `MudAutocomplete` (line 16). It sets `_announcement` (lines 174-176, and 183 on a failed search) and `_searchFailed` (line 182) and returns without asking the wrapper to render. The library re-renders its own subtree when the search returns; the wrapper does not, so the `aria-live` span (line 71) and the description with the "try again" button (lines 54-61) keep the previous render until something else re-renders the field.
- The mechanism is the one confirmed on screen for `AppSelectField` (F-114): `AppSelectField.razor:170-172` fixes it with `_ = InvokeAsync(StateHasChanged)` right after setting `_announcement`. `AppLookupField` has no such call.
- Not yet read on screen for `AppLookupField` itself: the on-screen reading of the `aria-live` span is the first step of the validation script and of the build's check (the AppHost is not started at refinement).

Duplicates (the same defect in another file, the same shape):
- `src/Hosts/Simulab.Web/Components/Ui/AppSuggestField.razor:121-127`: `SearchSuggestionsAsync` is the `SearchFunc` (line 23), sets `_announcement` (line 124) and returns, with no render request. Same defect; the field is used by the notice group name (F-74/F-75).
- `AppMultiPickField.razor:326-333`: `_results` is set in the wrapper's own `@oninput` handler, which re-renders the wrapper. Not affected.
- `AppDataTable.razor:209` already calls `InvokeAsync(StateHasChanged)` after setting its announcement (F-50). Not affected.
- `AppSelectField.razor:172`: fixed by F-114.

## Fix
- `AppLookupField.SearchOptionsAsync`: after setting `_announcement` (success and failure paths), request a render with `_ = InvokeAsync(StateHasChanged)`, the call `AppSelectField` already has. The failure path matters too: without it the "search failed, try again" description may not appear.
- `AppSuggestField.SearchSuggestionsAsync`: the same call after line 124.
- No new component, no new parameter, no new resource key.

## Regression test

Tests locked: e39e41049f58bfa75e1b99b1bad9a953a8608dfa (2 files); unlocks: 1

- One test per fixed component (`AppLookupFieldTests`, `AppSuggestFieldTests` in `tests/Hosts/Simulab.Web.Tests/Ui/`), seen failing before the fix: the search runs and the test reads the markup the wrapper last rendered, without forcing a render (a bare `WaitForAssertion` that re-renders is what hid the defect in F-114). The way to observe "no render happened" (render count of the wrapper, or the markup captured before the assertion) is settled at build with a throwaway probe before the test is locked.
- Criterion 4 is a validation step, not a test: bUnit does not drive MudBlazor's own re-render.

## Decisions
- 2026-10-12: fix `AppLookupField` and `AppSuggestField` in this item — owner chose "Corrigir os dois aqui" (the recommended one). Reason: same one-line defect, same project rule (F-114), leaving one keeps a known bug.
- 2026-10-12: one regression test per component, no source-reading guard test — owner chose the recommended option. Reason: three components, a rule already in `project.md`; a guard that reads `.razor` text is fragile.
- 2026-10-12: the failed-search path of `AppLookupField` is covered by the same fix (technical, no product impact): `_searchFailed` also changes inside the library callback.

- 2026-10-12: tests unlocked (owner, build): the lookup test used three equal options and Blazor rejects duplicate keys once the field re-renders; only the test data changed (ids), not the assertions.

- 2026-10-12: at ship the gate was red from main's `db6ebb1d` (resources renamed `simulab-api`, `simulab-web`, `simulab-mailpit`, `simulab-redis`). The rename also broke the app (the Web resolves `https+http://api`, both hosts call `AddRedisClient("redis")`, the Api reads `ConnectionStrings:mailpit`). Owner asked to fix the tests; the fix is in `AppHost.cs`: resource names back to `api`, `web`, `mailpit`, `redis`, the container names stay `simulab-*`. The AppHost and architecture tests pass unchanged. Outside this bug's scope, shipped with it on the owner's request.

## Out of scope
- A guard test over the kit's sources (decided above); `AppSelectField` (done in F-114); `AppMultiPickField` and `AppDataTable` (not affected).

## Open questions
- (none)

## Validation script
Needed to validate: a browser on `/dev/ui` (nothing else; in place now).

1. Stop any app host of this worktree, then start it with its own database. Git Bash: `Database__Name=simulab_b27 dotnet run --project src/Hosts/Simulab.AppHost`. PowerShell 7: `$env:Database__Name = "simulab_b27"; dotnet run --project src/Hosts/Simulab.AppHost`. Open `https://localhost:7125/dev/ui`. The data is the shared PostgreSQL server, database `simulab_b27` (the gallery needs no data).
2. (AC1, AC4) In the section of the lookup fields, click the "Issuing authority" field with the empty value and type `ni`. Wait a second. In the browser console run `document.querySelector('#gallery-kind-lookup-empty').closest('.app-field').querySelector('[aria-live]').textContent`. Expected: `Results: 2`.
3. With the list still open, keep typing so the text becomes `nic` (select all and type `nic` if the first letters were replaced). Run the same line again. Expected: `Results: 1` (before the fix it still said `Results: 2`).
4. Type `zzz`. Run the same line. Expected: `Nothing found for "zzz".`
5. (AC2) In the gallery, use the lookup field that fails on purpose (`#gallery-lookup-failing`) and type two letters, then two more. Expected: the line "The search failed." with a "Try again" button appears under the field right after each search, with no other click.
6. (AC3) In the gallery section of the suggest field (`#gallery-suggest`), type `c`, run the same console line with `#gallery-suggest` in place of the id, then type `o`. Expected: the span holds the count of the latest text each time.
7. Keyboard only: Tab to the lookup, type `ni`, arrow down and Enter. Expected: the value is chosen and the live region keeps reading the last count.

## Events

- 2026-10-12 question — asked 2, from context 3
- 2026-10-12 blocked — test wrong: test data used three equal options; Blazor rejects duplicate keys once the field re-renders
- 2026-10-12 test-fail — tests failed (Simulab.slnx): Simulab.AppHost.Tests.RequiredSettingsTests.Publish_EveryRequiredSettingHasASource(environm, 1 red

## Delivery
- Fix: `AppLookupField.RequestRender()` (`_ = InvokeAsync(StateHasChanged)`) after `_announcement` on the success and failure paths; the same call in `AppSuggestField.SearchSuggestionsAsync`.
- Tests: `AppLookupFieldTests.Search_Twice_TheLiveRegionHoldsTheCountOfTheSecondSearch`, `Search_SecondOneFails_ShowsTheFailureWithoutAnotherRender`, `AppSuggestFieldTests.Typing_Twice_TheLiveRegionHoldsTheCountOfTheSecondText`; seen failing before the fix (3 failed, 25 passed), green after. Gate: `agile gate GREEN` (Web 1445 passed, whole suite green). Scan: `agile scan GREEN`.
- On screen (AppHost, `/dev/ui`, `gallery-kind-lookup-empty`): `ni` read `Results: 2`; after `ci` the span read `Results: 1` with the list open.
- Also shipped, on the owner's request: `AppHost.cs` resource names back to `api`, `web`, `mailpit`, `redis` (main's `db6ebb1d` had prefixed them and broke service discovery, `AddRedisClient("redis")` and 12 AppHost tests); container names stay `simulab-*`.
- Branch `bug/B-27`, merge commit `c840abdf`, version 0.29.1. Ship gate: `agile gate GREEN` (Web 1445, AppHost 80, Api 58, Catalog 512, Identity 501, QuestionBank 98, Jobs 25; architecture 219). Scan `agile scan GREEN`, docs `agile docs GREEN`, test lock unchanged (1 unlock, recorded in Decisions).
- Manual: no change (a screen reader announcement, no visible behavior).
