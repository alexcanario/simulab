---
bug: B-8
feature: F-1
status: done
board: 734
severity: medium
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# Outlined primary buttons in the kit may fail AA contrast

Technical terms: [glossary](../glossary.md)

## What happens
Found during F-11 (AB#715), not yet verified on the kit components themselves.
1. `AppErrorState` ("Try again") and `AppEmptyState` (its action) use `MudButton Variant.Outlined Color.Primary`.
2. On `/account/security` (F-11) the same outlined primary button measured 3.53:1 on the dark card (4.60:1 light), below the AA 4.5:1 for text; F-11 switched its own button to the default colour.
3. The two kit components sit on the page background, not on a card, so their ratio there is not measured.

## Expected
Every kit button meets AA (4.5:1) in both themes, on every surface it sits on, and `ThemeContrastTests` asserts it.

## Cause
Confirmed in code on 2026-09-21. The premise holds and is wider than the two kit components.

The palette's primary colour is not AA as a text colour. `Primary = "#2478C5"` in both palettes
(`src/Hosts/Simulab.Web/Theme/SimulabTheme.cs:12` and `:41`); an outlined or text button, a link or a CSS
rule that paints text in it reads at:

| Theme | On surface (card) | On background (page) |
|---|---|---|
| Light | 4.60:1 (passes) | 4.21:1 (fails) |
| Dark | 3.53:1 (fails) | 3.87:1 (fails) |

White on the filled primary button is 4.60:1 (passes), so filled buttons are not affected.
`ThemeContrastTests` checks only the error colour; nothing asserts the primary.

Occurrences of primary as a text colour (each fails in at least one theme):
- `src/Hosts/Simulab.Web/Components/Ui/AppErrorState.razor:6`: outlined primary "Try again". Used on the page
  background by `Account.razor:26`, `Security.razor:29` and `LegalDocumentPage.razor:19` (4.21 light, 3.87 dark).
- `src/Hosts/Simulab.Web/Components/Ui/AppEmptyState.razor:8`: outlined primary action. No page uses it yet
  outside the gallery.
- `src/Hosts/Simulab.Web/Components/Pages/Identity/CheckEmail.razor:50-51`: outlined primary "Resend" on the
  auth card (3.53 dark).
- `src/Hosts/Simulab.Web/wwwroot/app.css:163`: `.app-skip-link` text in primary on the surface (3.53 dark).
- `src/Hosts/Simulab.Web/wwwroot/app.css:260`: `.app-auth-name`, the app name link in the auth bar on the page
  background (4.21 light, 3.87 dark).
- `src/Hosts/Simulab.Web/Components/Pages/Dev/UiGallery.razor:138`: outlined primary "Show snackbar" (dev page).

Duplicates (the same rule already fixed site by site, not in the palette): F-7 and B-6 moved auth links to
`text-primary` (`app.css:283-303`), F-9 did the same for chips (`app.css:448-457`), and F-11 switched its
own outlined button on `/account/security` to the default colour. No other reimplementation found.

## Fix
Fix the colour in the palette, once, instead of a fifth site-by-site patch (D1).

- `PaletteLight.Primary` `#2478C5` → `#216DB5`: 4.91:1 on the background, 5.36:1 on the surface, white on it 5.36:1.
  `PrimaryDarken` `#1858A0` stays (white on it 7.14:1).
- `PaletteDark.Primary` `#2478C5` → `#64A2E3`: 6.04:1 on the surface, 6.63:1 on the background.
  White on it would be 2.69:1, so the dark palette gets `PrimaryContrastText = "#19243E"` (5.74:1), as F-10 did
  for the error colour.
- `PaletteDark.PrimaryDarken` `#1858A0` → `#5096DC`: the hover of a filled button keeps the dark ink at 4.94:1
  (the old dark blue under dark ink would be unreadable).
- A comment on each changed colour, as F-10 left on the error colour, points to `ThemeContrastTests`.
- The six occurrences in `## Cause` need no markup change: they read the palette. `UiGallery.razor:138` is
  covered by the same change.
- The earlier site-by-site fixes (F-7, B-6, F-9, F-11) stay as they are (D3).

## Regression test
`tests/Hosts/Simulab.Web.Tests/Ui/ThemeContrastTests.cs`, seen failing before the palette change:
- `PrimaryText_ReadsAtAaOnItsSurface` (theory: light/dark × surface/background). Before: 3 of 4 cases fail
  (4.21, 3.53, 3.87).
- `PrimaryContrastText_ReadsAtAaOnThePrimaryColour` (theory: light/dark, on `Primary` and on `PrimaryDarken`).
  Before: the dark case with the new primary fails if the contrast text is left white; it guards D1's ink.

## Acceptance criteria
| # | Criterion | Test |
|---|---|---|
| AC1 | Given either theme, when text is painted in the primary colour on the surface or the page background, then it reads at 4.5:1 or more. | `ThemeContrastTests.PrimaryText_ReadsAtAaOnItsSurface` |
| AC2 | Given either theme, when a filled primary button is shown (rest and hover), then its text reads at 4.5:1 or more. | `ThemeContrastTests.PrimaryContrastText_ReadsAtAaOnThePrimaryColour` |
| AC3 | Given the dark theme, when `/account/security` fails to load, then "Try again" is readable blue on the page background, and the filled buttons on the sign-in pages show dark text on the lighter blue. | Validation script (on screen) |
| AC4 | Localization: no UI text is added or changed; the missing-key test stays green. | `ResourceParityTests` (existing) |

## Decisions
- D1 (owner, 2026-09-21): fix in the palette, not per site. Reason: the same contrast gap was already patched
  site by site four times (F-7, B-6, F-9, F-11); the palette fix covers every future use and the test holds it.
  Trade-off accepted: filled buttons in the dark theme become light blue with dark text.
- D2 (owner, 2026-09-21): all six occurrences in `## Cause` are in scope. Reason: the palette change fixes them
  together.
- D3 (owner, 2026-09-21): links and chips already moved to `text-primary` (F-7, B-6, F-9) stay as they are.
  Reason: the underlined link in the text colour is B-6's choice and already passes AA.
- D4 (Claude, 2026-09-21): the new shades were chosen by measured ratio (numbers in `## Fix`), keeping the hue
  of the current blue. Reason: a technical choice without product impact beyond D1.

## Out of scope
- A brand palette for Simulab (ADR-0001, decision 30 still applies).
- Reverting the earlier `text-primary` links and chips (D3).
- Non-text contrast of other colours (secondary, warning, success); only primary is measured here.

## Open questions
None.

## Screen check (Claude, 2026-09-21)
Opened through the app host on `/dev/ui`, both themes, colours read from the rendered page and measured:

| Element | Light | Dark |
|---|---|---|
| Outlined primary on the card (`app-retry`, `app-empty-action`) | 5.36 | 6.04 |
| Outlined primary on the page ("Show snackbar") | 4.91 | 6.63 |
| Outlined primary on hover (6% primary tint), card / page | 4.95 / 4.54 | 5.51 / 6.09 |
| Filled primary (`app-primary-action`, `app-form-save`) at rest | 5.36 (white) | 5.74 (dark ink) |
| Filled primary on hover (`PrimaryDarken`) | 7.14 | 4.94 |
| Skip link on the surface | 5.36 | 6.04 |

The auth pages (`.app-auth-name`, "Resend" on `/check-email`) were not opened: the pane had a signed-in
session and signing it out was not done. They read the same palette variables measured above.

## Validation script
1. Start the app from the repository root (same command in Git Bash and PowerShell 7):
   `dotnet run --project src/Hosts/Simulab.AppHost`, then open `https://localhost:7125`.
2. Signed out, on `/sign-in` in the light theme: the "Entrar" button is blue with white text and the app name
   "Simulab" in the top bar is readable blue.
3. Switch to the dark theme (moon icon): "Entrar" is now light blue with dark text; hover it, the text stays readable.
4. Sign in with your own account, open `/dev/ui` in the dark theme: "Tentar novamente" and "Adicionar" in the
   states section, and "Mostrar notificação" below, read as light blue on the card and on the page.
5. Switch to the light theme: the same three buttons read as blue, "Mostrar notificação" on the grey page background.
6. Keyboard only: reload, press Tab once: the "Pular para o conteúdo" link appears readable; activate it and
   Tab on to "Tentar novamente".
7. Switch the language to English: the gallery shows English texts, nothing shows a raw key.
8. No permission changed in this bug; nothing to check there.

Validated by the owner on screen, 2026-09-21.

## Delivery
- Branch: `bug/B-8`, merged into `main` with `--no-ff` (AB#734).
- Tests: full suite 618 passed, 0 failed, 30 s; build 16 s, 0 warnings (`gate.js ship`, 2026-09-21).
  New: `ThemeContrastTests.PrimaryText_ReadsAtAaOnItsSurface` (4 cases),
  `ThemeContrastTests.PrimaryContrastText_ReadsAtAaOnThePrimaryColour` (4 cases); seen failing 3 of 14 before the fix.
- App manual: unchanged; it does not describe colours, and no text, step or rule changed.
- Follow-up: B-9 (AB#735), the info colour and the email buttons still on the old blue.
