---
bug: B-2
feature: F-5
status: done
board: 725
severity: high
---
# The app bar's account menu does not open

## What happens
1. Sign in with any account.
2. Click the account icon at the top right of the app bar.
3. Nothing happens: no dropdown opens, no error is shown. The tooltip ("Account menu for ...") still
   shows on hover.

Found on screen while validating F-6 (an Admin could not reach the new "Roles" item through the menu
button, only by typing the URL).

## Expected
Clicking the account icon opens the menu with "Sign out" (BR10, F-5, AC10).

## Cause
`src/Hosts/Simulab.Web/Components/Layout/UserMenu.razor`: the signed-in branch put a custom
`MudIconButton` inside `MudMenu`'s `ActivatorContent`. That button never receives the
`mud-menu-icon-button-activator` class MudMenu (9.9.0) wires its click handling to — that class is only
added when `MudMenu` renders its own activator from the `Icon` parameter. Confirmed live: the menu's
`mud-popover` stayed `mud-popover-open: false` after a real click, while `LanguageSwitch.razor` (which
uses `MudMenu`'s `Icon` parameter, no custom `ActivatorContent`) opened normally in the same page.

## Fix
Use `MudMenu`'s own `Icon`/`Color`/`AriaLabel` parameters instead of a custom `ActivatorContent`, the
same pattern `LanguageSwitch.razor` already used successfully. The outer `MudTooltip` (redundant with the
button's own `aria-label`) is dropped in the same change, since it was also nested around the activator.

## Regression test
- Screen-only. A bUnit test that clicks the rendered button and asserts the menu-item list appears
  (`SignedIn_ClickingTheAccountButton_OpensTheMenuWithSignOut`) was written and confirmed **failing** on
  the broken code (`Bunit.ElementNotFoundException` for `.app-user-menu-signout`) — but it still failed
  after the fix: `MudMenu`'s popover positioning depends on real browser JS (`MudPopoverProvider`), which
  bUnit does not run. `UserMenuTests.cs` already documented this limitation before the bug; the fix
  keeps it screen-only, covered by the validation script below.

## Open questions
- (none)

## Validation script
1. Sign in with any account → click the account icon at the top right → the menu opens showing "Sign out".
2. Click "Sign out" → the session ends and the app bar shows "Sign in" again.

## Delivery
- Branch: `feature/F-6` (found and fixed while validating F-6's screen check, not on its own `bug/B-2`
  branch). The owner confirmed this process exception on 2026-09-19.
- Fix: `2560c39` (fix(B-2): account menu never opened, MudMenu activator missing its class).
- Merge: `a0a6f02` (Merge feature/F-6: permissions and seed roles, AB#710).
- Board: AB#725, created after the fact on 2026-09-19 and closed with this evidence.
