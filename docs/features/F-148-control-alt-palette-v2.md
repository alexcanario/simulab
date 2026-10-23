---
feature: F-148
epic: Foundation and identity
status: done
board: 206
version: 1
---
# Control Alt Systems palette v2 in the theme and kit

Technical terms: [glossary](../glossary.md)

## Summary
The Control Alt Systems identity changed palette on 2026-10-22 (`_cas-design-system`, `tokens.json` version 2, handoff H-04). Simulab recorded the previous palette in F-125. This item takes the new palette to the theme (`SimulabTheme`), `docs/design/DESIGN.md`, `docs/design/identity.tokens.json`, the kit stylesheet, the brand files (keys symbol and app icon) and the emails, and adds the kit's action shortcut (Ctrl + Alt + amber key, with a compact variant).

What changes for Simulab:
- Amber instead of orange: `#F5A020` in both themes, with the edge `#B87410`. Text and icons on amber and on teal are always navy ink (white fails contrast).
- Teal `#1A9E8C` in both themes; blue `#2478C5` (light) / `#47A0EB` (dark).
- Backgrounds: light `#F2F5FB`, `#E6ECF5`; dark `#0B2149`, `#12386F`, `#081938`.
- Navy `#0F3266` becomes the filled primary button in the light theme (the dark theme keeps a blue primary, `#47A0EB`) and the colour of the brand name; the sidebar keeps it in the light theme.
- Links `#1D64A8` (light) / `#8CC4F5` (dark): a new role.
- Error `#B83232` / `#FF9C94`, the design system's `danger`; warning stays Simulab's `#D98B1A`.
- The action button becomes the Ctrl + Alt + amber key shortcut, with a compact variant: a new kit component. No routed page uses it yet.

Source: `_cas-design-system/design-system/tokens.json` (version 2), `HANDOFFS.md` H-04 and `components/Button/README.md`.

## Start
- Depends on: F-125
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, in both themes, at desktop and phone width; Docker Desktop running for the app host — owner
- Model: sonnet (no signal: no door, no public contract, no schema, size M)
- Suggested path: `/agile:build F-148`
- Parallel with: none. Do it before the screens F-126 to F-134, which inherit the colours.

## Goal
Give every screen the new Control Alt Systems colours in the one place that already holds them (the theme, the kit stylesheet and the brand files), so the screen items F-126 to F-134 only change composition.

## Users and use cases
- UC1 A student, curator or admin opens any screen and sees the v2 palette: new page and surface tones, amber and teal keys, navy primary button, in the light theme.
- UC2 A user switches to the dark theme and sees the v2 dark tones, readable at WCAG 2.2 AA.
- UC3 A developer opens `/dev/ui` and sees the palette of the theme in use, every button variant and the new action shortcut (full and compact), to build the screen items from them.
- UC4 A user receives a verification, password, erasure or data export email and its button and text follow the v2 palette.

