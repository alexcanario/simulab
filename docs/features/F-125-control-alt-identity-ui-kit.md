---
feature: F-125
epic: Foundation and identity
status: validating
board: 176
version: 2
---
# Control Alt Systems identity in the theme and the UI kit

Technical terms: [glossary](../glossary.md)

## Summary
Every screen takes the Control Alt Systems identity in one change of `SimulabTheme`, the UI kit and the gallery `/dev/ui`: the palette of `## Approved list` (page `#F4F6F9`, ink `#0B1B2E`, primary dark blue `#064F80`, blue `#0872B8` for links and info, green `#00806B`, one orange `#E8650A` action per screen), Bricolage Grotesque and JetBrains Mono, keycap-shaped buttons, bordered cards with no soft shadow, a white app bar with the Control Alt Systems key symbol, "Simulab" and "Control Alt Systems" under it, and the sidebar kept in Simulab's navy `#0F3266` (`#081522` in the dark theme). Field icons and placeholders stay those of the current kit. The emails take the same button colour and type. Source: `_cas-design-system/design-system/tokens.json` and its README, the Stitch reference of the F-95 question form approved on 2026-10-17, and D-4. It also absorbs F-83 (the literal colours of `wwwroot/app.css` come from the tokens), records the brand decision ADR-0001 decision 30 left under "Revisit when" in ADR-0006, and updates `docs/design/identity.tokens.json` and `DESIGN.md`.

## Start
- Depends on: nothing
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, in both themes, desktop and phone width; Docker Desktop running for the app host — owner
- Model: opus (a wide visual change across theme, kit, gallery, emails and the contrast tests, with judgement on every pair of colours)
- Suggested path: `/agile:build F-125`
- Parallel with: none — F-126 to F-134 and F-139 depend on it

## Goal
Give Simulab its own brand (the Control Alt Systems identity) in one place, so every screen changes at once and the screen items F-126 to F-134 only change composition.

## Users and use cases
- UC1 A student, curator or admin opens any screen and sees the Control Alt Systems identity: palette, fonts, keycap buttons, bordered cards, the white app bar with the brand.
- UC2 A user switches between the light and the dark theme and both follow the identity, readable at WCAG 2.2 AA.
- UC3 A developer opens `/dev/ui` and sees every button variant, the type scale and the palette of both themes, to build the screen items from them.
- UC4 A user receives a verification, password, erasure or data export email and its button and type follow the identity.

## Business rules
- BR1 Two themes only, light and dark, both of the Control Alt Systems identity; the Simulae identity is removed, not kept as a choice.
- BR2 Every colour comes from `SimulabTheme` (light and dark), whose values are those of `## Approved list` (1. Palette); no screen and no stylesheet carries a literal colour, except the QR code's white quiet zone (`.app-security-qr`).
- BR3 Every text pair reads at 4.5:1 and every non-text pair (field border, icon, the keycap top — the fill of a filled button — against the page and the surface, focus ring) at 3:1, in both themes; the keycap edge is decorative and has no minimum; as `ThemeContrastTests` and `ThemePaletteTests` check today.
- BR4 Buttons follow the four variants of `## Approved list` (3. Buttons). Every `MudButton` takes its variant from its existing `Variant` and `Color`, set once in the kit stylesheet; no page changes a button to get the look.
- BR5 One orange action at most per screen. F-125 delivers the variant (`Color.Tertiary`, filled) in the kit and the gallery only; no routed page uses it until its screen item (F-126 to F-134) picks its action. The orange key of the brand symbol in the app bar is the brand mark and does not count as that action.
- BR6 No diffuse shadow: cards, the app bar, the sidebar, menus and dialogs are separated by a 1px `line` border; the only shadow is the keycap edge.
- BR7 Corner radii follow `## Approved list` (4. Shape): 6px tags and chips, 12px buttons and fields, 20px cards and dialogs.
- BR8 Fonts are served by the app (never a third-party host), with their OFL licence files next to them: Bricolage Grotesque 500 for text and 800 for titles, JetBrains Mono 500/700 for code, grids of numbers and keyboard shortcuts; the fallback stacks are those of `tokens.json`.
- BR9 The app bar shows, from left: the menu button, the brand symbol (three keys: Ctrl green, Alt blue, restart orange), "Simulab" (Bricolage 800) with "Control Alt Systems" under it in `ink-muted`, then the theme switch, the language switch and the user menu. The symbol is decorative (`aria-hidden`); the home link keeps its accessible name. Both texts are resource keys in the three locales. The same layout fits at 375px width.
- BR10 The favicon is the Control Alt Systems app icon (`icone-app.svg`).
- BR11 Field icons and placeholders stay those of the current kit (F-95, F-114).
- BR12 Emails use the primary dark blue `#064F80` with white text for their button, ink `#0B1B2E` for text, and the Bricolage Grotesque stack with its fallback (email clients do not load web fonts); "Simulab" and "Control Alt Systems" head each email as text, no image.

