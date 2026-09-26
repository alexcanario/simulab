---
bug: B-18
feature: F-43
status: validating
board: 769
severity: low
Autopilot: built
---
# The selected radio card's description is not pinned by a contrast test

## What happens
Nothing is wrong on screen: the text reads. What is missing is the guard. F-43's retro sweep, which the new
project rule asks for, went over every colour a `.app-*` rule paints and found one pair that no test holds:
`text-secondary` on `primary-lighten`, the description line inside a **selected** `AppRadioCards` card. Every
other pair is either held by `ThemeContrastTests` or is a surface the palette tests already cover.

Computed from the theme tokens on 2026-09-26: **6.33:1 light** and **4.60:1 dark**, both above the 4.5:1 of
ADR-0001 #29 — the dark one by 0.10. A later change to `primary-lighten` or to `text-secondary` would take it
under without a single test going red, which is exactly how B-16 and B-17 happened.

1. Open `/dev/ui` in dark mode and choose a card in the radio cards section.
2. Read the line under the chosen option's title. It is legible today; nothing holds it there.

## Start
- Depends on: nothing. F-43 (`done`, merged as `7c771d4`) brought both the component and the helper the test
  needs.
- Waits on: nothing.
- Suggested path: `/agile:autopilot B-18` — one theory case in `ThemeContrastTests`, using `AppCssColours` to
  read the selected card's own colours from `app.css`, over `primary-lighten` instead of the surface.
- Parallel with: anything: it touches one test file.

## Expected
`ThemeContrastTests` holds the selected card's text and description over `primary-lighten`, in both themes, the
way it now holds the field hint and the selected menu item. A tone change that broke either would fail the
build instead of reaching the screen.

## Cause
`src/Hosts/Simulab.Web/wwwroot/app.css` — `.app-radio-card-selected` sets `background-color: primary-lighten`,
and `.app-radio-card-description` inside it keeps `color: text-secondary`. The two rules are separate, so no
test pairs them: `ThemeContrastTests.FieldHint_ReadsAtAaOnItsSurfaces` measures `text-secondary` on `Surface`
and `Background` only.

No duplicate found: the selected card is the only place a `.app-*` rule puts text on `primary-lighten`. The
sweep over every `color` declaration in a `.app-*` rule is in the F-43 entry of `docs/agile/retro-log.md`.

Verified again on 2026-09-26, in today's code (`app.css:269-272` and `app.css:285-288`, unchanged since the
F-43 merge `7c771d4`):
- `.app-radio-card-selected` → `background-color: var(--mud-palette-primary-lighten)`; `.app-radio-card-description`
  → `color: var(--mud-palette-text-secondary)`. Two separate rules, so no test pairs them.
- `ThemeContrastTests.FieldHint_ReadsAtAaOnItsSurfaces` measures `text-secondary` against `Surface` and
  `Background` only; no test names `PrimaryLighten` as a background. `ThemePaletteTests` covers the tokens
  themselves, not this pair.
- Measured from the palette tokens (`#4A5A7A` on `#EBF3FD` light, `#7A8EB0` on `#162540` dark):
  **6.19:1 light** and **4.61:1 dark**. The dark value stands 0.11 above the 4.5:1 of ADR-0001 #29. The
  **6.33:1** the file reported for light does not reproduce; 6.19 is what the committed tokens give, and the
  conclusion is unchanged.
- `.app-radio-card-title` declares no colour: it inherits `text-primary`, which measures 13.77:1 light and
  12.43:1 dark on `primary-lighten` — no risk, and a sweep over `color` declarations cannot see it because
  there is none.
- The selected card's border (`primary` on `Surface`) is already held by
  `ThemeContrastTests.PrimaryText_ReadsAtAaOnItsSurface`; the unselected border (`lines-inputs`) by
  `ThemePaletteTests` as a non-text colour over the pages.

## Fix
One `Theory` in `ThemeContrastTests`, over both palettes, and nothing else. No CSS, no palette token, no
component changes: the screen is correct today and stays byte for byte as it is.