## Business rules
- BR1 The palette of `## Approved list` replaces F-125's in `SimulabTheme` (light and dark). Role names do not change, only values; one role is new: `link`.
- BR2 Every colour comes from `SimulabTheme`. The link colour and the keycap edges reach the stylesheet as variables written by `SimulabThemeProvider`; no screen and no stylesheet carries a literal colour, except the QR code's white quiet zone (F-125 BR2, unchanged).
- BR3 Every text pair reads at 4.5:1 and every non-text pair at 3:1, in both themes. A filled button is identified by its fill at 3:1 against the page and the surface, except the amber action in the light theme: its fill reads 1.94:1 on the page, so it is identified by its edge `#B87410` (3.47:1) and carries the edge always. The edge of every other keycap stays decorative (F-125 change note v2).
- BR4 Text and icons on teal and on amber are navy ink `#0B1B2E`; white is never used on them. White on the light primary (navy), on the light info blue and on the light error is allowed (12.59, 4.60 and 5.93:1).
- BR5 Primary (filled, `Color.Primary`): light theme, navy `#0F3266`, white text, edge `#6B7C91` (the design system's `line-strong`); dark theme, blue `#47A0EB`, ink text, edge `#1F6FB5`. Navy does not work as a fill on the dark page (1.26:1).
- BR6 Text buttons and every link take the `link` colour (light `#1D64A8`, dark `#8CC4F5`), no longer the info blue.
- BR7 One amber action at most per screen, only in the kit and the gallery; no routed page uses it until its screen item (F-126 to F-134) picks it (F-125 BR5, confirmed). The amber key of the brand symbol in the app bar is the brand mark and does not count.
- BR8 The action shortcut is a kit component with two variants. Shortcut: a Ctrl key (teal, navy label), an Alt key (blue, its label), `+` signs and the amber key with the restart icon and the caller's label; compact: the amber key alone, with the same icon and label, for areas narrower than 360px and toolbars. One element is clickable; Ctrl, Alt and `+` are decorative (`aria-hidden`) and the accessible name is the amber key's label. Minimum height 44px. Pressed, the amber key moves down and its edge shrinks, and in the shortcut Ctrl and Alt sink too and the icon turns half way (200 degrees, 0.4 s; none under `prefers-reduced-motion`). Disabled: the sunken fill, ink-muted text, no edge, a real `disabled`. It sits only on page, surface or white, never on the sunken or tint backgrounds.
- BR9 The app bar stays white (light) or the raised surface (dark), as decided in F-125 (D-4). The brand name "Simulab" takes navy `#0F3266` in the light theme and ink in the dark theme (navy on the dark bar reads 1.09:1); the symbol and the app icon are those of the v2 logos.
- BR10 The sidebar is navy `#0F3266` in the light theme and the sunken surface `#081938` in the dark theme (F-125 decision, confirmed: navy reads 1.26:1 on the dark page).
- BR11 Error adopts the design system's `danger` (`#B83232` with white text / `#FF9C94` with ink text). Warning keeps Simulab's `#D98B1A` with ink text. Success is the teal, as secondary is.
- BR12 Emails use the navy `#0F3266` with white text for their button, ink `#0B1B2E` for text and the Bricolage Grotesque stack, as F-125 BR12 with the new primary.

## Screens and API
- No route, endpoint or error code changes. Every routed page changes colour through the theme and the kit stylesheet.
- `/dev/ui` (dev only): `gallery-buttons` gains the action shortcut and its compact variant; `gallery-palette` lists the v2 roles, `link` included.
- Files: `Theme/SimulabTheme.cs`, `Theme/SimulabThemeProvider.cs`, `wwwroot/app.css`, `wwwroot/brand/` (three files), `Components/Layout/MainLayout.razor` (brand name colour), `Components/Ui/AppActionButton.razor` (new) and its icon in `AppIcons`, `Components/Pages/Dev/UiGallery.razor`, the resource files of the gallery, `Simulab.Identity.Infrastructure/Email/EmailHtml.cs`, `docs/design/identity.tokens.json`, `docs/design/DESIGN.md`, `docs/decisions/ADR-0007-control-alt-palette-v2.md` (new), ADR-0006 (a line saying the palette is superseded in part), `docs/glossary.md`.

### Wireframe: App bar (every screen with `MainLayout`)
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

### Wireframe: UI gallery buttons (`/dev/ui`, section `gallery-buttons`)
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

## Docs
- flow: update docs/flows/brand-identity-theme.md — the palette v2, the navy primary, the `link` role, the action shortcut, and the two wireframes above.

## Acceptance criteria
- AC1 Given `SimulabTheme`, when the light and dark palettes are built, then every role holds the value of `## Approved list` (1. Palette), `link` included, and `docs/design/identity.tokens.json` and `DESIGN.md` hold the same values (BR1, BR9, BR10, BR11).
- AC2 Given both themes, when every text pair and non-text pair of the palette is measured, then text reads at 4.5:1 or more and non-text at 3:1 or more, with the fill of each filled button on the page and the surface (the amber action in the light theme measured by its edge), and text on teal and amber is navy ink (BR3, BR4).
- AC3 Given a page with a filled primary, an outlined, a text and a filled tertiary `MudButton`, when it renders, then the primary is navy with white text in the light theme and blue with ink text in the dark theme, and each button keeps the classes and the 44px minimum height (36px small) (BR5).
- AC4 Given the kit stylesheet, when a text button and a link render, then both use the `link` colour from the theme provider, and no stylesheet carries a literal colour (BR2, BR6).
- AC5 Given the routed pages, when they are scanned, then none uses `Color.Tertiary` on a button or the action shortcut component (BR7).
- AC6 Given the action shortcut component, when it renders in each variant, then the shortcut shows the Ctrl key, the Alt key, the `+` signs and the amber key, the compact one only the amber key, Ctrl, Alt and `+` are `aria-hidden`, and the one clickable element is named by the caller's label (BR8).
- AC7 Given the action shortcut, when it is disabled, pressed or the user prefers reduced motion, then the disabled one has a real `disabled`, a sunken fill and no edge; the pressed one moves down and its edge shrinks; and the icon turn has no animation under `prefers-reduced-motion` (BR8).
- AC8 Given any page with `MainLayout`, when it renders, then the app bar is white (light) or the raised surface (dark), the name "Simulab" is navy (light) or ink (dark), and the sidebar is navy (light) or the sunken surface (dark) (BR9, BR10).
- AC9 Given the files `wwwroot/brand/keys-symbol.svg`, `keys-symbol-dark.svg` and `app-icon.svg`, when they are read, then the symbol keys use teal `#1A9E8C`, blue (`#2478C5` light, `#47A0EB` dark) and amber `#F5A020`, and the app icon is the v2 icon of the design system (BR9).
- AC10 Given the verification, password, erasure and data export emails, when they are sent, then their button is `#0F3266` with white text, their text is `#0B1B2E` and they open with "Simulab" and "Control Alt Systems" as text (BR12).
- AC11 Given `/dev/ui`, when it opens, then `gallery-buttons` shows the action shortcut and the compact variant, and `gallery-palette` lists the v2 values of the theme in use (UC3).
- AC12 Given ADR-0007, when `docs/decisions/` is read, then it records the palette v2 decisions and ADR-0006 points to it.
- AC13 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en) and the missing-key test is green.

