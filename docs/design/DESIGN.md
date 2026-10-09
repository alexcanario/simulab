---
name: Simulab
source: file
sourceRef: src/Hosts/Simulab.Web/Theme/SimulabTheme.cs
date: 2026-10-06
iconFamily: Material Outlined
color:
  primary:
    light: "#216DB5"
    dark: "#64A2E3"
  primaryContrastText:
    light: "#FFFFFF"
    dark: "#19243E"
  primaryDarken:
    light: "#1858A0"
    dark: "#5096DC"
  primaryLighten:
    light: "#EBF3FD"
    dark: "#162540"
  secondary:
    light: "#1A9E8C"
    dark: "#1A9E8C"
  secondaryContrastText:
    light: "#19243E"
    dark: "#19243E"
  secondaryDarken:
    light: "#24AE9A"
    dark: "#24AE9A"
  secondaryLighten:
    light: "#1FC1AB"
    dark: "#1FC1AB"
  tertiary:
    light: "#F5A020"
    dark: "#F5A020"
  tertiaryContrastText:
    light: "#19243E"
    dark: "#19243E"
  tertiaryDarken:
    light: "#E08B0B"
    dark: "#E08B0B"
  tertiaryLighten:
    light: "#F6B046"
    dark: "#F6B046"
  info:
    light: "#2478C5"
    dark: "#2478C5"
  infoDarken:
    light: "#1E63A4"
    dark: "#1E63A4"
  infoLighten:
    light: "#398CDB"
    dark: "#398CDB"
  success:
    light: "#1A9E8C"
    dark: "#1A9E8C"
  successContrastText:
    light: "#19243E"
    dark: "#19243E"
  successDarken:
    light: "#24AE9A"
    dark: "#24AE9A"
  successLighten:
    light: "#1FC1AB"
    dark: "#1FC1AB"
  warning:
    light: "#D98B1A"
    dark: "#D98B1A"
  warningContrastText:
    light: "#19243E"
    dark: "#19243E"
  warningDarken:
    light: "#CF8419"
    dark: "#CF8419"
  warningLighten:
    light: "#E79B32"
    dark: "#E79B32"
  error:
    light: "#BE3737"
    dark: "#E06C6C"
  errorContrastText:
    light: "#FFFFFF"
    dark: "#19243E"
  errorDarken:
    light: "#9E2E2E"
    dark: "#DE6868"
  errorLighten:
    light: "#CB4D4D"
    dark: "#E68989"
  textPrimary:
    light: "#19243E"
    dark: "#E0E8F8"
  textSecondary:
    light: "#4A5A7A"
    dark: "#7A8EB0"
  actionDefault:
    light: "#4A5A7A"
    dark: "#7A8EB0"
  background:
    light: "#F2F5FB"
    dark: "#0E1828"
  surface:
    light: "#FFFFFF"
    dark: "#172035"
  drawerBackground:
    light: "#0F3266"
    dark: "#091422"
  drawerText:
    light: "#FFFFFFEA"
    dark: "#FFFFFF7F"
  drawerIcon:
    light: "#FFFFFFEA"
    dark: "#FFFFFF7F"
  appbarBackground:
    light: "#0F3266"
    dark: "#091422"
  linesInputs:
    light: "#7E8AA1"
    dark: "#61718F"
  tableLines:
    light: "#D0D8EA"
    dark: "#263352"
  divider:
    light: "#D0D8EA"
    dark: "#263352"
typography:
  fontFamily: "-apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif"
  h1:
    fontSize: 1.75rem
    fontWeight: 700
    lineHeight: 1.2
  h2:
    fontSize: 1.25rem
    fontWeight: 600
    lineHeight: 1.3
  body1:
    fontSize: 0.9375rem
    lineHeight: 1.65
  body2:
    fontSize: 0.8125rem
    lineHeight: 1.5
  caption:
    fontSize: 0.75rem
shape:
  borderRadius: 10px
---
# Simulab design

The identity of the app as it is today, recorded from `SimulabTheme` (F-63). Nothing here is new: the colors are the ones F-10, F-17, B-8, B-9 and B-10 tuned to WCAG 2.2 AA, and `IdentityTokensTests` fails when this file, `identity.tokens.json` and the theme drift apart. A new brand (logo, colors, a web font) is a later item.

The values above and in `identity.tokens.json` are the source for every screen and mockup. A mockup uses these colors and no other.

## Overview
A calm study tool: a deep navy frame (app bar and drawer) around a pale page with white cards in light mode, and a near-black navy page with slightly lighter cards in dark mode. One blue for everything interactive, a teal and an amber as accents, and a plain red for what is wrong. Text is dark navy on light surfaces and pale blue-white on dark ones, never pure black or pure white on a large area.

