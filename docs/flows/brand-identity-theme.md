---
title: Brand identity and theme
summary: How the Control Alt Systems identity (palette v2) reaches every screen from one theme, how the keycap buttons, the link role, the action shortcut, borders and fonts are styled on MudBlazor's classes, and which tests hold each value.
items: [F-125, F-148]
---
# Brand identity and theme

Technical terms: [glossary](../glossary.md)

Manual page for the user: [Como navegar](../manual/pt-BR/getting-around.md)

## Trigger
Any page renders inside `MainLayout` or `AuthLayout`. Both start with `SimulabThemeProvider`, which writes the theme as CSS variables on `:root` for the theme in use (light or dark, from `ThemeState`), and `App.razor` loads `wwwroot/app.css` and links the favicon.

## Steps
1. `SimulabTheme.Create()` builds both palettes from the approved list of F-148 (palette v2, ADR-0007), the Bricolage Grotesque type scale, a 12px default radius and no elevation (every MudBlazor shadow is `none`).
2. `SimulabThemeProvider` (a `MudThemeProvider`) writes MudBlazor's `--mud-palette-*` variables and adds what the palette has no slot for, from `SimulabTheme`: `--app-keycap-{primary,secondary,tertiary,info,error}-edge` (`LightKeycapEdges` or `DarkKeycapEdges`), `--app-link` (the link role, `#1D64A8` / `#8CC4F5`) and `--app-brand-name` (navy in light, ink in dark).
3. `app.css` serves the four brand fonts from `wwwroot/fonts/` (`@font-face`, OFL licences beside them) and styles MudBlazor's own classes: `.mud-button-filled.mud-button-filled-{primary,tertiary,error}` are keycaps (fill, contrast text, the edge as a `0 4px 0` shadow), `.mud-button-root.mud-button-outlined` is the secondary button, `.mud-button-root.mud-button-text` is a link in `--app-link`. A page sets only `Variant` and `Color`. The primary is navy with white text in light and blue with ink in dark.
4. Surfaces lose their shadow and get a 1px `divider` line (`.mud-paper`, `.mud-appbar`, the drawer, `.mud-popover`, `.mud-dialog`); radii come from `--app-radius-sm|md|lg` (6, 12, 20px).
5. `MainLayout` draws the brand in the app bar: `wwwroot/brand/keys-symbol.svg` (or `-dark.svg` in the dark theme, decorative), then `App.Name` (in `--app-brand-name`) and `App.Company` from the shared resources inside the home link.
6. `AppActionButton` is the action of the brand: `.app-action` holds Ctrl, Alt, two plus signs (all `aria-hidden`) and the one `button.app-action-key` (the amber key, the restart icon `AppIcons.Restart` and the caller's `Label`); `Compact` leaves the amber key alone. Pressed, the key and, in the shortcut, Ctrl and Alt sink and the icon turns 200 degrees; disabled is a real `disabled` on the sunken fill (`:has()` in `app.css`). No routed page uses it until its screen item picks it.
7. The Identity emails build their page through `EmailHtml.Page`: the brand as text, ink, the Bricolage stack with a system fallback, and the `#0F3266` button with white text.

## Rules
- Every colour comes from `SimulabTheme`; no stylesheet carries a literal colour but the QR code's quiet zone (`StylesheetLiteralColourTests`).
- Text pairs read at 4.5:1 and non-text pairs at 3:1 in both themes; the fill of every filled button is measured on the page and the surface, except the amber action in the light theme, which is identified by its edge `#B87410`; the other keycap edges are decorative (`KeycapContrastTests`, `ThemePaletteTests`).
- Text and icons on teal and amber are ink, never white. A primary or info blue is a fill, not text: blue text is the link role.
- The amber action (`Color.Tertiary` or `AppActionButton`) appears only in the kit and the gallery until a screen item picks its one action (`ActionColourScopeTests`).
- `docs/design/identity.tokens.json` and `DESIGN.md` equal the theme, `link` and `brandName` included (`IdentityTokensTests`); the approved values are pinned by `ApprovedPaletteTests`.
- A `*Darken` tone is the hover of its fill: 8% brighter while the fill's text keeps 4.5:1, else 8% darker.

## Wireframe
Mockup: none (the wireframes below are the screen design of F-148).

### App bar (every screen with `MainLayout`)
```text
+----------------------------------------------------------+
| [=] [T][B][A] Simulab              (moon) (A) (avatar)   |
|               Control Alt Systems                        |
|   [changed: new symbol colours, "Simulab" in navy]       |
+--------+-------------------------------------------------+
| navy   | page #F2F5FB                                    |
| menu   | +-------------------------------------------+   |
|        | | card: white, 1px line #D9E1EE     [changed]|   |
|        | | [Salvar]=  [Filtrar]  Cancelar    [changed]|   |
|        | +-------------------------------------------+   |
+--------+-------------------------------------------------+
```

### UI gallery buttons (`/dev/ui`, section `gallery-buttons`)
```text
+----------------------------------------------------------+
| Buttons                                                  |
|  [Save]=  [Filter]  Cancel  [Delete]=  [Delete]   [changed]
|    navy    grey 2px  link    red       red outline   |
| Action (one per screen)                           [new]  |
|  [Ctrl]+[Alt]+[ (R) Start exam ]=      shortcut          |
|  [ (R) Start exam ]=                   compact           |
|  disabled: [Ctrl]+[Alt]+[ (R) Start exam ]  (all sunken) |
+----------------------------------------------------------+
```