- BR1 The description of a selected radio card reads at AA (4.5:1, ADR-0001 #29) over the background that card
  paints, in the light and in the dark palette.
- BR2 Both colours are read from `app.css` through `AppCssColours` — `color` from `.app-radio-card-description`
  and `background-color` from `.app-radio-card-selected` — and resolved against the palette. Neither is repeated
  by hand, so what fails is the colour the CSS chose (`project.md`, UI tests, from B-16 and B-17).
- BR3 The threshold is the shared `MinimumForText`, never a number invented for this pair: the dark value stands
  at 4.61:1 and the guard holds it at 4.5:1, so the test passes today and goes red the day a tone takes it under.
- BR4 The unselected card keeps no guard of its own: its description sits on `Surface` and `Background`, which
  `FieldHint_ReadsAtAaOnItsSurfaces` already measures for the same token.

## Regression test
- `ThemeContrastTests.SelectedRadioCardDescription_ReadsAtAaOnItsOwnBackground` — `tests/Hosts/Simulab.Web.Tests`
  (a `Theory` over `BothPalettes`, one case per palette).
- Seen failing before it ships (`project.md`, UI tests, F-22): the injected mistake is a tone change to
  `PrimaryLighten` or `TextSecondary` in `SimulabTheme` that takes the dark pair under 4.5:1. The injected diff is
  shown before the run, so a green run cannot be an edit that never applied (`project.md`, from F-45).

Done on 2026-09-26. With dark `TextSecondary` set to `#758AAB` (the diff shown first, one line in the dark
palette), the run over `ThemeContrastTests` and `ThemePaletteTests` gave
`Failed! - Failed: 1, Passed: 98, Total: 99`, the one failure being
`SelectedRadioCardDescription_ReadsAtAaOnItsOwnBackground(what: "dark", light: False)`:
`Expected Contrast(description, surface) to be greater than or equal to 4.5 ... but found 4.356015052748283`.
No other test moved, so the guard is about this pair and nothing else. After `git checkout` of the theme
(`git diff src/` empty), the whole Web project: `Passed! - Failed: 0, Passed: 616, Total: 616, Duration: 6 s`
— two cases more than the 614 on `main`.

## Coverage
| Criterion | Test(s) |
|---|---|
| AC1 | `ThemeContrastTests.SelectedRadioCardDescription_ReadsAtAaOnItsOwnBackground` — both cases green today (light and dark) |
| AC2 | the same test, seen failing at 4.356:1 with dark `TextSecondary` `#758AAB`, and green again once restored |
| AC3 | the same test: both colours come from `AppCssColours.Declaration`, which asserts that exactly one rule for the selector exists and that it still declares the property — a rename or a dropped `color` fails it |
| AC4 | `git diff` against `main` touches one file, `tests/Hosts/Simulab.Web.Tests/Ui/ThemeContrastTests.cs`; no UI text, no `app.css`, no palette, no component |

## Acceptance criteria
- AC1 Given today's palette, when the guard runs, then the selected card's description measures at least 4.5:1 in
  both palettes (6.19:1 light, 4.61:1 dark) and the test passes.
- AC2 Given a `TextSecondary` or `PrimaryLighten` tone that takes the dark pair under 4.5:1, when the suite runs,
  then this test fails and names the palette and the measured ratio.
- AC3 Given the pair, when the test reads it, then both colours come from `app.css` through `AppCssColours`: a
  rename of either class, or a `color` dropped from the description rule, fails the test instead of silently
  measuring nothing.
- AC4 No new UI text and no rendered output changes: the localization criterion does not apply, and `app.css`,
  `SimulabTheme` and every component are untouched by this item.

## Decisions
- 2026-09-26 — the guard covers only the declared pair (owner) — it is the only pair at risk (4.61:1 dark) and the
  rule from B-16/B-17 asks for the pair the stylesheet declares; the title declares no colour and sits above 12:1.
- 2026-09-26 — the threshold stays the shared 4.5:1 and no palette tone changes (owner) — nothing is wrong on
  screen, and `text-secondary` is used across the whole app, so widening its margin in a low-severity bug would
  reopen every pair `ThemePaletteTests` holds.
- 2026-09-26 — the injected mistake for the "seen failing" step is dark `TextSecondary` `#758AAB` — measured at
  4.36:1 on `primary-lighten` while the field hint keeps 4.62:1 on the card and 5.07:1 on the page, so exactly
  this test goes red and no other. `#6E819F` was rejected for taking the field hint down with it (4.10:1).
- 2026-09-26 — the light ratio recorded when the bug was captured (6.33:1) does not reproduce; the committed
  tokens give 6.19:1 — recorded in `## Cause` and the conclusion is unchanged.
- 2026-09-26 — no new package.

## Out of scope
- Any change to `app.css`, to a palette token or to `AppRadioCards`.
- Declaring the title's inherited colour so it too could be read from the stylesheet (13.77:1 light, 12.43:1 dark
  — no risk).
- A sweep test that would refuse any future `.app-*` rule painting text on `primary-lighten` without a measured
  pair.

## Open questions
- (none)

## Validation script
No screen change to look at: the item is one test. Step 1 was run by Claude in both shells before this script was
handed over: Git Bash and PowerShell 7 each gave `Passed! - Failed: 0, Passed: 32` over `ThemeContrastTests`.

1. In the worktree `D:\dev\_icontrol\wt\simulab\bug-18`, run the theme tests.
   Git Bash: `dotnet test tests/Hosts/Simulab.Web.Tests/Simulab.Web.Tests.csproj --nologo -v q --filter "FullyQualifiedName~ThemeContrastTests"`
   PowerShell 7: `dotnet test tests\Hosts\Simulab.Web.Tests\Simulab.Web.Tests.csproj --nologo -v q --filter "FullyQualifiedName~ThemeContrastTests"`
   → `Passed!` with two more cases than before, both named `SelectedRadioCardDescription_ReadsAtAaOnItsOwnBackground`.
2. See it bite: in `src/Hosts/Simulab.Web/Theme/SimulabTheme.cs`, change the dark `TextSecondary` from `#7A8EB0`
   to `#758AAB` and rerun step 1 → only the dark case of this test fails, naming the ratio (4.36:1). That tone was
   chosen so nothing else goes red: the field hint keeps 4.62:1 on the card and 5.07:1 on the page. Then
   `git checkout src/Hosts/Simulab.Web/Theme/SimulabTheme.cs`, confirm `git diff` is empty, and rerun → green again.
3. Open `/dev/ui` in dark mode, pick a radio card, and read the line under the chosen option → unchanged from
   today; this item paints nothing.

## Delivery
- Branch: `bug/B-18`
- Merge: <commit>
