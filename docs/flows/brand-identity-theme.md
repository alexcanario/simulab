---
title: Brand identity and theme
summary: How the Control Alt Systems identity reaches every screen from one theme, how the keycap buttons, borders and fonts are styled on MudBlazor's classes, and which tests hold each value.
items: [F-125]
---
# Brand identity and theme

Technical terms: [glossary](../glossary.md)

Manual page for the user: [Como navegar](../manual/pt-BR/getting-around.md)

## Trigger
Any page renders inside `MainLayout` or `AuthLayout`. Both start with `SimulabThemeProvider`, which writes the theme as CSS variables on `:root` for the theme in use (light or dark, from `ThemeState`), and `App.razor` loads `wwwroot/app.css` and links the favicon.

## Steps
1. `SimulabTheme.Create()` builds both palettes from the approved list of F-125, the Bricolage Grotesque type scale, a 12px default radius and no elevation (every MudBlazor shadow is `none`).
2. `SimulabThemeProvider` (a `MudThemeProvider`) writes MudBlazor's `--mud-palette-*` variables and adds `--app-keycap-{primary,secondary,tertiary,error}-edge` from `SimulabTheme.LightKeycapEdges` or `DarkKeycapEdges`: the palette has no slot for them.
3. `app.css` serves the four brand fonts from `wwwroot/fonts/` (`@font-face`, OFL licences beside them) and styles MudBlazor's own classes: `.mud-button-filled.mud-button-filled-{primary,tertiary,error}` are keycaps (fill, contrast text, the edge as a `0 4px 0` shadow), `.mud-button-root.mud-button-outlined` is the secondary button, `.mud-button-root.mud-button-text` is a link. A page sets only `Variant` and `Color`.
4. Surfaces lose their shadow and get a 1px `divider` line (`.mud-paper`, `.mud-appbar`, the drawer, `.mud-popover`, `.mud-dialog`); radii come from `--app-radius-sm|md|lg` (6, 12, 20px).
5. `MainLayout` draws the brand in the app bar: `wwwroot/brand/keys-symbol.svg` (or `-dark.svg` in the dark theme, decorative), then `App.Name` and `App.Company` from the shared resources inside the home link.
6. The Identity emails build their page through `EmailHtml.Page`: the brand as text, ink, the Bricolage stack with a system fallback, and the `#064F80` button.

## Rules
- Every colour comes from `SimulabTheme`; no stylesheet carries a literal colour but the QR code's quiet zone (`StylesheetLiteralColourTests`).
- Text pairs read at 4.5:1 and non-text pairs at 3:1 in both themes; the fill of every filled button is measured on the page and the surface; the keycap edge is decorative (`KeycapContrastTests`, `ThemePaletteTests`).
- The orange `Color.Tertiary` button appears only in the gallery until a screen item picks its one action (`ActionColourScopeTests`).
- `docs/design/identity.tokens.json` and `DESIGN.md` equal the theme (`IdentityTokensTests`); the approved values are pinned by `ApprovedPaletteTests`.
- A `*Darken` tone is the hover of its fill: 8% brighter while the fill's text keeps 4.5:1, else 8% darker.

## Wireframe
```text
+----------------------------------------------------------+
| [=] [G][B][O] Simulab              (moon) (A) (avatar)   |
|               Control Alt Systems                        |
|     [changed: white bar, brand symbol, two-line name]    |
+--------+-------------------------------------------------+
| navy   | page #F4F6F9                                    |
| menu   | +-------------------------------------------+   |
|        | | card: white, 1px line, radius 20 [changed]|   |
|        | | [ Field ________ ] radius 12    [changed] |   |
|        | | [Salvar]=  [Filtrar]  Cancelar  [changed] |   |
|        | +-------------------------------------------+   |
+--------+-------------------------------------------------+
```

```text
+----------------------------------------------------------+
| UI gallery                                               |
| ...existing sections, in the new look...      [changed]  |
| Buttons                                           [new]  |
|  [Primary]=  [Secondary]  Text link  [Action]=           |
|  small size | hover | pressed | focus | disabled         |
| Type                                              [new]  |
|  ui-title 28 / ui-heading 20 / ui-body 15 / mono 14     |
| Palette (theme in use)                            [new]  |
|  [##] primary #064F80  [##] info #0872B8  ...            |
+----------------------------------------------------------+
```
