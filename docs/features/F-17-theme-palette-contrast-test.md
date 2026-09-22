---
feature: F-17
epic: Foundation and identity
status: approved
board: 730
version: 1
---
# Theme palette contrast test

## Summary
The `ui` rule that arrived with agile@canary 0.0.35 asks for a test over the theme tokens that checks the contrast ratio of every foreground/background pair, in both themes: a screen check only sees the pairs that screen happens to render.
`ThemeContrastTests`, written for F-10, covers the pairs the current screens use. Extend it to the whole palette, so a token that no screen has rendered yet (as the dark error red was until F-10) fails in the build instead of in a later feature.

## Goal
Every colour of `SimulabTheme` has a declared role, and the build proves each role's contrast in both themes. A new or changed token that fails WCAG 2.2 AA stops the build instead of reaching a screen.

## What already exists (verified 2026-09-22)
- `SimulabTheme.Create()` (`src/Hosts/Simulab.Web/Theme/SimulabTheme.cs`) overrides part of MudBlazor 9.9.0's `PaletteLight` / `PaletteDark`; the rest are MudBlazor defaults.
- `ThemeContrastTests` (`tests/Hosts/Simulab.Web.Tests/Ui/ThemeContrastTests.cs`) asserts, in both themes: `Error` and `Primary` as text on `Surface` and `Background` (F-10, B-8); `ErrorContrastText` on `Error`; `PrimaryContrastText` on `Primary` and `PrimaryDarken`; `Info`/`Success`/`Warning` contrast text on their colour (B-9); `ActionDefault` on both surfaces at 3:1 (B-10). Its colour parser ignores alpha.
- Measured today over every pair the roles below define (MudBlazor defaults included, alpha composited over the background): **27 pairs fail**.
  - Text on `Surface` / `Background`, light: `Secondary` 3.33 / 3.05, `Tertiary` 2.11 / 1.94, `Success` 3.33 / 3.05, `Warning` 2.74 / 2.51, `Info` on background 4.21. Dark: `Info` 3.53 / 3.87.
  - Fill text at rest, both themes: `SecondaryContrastText` on `Secondary` 3.33, `TertiaryContrastText` on `Tertiary` 2.11.
  - Fill text on the hover tone, both themes: `Tertiary` 2.67, `Success` 3.00, `Warning` 4.02; dark `ErrorContrastText` on `ErrorDarken` 3.80 (the filled destructive button of F-10).
  - Non-text at 3:1: `LinesInputs` (the border of every `AppTextField`, `Variant.Outlined`) light 1.88 / 1.72, dark 2.67 / 2.67; light `DrawerIcon` 2.03 (not painted: `app.css` makes nav icons inherit the drawer text).
- No screen uses `Secondary`, `Tertiary`, `Success`, `Warning` or `Info` as a text colour today: they appear as icons next to text (`VerifyEmail`, `ResetPassword`), the password strength bar (with its word), the status dot (`.app-status-*`, with its word) and filled alerts.

## Users and use cases
- UC1 A developer changes or adds a theme token; the build fails with the pair and its ratio when the token breaks AA for its role.
- UC2 A developer upgrades MudBlazor and the palette gains a token; the build fails until the token gets a role.
- UC3 A student with low vision sees the border of every text field and reads a filled button while the pointer is over it, in both themes.

## Business rules
- BR1 Every colour token of `PaletteLight` and `PaletteDark` has exactly one declared role in the test: **text**, **fill** (the colour with its contrast text), **non-text** (an icon or a control boundary) or **exempt** (with a one-line reason). A token without a role fails.
- BR2 A **text** token reads at 4.5:1 or more on `Surface` and on `Background`, in both themes.
- BR3 A **fill** token's contrast text reads at 4.5:1 or more on the colour and on its hover tone (`<Colour>Darken`), in both themes.
- BR4 A **non-text** token reads at 3:1 or more on the surfaces it sits on, in both themes (WCAG 2.2 1.4.11).
- BR5 `Secondary`, `Tertiary`, `Success`, `Warning` and `Info` are fill tokens only: no screen or stylesheet uses them as a text colour. A text use needs its own text token first.
- BR6 A failure is fixed in the palette (`SimulabTheme`), never on the screen that shows it, and the whole family is fixed together (project rule, B-8, B-9).
- BR7 Alpha is composited over the background before the ratio is computed (project rule, B-9).
- BR8 Every pair asserted today by `ThemeContrastTests` stays asserted.

