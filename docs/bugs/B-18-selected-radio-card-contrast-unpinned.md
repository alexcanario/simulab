---
bug: B-18
feature: F-43
status: idea
board: 769
severity: low
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
<!-- What this bug needs before it can be fixed, and how to run it. Filled when the bug is created, confirmed at refinement. -->
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

## Fix
<!-- What changes. Keep it minimal. -->
<!-- If duplicates were listed above: which are fixed here, which are deferred and why. -->

## Regression test
<!-- The test that fails before the fix and passes after it. One per occurrence fixed. -->
- <Test name> — <project>

## Open questions
- (none)

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: <bug/B-<number>>
- Merge: <commit>
