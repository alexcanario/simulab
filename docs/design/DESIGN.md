---
name: Simulab
source: file
sourceRef: src/Hosts/Simulab.Web/Theme/SimulabTheme.cs
date: 2026-10-22
iconFamily: Material Outlined
color:
  primary:
    light: "#0F3266"
    dark: "#47A0EB"
  primaryContrastText:
    light: "#FFFFFF"
    dark: "#0B1B2E"
  primaryDarken:
    light: "#10366E"
    dark: "#4DADFE"
  primaryLighten:
    light: "#DCE8F5"
    dark: "#1A4583"
  secondary:
    light: "#1A9E8C"
    dark: "#1A9E8C"
  secondaryContrastText:
    light: "#0B1B2E"
    dark: "#0B1B2E"
  secondaryDarken:
    light: "#1CAB97"
    dark: "#1CAB97"
  secondaryLighten:
    light: "#21C5AF"
    dark: "#21C5AF"
  tertiary:
    light: "#F5A020"
    dark: "#F5A020"
  tertiaryContrastText:
    light: "#0B1B2E"
    dark: "#0B1B2E"
  tertiaryDarken:
    light: "#FFAD23"
    dark: "#FFAD23"
  tertiaryLighten:
    light: "#F6B046"
    dark: "#F6B046"
  info:
    light: "#2478C5"
    dark: "#47A0EB"
  infoContrastText:
    light: "#FFFFFF"
    dark: "#0B1B2E"
  infoDarken:
    light: "#216EB5"
    dark: "#4DADFE"
  infoLighten:
    light: "#398CDB"
    dark: "#6CB4EF"
  success:
    light: "#1A9E8C"
    dark: "#1A9E8C"
  successContrastText:
    light: "#0B1B2E"
    dark: "#0B1B2E"
  successDarken:
    light: "#1CAB97"
    dark: "#1CAB97"
  successLighten:
    light: "#21C5AF"
    dark: "#21C5AF"
  warning:
    light: "#D98B1A"
    dark: "#D98B1A"
  warningContrastText:
    light: "#0B1B2E"
    dark: "#0B1B2E"
  warningDarken:
    light: "#EA961C"
    dark: "#EA961C"
  warningLighten:
    light: "#E79B32"
    dark: "#E79B32"
  error:
    light: "#B83232"
    dark: "#FF9C94"
  errorContrastText:
    light: "#FFFFFF"
    dark: "#0B1B2E"
  errorDarken:
    light: "#C73636"
    dark: "#FFA8A0"
  errorLighten:
    light: "#CD4747"
    dark: "#FFBCB8"
  textPrimary:
    light: "#0B1B2E"
    dark: "#F2F5FB"
  textSecondary:
    light: "#52647A"
    dark: "#B8C4D2"
  actionDefault:
    light: "#52647A"
    dark: "#B8C4D2"
  actionDisabled:
    light: "#52647A"
    dark: "#B8C4D2"
  actionDisabledBackground:
    light: "#E6ECF5"
    dark: "#081938"
  background:
    light: "#F2F5FB"
    dark: "#0B2149"
  backgroundGray:
    light: "#E6ECF5"
    dark: "#081938"
  surface:
    light: "#FFFFFF"
    dark: "#12386F"
  drawerBackground:
    light: "#0F3266"
    dark: "#081938"
  drawerText:
    light: "#FFFFFFEB"
    dark: "#B8C4D2"
  drawerIcon:
    light: "#FFFFFFEB"
    dark: "#B8C4D2"
  appbarBackground:
    light: "#FFFFFF"
    dark: "#12386F"
  appbarText:
    light: "#0B1B2E"
    dark: "#F2F5FB"
  linesDefault:
    light: "#D9E1EE"
    dark: "#24497F"
  linesInputs:
    light: "#6B7C91"
    dark: "#7F9BC0"
  tableLines:
    light: "#D9E1EE"
    dark: "#24497F"
  divider:
    light: "#D9E1EE"
    dark: "#24497F"
  link:
    light: "#1D64A8"
    dark: "#8CC4F5"
  brandName:
    light: "#0F3266"
    dark: "#F2F5FB"