## Colors by role
The token names are the MudBlazor palette property names in camelCase. Every pair of text and background was measured by `ThemeContrastTests` and `ThemePaletteTests`: text at 4.5:1 or more, icons and field borders at 3:1 or more.

- **Primary** (`primary`, `primaryContrastText`, `primaryDarken`, `primaryLighten`): the brand blue. It is also a text color (links, outlined buttons, the app name), which is why light mode uses the darker `#216DB5` and dark mode the lighter `#64A2E3`. In dark mode a filled primary button takes dark ink, because white cannot sit on that blue. `primaryLighten` is the quiet tint behind the role badge.
- **Secondary** (`secondary`, `secondaryContrastText`, `secondaryDarken`, `secondaryLighten`): the teal. Filled secondary buttons carry the dark ink `#19243E`, since white reads at 3.33:1 on this teal. The hover tone `secondaryDarken` is lighter than the resting teal on purpose, so the dark ink keeps its contrast on hover. `secondaryLighten` is derived by MudBlazor and no component paints with it.
- **Tertiary** (`tertiary`, `tertiaryContrastText`, `tertiaryDarken`, `tertiaryLighten`): the amber accent, with the same dark ink. The two tones are derived by MudBlazor.
- **Info** (`info`, `infoDarken`, `infoLighten`): informational alerts and chips. Its contrast text is MudBlazor's default white and is not overridden.
- **Success** (`success`, `successContrastText`, `successDarken`, `successLighten`): the same teal as secondary, with dark ink on a filled alert or button.
- **Warning** (`warning`, `warningContrastText`, `warningDarken`, `warningLighten`): an amber a little deeper than the tertiary, with dark ink.
- **Error** (`error`, `errorContrastText`, `errorDarken`, `errorLighten`): error text on the page and the destructive filled button. Light mode: a dark red with white text. Dark mode: a lighter red with dark ink.
- **Text** (`textPrimary`, `textSecondary`): body text and headings, then helper text and captions. Both are measured on the page and on the surface; `textPrimary` is also measured on `primaryLighten`.
- **Action** (`actionDefault`): the color of every icon-only button and menu icon.
- **Page and cards** (`background`, `surface`): the page, and cards, dialogs, menus and form panels. In light mode the card is white on a pale blue page; in dark mode it is a lighter navy on a darker one.
- **Frame** (`appbarBackground`, `drawerBackground`, `drawerText`, `drawerIcon`): the app bar and the navigation drawer. Drawer text and drawer icons share one translucent white ink (92% in light mode, 50% in dark mode, written as 8-digit hex). The app bar text is MudBlazor's own default (white in light mode, 70% white in dark mode), set by the theme to the same value, so it has no token.
- **Lines** (`linesInputs`, `tableLines`, `divider`): `linesInputs` is the border of every outlined field and is a control boundary (3:1 against the page and the surface). `tableLines` and `divider` are decorative.

Colors the theme leaves at MudBlazor's default in both modes (disabled states, overlays, grays, `black`, `white`, the hover and striped row tints) have no token: they are indistinguishable from unset.

## Typography
System font stack, no web font: `-apple-system`, `BlinkMacSystemFont`, `Segoe UI`, `Roboto`, `sans-serif`. The scale the theme sets:

- H1: 1.75rem, weight 700, line height 1.2.
- H2: 1.25rem, weight 600, line height 1.3.
- Body 1: 0.9375rem, line height 1.65.
- Body 2: 0.8125rem, line height 1.5.
- Caption: 0.75rem.

Every other style (H3 to H6, subtitles, button, overline) is MudBlazor's default.

## Shape
One corner radius: 10px, for cards, fields, buttons and dialogs. Do not mix in other radii.

## Icons
Material Outlined, always through `AppIcons`. One family, no filled or two-tone variants, no emoji as icons.

## Do
- Draw a mockup with these tokens only, in both light and dark mode.
- Pick the dark ink (`*ContrastText`) for a filled teal, amber or, in dark mode, blue or red surface.
- Use `primary` for anything interactive, and `error` only for what is wrong or destructive.
- Keep the page, card and frame contrast the way the tokens set it: a card is always lighter than its page in dark mode and whiter in light mode.

## Don't
- Don't put white text on a teal or amber fill: it reads at 3.33:1 and 2.11:1.
- Don't use `primary` or `error` from light mode on a dark surface, or the other way round: each mode has its own value.
- Don't paint a literal color in a stylesheet or a component when a token exists (the existing literals in `app.css` are F-83's).
- Don't use a decorative line (`divider`, `tableLines`) as the border of a control: use `linesInputs`.
- Don't change a value here without changing `SimulabTheme.cs`, or the other way round: the test fails and names the token.
