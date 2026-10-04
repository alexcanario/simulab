---
feature: F-63
epic: Foundation and identity
status: refining
board: 102
version: 1
---
# The app's visual identity is recorded

Technical terms: [glossary](../glossary.md)

## Summary
The app has no `docs/design/identity.tokens.json` (DTCG) or `docs/design/DESIGN.md`, the source agile@canary (since 0.0.86) uses for every screen's and mockup's colors, fonts and radius. The identity itself already exists in code: `SimulabTheme` (`src/Hosts/Simulab.Web/Theme/SimulabTheme.cs`) holds both palettes, the font stack, the type scale and the radius, tuned to WCAG 2.2 AA by F-10, F-17, B-8, B-9 and B-10. This item records that identity as tokens, unchanged, and adds a test that keeps theme and tokens equal. Raised by `/agile:sync` to 0.0.102; captured on 2026-10-02; F-70 (#112, the same request from the 0.1.0 sync) is folded into this item.

## Start
- Depends on: nothing.
- Waits on (to start): nothing (the source is the current theme, decided 2026-10-04).
- Needed to validate: the owner reads `docs/design/DESIGN.md` and the token table — owner.
- Suggested path: `/agile:build F-63`; the build runs the `visual-identity` skill to write the two files, then adds the test.
- Parallel with: any item that does not touch `SimulabTheme.cs`, `docs/decisions/ADR-0001-foundation.md` or `docs/design/`. F-65 also edits `docs/glossary.md` (small conflict expected). The F-70 worktree (`D:\wt\simulab\f-70-visual-identity`) holds the same scope and must not be built.

## Goal
Give screens, mockups and agents one written source of the app's colors, fonts and radius, without changing what the app looks like today.

## Users and use cases
- UC1 The `ux-designer` agent (screen design) reads the tokens and draws a mockup in the app's real colors, light and dark, with no color the tokens lack.
- UC2 A developer who changes a color, the type scale or the radius in `SimulabTheme` without updating the tokens (or the other way round) gets a failing test that names the token and both values.
- UC3 The owner reads `DESIGN.md` to see each color's role, where it is used, and the do and don't of the identity.

## Business rules
- BR1 The tokens hold the current `SimulabTheme` values exactly, with no visual change: every palette color `SimulabTheme` sets in either theme (a value different from MudBlazor's default palette for that theme), each with its light and its dark value as the theme really resolves them; the font stack; the type scale `SimulabTheme` sets (H1, H2, Body1, Body2, Caption: size, weight, line height where set); the default border radius; the icon family Material Outlined.
- BR2 `SimulabTheme.cs` is not changed by this item. The existing `ThemeContrastTests` and `ThemePaletteTests` stay green without edits, so the AA contrast already measured carries over.
- BR3 The source is recorded as `file`, with `sourceRef` `src/Hosts/Simulab.Web/Theme/SimulabTheme.cs` and the date of the build.
- BR4 `DESIGN.md`'s YAML front matter is generated from the tokens and gives the same value for every token; its prose explains the role of each color, where it is used, and do and don't.
- BR5 Theme and tokens are compared by a test in both directions: a color the theme sets with no token fails, a token the theme does not set fails, and a value that differs fails, naming the token, the theme (light or dark) and both values.
- BR6 ADR-0001 decision 30 is updated: the design system's values live in `docs/design/identity.tokens.json` (source: `SimulabTheme`, F-63), primary `#216DB5` (the `#2478C5` written there is stale since B-8); "revisit when Simulab has its own brand" stays.
- BR7 The item writes only `docs/design/`, the ADR-0001 line, the glossary and the test. No application code and no new package.

## Screens and API
- No screen, no endpoint, no error code.
- Files: `docs/design/identity.tokens.json` (DTCG 2025.10), `docs/design/DESIGN.md`.
- Test: `tests/Hosts/Simulab.Web.Tests/Ui/IdentityTokensTests.cs`.

## Acceptance criteria
- AC1 (BR1, BR5) Given the tokens file, when the test runs, then every color token's light `hex` equals `SimulabTheme`'s light palette value and its `$extensions.mode.dark` `hex` equals the dark palette value.
- AC2 (BR1, BR5) Given `SimulabTheme`, when the test runs, then every palette color that differs from MudBlazor's default in either theme has a token, and every token names a palette color; a theme altered in the test with one extra color or one changed value makes the comparison fail and name that token.
- AC3 (BR1) Given the tokens file, when the test runs, then the font stack, each type-scale entry `SimulabTheme` sets and the default border radius equal the theme's values.
- AC4 (BR1) Given the tokens file, when the test runs, then `$extensions.agile.iconFamily` is Material Outlined, and `source` is `file` with `sourceRef` `src/Hosts/Simulab.Web/Theme/SimulabTheme.cs` (BR3).
- AC5 (DTCG shape) Given the tokens file, when the test runs, then it has a `$schema`, and every color token has `$type` `color` and a `$value` with `colorSpace`, `components` and `hex`, and a dark value under `$extensions.mode.dark`.
- AC6 (BR4) Given `DESIGN.md`, when the test runs, then every value in its YAML front matter equals the token of the same name, and no token is missing from it.
- AC7 (BR2) Given the branch, when the full suite runs, then `ThemeContrastTests` and `ThemePaletteTests` are green and `git diff main -- src/Hosts/Simulab.Web/Theme/SimulabTheme.cs` is empty (checked in the validation script).
- AC8 (BR6) Given ADR-0001, when the owner reads decision 30, then it points to `docs/design/identity.tokens.json` and says primary `#216DB5` (checked in the validation script).
- AC9 (localization) This item adds no UI text; the missing-key test stays green.

## Decisions
- 2026-10-04 — Source: the current `SimulabTheme`, copied as it is (owner). — Nothing changes on screen and the AA work of F-10, F-17, B-8, B-9 and B-10 is kept; a real brand later is a new item.
- 2026-10-04 — Scope: the two files plus a test that compares theme and tokens (owner). — Without the test, the first color fix on one side leaves the other behind.
- 2026-10-04 — Coverage: every color the theme sets, not only the skill's eight roles (owner). — The test covers the whole palette and a mockup has every color the app paints (drawer, app bar, field borders, contrast inks).
- 2026-10-04 — ADR-0001 decision 30 updated; the literal colors of `app.css` become a separate item, F-83 (#126) (owner). — The ADR is stale since B-8; the stylesheet is application code, outside this item.
- 2026-10-04 — F-70 (#112) is the same request; it had reached `refining` in another session (worktree `D:\wt\simulab\f-70-visual-identity`, commit `33ec94f`). The owner chose to keep F-63. #112 was closed as a duplicate; the F-70 worktree is left untouched until the owner stops that session and asks for its removal. — F-63 is older and already had the owner's answers.
- 2026-10-04 — Technical: "a color the theme sets" means a palette property whose value differs from a fresh `PaletteLight` / `PaletteDark` in at least one theme; for such a color, both themes' resolved values are recorded, so a value left at MudBlazor's default in one theme is still written down. A color set to exactly MudBlazor's default in both themes is indistinguishable from an unset one and is not recorded. — Reflection over the palette is what `ThemePaletteTests` already does; it needs no list typed by hand.
- 2026-10-04 — Technical: token names are the MudBlazor palette property names in camelCase (`primary`, `drawerBackground`, `linesInputs`). — One name on both sides keeps the test a plain comparison; `DESIGN.md` explains each role in words.
- 2026-10-04 — Technical: translucent colors (`rgba(255,255,255,0.92)`) are written with an `alpha` in the DTCG color object and an 8-digit `hex`. — DTCG 2025.10 supports alpha; the test compares the resolved RGBA.
- 2026-10-04 — Technical: the comparison logic is a helper that takes a `MudTheme` and the parsed tokens, so AC2 can feed it an altered theme. — Proves the test fails when it should, not only that it passes today.
- 2026-10-04 — Technical: the JSON is read with `System.Text.Json` and the YAML front matter with a small line parser in the test (flat `key: value` lines). — No new package (BR7).
- 2026-10-04 — Technical: the `visual-identity` skill's own step 12 ("capture the idea 'Apply the identity to the theme'") is satisfied by this item's test and is not captured again. — The theme already equals the tokens, and the test keeps it so.

## Out of scope
- A new brand (logo, new colors, a web font): a later item when Simulab has one.
- Generating `SimulabTheme` from the tokens at run time.
- The literal colors in `app.css`: F-83 (#126).
- Changing any screen.

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