typography:
  fontFamily: "Bricolage Grotesque, system-ui, sans-serif"
  fontWeight: 500
  h1:
    fontSize: 1.75rem
    fontWeight: 800
    lineHeight: 2rem
    letterSpacing: -0.02em
  h2:
    fontSize: 1.25rem
    fontWeight: 800
    lineHeight: 1.5rem
    letterSpacing: -0.01em
  h3:
    fontSize: 1rem
    fontWeight: 800
    lineHeight: 1.25rem
    letterSpacing: -0.01em
  subtitle1:
    fontWeight: 800
    lineHeight: 1.25rem
    letterSpacing: -0.01em
  body1:
    fontSize: 0.9375rem
    lineHeight: 1.375rem
    letterSpacing: 0
  body2:
    fontSize: 0.875rem
    lineHeight: 1.25rem
    letterSpacing: 0
  button:
    fontSize: 0.875rem
    fontWeight: 800
    lineHeight: 1.25rem
    letterSpacing: 0
    textTransform: none
  caption:
    fontSize: 0.75rem
    lineHeight: 1rem
    letterSpacing: 0
  overline:
    fontSize: 0.75rem
    fontWeight: 800
    lineHeight: 1rem
    letterSpacing: 0.04em
shape:
  borderRadius: 12px
---
# Simulab design

Technical terms: [glossary](../glossary.md)

