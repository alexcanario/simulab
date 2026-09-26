---
bug: B-17
feature: F-2
status: done
board: 766
severity: low
---
# Selecting a menu item makes its label harder to read in dark mode

## What happens
`NavMenu` paints the selected item with a translucent white background (`rgba(255,255,255,0.14)`) while the
label keeps the same translucent white text. In dark mode the background lightens but the text does not, so
the selected item — the one the user is looking at — reads worse than the ones around it. Measured on screen
through the app host on 2026-09-25, compositing every alpha layer:

| Theme | Item | Text over surface | Ratio | WCAG 2.2 AA needs |
|---|---|---|---|---|
| Dark | selected | `rgb(148,153,159)` on `rgb(43,52,64)` | **4.37:1** | 4.5:1 |
| Dark | not selected | `rgb(131,137,144)` on `rgb(9,20,34)` | 5.25:1 | 4.5:1 |
| Light | selected | — | 7.31:1 | 4.5:1 |
| Light | not selected | — | 10.84:1 | 4.5:1 |

Steps:
1. Start the app host and switch to dark mode.
2. Open `/dev/ui` and look at "Kit de interface" in the menu; then open `/dev/ai` and look at "Gateway de IA".
3. In both cases the selected entry is the dimmest label in the menu.

It is not one page: the same pair appears for every menu entry, so it is every back-office screen in dark mode.
Light mode is fine. Found while checking F-41's new menu entry on screen; the entry itself behaves exactly like
the ones that were already there, so this is the component, not F-41.

## Start
- Depends on: nothing. The fix is the selected item's text token in `NavMenu`, or an opaque selected surface.
- Waits on: nothing.
- Suggested path: `/agile:autopilot B-17` — small and measured, but the owner should see the new tone on screen.
- Parallel with: B-16 (field hint contrast) touches the same theme tokens; doing them together would avoid
  two passes over the same decision.

## Delivery
- Fixed inside F-43 (merge `7c771d4`, 2026-09-26): the selected item keeps its lighter background and paints
  its label white. Seen failing first at 4.37:1 dark, then measured on screen at 18.51. The pair is held by
  `ThemeContrastTests.SelectedNavigationItem_ReadsAtAaOnItsOwnBackground`.