## Screens and API
- No route, endpoint or error code changes. Every routed page changes look through the theme and the kit stylesheet.
- `/dev/ui` (dev only) — gains three sections: `gallery-buttons` (four variants, sizes and states), `gallery-type` (the type scale and the mono font), `gallery-palette` (the palette of the theme in use, with each role's name and value).
- Files: `Theme/SimulabTheme.cs`, `wwwroot/app.css`, `wwwroot/fonts/` (new), `wwwroot/brand/` (new: symbol light and dark, favicon), `Components/App.razor` (favicon link), `Components/Layout/MainLayout.razor` (app bar), `Simulab.Identity.Infrastructure/Email/EmailHtml.cs` and the mailers, `docs/design/identity.tokens.json`, `docs/design/DESIGN.md`, `docs/decisions/ADR-0006-control-alt-identity.md` (new), ADR-0001 decision 30.

### Wireframe: App bar (every screen with `MainLayout`)
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

### Wireframe: UI gallery (`/dev/ui`)
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

## Docs
- flow: new docs/flows/brand-identity-theme.md — the item draws two screens (app bar, UI gallery); the page carries their wireframes and how the theme reaches every screen (added at the ship: `techdocs.js check` refuses `none` for an item with a wireframe)

## Acceptance criteria
- AC1 Given `SimulabTheme`, when the light and the dark palettes are built, then every role holds the value of `## Approved list` (1. Palette) and `docs/design/identity.tokens.json` and `DESIGN.md` hold the same values (BR1, BR2).
- AC2 Given both themes, when every text pair and every non-text pair of the palette is measured, then text reads at 4.5:1 or more and non-text at 3:1 or more, the fill of every filled button variant against the page and the surface included; the keycap edge is decorative (BR3).
- AC3 Given `wwwroot/app.css`, when its colours are read, then no literal colour is left but the QR code's white quiet zone; the validation outlines, the validation message, the error boundary and the checkbox border use theme variables (BR2, F-83).
- AC4 Given a page with a filled primary, an outlined, a text and a filled tertiary `MudButton`, when it renders, then each has the classes the kit stylesheet styles as keycap primary, secondary, text link and action, with a minimum height of 44px (36px for the small size) (BR4).
- AC5 Given the routed pages, when they are scanned, then none uses `Color.Tertiary` on a button (BR5).
- AC6 Given the kit stylesheet and the theme, when cards, the app bar, the sidebar, menus and dialogs render, then none has a diffuse shadow and each has a 1px border in `line` (BR6).
- AC7 Given the theme and the kit stylesheet, when tags, buttons, fields, cards and dialogs render, then their radii are 6px, 12px, 12px, 20px and 20px (BR7).
- AC8 Given the app head, when a page loads, then the four `woff2` files are served from the app (no external host), each `@font-face` names its weight, body text uses Bricolage Grotesque 500, titles 800 and code JetBrains Mono, and the OFL files sit next to the fonts (BR8).
- AC9 Given any page with `MainLayout`, when it renders in light and in dark, then the app bar is white (light) or `surface-raised` (dark), shows the decorative brand symbol, "Simulab" and "Control Alt Systems" from resource keys, and the home link keeps its accessible name; at 375px width nothing wraps or overflows (BR9).
- AC10 Given the app head, when a page loads, then the favicon link points at the Control Alt Systems app icon (BR10).
- AC11 Given a field of the kit, when it renders, then its leading icon and placeholder are those it had before (BR11).
- AC12 Given the verification, password, erasure and data export emails, when they are sent, then their button is `#064F80` with white text, their text is `#0B1B2E`, their font stack starts with Bricolage Grotesque and they open with "Simulab" and "Control Alt Systems" as text (BR12).
- AC13 Given `/dev/ui`, when it opens, then it shows the sections `gallery-buttons`, `gallery-type` and `gallery-palette` (UC3).
- AC14 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en) and the missing-key test is green.

