---
adr: 0007
status: accepted
date: 2026-10-22
---
# ADR-0007: Simulab takes the Control Alt Systems palette v2

Technical terms: [glossary](../glossary.md)

## Context
ADR-0006 and F-125 brought the Control Alt Systems identity to Simulab. On 2026-10-22 the design system changed palette (`_cas-design-system/design-system/tokens.json` version 2, `HANDOFFS.md` H-04): amber replaces the orange of the action, the teal and blues move, the page and surface tones become blue-greys and navies, navy becomes the filled primary button, and the action button becomes the Ctrl + Alt + amber key shortcut. F-148 takes this to the theme, the kit stylesheet, the brand files and the emails, so the screen items (F-126 to F-134) only change composition.

## Options
**A. Keep F-125's palette and adopt only the new action shortcut.** Two palettes in one product: the shortcut's keys (teal, blue, amber) would not match the rest of the screen.

**B. Take the whole of palette v2 in one change** (chosen, F-148). One source of colours, still `SimulabTheme`.

## Decision
1. **Palette**: the values of F-148's `## Approved list`. Amber `#F5A020` and teal `#1A9E8C` in both themes; blue `#2478C5` (light) / `#47A0EB` (dark); page `#F2F5FB` / `#0B2149`, surface `#FFFFFF` / `#12386F`, sunken `#E6ECF5` / `#081938`. F-125's list is superseded; its file is not rewritten.
2. **Primary** is the brand navy `#0F3266` with white text in the light theme and the blue `#47A0EB` with ink in the dark one: navy reads 1.26:1 on the dark page. A primary or info blue is a fill, not text (4.12:1 on the dark surface).
3. **A link role**, `#1D64A8` / `#8CC4F5`, for text buttons and every link, written by `SimulabThemeProvider` as `--app-link` because MudBlazor's palette has no slot for it. The brand name has the same route (`--app-brand-name`: navy in light, ink in dark, since navy reads 1.09:1 on the dark bar).
4. **Text on teal and on amber is ink** `#0B1B2E`, never white. **Error** adopts the design system's danger (`#B83232` / `#FF9C94`); **warning** keeps `#D98B1A`; success is the teal.
5. **The amber action is identified by its edge** `#B87410` in the light theme: its fill reads 1.94:1 on the page and the edge 3.47:1. It carries the edge always. Every other filled button is still identified by its fill, and its edge stays decorative.
6. **The action is a kit component**, `AppActionButton`: the shortcut (Ctrl, Alt, plus signs, the amber key) and a compact variant (the amber key alone). One element is clickable; Ctrl, Alt and the signs are decorative. Still one at most per screen, and no routed page uses it until its screen item picks it (F-125 BR5).
7. **App bar and sidebar** keep ADR-0006's choices: a white bar (a raised surface in dark), a navy sidebar (the sunken surface in dark).
8. **Brand files** are the design system's v2 logos; the dark symbol is derived from them by hand, since the design system ships one symbol only. **Emails** use the navy button with white text.
9. **A marked answer** (F-169, 2026-10-27): `primarySelected`, `#D1E4FE` / `#1A4583`, the fill of a marked option and an answered answer-sheet cell, always with a 2 px `primary` border. Written by `SimulabThemeProvider` as `--app-primary-selected`, like the link role. Ink on it 13.4:1 (light), `#F2F5FB` on it 8.6:1 (dark).

## Consequences
- `docs/design/identity.tokens.json` and `DESIGN.md` record palette v2, with `link` and `brandName` as tokens beside the palette's own; `IdentityTokensTests`, `ApprovedPaletteTests`, `ThemePaletteTests` and `KeycapContrastTests` hold it.
- The old item mockups under `docs/features/mockups/` keep their colours as historical records.
- ADR-0006 stays accepted; its palette decision (2) is superseded in part by this one.