## Screens and API
- No new screen, route, endpoint or error code.
- Visible change, both themes: text field borders are darker; filled buttons keep readable text on hover; secondary and tertiary fills get dark ink, like success and warning (B-9). No screen uses those two fills today.

## Acceptance criteria
- AC1 Given the palettes of `SimulabTheme`, when the test enumerates every colour property of `PaletteLight` and `PaletteDark`, then each has one declared role, and a property without a role fails naming it. (BR1)
- AC2 Given every text token, when measured on `Surface` and `Background` in both themes, then each ratio is at least 4.5. (BR2)
- AC3 Given every fill token, when its contrast text is measured on the colour and on its hover tone in both themes, then each ratio is at least 4.5. (BR3)
- AC4 Given every non-text token, including `LinesInputs`, `ActionDefault` and `DrawerIcon`, when measured on the surfaces it sits on in both themes, then each ratio is at least 3.0. (BR4)
- AC5 Given the Web project's `.razor` and `.css` files, when an architecture test scans them, then no text component or `color:` declaration uses `Secondary`, `Tertiary`, `Success`, `Warning` or `Info`. (BR5)
- AC6 Given a colour with alpha, when its ratio is computed, then it is composited over the background first. (BR7)
- AC7 Given the pairs asserted before F-17, when the new test runs, then each is still asserted with the same minimum. (BR8)
- AC8 Given the sign-in page, in light and in dark, when the owner looks at a text field and hovers a filled button, then the border is visible and the button text readable (validation script; ratio measured on screen by Claude). (UC3, BR6)
- AC9 No new UI text; the missing-key test stays green in pt-BR, pt-PT and en.

## Decisions
- 2026-09-22 — Approved by the owner ("aprovo F-17").
- 2026-09-22 — Every token gets a declared role; an unclassified token fails — owner (Q1): a MudBlazor upgrade that adds a token cannot slip past the test.
- 2026-09-22 — `Secondary`, `Tertiary`, `Success`, `Warning` and `Info` are forbidden as text instead of darkened — owner (Q2): no screen uses them as text, and darkening would visibly change the teal, amber and blue.
- 2026-09-22 — `LinesInputs` is darkened to 3:1 in both themes — owner (Q3): WCAG 1.4.11 asks for a visible field boundary.
- 2026-09-22 — Every other failure (hover tones, secondary and tertiary contrast text) is fixed in the palette in this item — owner (Q4): project rule, the family is fixed together.
- 2026-09-22 — The forbidden text use is enforced by an architecture test over `.razor` (`MudText`, `MudLink`, text and outlined `MudButton` with those `Color` values) and `.css` (`color: var(--mud-palette-<name>)`) — Claude: a palette test alone cannot see how a screen uses a colour.
- 2026-09-22 — `DrawerIcon` is set equal to `DrawerText` in both themes — Claude: `app.css` already paints nav icons in the drawer text colour; the token then matches what is shown and passes as non-text.
- 2026-09-22 — Disabled states (`TextDisabled`, `ActionDisabled`, `ActionDisabledBackground`), dividers, table lines, skeleton, overlays, surfaces and the `*Lighten` tones are exempt, each with its reason in the test — Claude: WCAG 2.2 exempts disabled controls and decorative lines.
- 2026-09-22 — The contrast helper moves to one place in the test project and parses alpha — Claude: BR7; the architecture test and the palette test share it.
- 2026-09-22 — No new package — Claude: xUnit, AwesomeAssertions and the existing architecture tests cover it.

## Out of scope
- A new brand palette (ADR-0001 decision 30 revisits it when Simulab has its own brand).
- Large-text minimum (3:1): every pair is held to the normal-text minimum.
- Chart and data-visualization colours: no chart exists yet.

## Open questions
- (none)

## Change notes
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
