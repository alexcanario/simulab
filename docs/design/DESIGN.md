---
name: Simulab
source: file
sourceRef: src/Hosts/Simulab.Web/Theme/SimulabTheme.cs
date: 2026-10-18
iconFamily: Material Outlined
color:
  primary:
    light: "#064F80"
    dark: "#2AA8F0"
  primaryContrastText:
    light: "#FFFFFF"
    dark: "#0B1B2E"
  primaryDarken:
    light: "#06558A"
    dark: "#2DB5FF"
  primaryLighten:
    light: "#DCEBF7"
    dark: "#16344F"
  secondary:
    light: "#00806B"
    dark: "#00A68C"
  secondaryContrastText:
    light: "#FFFFFF"
    dark: "#0B1B2E"
  secondaryDarken:
    light: "#007662"
    dark: "#00B397"
  secondaryLighten:
    light: "#00A388"
    dark: "#00CCAD"
  tertiary:
    light: "#E8650A"
    dark: "#FF7A2E"
  tertiaryContrastText:
    light: "#0B1B2E"
    dark: "#0B1B2E"
  tertiaryDarken:
    light: "#FB6D0B"
    dark: "#FF8432"
  tertiaryLighten:
    light: "#F6781E"
    dark: "#FF9152"
  info:
    light: "#0872B8"
    dark: "#2AA8F0"
  infoContrastText:
    light: "#FFFFFF"
    dark: "#0B1B2E"
  infoDarken:
    light: "#0769A9"
    dark: "#2DB5FF"
  infoLighten:
    light: "#098BE1"
    dark: "#4AB5F2"
  success:
    light: "#00806B"
    dark: "#00A68C"
  successContrastText:
    light: "#FFFFFF"
    dark: "#0B1B2E"
  successDarken:
    light: "#007662"
    dark: "#00B397"
  successLighten:
    light: "#00A388"
    dark: "#00CCAD"
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
    light: "#BE3737"
    dark: "#E06C6C"
  errorContrastText:
    light: "#FFFFFF"
    dark: "#0B1B2E"
  errorDarken:
    light: "#CD3B3B"
    dark: "#F27575"
  errorLighten:
    light: "#CB4D4D"
    dark: "#E68989"
  textPrimary:
    light: "#0B1B2E"
    dark: "#F4F6F9"
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
    light: "#E9EDF2"
    dark: "#081522"
  background:
    light: "#F4F6F9"
    dark: "#0B1B2E"
  backgroundGray:
    light: "#E9EDF2"
    dark: "#081522"
  surface:
    light: "#FFFFFF"
    dark: "#12263A"
  drawerBackground:
    light: "#0F3266"
    dark: "#081522"
  drawerText:
    light: "#FFFFFFEB"
    dark: "#B8C4D2"
  drawerIcon:
    light: "#FFFFFFEB"
    dark: "#B8C4D2"
  appbarBackground:
    light: "#FFFFFF"
    dark: "#12263A"
  appbarText:
    light: "#0B1B2E"
    dark: "#F4F6F9"
  linesDefault:
    light: "#DDE3EA"
    dark: "#26405A"
  linesInputs:
    light: "#6B7C91"
    dark: "#6A829A"
  tableLines:
    light: "#DDE3EA"
    dark: "#26405A"
  divider:
    light: "#DDE3EA"
    dark: "#26405A"
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

Simulab in the Control Alt Systems identity (F-125, ADR-0006), recorded from `SimulabTheme`. The source of the brand is the Control Alt Systems design system (`tokens.json` and its README); F-125's `## Approved list` holds the values Simulab took from it and the few it keeps of its own. `IdentityTokensTests` fails when this file, `identity.tokens.json` and the theme drift apart, and `ApprovedPaletteTests` when the theme leaves the approved list.

The values above and in `identity.tokens.json` are the source for every screen and mockup. A mockup uses these colors and no other.

## Overview
Keys on a calm page. A pale grey page with white cards (light) or a deep navy page with lighter navy cards (dark), a white app bar with the Control Alt Systems key symbol, and Simulab's navy sidebar. Buttons are keycaps: a filled key with an edge under it that sinks when pressed. One orange key per screen, at most, is the action that matters; the rest of the screen speaks in blue and ink. Nothing floats: a 1px line separates every surface instead of a shadow.

## Colors by role
The token names are the MudBlazor palette property names in camelCase. Every pair was measured by `ThemePaletteTests`, `ThemeContrastTests` and `KeycapContrastTests`: text at 4.5:1 or more; icons, field borders, the fill of a filled button against the page and the surface, and the focus ring at 3:1 or more.

