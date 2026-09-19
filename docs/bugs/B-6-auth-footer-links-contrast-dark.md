---
bug: B-6
feature: F-4
status: refining
board: 722
severity: medium
---
# Links fail the AA contrast (auth footer and three more)

## What happens
1. Switch to the dark theme and open `/sign-in`.
2. The "Terms of use" and "Privacy policy" links in the `AuthLayout` footer are primary blue (#2478C5) on the dark background: about 3.5:1, under WCAG 2.2 AA (4.5:1).

## Expected
The footer links meet AA in both themes, like the in-card links fixed in F-7 (underlined, text colour).

## Cause
Confirmed in code on 2026-09-19 (contrast computed from `SimulabTheme` with the WCAG formula; not measured on screen yet):
- `src/Hosts/Simulab.Web/Components/Layout/AuthLayout.razor:30-31` — the footer links are `MudLink Color="Color.Primary"`, text `#2478C5`, 14 px (`body2`), on the page background, and MudBlazor underlines them only on hover. Contrast: **3.87:1 on the dark background** (`#0E1828`) and **4.21:1 on the light one** (`#F2F5FB`). AA for normal text is 4.5:1, so they fail in **both** themes, not only in dark: the premise "dark theme only" is partly false.
- The F-7 fix (`src/Hosts/Simulab.Web/wwwroot/app.css:286-293`, underlined links in the text colour) targets `.app-auth-card p a` and `.app-auth-footer-line a`; the layout footer (`.app-auth-footer`) was never in it.
- Root: there is no kit link. Pages use MudBlazor's `MudLink`, whose default colour is the primary blue, and each page decides alone.

Same cause elsewhere (the primary blue on a dark card is 3.53:1; on a light card 4.60:1):
- `src/Hosts/Simulab.Web/Components/Pages/Identity/Account.razor:66` — "Change password" (F-8), primary blue on the card: fails in dark (3.53:1).
- `src/Hosts/Simulab.Web/Components/Pages/Identity/VerifyEmail.razor:94` — "Back to sign-up", `Color.Primary` inside `.app-auth-links` on the card, not covered by the F-7 rule: fails in dark (3.53:1).
- `src/Hosts/Simulab.Web/Components/Pages/Dev/UiGallery.razor:87` — dev-only gallery link, primary blue on the background: fails in both themes.

Found in passing, same lines: the footer `nav` is named with `Layout.LanguageSwitch.Label` ("Language"), so a screen reader announces the legal links as the language navigation (`AuthLayout.razor:29`).

## Fix
- BR1 The kit gains `AppLink`: a real link (`a`), underlined, in the text colour of the surface it sits on (14.5:1 on the dark background, over 4.5:1 in both themes), with the kit's hover and focus ring. It is shown in the gallery.
- BR2 The four links of `## Cause` use `AppLink`: the auth footer (terms, privacy), "Change password" on My account, "Back to sign-up" on email verification, and the gallery's leave link.
- BR3 The auth footer's navigation is named for what it holds: new key `Auth.Legal.Label` ("Legal" / "Documentos legais" / "Documentos legais") instead of `Layout.LanguageSwitch.Label`.
- BR4 An architecture test fails when a page or layout uses `MudLink` outside the kit, except with `Color="Color.Inherit"`: the app-bar links (the app name and "Sign in"), whose colour and contrast come from the bar.

## Acceptance criteria
- AC1 Given the gallery's `AppLink`, then it renders an `a` with the link's `href`, underlined, in the text colour (no primary-colour class). (BR1)
- AC2 Given `/sign-in` (any auth page), then the terms and privacy links in the footer are `AppLink`s, and the footer `nav` is named "Legal" (pt-BR and pt-PT: "Documentos legais"). (BR2, BR3)
- AC3 Given `/account` and `/verify-email` with an invalid link, then "Change password" and "Back to sign-up" are `AppLink`s. (BR2)
- AC4 Given the Web project, then no `.razor` file outside `Components/Ui/` uses `MudLink` except with `Color="Color.Inherit"`; the test fails on a new one. (BR4)
- AC5 Measured on screen through the app host, in light and dark: every changed link has at least 4.5:1 against its background. (BR1; validation script)
- AC6 The new text `Auth.Legal.Label` exists in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-19 — A kit `AppLink` plus an architecture test, not CSS on four places or a lighter dark-theme primary — owner; the ui rule says to extend the kit, the test stops the next page from repeating the bug, and a lighter primary would change buttons across the app and still fail the light footer (4.21:1).
- 2026-09-19 — All four occurrences and the footer's accessible name are fixed now — owner; same lines, same cause.
- 2026-09-19 — Out of scope: a contrast audit of the whole app (buttons, chips, secondary text) — owner; only links. No new packages.
- 2026-09-19 — The app-bar links keep `MudLink` with `Color="Color.Inherit"`: their colour is the bar's text colour, which passes; moving them would change the bar for no gain.
- 2026-09-19 — The contrast values in `## Cause` are computed from `SimulabTheme` with the WCAG formula; AC5 measures them on screen, because the component library's own styles can override a colour.

## Out of scope
- A contrast audit of the whole app.
- Plain `<a>` links already styled by the F-7 rule (`.app-auth-card p a`, `.app-auth-footer-line a`).

## Regression test

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