## Criterion → test

Tests locked: aaf78b5cf96c76f3364f5f8b94693547c1ea4bff (11 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `ApprovedPaletteTests` (`Role_HoldsTheApprovedValue`, `KeycapEdges_HoldTheApprovedValues`, `LinkAndBrandName_HoldTheApprovedValues`); `IdentityTokensTests` (`Colours_EqualTheThemeInBothModes`, `DesignMd_FrontMatterGivesTheValueOfEveryToken`) |
| AC2 | `ThemePaletteTests.Pair_ReadsAtTheMinimumOfItsRole`; `KeycapContrastTests` (`FilledButton_TextReadsAtAaAtRestAndOnHover`, `FilledButton_FillReadsAtTheNonTextMinimumOnThePageAndTheSurface`, `TealAndAmber_CarryInkNeverWhite`, `LinkRole_ReadsAtAaOnItsSurfaces`) |
| AC3 | `KeycapButtonTests` (`MudButton_RendersTheClassesTheKitStyles`, `FilledVariant_IsAKeycapWithItsEdge`, `Buttons_HaveTheMinimumHeights`); `KeycapContrastTests.FilledButton_TextReadsAtAaAtRestAndOnHover` |
| AC4 | `KeycapButtonTests` (`TextVariant_IsAnUnderlinedLink`, `Links_TakeTheLinkRole`, `ThemeProvider_WritesTheLinkAndTheBrandNameOfTheThemeInUse`); `StylesheetLiteralColourTests` |
| AC5 | `ActionColourScopeTests.RoutedPages_PutNoTertiaryColourOnAButtonAndNoActionShortcut` |
| AC6 | `AppActionButtonTests` (`Shortcut_ShowsCtrlAltTwoPlusSignsAndTheAmberKey`, `Compact_ShowsTheAmberKeyAlone`, `TheClickableElementIsOneButtonNamedByTheLabel`, `CtrlAltAndPlusSigns_AreDecorative`, `Click_RaisesOnClick`) |
| AC7 | `AppActionButtonTests` (`Disabled_IsARealDisabledAndMarksTheWholeShortcut`, `Disabled_IsSunkenWithNoEdge`, `Pressed_MovesTheKeyDownShrinksTheEdgeAndTurnsTheIcon`, `ReducedMotion_StopsTheTurnAndTheTransitions`) |
| AC8 | `KeycapContrastTests.BrandName_ReadsAtAaOnTheBar`; `ApprovedPaletteTests.Role_HoldsTheApprovedValue` (app bar, sidebar); `AppBarBrandTests.Bar_IsTheThemesAppBarColourWithItsLineBelow` |
| AC9 | `BrandFilesTests` (`Symbol_HasTheTealBlueAndAmberKeys`, `DarkSymbol_TakesTheDarkThemesBlueAndKeepsTealAndAmber`, `AppIcon_IsTheNavyTileWithTheAmberKey`, `BrandFile_HoldsNoColourOfTheOldPalette`) |
| AC10 | `EmailIdentityTests` (`Email_ButtonIsTheNavyPrimaryWithWhiteText`, `Email_OpensWithTheBrandAsTextInInkAndTheBrandFont`) |
| AC11 | `UiGalleryIdentityTests` (`Buttons_ShowTheFourVariantsTheSmallSizeAndTheStates`, `Palette_ShowsEveryRoleOfTheThemeInUseWithItsValue`); `ActionColourScopeTests.Gallery_ShowsTheActionShortcutAndItsCompactVariant` |
| AC12 | `PaletteV2DecisionTests` (`Adr0007_RecordsThePaletteV2Decisions`, `Adr0006_PointsToAdr0007`) |
| AC13 | `ResourceParityTests` (existing missing-key test, green with the new gallery keys) |

## Approved list

### 1. Palette
Light / dark. Hover tones (the `*Darken` slots) and the keycap edges not listed are derived at build by the F-125 rule (8% brighter while the contrast text keeps 4.5:1, else 8% darker) and pinned by `ThemePaletteTests`.
1. Background (page): `#F2F5FB` / `#0B2149`
2. Surface (cards, dialogs, fields): `#FFFFFF` / `#12386F`
3. Surface sunken (disabled, wells): `#E6ECF5` / `#081938`
4. Text primary (ink): `#0B1B2E` / `#F2F5FB`
5. Text secondary and action default (ink-muted): `#52647A` / `#B8C4D2`
6. Inputs and secondary button border (line-strong): `#6B7C91` / `#7F9BC0`
7. Divider and table lines (line): `#D9E1EE` / `#24497F`
8. Primary: `#0F3266`, text `#FFFFFF`, edge `#6B7C91` / `#47A0EB`, text `#0B1B2E`, edge `#1F6FB5`
9. Info: `#2478C5`, text `#FFFFFF` / `#47A0EB`, text `#0B1B2E`
10. Link (new role): `#1D64A8` / `#8CC4F5`
11. Secondary and success (teal): `#1A9E8C`, text `#0B1B2E`, edge `#107264` / `#1A9E8C`, text `#0B1B2E`, edge `#107264`
12. Tertiary (action, amber): `#F5A020`, text `#0B1B2E`, edge `#B87410` / `#F5A020`, text `#0B1B2E`, edge `#B87410`
13. Warning (kept): `#D98B1A`, text `#0B1B2E` / `#D98B1A`, text `#0B1B2E`
14. Error: `#B83232`, text `#FFFFFF`, edge derived (error x0.72, `#842424`) / `#FF9C94`, text `#0B1B2E`, edge derived (error x0.65, `#A66560`)
15. App bar: `#FFFFFF`, text `#0B1B2E`, subline `#52647A`, brand name `#0F3266` / `#12386F`, text `#F2F5FB`, subline `#B8C4D2`, brand name `#F2F5FB`
16. Sidebar (drawer): `#0F3266`, text and icon `rgba(255,255,255,0.92)` / `#081938`, text and icon `#B8C4D2`
17. Focus ring: `#2478C5` / `#47A0EB`, 3px, 2px offset (inside the sidebar it takes the drawer's text colour, as in F-125)
18. Tint blue (informative highlights): `#DCE8F5` / `#1A4583`

### 2. Action shortcut
1. Shortcut: Ctrl key (teal, navy label), Alt key (blue, its label), `+` signs in navy, amber key with the restart icon and the label
2. Compact: the amber key alone
3. The amber key carries the edge `#B87410` always; Ctrl and Alt carry their edges `#107264` and the blue key edge of the theme

## Decisions
- 2026-10-22 — One amber action at most per screen, only in the kit and the gallery; no routed page uses it until F-126 to F-134 — F-125 BR5, still true — confirmed: docs/features/F-125-control-alt-identity-ui-kit.md
- 2026-10-22 — Dark sidebar is the sunken surface `#081938`, the light one navy — navy reads 1.26:1 on the dark page — confirmed: docs/features/F-125-control-alt-identity-ui-kit.md
- 2026-10-22 — Emails follow the new identity (navy button, same ink and font stack) — confirmed: docs/features/F-125-control-alt-identity-ui-kit.md
- 2026-10-22 — Primary in the dark theme is blue `#47A0EB` with ink text and edge `#1F6FB5`; navy only in the light theme — navy reads 1.26:1 on the dark page, blue 5.64:1, ink on it 6.19:1 — asked
- 2026-10-22 — The app bar stays white (light) and raised (dark); only the name "Simulab" takes navy (light) or ink (dark) — the owner approved a white bar in F-125 (D-4), and navy on the dark bar reads 1.09:1 — asked
- 2026-10-22 — The action shortcut and its compact variant are built now as one kit component — the design system defines the button that way and F-126 to F-134 then find it ready; no routed page uses it — asked
- 2026-10-22 — The amber action is identified by its edge `#B87410` in the light theme (fill 1.94:1 on the page, edge 3.47:1); every other filled button is still identified by its fill — the design system's own rule for amber — asked
- 2026-10-22 — Error adopts the design system's `danger` (`#B83232` / `#FF9C94`) — one source for the brand — asked
- 2026-10-22 — Warning keeps `#D98B1A`: an alert strip with text and icon is not a keycap, and a screen has one action — asked
- 2026-10-22 — The primary edge in the light theme is `#6B7C91` (`line-strong`), as the design system's button README says; the owner sees it on screen at validation — Claude
- 2026-10-22 — The F-125 palette (`## Approved list`) is superseded by this item's list; F-125's file is not rewritten — it is a delivered item. ADR-0007 records palette v2 and ADR-0006 gets one line pointing to it — Claude
- 2026-10-22 — The link role is a CSS variable (`--app-link`) written by `SimulabThemeProvider` next to the keycap edges, since MudBlazor's palette has no slot for it — Claude
- 2026-10-22 — Success stays the teal (as secondary); `#166F49` is the Cais's own derived success and is not used here — Claude
- 2026-10-22 — The restart icon is `AppIcons` Material Outlined `RestartAlt`; the key labels "Ctrl" and "Alt" in the shortcut are brand marks, decorative like the symbol, and carry no resource key — the rule `ui-project` (icons from `AppIcons`) — Claude
- 2026-10-22 — The dark brand symbol is derived by hand from the v2 tokens, as in F-125, because the design system ships one `simbolo-teclas.svg` only — Claude
- 2026-10-22 — No new package; the old item mockups under `docs/features/mockups/` keep their colours (historical records) — Claude
- 2026-10-22 — Manual: `docs/manual/pt-BR/getting-around.md` (lines 13, 14 and 16) names the primary button and the email button "azul-escuro" and the app bar white; navy `#0F3266` is still a dark blue, so those lines stay true and the manual is unchanged. The build re-reads them against the screen; if one is false, it is fixed in pt-BR only (pt-PT and en are frozen, F-124) — Claude
- 2026-10-22: plan — 1 tests first: bring the F-125 palette, contrast, tokens, brand, email and gallery tests to v2 values and add the action shortcut, link, amber edge, SVG, ADR tests → red, `test(F-148)` commit and lock; 2 `SimulabTheme` palettes and `SimulabThemeProvider` (`link`, edges incl. primary) → AC1, AC2; 3 `app.css` buttons, links, brand name via variables → AC3, AC4, AC8; 4 brand SVGs, `MainLayout`, `EmailHtml` → AC8, AC9, AC10; 5 `AppActionButton` + `AppIcons.RestartAlt` + css + gallery + resources in three locales → AC5, AC6, AC7, AC11, AC13; 6 `identity.tokens.json`, `DESIGN.md`, ADR-0007, ADR-0006 line, glossary, flow page `brand-identity-theme.md` → AC1, AC12; 7 gate, coverage table, review/scan, validation script.
- 2026-10-23: tests unlocked (owner chose "Destravar e corrigir"): `TealAndAmber_CarryInkNeverWhite` compared an rgba string with a hex and `Disabled_IsARealDisabledAndMarksTheWholeShortcut` clicked a disabled button; assertions of the criteria unchanged — Claude, asked
- 2026-10-23: dark primary and info blue are fills, not text (4.12:1 on the dark surface): blue text in the app is the `link` role, and section titles and the auth app name take `--app-brand-name`; `ThemePaletteTests` and `ThemeContrastTests` no longer measure the dark primary as text — Claude
- 2026-10-23: `link` and `brandName` are tokens of `identity.tokens.json` and `DESIGN.md` beside the palette's own (`IdentityTokensComparison` adds them to the theme side) — Claude

## Out of scope
- Composition of each screen and which screen gets the amber action: F-126 to F-134.
- A partner's own name and logo: F-139.
- Exam session screens (distraction-free exception): wait on the first exam session screen (D-4).
- The Control Alt Systems logo in emails as an image.
- Rewriting the old item mockups under `docs/features/mockups/`.

## Open questions
- (none)

## Change notes

## Validation script
### English
Needed to validate: you, on screen, in both themes, at desktop and phone width, and Docker Desktop running for the app host. Both are in place now (Docker Desktop was running during this build).

1. Stop any app host you have open (a running one locks `bin/`), then start this item's app host from the worktree folder. It runs on its own database, `simulab_f148`, on the local PostgreSQL server; nothing lands in the shared `simulab` database. Wait until the Aspire dashboard address is printed, then open https://localhost:7125.

```powershell
cd D:\wt\simulab\f-148-control-alt-palette
$env:Database__Name = "simulab_f148"; dotnet run --project src/Hosts/Simulab.AppHost
```

```bash
cd /d/wt/simulab/f-148-control-alt-palette
Database__Name=simulab_f148 dotnet run --project src/Hosts/Simulab.AppHost
```

   Expected: a line `Dashboard: https://localhost:<port>/login?t=<token>`; https://localhost:7125 answers with the home page.
2. The app bar (the bar at the top of every page) and the sidebar in the light theme:
   - [ ] 2.1 The page is a pale blue-grey; the app bar is white with a thin line under it; "Simulab" is navy and "Control Alt Systems" is grey under it.
   - [ ] 2.2 The brand symbol has three keys: teal Ctrl, blue Alt and an amber restart key. The browser tab shows the new app icon (amber key on navy).
   - [ ] 2.3 The sidebar (left menu) is navy.
3. The UI gallery: in the sidebar, under Development, click UI kit; the page "UI kit gallery" opens. Scroll to "Buttons".
   - [ ] 3.1 Save is a navy key with white text and a grey edge under it; Filter has a grey 2px border; Cancel is an underlined blue link; Delete appears red, filled and outlined.
   - [ ] 3.2 Under "Action (one per screen)": the shortcut shows a teal Ctrl key, a blue Alt key, two plus signs and an amber key with the restart icon and "Start exam"; the line below shows the amber key alone. The amber key has a darker amber edge under it, and its text and icon are dark navy, not white.
   - [ ] 3.3 Point at Save (it turns a little brighter), hold the mouse button down on it (it sinks onto its edge); hold it down on the amber key of the shortcut: the amber key, Ctrl and Alt sink and the restart arrow turns half way.
   - [ ] 3.4 In the disabled row (the row with the greyed buttons), all keys of the disabled shortcut are grey with no edge and the amber key does not react to the mouse.
   - [ ] 3.5 "Palette" lists each role with its value, `link` included: primary `#0F3266`, link `#1D64A8`, secondary `#1A9E8C`, tertiary `#F5A020`, background `#F2F5FB`.
4. The dark theme: click the theme switch (the moon at the top right).
   - [ ] 4.1 The page turns deep navy, cards a lighter navy, the app bar takes the card colour, "Simulab" is light (not navy), the sidebar is the darkest navy and the brand symbol shows a lighter blue Alt key.
   - [ ] 4.2 In the gallery, Save is a light blue key with dark text, Cancel is a light blue link, and "Palette" now shows primary `#47A0EB`, link `#8CC4F5` and background `#0B2149`.
   - [ ] 4.3 Every text is easy to read on its background; the Alt key label of the shortcut is dark. Switch back to light with the theme switch.
5. [ ] Phone width: open the browser developer tools (F12), turn on the device toolbar (Ctrl+Shift+M) and choose 375 px wide. The app bar keeps the symbol, "Simulab" and "Control Alt Systems" on one line each, nothing runs off the right edge, and the compact action (the amber key alone) is the one to use there: it fits the gallery column.
6. [ ] Language: in the language switch (the globe at the top right) choose Português (Brasil). The gallery (sidebar > Desenvolvimento > Kit de interface) reads "Ação (uma por tela)" over the shortcut. Switch back to English.
7. Keyboard only, on the UI gallery, without the mouse:
   - [ ] 7.1 Press Tab from the top of the page: each control you reach shows a blue focus ring; the shortcut shows one ring, on the amber key only (Ctrl and Alt are skipped).
   - [ ] 7.2 Tab to the amber key and hold Space: it sinks onto its edge; press Enter on Save: the button reacts.
8. The emails: open https://localhost:7125/sign-up and create an account with an address of your choice (any, Mailpit catches it); then open Mailpit from the Aspire dashboard (the "mailpit" resource, its web link).
   - [ ] 8.1 The verification email opens with "Simulab" and "Control Alt Systems" as text, then its heading; its button is navy (`#0F3266`) with white text.
   - [ ] 8.2 At https://localhost:7125/forgot-password ask a reset for the same address: the reset email has the same top lines and the same button.

### pt-BR
Necessário para validar: você, na tela, nos dois temas, em largura de computador e de celular, e o Docker Desktop rodando para o app host. Os dois estão disponíveis agora (o Docker Desktop estava rodando durante esta construção).

1. Pare qualquer app host que esteja aberto (um app host rodando trava a pasta `bin/`) e suba o app host deste item a partir da pasta do worktree. Ele roda no seu próprio banco, `simulab_f148`, no PostgreSQL local; nada cai no banco compartilhado `simulab`. Espere o endereço do painel do Aspire aparecer e abra https://localhost:7125.

```powershell
cd D:\wt\simulab\f-148-control-alt-palette
$env:Database__Name = "simulab_f148"; dotnet run --project src/Hosts/Simulab.AppHost
```

```bash
cd /d/wt/simulab/f-148-control-alt-palette
Database__Name=simulab_f148 dotnet run --project src/Hosts/Simulab.AppHost
```

   Esperado: uma linha `Dashboard: https://localhost:<porta>/login?t=<token>`; https://localhost:7125 responde com a página inicial.
2. A barra superior (a barra no topo de toda página) e o menu lateral no tema claro:
   - [ ] 2.1 A página é um cinza-azulado claro; a barra superior é branca com uma linha fina embaixo; "Simulab" está em azul-marinho e "Control Alt Systems" em cinza embaixo.
   - [ ] 2.2 O símbolo da marca tem três teclas: Ctrl verde-água, Alt azul e a tecla âmbar de reiniciar. A aba do navegador mostra o ícone novo do app (tecla âmbar sobre azul-marinho).
   - [ ] 2.3 O menu lateral (à esquerda) é azul-marinho.
3. A galeria do kit: no menu lateral, em Desenvolvimento, clique em Kit de interface; abre a página da galeria. Role até "Botões".
   - [ ] 3.1 Salvar é uma tecla azul-marinho com texto branco e uma aresta cinza embaixo; Filtrar tem borda cinza de 2px; Cancelar é um link azul sublinhado; Excluir aparece vermelho, cheio e contornado.
   - [ ] 3.2 Em "Ação (uma por tela)": o atalho mostra a tecla Ctrl verde-água, a tecla Alt azul, dois sinais de mais e a tecla âmbar com o ícone de reiniciar e "Iniciar simulado"; a linha de baixo mostra só a tecla âmbar. A tecla âmbar tem uma aresta âmbar mais escura embaixo, e o texto e o ícone são azul-marinho escuro, não brancos.
   - [ ] 3.3 Passe o mouse em Salvar (fica um pouco mais claro) e segure o botão do mouse nele (afunda sobre a aresta); segure na tecla âmbar do atalho: a tecla âmbar, o Ctrl e o Alt afundam e a seta de reiniciar gira meia volta.
   - [ ] 3.4 Na linha dos desativados (a linha dos botões acinzentados), todas as teclas do atalho desativado ficam cinza, sem aresta, e a tecla âmbar não reage ao mouse.
   - [ ] 3.5 "Paleta" lista cada papel com seu valor, `link` incluído: primary `#0F3266`, link `#1D64A8`, secondary `#1A9E8C`, tertiary `#F5A020`, background `#F2F5FB`.
4. O tema escuro: clique no botão de tema (a lua, no canto superior direito).
   - [ ] 4.1 A página fica azul-marinho profundo, os cartões um azul-marinho mais claro, a barra superior toma a cor do cartão, "Simulab" fica claro (não azul-marinho), o menu lateral é o azul mais escuro e o símbolo mostra a tecla Alt num azul mais claro.
   - [ ] 4.2 Na galeria, Salvar é uma tecla azul clara com texto escuro, Cancelar é um link azul claro, e "Paleta" agora mostra primary `#47A0EB`, link `#8CC4F5` e background `#0B2149`.
   - [ ] 4.3 Todo texto é fácil de ler sobre o fundo; o rótulo da tecla Alt do atalho é escuro. Volte ao claro com o botão de tema.
5. [ ] Largura de celular: abra as ferramentas do desenvolvedor do navegador (F12), ligue a barra de dispositivos (Ctrl+Shift+M) e escolha 375 px de largura. A barra superior mantém o símbolo, "Simulab" e "Control Alt Systems" em uma linha cada, nada sai pela borda direita, e a ação compacta (só a tecla âmbar) é a que deve ser usada ali: ela cabe na coluna da galeria.
6. [ ] Idioma: no seletor de idioma (o globo no canto superior direito) escolha Português (Brasil). A galeria (menu lateral > Desenvolvimento > Kit de interface) mostra "Ação (uma por tela)" sobre o atalho. Volte para o inglês.
7. Só com o teclado, na galeria:
   - [ ] 7.1 Aperte Tab a partir do topo da página: cada controle alcançado mostra um anel de foco azul; o atalho mostra um único anel, só na tecla âmbar (Ctrl e Alt são pulados).
   - [ ] 7.2 Vá com Tab até a tecla âmbar e segure Espaço: ela afunda sobre a aresta; aperte Enter em Salvar: o botão reage.
8. Os e-mails: abra https://localhost:7125/sign-up e crie uma conta com um endereço qualquer (o Mailpit recebe); depois abra o Mailpit pelo painel do Aspire (o recurso "mailpit", seu link web).
   - [ ] 8.1 O e-mail de verificação abre com "Simulab" e "Control Alt Systems" como texto, depois o título; o botão é azul-marinho (`#0F3266`) com texto branco.
   - [ ] 8.2 Em https://localhost:7125/forgot-password peça a redefinição para o mesmo endereço: o e-mail de redefinição tem as mesmas linhas no topo e o mesmo botão.

## Events

- 2026-10-22 question — asked 6, from context 3
- 2026-10-23 blocked — test wrong: two locked tests had test-side errors: colour format compared with hex, and a click on a disabled button

## Delivery

- Branch: `feature/F-148` (removed), merge commit `e641e78` on `main`, pushed; app version 1.15.0 → 1.16.0.
- Tests (full gate, `agile gate GREEN`): Web 2445, QuestionBank 677, Catalog 665, Identity 548, Architecture 301, AppHost 122, Api 83, Persistence 25, Jobs 25, Email 19, SharedKernel 12, ApiResults 13, Storage 8, Ai 10; 0 failed. Security scan `agile scan GREEN`.
- Manual: `docs/manual/pt-BR/getting-around.md` (button edge wording); pt-PT and en are frozen (F-124). Technical page: `docs/flows/brand-identity-theme.md`. ADR-0007.
- Validation: the owner said "validado" in chat on 2026-10-23; the boxes of the script were not ticked in the tool, so the evidence below reads `not tested` per step.
- Validation evidence: see the board comment of the issue; step boxes were not ticked.

Gate time: 7 passes, 29 min 24 s — turn 3 (3 min), ship 2 (17 min 27 s), scan 2 (8 min 57 s); 0 red
Usage: 4881.9k weighted (estimate) — refine 14% · build 76% · ship 10% · other 1% — context per turn 185k → 420k — 2 sessions, 180 turns