Simulab in the Control Alt Systems identity (F-125, ADR-0006), palette v2 (F-148, ADR-0007: its `## Approved list` supersedes F-125's), recorded from `SimulabTheme`. The source of the brand is the Control Alt Systems design system (`tokens.json` and its README); F-125's `## Approved list` holds the values Simulab took from it and the few it keeps of its own. `IdentityTokensTests` fails when this file, `identity.tokens.json` and the theme drift apart, and `ApprovedPaletteTests` when the theme leaves the approved list.

The values above and in `identity.tokens.json` are the source for every screen and mockup. A mockup uses these colors and no other.

## Overview
Keys on a calm page. A pale grey page with white cards (light) or a deep navy page with lighter navy cards (dark), a white app bar with the Control Alt Systems key symbol, and Simulab's navy sidebar. Buttons are keycaps: a filled key with an edge under it that sinks when pressed. One amber key per screen, at most, is the action that matters; the rest of the screen speaks in blue and ink. Nothing floats: a 1px line separates every surface instead of a shadow.

## Colors by role
The token names are the MudBlazor palette property names in camelCase. Every pair was measured by `ThemePaletteTests`, `ThemeContrastTests` and `KeycapContrastTests`: text at 4.5:1 or more; icons, field borders, the fill of a filled button against the page and the surface, and the focus ring at 3:1 or more.

- **Primary** (`primary`, `primaryContrastText`, `primaryDarken`, `primaryLighten`): the brand navy `#0F3266` in light, the blue `#47A0EB` in dark (navy reads 1.26:1 on the dark page). The filled primary button, the selected item. White text in light, ink in dark. It is a fill, not a text colour: text in blue is the link role. `primaryLighten` is the tint blue of informative highlights.
- **Info** (`info`, `infoContrastText`, `infoDarken`): the brand blue of the Alt key, `#2478C5` / `#47A0EB`. The focus ring and informational alerts.
- **Link** (`link`): `#1D64A8` / `#8CC4F5`, the text button and every link. MudBlazor's palette has no slot for it, so `SimulabThemeProvider` writes it as `--app-link`.
- **Secondary and success** (`secondary*`, `success*`): the brand teal `#1A9E8C` of the Ctrl key, in both themes. Text on it is ink, never white (white reads 3.3:1).
- **Tertiary** (`tertiary*`): the brand amber `#F5A020` of the restart key, the one action of a screen, with ink text, never white (2.1:1). In the light theme its fill reads 1.94:1 on the page, so the edge `#B87410` (3.47:1) identifies it and it carries the edge always. F-148 puts it only in the kit and the gallery; each screen item picks its own action.
- **Warning and error** (`warning*`, `error*`): warning keeps Simulab's amber `#D98B1A`; error is the design system's danger, `#B83232` with white text in light and `#FF9C94` with ink text in dark.
- **Hover tones** (`*Darken`): 8% brighter than the fill (each channel x1.08) while the fill's text keeps 4.5:1 there, else 8% darker: the light info carries white text that no brighter tone keeps.
- **Keycap edges**: the edge under each filled key, written by `SimulabThemeProvider` as `--app-keycap-{primary,secondary,tertiary,info,error}-edge` because MudBlazor's palette has no slot for it. Light `#6B7C91`, `#107264`, `#B87410`, `#17528A`, `#842424`; dark `#1F6FB5`, `#107264`, `#B87410`, `#1F6FB5`, `#A66560`. Only the amber edge identifies a button (light theme); the others are decorative: the fill identifies the button.
- **Brand name** (`brandName`): "Simulab" in the app bar and the plus signs of the action shortcut, `--app-brand-name`: navy `#0F3266` in light, ink `#F2F5FB` in dark (navy reads 1.09:1 on the dark bar).
- **Text** (`textPrimary`, `textSecondary`, `actionDefault`, `actionDisabled`): ink and ink-muted. Ink-muted is helper text, captions, every icon button, disabled text and the company line under "Simulab".
- **Page, cards, wells** (`background`, `surface`, `backgroundGray`, `actionDisabledBackground`): the page, the raised surface of cards and dialogs, and the sunken surface of wells and disabled fills.
- **Frame** (`appbarBackground`, `appbarText`, `drawerBackground`, `drawerText`, `drawerIcon`): the app bar is white (light) or the raised surface (dark) with ink; the sidebar is the brand navy `#0F3266` in light and takes the sunken surface `#081938` in dark, where the navy disappears against the page.
- **Lines** (`linesInputs`, `linesDefault`, `tableLines`, `divider`): line-strong is the border of every field and of the secondary button, a control boundary at 3:1. Line is decorative: dividers, table rules and the 1px border around surfaces.

Colors the theme leaves at MudBlazor's default in both modes (overlays, grays, `black`, `white`, the hover and striped row tints) have no token.

## Typography
Bricolage Grotesque for text, JetBrains Mono for code, grids of numbers and keyboard shortcuts. Both are files of the app (`wwwroot/fonts/`, with their OFL licences), never a third-party host. Fallbacks: `system-ui, sans-serif` and `ui-monospace, Consolas, monospace`. Text is weight 500 and titles 800; titles keep a negative letter spacing, body and control text none.

- ui-title (H1): 28/32.
- ui-heading (H2): 20/24.
- ui-subheading (H3, subtitle 1): 16/20.
- ui-body (body 1): 15/22.
- ui-control (body 2, button): 14/20. Buttons are sentence case, never uppercase.
- ui-caption (caption): 12/16.
- ui-label (overline): 12/16, weight 800.
- Mono: code 14/22, grid 13/20, kbd 12/16.

Emails use the Bricolage Grotesque stack with the system fallback, since email clients load no web font.

## Shape
Tags and chips 6px; buttons and fields 12px (the theme's default radius); cards and dialogs 20px; the brand keys 22%. No diffuse shadow anywhere: the theme draws no elevation and a 1px line separates cards, the app bar, the sidebar, menus and dialogs. The only shadow is the keycap edge.

## Buttons
- Primary (filled, `Color.Primary`): navy with white text and the line-strong edge in light; blue with ink text and a blue edge in dark.
- Secondary (outlined): the surface inside a 2px line-strong border, ink text.
- Text: no fill, the link colour, underlined.
- Action (`AppActionButton`): the logo as a button. The shortcut is a teal Ctrl key (ink label), a blue Alt key (its label), `+` signs and the amber key with the restart icon and the caller's label, with the edge `#B87410` always; the compact variant is the amber key alone, for areas narrower than 360px and toolbars. One per screen, on the page, a surface or white, never on the sunken fill or the tint. One element is clickable; Ctrl, Alt and the signs are decorative.
- Destructive (`Color.Error`): the error colours, the same shapes.
- States: at least 44px high (36px small); hover 8% brighter; pressed moves down 2px and the edge shrinks (in the shortcut Ctrl and Alt sink too and the icon turns half way, 200 degrees in 0.4 s), at once with reduced motion; the focus ring is 3px of info blue, 2px away; disabled is the sunken fill with ink-muted text and no edge, with a real `disabled`.

A page sets only `Variant` and `Color` on a `MudButton`; the kit stylesheet does the rest.

## Brand
The app bar shows the Control Alt Systems key symbol (decorative), "Simulab" in Bricolage 800 and "Control Alt Systems" under it in ink-muted. The symbol is a picture with fixed colours, so each theme has its own file (`wwwroot/brand/keys-symbol.svg`, `keys-symbol-dark.svg`); never recolour or reorder the keys. The favicon is the app icon (`wwwroot/brand/app-icon.svg`). The amber key of the symbol is the brand mark and is not the screen's action.

## Icons
Material Outlined, always through `AppIcons`. One family, no filled or two-tone variants, no emoji as icons.

## Do
- Draw a mockup with these tokens only, in both light and dark mode.
- Use `primary` for the main filled action, `link` for links and the text button, and `tertiary` (the action shortcut) for at most one action per screen.
- Separate surfaces with the 1px line, never with a shadow.

## Don't
- Don't put the amber action on the sunken surface or the tint blue: it reads under 3:1 there; use the primary button.
- Don't use `primary` or `error` from light mode on a dark surface, or the other way round: each mode has its own value.
- Don't paint a literal color in a stylesheet or a component: every color comes from the theme (`StylesheetLiteralColourTests`; the one exception is the QR code's white quiet zone).
- Don't use a decorative line (`divider`, `tableLines`) as the border of a control: use `linesInputs`.
- Don't change a value here without changing `SimulabTheme.cs`, or the other way round: the test fails and names the token.
