---
adr: 0006
status: accepted
date: 2026-10-18
---
# ADR-0006: Simulab takes the Control Alt Systems identity

Technical terms: [glossary](../glossary.md)

## Context
ADR-0001 decision 30 started `SimulabTheme` from Simulae's theme and left it under "Revisit when: Simulab has its own brand". The owner chose the Control Alt Systems identity for Simulab (D-4): its palette, Bricolage Grotesque and JetBrains Mono, keycap buttons and the key symbol (`_cas-design-system/design-system/tokens.json` and its README). F-125 brings it in one change of the theme and the UI kit, so the screen items (F-126 to F-134) only change composition.

## Options
**A. Keep Simulae's identity beside the new one, as a theme to pick.** Four themes (two identities, light and dark) to keep at WCAG 2.2 AA, and every screen designed twice.

**B. Two themes, light and dark, both Control Alt Systems** (chosen, F-125). One identity, changed in one place.

## Decision
1. **Two themes only**, light and dark, both of the Control Alt Systems identity; the Simulae identity is removed, not kept as a choice.
2. **Palette**: the values of F-125's `## Approved list` (superseded in part by palette v2, [ADR-0007](ADR-0007-control-alt-palette-v2.md); the values below are F-125's). The primary is the brand's dark blue `#064F80` (`#2AA8F0` in dark); the brand blue `#0872B8` is links, the text button and the focus ring; green is secondary and success; the orange `#E8650A` is the one action of a screen. Error and warning keep Simulab's values, and the sidebar keeps Simulab's navy (`#081522` in dark).
3. **Every colour comes from `SimulabTheme`**: no stylesheet carries a literal colour but the QR code's quiet zone. The keycap edges, which MudBlazor's palette has no slot for, are written by `SimulabThemeProvider` beside the palette's variables.
4. **Buttons are keycaps**, styled once in the kit stylesheet on the classes MudBlazor renders: primary, secondary (outlined), text and action (`Color.Tertiary`), with destructive in the error colours. A page sets only `Variant` and `Color`. The keycap edge is decorative; the fill identifies the button (F-125 change note v2).
5. **No diffuse shadow**: a 1px line separates every surface. Radii 6px (tags, chips), 12px (buttons, fields), 20px (cards, dialogs).
6. **Fonts are files of the app** (`wwwroot/fonts/`, OFL licences beside them), never a third-party host.
7. **The app bar** shows the key symbol (decorative, one file per theme), "Simulab" and "Control Alt Systems" from resource keys; the favicon is the app icon. A partner's own name and logo is F-139, which can make these come from the tenant.
8. **Emails** take the primary button colour, ink and the Bricolage Grotesque stack with its fallback, and open with the brand as text.

## Consequences
- `docs/design/identity.tokens.json` and `DESIGN.md` record the identity; `IdentityTokensTests`, `ApprovedPaletteTests`, `ThemePaletteTests` and `KeycapContrastTests` hold it.
- The gallery `/dev/ui` shows the button variants, the type scale and the palette; screen mockups use only what it shows.
- Each screen item picks its own orange action; until then no routed page uses `Color.Tertiary` on a button (`ActionColourScopeTests`).
- Exam session screens keep their distraction-free exception and wait for their own item.