## Criterion → test

Tests locked: 7c2e7fb0d3ddceed935da8300c0615f0539c3f31 (12 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `ApprovedPaletteTests` (every role, both themes; keycap edges; focus ring); `IdentityTokensTests` (tokens and `DESIGN.md` equal the theme) |
| AC2 | `KeycapContrastTests` (filled fills on page and surface, text at rest and on hover, outlined border, text link, focus rings, app bar brand, sunken surface); `ThemePaletteTests` |
| AC3 | `StylesheetLiteralColourTests` |
| AC4 | `KeycapButtonTests` |
| AC5 | `ActionColourScopeTests` |
| AC6 | `SurfaceBorderTests` |
| AC7 | `ShapeRadiusTests` |
| AC8 | `BrandFontTests`; `BrandAssetsHostTests.Get_Font_IsServedByTheAppAsWoff2` |
| AC9 | `AppBarBrandTests`; `KeycapContrastTests.AppBarBrand_ReadsAtAaOnTheBar`; validation step (375px) |
| AC10 | `BrandAssetsHostTests.Get_Home_LinksTheAppIconAsTheFaviconAndNoFontHost`, `Get_BrandFile_IsServedAsSvg` |
| AC11 | `AppFieldLeadingIconTests`, `AppLookupFieldTests`, `AppMultiPickFieldTests`, `AppChipsFieldTests` (icons and placeholders, unchanged and green; no file of `Components/Ui` changed) |
| AC12 | `EmailIdentityTests` |
| AC13 | `UiGalleryIdentityTests`; `UiGalleryTests.Render_Development_ShowsHeaderAndEverySection`; `DevPagesHostTests.Get_GalleryInDevelopment_RendersEverySection` |
| AC14 | `ResourceParityTests`; `AppBarBrandTests.Brand_TextsComeFromTheResources` |

## Approved list

### 1. Palette
Light / dark. Darken and lighten shades not listed are derived at build and pinned by `ThemePaletteTests`.
1. Background (page): `#F4F6F9` / `#0B1B2E`
2. Surface (cards, dialogs): `#FFFFFF` / `#12263A`
3. Surface sunken (disabled, wells): `#E9EDF2` / `#081522`
4. Text primary (ink): `#0B1B2E` / `#F4F6F9`
5. Text secondary and action default (ink-muted): `#52647A` / `#B8C4D2`
6. Lines and inputs (line-strong): `#6B7C91` / `#6A829A`
7. Divider and table lines (line): `#DDE3EA` / `#26405A`
8. Primary (filled button, selected item, focus): `#064F80`, text `#FFFFFF`, keycap edge `#043A5E` / `#2AA8F0`, text `#0B1B2E`, keycap edge `#0F6BA8`
9. Info and links: `#0872B8`, text `#FFFFFF` / `#2AA8F0`, text `#0B1B2E`
10. Secondary and success (green): `#00806B`, text `#FFFFFF`, edge `#00574A` / `#00A68C`, text `#0B1B2E`, edge `#006B5B`
11. Tertiary (action, orange): `#E8650A`, text `#0B1B2E`, edge `#A84606` / `#FF7A2E`, text `#0B1B2E`, edge `#B24A0C`
12. Warning: `#D98B1A`, text `#0B1B2E` / `#D98B1A`, text `#0B1B2E`
13. Error: `#BE3737`, text `#FFFFFF` / `#E06C6C`, text `#0B1B2E`
14. App bar: `#FFFFFF`, text `#0B1B2E`, subline `#52647A` / `#12263A`, text `#F4F6F9`, subline `#B8C4D2`
15. Sidebar (drawer): `#0F3266`, text and icon `rgba(255,255,255,0.92)` / `#081522`, text and icon `#B8C4D2`
16. Focus ring: `#0872B8` / `#2AA8F0`, 3px, 2px offset
17. Tint blue (informative highlights): `#DCEBF7` / `#16344F`

### 2. Type
1. Families: Bricolage Grotesque (500 text, 800 titles), fallback `system-ui, sans-serif`; JetBrains Mono (500, 700), fallback `ui-monospace, Consolas, monospace`
2. Scale (`ui-*`, px size / line height): title 28/32, heading 20/24, subheading 16/20, body 15/22, control 14/20, caption 12/16, label 12/16; mono code 14/22, grid 13/20, kbd 12/16
3. Titles keep a negative letter spacing; body and control text none

### 3. Buttons
1. Primary (filled, `Color.Primary`): primary fill, keycap edge below, white text (dark theme: ink text)
2. Secondary (outlined): surface fill, 2px inner border in line-strong, ink text
3. Text: no fill, info blue, underlined
4. Action (filled, `Color.Tertiary`): orange fill, ink text, orange keycap edge; one per screen, on surface only
5. Destructive (filled or outlined, `Color.Error`): error colours, same shapes
6. States: minimum height 44px (small 36px); hover 8% brighter; pressed moves down and the edge shrinks, at once with `prefers-reduced-motion`; focus ring of item 16 of the palette; disabled sunken fill, ink-muted text, no edge, real `disabled`

### 4. Shape
1. Radius: tags and chips 6px, buttons and fields 12px, cards and dialogs 20px, brand keys 22%
2. Shadow: only the keycap edge; every other separation is a 1px `line` border

## Decisions
- 2026-10-17 — Two themes (light and dark), both Control Alt Systems; the Simulae identity is not kept as a choice — owner chose "4 themes (current + new)" over the recommended "2 themes" (average 4.8 vs 7.6); switched — asked
- 2026-10-17 — Palette, sidebar navy and white app bar, field icons and placeholders kept, F-83 absorbed and F-70 kept apart, the orange action only in the kit and the gallery — the owner's D-4 decisions, still true — confirmed: D-4
- 2026-10-17 — Primary is the dark blue `#064F80` (the brand's `key-blue-shadow`, 8.61:1 under white); `#2AA8F0` in the dark theme — the owner did not like the ink button and asked for a darker blue; this one is already a brand token — asked
- 2026-10-17 — Error and warning keep Simulab's values — the design system has none, and an orange warning would confuse an alert with the action — asked
- 2026-10-17 — Dark sidebar `#081522` (surface-sunken) — the navy against the dark page reads at 1.38:1 and its edge disappears — asked
- 2026-10-17 — Radii of the tokens (6 / 12 / 20) — they are part of the identity — asked
- 2026-10-17 — The four button variants of the brand — one look for every button of a screen, changed in one place — asked
- 2026-10-17 — App bar with the key symbol, "Simulab" and "Control Alt Systems" under it — owner chose "Control Alt Systems logo" over the recommended "Simulab name only" (average 6.5 vs 7.3); kept; the owner then drew the two-line layout — asked
- 2026-10-17 — The orange key of the brand symbol does not count as the screen's orange action — the symbol is the brand mark and is never redrawn (README: do not change colours or order of the keys) — asked
- 2026-10-17 — Favicon is the Control Alt Systems app icon — consistent with the symbol in the app bar — asked
- 2026-10-17 — Emails are in scope — owner chose "in F-125" over the recommended "out, as an idea" (average 7.5 vs 7.0, the chosen option scores higher: the recommendation was the weaker one); kept — asked
- 2026-10-17 — Reference for the build: `## Approved list` and the gallery; the Stitch screen is not exported — the Stitch API lists only `DESIGN.md` in both projects ("Simulab - F-95 Questions (CAS identity)" and "Simulab"), so the file carries every value instead — asked
- 2026-10-17 — Partner brand per tenant captured as F-139 in the new epic Partner tenants — the owner wants a partner who rents Simulab to show its own name and logo; F-125 keeps the brand in resource keys and files that F-139 can make come from the tenant — asked
- 2026-10-17 — No new package; fonts are files copied from `_cas-design-system/design-system/fonts/` with their OFL licences — Claude
- 2026-10-17 — Buttons are styled by CSS on MudBlazor's classes (`mud-button-filled-primary`, `mud-button-outlined`, `mud-button-text`, `mud-button-filled-tertiary`) in the kit stylesheet, written from MudBlazor 9.9.0's own CSS — the kit has no button component and 73 `MudButton`s in 36 files must change at once (ui-project.md: CSS on a library class is written from the package's CSS) — Claude
- 2026-10-17 — `Elevation` values stay in the pages; the theme sets no shadows and the stylesheet draws the border — removing 40 attributes in 13 pages would touch screen items' files for no visible gain — Claude
- 2026-10-17 — Brand files (symbol light and dark, favicon) go to `wwwroot/brand/`; the dark symbol uses the dark key colours of `tokens.json` — Claude
- 2026-10-18: plan — 1 palette, hover tones and keycap edges in SimulabTheme (edges emitted by a SimulabThemeProvider), tokens and DESIGN.md → AC1, AC2; 2 kit stylesheet: keycap buttons, no literal colour, borders instead of shadows, radii → AC3, AC4, AC6, AC7; 3 fonts served from wwwroot/fonts with the OFL files and the type scale → AC8; 4 app bar brand, favicon and resource keys → AC9, AC10, AC14; 5 gallery sections buttons, type and palette, and the scan for the orange action → AC13, AC5; 6 emails through one EmailHtml page → AC12; 7 ADR-0006, ADR-0001 decision 30, field icons and placeholders kept → AC11, gate; 8 app-host screen check, scan, validation script → validation steps
- 2026-10-18 — Hover tones (the `*Darken` tokens) are 8% brighter (each channel x1.08) when the fill's contrast text keeps 4.5:1 there, else 8% darker: light green (`#00806B`, white text reads 4.30:1 on any brighter tone) and light info get the darker tone; neither is a filled button variant — Claude
- 2026-10-18 — The destructive keycap edge, which `## Approved list` does not give, is derived like the others: light `#892828` (error x0.72), dark `#924646` (error x0.65) — Claude
- 2026-10-18 — BR2 covers every stylesheet: the scoped `MainLayout.razor.css` and `ReconnectModal.razor.css` lose their literal colours too, and the scan of AC3 reads them — Claude
- 2026-10-18 — Brand file names are English (`keys-symbol.svg`, `keys-symbol-dark.svg`, `app-icon.svg`), copied from `simbolo-teclas.svg` and `icone-app.svg` — Claude
- 2026-10-18: tests unlocked (owner, "Destravar e corrigir": `KeycapButtonTests` and `UiGalleryIdentityTests` looked for `mud-button-size-small`, which MudBlazor 9.9.0 never renders — it names the size per variant; `BrandAssetsHostTests` required a literal `+` that Blazor writes as `&#x2B;`; `AppBarBrandTests` had an unused using and `BrandFontTests` an unresolved cref, two new build warnings). Only those lines changed; locked again
- 2026-10-18 — Reuse: MudBlazor's own classes for the four button variants and its `GenerateTheme` for the keycap edges (no new component); `AppCssColours`, `ColourContrast` and `IdentityTokensComparison` for every new contrast and token test; the design system's font, licence and logo files copied as they are; `EmailHtml` grew one `Page` for the four mailers instead of four copies — Claude
- 2026-10-18 — Screen check through the app host (database `simulab_f125`): found that JetBrains Mono widened two gallery labels past the page and that the company line lost 25px at 375px; both fixed in `app.css` (5078271) — Claude
- 2026-10-18 — Validated on screen by the owner ("validado F-125"); the merge waits for `/agile:ship F-125` — owner
- 2026-10-17 — ADR-0006 records the brand; ADR-0001 decision 30 points to it and its "Revisit when" line is closed — D-4 asked for an ADR — confirmed: D-4

## Out of scope
- Composition and new actions of each screen: F-126 to F-134 (and which action is orange on each).
- The actions of the Stitch screen captured as F-135 to F-138.
- A partner's own name and logo: F-139.
- Exam session screens (distraction-free exception): wait on the first exam session screen (D-4).
- The Control Alt Systems logo in emails as an image.

## Open questions
- (none)

## Change notes

### v2 — 2026-10-18
- What: the keycap edge is decorative; BR3 and AC2 measure 3:1 for the keycap top (the fill of a filled button) against the page and the surface, in both themes, instead of the edge. No palette value changes.
- Why: found at the build — the dark edges of `## Approved list` read 2.70:1 (primary `#0F6BA8`), 2.39:1 (green `#006B5B`) and 2.84:1 (orange `#B24A0C`) on the dark surface `#12263A`; the fill is what identifies the control (WCAG 2.2 1.4.11) and reads 5.01 to 6.67:1 in dark, 3.08:1 or more in light. The owner compared both options drawn on screen and chose to keep the approved edges over lightening them to `#1373B1`, `#007D6A`, `#B84E0F`.
- Affected: BR3, AC2.
- Re-approved: 2026-10-18 (owner, "aprovo").

## Validation script
### English
Needed to validate: you, on screen, in both themes, at desktop and phone width, and Docker Desktop running for the app host. Both are in place now (the app host started on 2026-10-18 for the screen check).

1. Stop any app host you have open (a running one locks `bin/`), then start this item's app host from the worktree folder. It runs on its own database, `simulab_f125`, on the local PostgreSQL server; nothing lands in the shared `simulab` database. Wait until the Aspire dashboard address is printed, then open https://localhost:7125.

```powershell
cd D:\wt\simulab\f-125-control-alt-identity
$env:Database__Name = "simulab_f125"; dotnet run --project src/Hosts/Simulab.AppHost
```

```bash
cd /d/wt/simulab/f-125-control-alt-identity
Database__Name=simulab_f125 dotnet run --project src/Hosts/Simulab.AppHost
```

   Expected: a line `Dashboard: https://localhost:<port>/login?t=<token>`; https://localhost:7125 answers with the home page.
2. The app bar (the white bar at the top of every page) in the light theme:
   - [ ] 2.1 From the left you see the menu button, the brand symbol (three keys: green Ctrl, blue Alt, orange restart), "Simulab" in bold and "Control Alt Systems" under it in grey, then the theme switch (the moon or sun at the top right), the language switch and Sign in.
   - [ ] 2.2 The bar is white with a thin line under it and no shadow; the sidebar (left menu) stays navy; the browser tab shows the Control Alt Systems app icon (the orange key on navy).
3. The UI gallery: in the sidebar, under Development, click UI kit; the page "UI kit gallery" opens.
   - [ ] 3.1 Scroll to "Buttons": Save is a dark blue key with a darker edge under it, Filter has a grey 2px border, Cancel is an underlined blue link, Start exam is the one orange key, and Delete appears red, filled and outlined.
   - [ ] 3.2 Point at Save (it turns a little brighter), hold the mouse button down on it (it sinks onto its edge), and check the disabled row: grey fill, grey text, no edge.
   - [ ] 3.3 "Type" shows the scale from ui-title 28/32 to ui-label 12/16 in Bricolage Grotesque, and code, a grid of numbers and Ctrl + K in JetBrains Mono.
   - [ ] 3.4 "Palette" lists each role with its value: primary `#064F80`, info `#0872B8`, secondary `#00806B`, tertiary `#E8650A`, background `#F4F6F9`.
   - [ ] 3.5 Cards have round 20px corners and a thin line, and no card, menu or dialog has a soft shadow.
4. The dark theme: click the theme switch.
   - [ ] 4.1 The page turns deep navy, cards a lighter navy, the app bar takes the card colour with light text, the sidebar turns almost black (`#081522`), and the brand symbol shows its dark keys.
   - [ ] 4.2 In the gallery, Save is a light blue key with dark text, and "Palette" now shows primary `#2AA8F0` and background `#0B1B2E`.
   - [ ] 4.3 Every text is easy to read on its background; switch back to light with the theme switch.
5. [ ] Phone width: open the browser developer tools (F12), turn on the device toolbar (Ctrl+Shift+M) and choose 375 px wide. The app bar keeps the symbol, "Simulab" and "Control Alt Systems" on one line each, nothing wraps or runs off the right edge, and the page has no sideways scroll.
6. [ ] Language: in the language switch (the globe at the top right) choose Português (Brasil). The home link is still named "Simulab, página inicial", the company line still reads "Control Alt Systems", and in the gallery (sidebar > Desenvolvimento > Kit de interface) the new sections read "Botões", "Tipografia" and "Paleta". Switch back to English.
7. Keyboard only, on the UI gallery, without the mouse:
   - [ ] 7.1 Press Tab from the top of the page: each control you reach shows a blue focus ring, 3 px wide, 2 px away from it; in the sidebar the ring is light instead of blue.
   - [ ] 7.2 Tab to the theme switch and press Space: the theme changes; press Space again to change it back.
   - [ ] 7.3 Tab to Save in "Buttons" and hold Space: the key sinks onto its edge.
8. The emails: open https://localhost:7125/sign-up and create an account with an address of your choice (any, Mailpit catches it); then open Mailpit from the Aspire dashboard (the "mailpit" resource, its web link).
   - [ ] 8.1 The verification email opens with "Simulab" and "Control Alt Systems" as text, then its heading; its button is dark blue (`#064F80`) with white text.
   - [ ] 8.2 At https://localhost:7125/forgot-password ask a reset for the same address: the reset email has the same top lines and the same button.

### pt-BR
Necessário para validar: você, na tela, nos dois temas, em largura de computador e de celular, e o Docker Desktop rodando para o app host. Os dois estão disponíveis agora (o app host subiu em 2026-10-18 para a conferência na tela).

1. Pare qualquer app host que esteja aberto (um app host rodando trava a pasta `bin/`) e suba o app host deste item a partir da pasta do worktree. Ele roda no seu próprio banco, `simulab_f125`, no PostgreSQL local; nada cai no banco compartilhado `simulab`. Espere o endereço do painel do Aspire aparecer e abra https://localhost:7125.

```powershell
cd D:\wt\simulab\f-125-control-alt-identity
$env:Database__Name = "simulab_f125"; dotnet run --project src/Hosts/Simulab.AppHost
```

```bash
cd /d/wt/simulab/f-125-control-alt-identity
Database__Name=simulab_f125 dotnet run --project src/Hosts/Simulab.AppHost
```

   Esperado: uma linha `Dashboard: https://localhost:<porta>/login?t=<token>`; https://localhost:7125 responde com a página inicial.
2. A barra superior (a faixa branca no alto de todas as páginas) no tema claro:
   - [ ] 2.1 Da esquerda para a direita aparecem o botão de menu, o símbolo da marca (três teclas: Ctrl verde, Alt azul, reiniciar laranja), "Simulab" em negrito e "Control Alt Systems" embaixo em cinza; depois o botão de tema (a lua ou o sol no canto superior direito), o seletor de idioma e Entrar.
   - [ ] 2.2 A barra é branca, com uma linha fina embaixo e sem sombra; o menu lateral esquerdo continua azul-marinho; a aba do navegador mostra o ícone da Control Alt Systems (a tecla laranja sobre azul-marinho).
3. A galeria do kit: no menu lateral, em Desenvolvimento, clique em Kit de interface; abre a página "Galeria do kit de interface".
   - [ ] 3.1 Role até "Botões": Salvar é uma tecla azul-escura com uma borda mais escura embaixo, Filtrar tem uma borda cinza de 2px, Cancelar é um link azul sublinhado, Iniciar simulado é a única tecla laranja, e Excluir aparece em vermelho, cheio e com contorno.
   - [ ] 3.2 Passe o mouse sobre Salvar (ele fica um pouco mais claro), segure o botão do mouse sobre ele (a tecla desce sobre a borda) e confira a linha de desativados: fundo cinza, texto cinza, sem borda.
   - [ ] 3.3 "Tipografia" mostra a escala de ui-title 28/32 a ui-label 12/16 em Bricolage Grotesque, e código, uma grade de números e Ctrl + K em JetBrains Mono.
   - [ ] 3.4 "Paleta" lista cada papel com seu valor: primary `#064F80`, info `#0872B8`, secondary `#00806B`, tertiary `#E8650A`, background `#F4F6F9`.
   - [ ] 3.5 Os cartões têm cantos arredondados de 20px e uma linha fina, e nenhum cartão, menu ou diálogo tem sombra esfumaçada.
4. O tema escuro: clique no botão de tema.
   - [ ] 4.1 A página fica azul-marinho profundo, os cartões num azul-marinho mais claro, a barra superior na cor dos cartões com texto claro, o menu lateral quase preto (`#081522`), e o símbolo da marca mostra as teclas escuras.
   - [ ] 4.2 Na galeria, Salvar é uma tecla azul-clara com texto escuro, e "Paleta" agora mostra primary `#2AA8F0` e background `#0B1B2E`.
   - [ ] 4.3 Todo texto fica fácil de ler sobre o seu fundo; volte ao claro pelo botão de tema.
5. [ ] Largura de celular: abra as ferramentas do navegador (F12), ligue a barra de dispositivo (Ctrl+Shift+M) e escolha 375 px de largura. A barra superior mantém o símbolo, "Simulab" e "Control Alt Systems" numa linha cada, nada quebra nem passa da borda direita, e a página não rola para o lado.
6. [ ] Idioma: no seletor de idioma (o globo no canto superior direito) escolha Português (Brasil). O link da página inicial continua com o nome "Simulab, página inicial", a linha da empresa continua "Control Alt Systems", e na galeria (menu lateral > Desenvolvimento > Kit de interface) as seções novas se chamam "Botões", "Tipografia" e "Paleta". Volte para English.
7. Só teclado, na galeria do kit, sem o mouse:
   - [ ] 7.1 Aperte Tab desde o alto da página: cada controle alcançado mostra um anel de foco azul, de 3 px, a 2 px dele; no menu lateral o anel é claro em vez de azul.
   - [ ] 7.2 Vá com Tab até o botão de tema e aperte Espaço: o tema muda; aperte Espaço de novo para voltar.
   - [ ] 7.3 Vá com Tab até Salvar em "Botões" e segure Espaço: a tecla desce sobre a borda.
8. Os e-mails: abra https://localhost:7125/sign-up e crie uma conta com um endereço qualquer (o Mailpit captura); depois abra o Mailpit pelo painel do Aspire (o recurso "mailpit", o link web dele).
   - [ ] 8.1 O e-mail de verificação começa com "Simulab" e "Control Alt Systems" como texto, depois o título; o botão é azul-escuro (`#064F80`) com texto branco.
   - [ ] 8.2 Em https://localhost:7125/forgot-password peça a redefinição para o mesmo endereço: o e-mail de redefinição tem as mesmas linhas no topo e o mesmo botão.

## Events

- 2026-10-17 question — round 1: asked 10, from context 4
- 2026-10-17 question — follow-up: asked 4, from context 0
- 2026-10-17 override — themes: 4 chosen over 2 recommended (4.8 vs 7.6); switched
- 2026-10-17 override — app bar: CAS logo chosen over Simulab name (6.5 vs 7.3); kept
- 2026-10-17 override — emails: in chosen over out (7.5 vs 7.0); kept
- 2026-10-17 blocked — impossible criterion: dark keycap edges read 2.39-2.84:1 on the surface, BR3/AC2 ask 3:1
- 2026-10-17 question — asked 1, from context 0
- 2026-10-18 change — v2: keycap edge decorative; BR3 and AC2 measure the button fill on page and surface
- 2026-10-18 blocked — test wrong: small-button class, favicon type encoding, unused using and cref in 5 locked files
- 2026-10-18 question — asked 1, from context 0
- 2026-10-18 blocked — test wrong: owner 2026-10-18: small-button class MudBlazor never renders, favicon type encoding, unused using and unresolved cref

## Delivery
