---
bug: B-9
feature: F-1
status: done
board: 735
severity: medium
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# Info colour and email buttons still use the old primary blue

## What happens
Found during B-8 (AB#734), not yet measured.
1. `Info = "#2478C5"` in both palettes (`src/Hosts/Simulab.Web/Theme/SimulabTheme.cs`), the blue B-8 replaced as
   the primary. As a text colour it read at 4.21:1 on the light background and 3.53:1 on the dark surface; where
   the info colour is used as text (for example an info alert) it may fail AA the same way. Not measured.
2. The email buttons hardcode the old primary `#2478C5` inline
   (`src/Modules/Identity/Simulab.Identity.Infrastructure/Email/PasswordMailer.cs:87`,
   `src/Modules/Identity/Simulab.Identity.Infrastructure/Email/VerificationMailer.cs:50`). White on it is 4.60:1,
   so it passes; the colour no longer matches the app's primary.

## Expected
Every alert of the kit meets AA (4.5:1) for its text in both themes, `ThemeContrastTests` asserts it for info,
success and warning as it already does for error and primary, and the email button uses the app's primary blue,
written once.

## Cause
Confirmed in code on 2026-09-21 (branch `bug/B-8`, which already carries B-8's palette change). Ratios computed
with MudBlazor 9.9.0's own derived tones (`InfoDarken`, hover at 0.06 opacity), not yet measured on screen.

The premise holds only in part: the info colour fails AA in **one** place today, not wherever it is used.

`Info = "#2478C5"` in both palettes (`src/Hosts/Simulab.Web/Theme/SimulabTheme.cs:34` and `:68`). How each use
paints it:

| Use | How MudBlazor paints it | Light | Dark |
|---|---|---|---|
| `AppAlert` info (filled): `SignIn.razor:97`, `:102`, `UserRolesDialog.razor:15`, `UiGallery.razor:123` | white (`InfoContrastText`) on `Info` | 4.60 (passes) | 4.60 (passes) |
| Plain `MudAlert Severity.Info` (text variant): `src/Hosts/Simulab.Web/Components/Pages/Home.razor:8` | `InfoDarken` `#1E63A4` on a 6% info tint over the page background | 5.32 (passes) | **2.70 (fails)** |

- The failing use is the home page status alert in the dark theme. It is also the only info alert that
  bypasses the kit (`AppAlert` is always filled); rule `ui` says kit first.
- No info button, chip or text uses the info colour as text; `Info` on the page background would be 4.21 light
  and 3.87 dark if one ever did.
- `ThemeContrastTests` asserts error and primary, not info.

Emails: the button colour is a literal in two places, with no shared constant:
- `src/Modules/Identity/Simulab.Identity.Infrastructure/Email/PasswordMailer.cs:87` (`Button` helper).
- `src/Modules/Identity/Simulab.Identity.Infrastructure/Email/VerificationMailer.cs:50` (inline).
- `ErasureMailer.cs` has no button. White on `#2478C5` is 4.60:1 (passes); on B-8's light primary `#216DB5` it
  would be 5.36:1. No test covers the mailers' HTML.

Duplicates: the two mailer literals above (the same colour written twice). No other reimplementation found.

Found while measuring, outside this bug's premise (filled `AppAlert`, rule of the kit):
| Alert | Light | Dark |
|---|---|---|
| Success (`#0D6056` light / white dark on `#1A9E8C`) | 2.24 | 3.33 |
| Warning (`#7A4A00` light / white dark on `#D98B1A`) | 2.73 | 2.74 |

Both fail AA in both themes and appear on sign-in, sign-up, check email, account, password and security pages.

## Fix
1. `Home.razor:8`: the plain `MudAlert Severity.Info` becomes `AppAlert Severity="Severity.Info"` with the same
   text key `Home.Status` (D1). The filled kit alert is 4.60:1 in both themes; no colour changes.
2. `Info` stays `#2478C5` in both palettes (D2). Only a test is added for it.
3. The filled success and warning alerts get dark ink instead of the tones that fail (D4), as F-10 and B-8 did
   for error and primary. The hues do not change:
   - `PaletteLight.SuccessContrastText` `#0D6056` → `#19243E`: 4.63:1 on `Success` (was 2.24).
   - `PaletteDark.SuccessContrastText` (white by default) → `#19243E`: 4.63:1 (was 3.33).
   - `PaletteLight.WarningContrastText` `#7A4A00` → `#19243E`: 5.62:1 on `Warning` (was 2.73).
   - `PaletteDark.WarningContrastText` (white by default) → `#19243E`: 5.62:1 (was 2.74).
   - A comment on each changed colour points to `ThemeContrastTests`, as B-8 left on the primary.
4. Emails (D3): the `Button` helper of `PasswordMailer` moves to a shared `EmailHtml` in the same
   folder, with the colour as a single constant `#216DB5` (white on it 5.36:1); `VerificationMailer` uses the
   helper instead of its inline anchor. The markup stays the same apart from the colour.
5. Nothing else in the palette changes; the info, success and warning hues stay as they are.
6. Found on screen while checking the fix (D7): the two error alerts of `UiGallery.razor:132-133` are bare
   `MudAlert`s too and read at 4.12:1 in the dark theme. They become `AppAlert`, and `UiKitBoundaryTests`
   now forbids `<MudAlert` outside the kit, so the third occurrence of this defect is the last one.

## Regression test
`tests/Hosts/Simulab.Web.Tests/Ui/ThemeContrastTests.cs`, each seen failing before the change:
- `AlertContrastText_ReadsAtAaOnItsSeverityColour` (theory: light/dark × info/success/warning). Before: 4 of the
  6 cases fail (success 2.24 and 3.33, warning 2.73 and 2.74); the two info cases pass and guard D2.
- `tests/Hosts/Simulab.Web.Tests/Ui/HomeTests.cs` (new, bUnit through `KitTestContext`):
  `StatusNotice_UsesTheFilledKitAlert` — the rendered home page carries `mud-alert-filled-info`. Before: fails
  (the page renders the text variant).
- `tests/Modules/Identity/Simulab.Identity.Tests/EmailButtonTests.cs` (new, in the existing project):
  `Button_UsesAColourThatIsReadableUnderWhiteText` (>= 4.5:1) and
  `VerificationAndPasswordEmails_UseTheSameButtonColour`, both reading the e-mails the mailers produce.
  Before: the second fails on the inline anchor of `VerificationMailer` (seen failing by pinning the constant
  back to `#2478C5`: `Failed: 1, Passed: 1`).
- `tests/Simulab.ArchitectureTests/UiKitBoundaryTests.cs`: `<MudAlert` joins the tokens forbidden outside the
  kit, with the temp-folder case `FindViolations_FileOutsideKit_NamesTheFile` naming a bad file. Before the
  page fixes, `Web_OutsideKit_UsesNoRawTableOrIconConstant` names `Home.razor` and `UiGallery.razor`.

## Acceptance criteria
| # | Criterion | Test |
|---|---|---|
| AC1 | Given either theme, when a filled kit alert of severity info, success or warning is shown, then its text reads at 4.5:1 or more on the alert colour. | `ThemeContrastTests.AlertContrastText_ReadsAtAaOnItsSeverityColour` |
| AC2 | Given the home page, when the status notice is shown, then it is the kit alert (`AppAlert`), so it is readable in the dark theme. | `HomeTests.StatusNotice_UsesTheFilledKitAlert` + validation script |
| AC3 | Given a verification or password e-mail, when its button is rendered, then both e-mails use the same colour `#216DB5`, readable under white text. | `EmailButtonTests` (two tests) |
| AC4 | Localization: no UI text is added or changed (`Home.Status` and the e-mail keys stay); the missing-key test stays green. | `ResourceParityTests` (existing) |
| AC5 | Given any page outside the kit, when it shows a notice, then it uses `AppAlert`; a bare `MudAlert` fails the build. | `UiKitBoundaryTests.Web_OutsideKit_UsesNoRawTableOrIconConstant` + `FindViolations_FileOutsideKit_NamesTheFile` |

## Decisions
- D1 (owner, 2026-09-21): the home alert moves to `AppAlert` instead of changing the info colour. Reason: it is
  the only info alert that bypasses the kit (rule `ui`), and the kit's filled alert already passes at 4.60:1.
- D2 (owner, 2026-09-21): `Info` stays `#2478C5`; only a test is added. Reason: it passes in every use the kit
  has; aligning it with the primary would make two semantic colours identical.
- D3 (owner, 2026-09-21): the e-mail button becomes `#216DB5` in a single shared constant. Reason: the app's
  light primary since B-8, and the colour was written twice.
- D4 (owner, 2026-09-21): the success and warning alerts found outside the premise are fixed here, not in a new
  bug. Reason: same palette, same test, one validation pass.
- D5 (Claude, 2026-09-21): success and warning keep their hues and get dark ink (`#19243E`), instead of darker
  hues with white ink. Reason: dark ink passes in both themes with one value, and the darker teal that white
  would need (4.88:1) would fail as text on the dark surface; same choice as F-10 (error) and B-8 (primary).
- D6 (Claude, 2026-09-21): the shared e-mail helper is `EmailHtml`, a static class in the mailers' own folder,
  not a new building block. Reason: only the Identity module sends these e-mails; it is public because the
  module's test project reads the colour from it.
- D7 (Claude, 2026-09-21): the two bare error alerts of the gallery are fixed here and `<MudAlert` outside the
  kit becomes a build failure. Reason: same cause as D1, found on screen while checking this fix (4.12:1 in
  the dark theme); a rule the build holds stops the fourth occurrence. Two lines of markup, one test token.

## Out of scope
- A brand palette for Simulab (ADR-0001, decision 30 still applies).
- Non-text contrast: the password-strength bar and the status icons (`Color.Warning`, `Color.Success`) are
  graphics, measured against 3:1, and are not touched here.
- The secondary and tertiary colours: no use paints text in them today.
- Changing the e-mail layout, texts or the mailers' behaviour.

## Open questions
None.

## Screen check (Claude, 2026-09-21)
Opened through the app host (`https://localhost:7125`), both themes, colours read from the rendered page after
the theme transition settled and measured:

| Element | Light | Dark |
|---|---|---|
| Home status notice (now `AppAlert`) | 4.60 | 4.60 |
| Gallery info alert | 4.60 | 4.60 |
| Gallery warning alert | 5.62 | 5.62 |
| Gallery success alert | 4.63 | 4.63 |
| Gallery error alert and its action ("Tentar novamente") | 5.54 | 4.79 |
| The two error-code alerts (now `AppAlert`) | 5.54 | 4.79 |
| Success snackbar | 4.63 | 4.63 |

Before the change the same page read 2.24 (success, light), 3.33 (success, dark), 2.73 / 2.74 (warning) and
4.12 (the error-code alerts, dark). The e-mails were checked by test, not on screen; step 6 below covers them.

## Validation script
1. Start the app from the repository root: `dotnet run --project src/Hosts/Simulab.AppHost`, then open
   `https://localhost:7125` and sign in.
2. Dark theme, home page: the status notice is a filled blue alert with readable text (it is no longer a pale
   blue block with dark-blue text).
3. Still in the dark theme, open `/dev/ui`: the success alert (teal) and the warning alert (amber) show dark
   text that is easy to read; the info alert is unchanged.
4. Switch to the light theme on `/dev/ui`: the same two alerts keep dark text, now on the same teal and amber.
5. Trigger a snackbar on `/dev/ui` ("Mostrar notificação") in both themes: its text is readable.
6. Sign out and ask for a password reset at `/forgot-password`; open Mailpit from the Aspire dashboard (its
   web UI is linked there, `docs/infra.md`): the button in the e-mail is the same blue as the app's buttons,
   with white text on it.
7. Still on `/dev/ui`, in the "Mensagens de erro" section: both error-code notices are filled red alerts with
   dark text in the dark theme (they used to be a pale red block).
8. Switch the language to English on `/dev/ui` and, keyboard only, press Tab to the error alert's action
   ("Try again") and activate it with Enter: the focus ring is visible, nothing shows a raw key.

## Delivery
- Branch: `bug/B-9`, merged with `dee5225` (AB#735).
- Approved by the owner on 2026-09-21; the file was committed once B-8 was merged.
- Validated on screen by the owner on 2026-09-21.
- Full suite before the merge: 627 tests, 32 s; build 13 s, 0 warnings (`agile gate GREEN`).
- The app manual is unchanged: no screen, field, rule or message changed, only the contrast of the notices.
- Tests at the end of the build: `Simulab.Web.Tests` 312 passed (3 s), `Simulab.ArchitectureTests` 29 passed
  (237 ms), `Simulab.Identity.Tests` 232 passed (25 s). The regression tests were seen failing first
  (5 of the web ones, 1 of the e-mail ones).
