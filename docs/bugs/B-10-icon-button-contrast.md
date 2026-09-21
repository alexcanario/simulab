---
bug: B-10
feature: F-1
status: approved
board: 737
severity: medium
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# Icon buttons below 3:1 contrast

## What happens
Every icon-only button of the kit (row actions such as Edit, Delete and History, the filter chip's remove button) draws its icon in the theme token `ActionDefault`, which is under the 3:1 contrast WCAG 2.2 asks for icons: 1.94:1 in the dark theme (`#404E6A` on the surface `#172035`) and 2.97:1 in the light theme (`#8A96B0` on white). Present since F-1; measured on the kit gallery during the F-14 build (2026-09-21).
1. Open `/dev/ui` in dark mode and look at the row actions of the table → the icons are barely visible.

## Expected
Every enabled action icon of the kit (icon-only buttons and menu item icons) reads at 3:1 or more against the
card and the page background, in both themes, and `ThemeContrastTests` asserts it. Disabled icons keep their
current look (D2).

## Cause
Confirmed in code on 2026-09-21. The premise holds, with one correction: Delete is not affected.

`ActionDefault` is `#8A96B0` in the light palette (`src/Hosts/Simulab.Web/Theme/SimulabTheme.cs:28`) and
`#404E6A` in the dark one (`:61`). MudBlazor 9.9.0 paints `.mud-icon-button` in `var(--mud-palette-action-default)`
(`MudBlazor.min.css`), and also `.mud-menu-item > .mud-icon-root` and `.mud-list-item-icon`. Measured:

| Theme | On surface (card, menu) | On background (page) |
|---|---|---|
| Light | 2.97:1 | 2.72:1 |
| Dark | 1.94:1 | 2.13:1 |

WCAG 2.2 1.4.11 asks 3:1 for an icon that is the only sign of a control. `ThemeContrastTests` does not check
`ActionDefault`.

Occurrences (each reads the palette token, none sets its own colour):
- `src/Hosts/Simulab.Web/Components/Ui/AppRowActions.razor:9-10`: the visible row actions with `Color.Default`
  (Edit, History and any `MoreActions`). Delete uses `Color.Error` (`:10`), which already reads at 5.05:1 or more.
- `src/Hosts/Simulab.Web/Components/Ui/AppRowActions.razor:19`: the "More" menu button; its items' icons (`:27`)
  also take the token.
- `src/Hosts/Simulab.Web/Components/Ui/AppFilterChip.razor:7`: the remove button; the chip has no fill
  (`app.css:509`), so it sits on the card or the page.
- `src/Hosts/Simulab.Web/Components/Layout/UserMenu.razor:15-16`: the Account and Sign out item icons in the
  user menu (on the menu's surface).
- Not affected: the app bar buttons (`MainLayout.razor:21`, `ThemeSwitch.razor:7`, `UserMenu.razor:11`) use
  `Color.Inherit`, the app bar text colour.

No duplicate found: no CSS in `app.css` and no component sets an icon colour of its own; the two hex values
appear only in the palette (and in the F-4 and F-5 mockups, which are not shipped).

## Fix
Fix the token in the palette, once, as B-8 did for primary (D1).

- `PaletteLight.ActionDefault` `#8A96B0` → the light `TextSecondary` `#4A5A7A`: 6.92:1 on the surface, 6.34:1 on
  the background.
- `PaletteDark.ActionDefault` `#404E6A` → the dark `TextSecondary` `#7A8EB0`: 4.89:1 on the surface, 5.36:1 on
  the background.
- A comment on each changed line, as B-8 left on primary, points to `ThemeContrastTests`.
- No markup change: every occurrence in `## Cause` reads the token. `ActionDefaultHover` and `ActionDisabled`
  stay as they are.

## Regression test
- `ThemeContrastTests.ActionIcon_ReadsAtNonTextMinimumOnItsSurfaces` — `tests/Hosts/Simulab.Web.Tests`
  (theory: light/dark × surface/background, minimum 3:1). Before the fix all 4 cases fail (2.97, 2.72, 1.94, 2.13).

## Acceptance criteria
| # | Criterion | Test |
|---|---|---|
| AC1 | Given either theme, when an enabled action icon is shown on the card or the page background, then it reads at 3:1 or more. | `ThemeContrastTests.ActionIcon_ReadsAtNonTextMinimumOnItsSurfaces` |
| AC2 | Given the dark theme, when a table row with actions, the "More" menu, the user menu and a filter chip are shown, then their icons are clearly visible and Delete stays red. | Validation script (on screen) |
| AC3 | Localization: no UI text is added or changed; the missing-key test stays green. | `ResourceParityTests` (existing) |

## Decisions
- D1 (owner, 2026-09-21): `ActionDefault` takes each theme's `TextSecondary`. Reason: no new colour in the
  palette, and a wide margin above 3:1 (4.89:1 at the lowest).
- D2 (owner, 2026-09-21): disabled icons (`ActionDisabled`) are out of scope. Reason: WCAG 2.2 1.4.11 exempts
  inactive controls, the reason is in the tooltip, and a lighter disabled icon would look enabled.
- D3 (owner, 2026-09-21): the regression test measures the card and the page background only, not the hover
  tint. Reason: the hover is a 4-10% shade; B-8 measured the same two surfaces.
- D4 (Claude, 2026-09-21): the minimum is 3:1 (non-text, 1.4.11), not the 4.5:1 of text used elsewhere in
  `ThemeContrastTests`. Reason: these icons carry no text; the test gets its own constant.

## Out of scope
- Disabled icons (D2).
- The hover tint under the icon (D3).
- The app bar buttons, which use `Color.Inherit` (see `## Cause`).
- The hard-coded colours in the F-4 and F-5 mockups (documents, not shipped).

## Open questions
- (none)

## Validation script
Written at the end of the build.

## Delivery
- Branch: <bug/B-<number>>
- Merge: <commit>
