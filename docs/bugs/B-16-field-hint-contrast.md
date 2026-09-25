---
bug: B-16
feature: F-4
status: idea
board: 764
severity: medium
---
# Field hint text fails AA contrast in both themes

## What happens
The hint under every kit field (`.app-field-hint`) is painted with MudBlazor's `text-disabled` token, which
is translucent. Measured on screen at `/dev/ui` through the app host on 2026-09-24, on the card the fields
sit on:

| Theme | Colour | Surface | Ratio | WCAG 2.2 AA needs |
|---|---|---|---|---|
| Dark | `rgba(255,255,255,0.2)` | `#172035` | **1.90:1** | 4.5:1 |
| Light | `rgba(0,0,0,0.376)` | `#FFFFFF` | **2.64:1** | 4.5:1 |

Steps:
1. Start the app host and open `/dev/ui`.
2. Look at "Type at least 2 characters" under any field, in light and then in dark mode.
3. The dark one is barely visible; both are under the 4.5:1 that ADR-0001 #29 commits to.

It is not one screen: the hint is part of `AppTextField`, `AppSelectField`, `AppPasswordField` and (since
F-34) `AppLookupField`, so it is every form in the app — sign-up, the account page, the organizer dialog,
the exam form. The error text next to it is fine (5.05:1 dark), and so is every other pair the theme tests
cover.

## Start
- Depends on: nothing. The fix is one CSS rule plus the token it reads.
- Waits on: nothing.
- Suggested path: `/agile:autopilot B-16` — the cause is measured and the fix is small, but the owner should
  see the new tone on screen before the merge.
- Parallel with: none decided — unknown, settled at `/agile:refine`.

## Expected
The hint reads at 4.5:1 or better on every surface it sits on (card, page background), in both themes.

## Cause
`src/Hosts/Simulab.Web/wwwroot/app.css:423` — `.app-field-hint { color: var(--mud-palette-text-disabled); }`,
from F-4. `ThemeContrastTests` and `ThemePaletteTests` never look at `text-disabled`, which is why no test
caught it: they check the pairs the palette declares, and this one is the library's default.

No duplicate found: `.app-field-hint` is the only rule that paints a hint, and every field component uses it.

## Fix
To be decided at refinement. The obvious candidate is `--mud-palette-text-secondary`, which F-17 and B-10
already measured at 6.92:1 light and 4.89:1 dark on the surface — but it is the same colour as the label,
so the hint would stop reading as secondary. Whatever is chosen, `ThemeContrastTests` gains the pair so the
build holds it from then on.

## Regression test
- A new case in `ThemeContrastTests` for the hint colour against `Surface` and `Background`, in both themes —
  seen failing with today's token before the fix.

## Open questions
- (none)

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: bug/B-16
- Merge: <commit>