- **Primary** (`primary`, `primaryContrastText`, `primaryDarken`, `primaryLighten`): the dark blue `#064F80` (the brand's blue key edge) in light, `#2AA8F0` in dark. The filled primary button, the selected item. White text in light, ink in dark. `primaryLighten` is the tint blue of informative highlights.
- **Info** (`info`, `infoContrastText`, `infoDarken`): the brand blue of the Alt key, `#0872B8` / `#2AA8F0`. Links, the text button, the focus ring and informational alerts.
- **Secondary and success** (`secondary*`, `success*`): the brand green of the Ctrl key. White text in light, ink in dark.
- **Tertiary** (`tertiary*`): the brand orange of the restart key, the one action of a screen, with ink text. F-125 puts it only in the kit and the gallery; each screen item picks its own action.
- **Warning and error** (`warning*`, `error*`): Simulab's amber and red, kept: the design system has none, and an orange warning would read as the action.
- **Hover tones** (`*Darken`): 8% brighter than the fill (each channel x1.08) while the fill's text keeps 4.5:1 there, else 8% darker: the light green and the light info carry white text that no brighter tone keeps.
- **Keycap edges**: the edge under each filled key, written by `SimulabThemeProvider` as `--app-keycap-{primary,secondary,tertiary,error}-edge` because MudBlazor's palette has no slot for it. Light `#043A5E`, `#00574A`, `#A84606`, `#892828`; dark `#0F6BA8`, `#006B5B`, `#B24A0C`, `#924646`. The edge is decorative (F-125 change note v2): the fill identifies the button.
- **Text** (`textPrimary`, `textSecondary`, `actionDefault`, `actionDisabled`): ink and ink-muted. Ink-muted is helper text, captions, every icon button, disabled text and the company line under "Simulab".
- **Page, cards, wells** (`background`, `surface`, `backgroundGray`, `actionDisabledBackground`): the page, the raised surface of cards and dialogs, and the sunken surface of wells and disabled fills.
- **Frame** (`appbarBackground`, `appbarText`, `drawerBackground`, `drawerText`, `drawerIcon`): the app bar is white (light) or the raised surface (dark) with ink; the sidebar keeps Simulab's navy `#0F3266` in light and takes the sunken surface `#081522` in dark, where the navy disappears against the page.
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
- Primary (filled, `Color.Primary`): the primary fill, its edge, white text (ink in dark).
- Secondary (outlined): the surface inside a 2px line-strong border, ink text.
- Text: no fill, the info blue, underlined.
- Action (filled, `Color.Tertiary`): the orange fill, ink text, the orange edge. One per screen, on a surface only.
- Destructive (`Color.Error`): the error colours, the same shapes.
- States: at least 44px high (36px small); hover 8% brighter; pressed moves down 2px and the edge shrinks, at once with reduced motion; the focus ring is 3px of info blue, 2px away; disabled is the sunken fill with ink-muted text and no edge, with a real `disabled`.

A page sets only `Variant` and `Color` on a `MudButton`; the kit stylesheet does the rest.

## Brand
The app bar shows the Control Alt Systems key symbol (decorative), "Simulab" in Bricolage 800 and "Control Alt Systems" under it in ink-muted. The symbol is a picture with fixed colours, so each theme has its own file (`wwwroot/brand/keys-symbol.svg`, `keys-symbol-dark.svg`); never recolour or reorder the keys. The favicon is the app icon (`wwwroot/brand/app-icon.svg`). The orange key of the symbol is the brand mark and is not the screen's action.

## Icons
Material Outlined, always through `AppIcons`. One family, no filled or two-tone variants, no emoji as icons.

## Do
- Draw a mockup with these tokens only, in both light and dark mode.
- Use `primary` for the main filled action, `info` for links and the text button, and `tertiary` for at most one action per screen.
- Separate surfaces with the 1px line, never with a shadow.

## Don't
- Don't put the orange action on the sunken surface or the tint blue: it reads under 3:1 there; use the primary button.
- Don't use `primary` or `error` from light mode on a dark surface, or the other way round: each mode has its own value.
- Don't paint a literal color in a stylesheet or a component: every color comes from the theme (`StylesheetLiteralColourTests`; the one exception is the QR code's white quiet zone).
- Don't use a decorative line (`divider`, `tableLines`) as the border of a control: use `linesInputs`.
- Don't change a value here without changing `SimulabTheme.cs`, or the other way round: the test fails and names the token.
