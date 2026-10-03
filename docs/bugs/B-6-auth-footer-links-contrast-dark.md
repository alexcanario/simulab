---
bug: B-6
feature: F-4
status: done
board: 722
severity: medium
---
# Links fail the AA contrast (auth footer and three more)

Technical terms: [glossary](../glossary.md)

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
- 2026-09-19 (build) — `AppLink` renders a plain `a` with the kit class `app-link` (text colour, underline, thicker underline on hover) instead of wrapping `MudLink`: the library's colour classes are what failed, and the shared focus ring and pointer already apply to every `a`.
- 2026-09-19 (build) — The auth footer keeps its 14 px text (`font-size: 0.875rem` on `.app-auth-footer`), as with `Typo.body2` before.
- 2026-09-19 (build) — No `/agile:review`: markup and CSS of four links, a kit component and an architecture test; no logic, data or contract change.

## Out of scope
- A contrast audit of the whole app.
- Plain `<a>` links already styled by the F-7 rule (`.app-auth-card p a`, `.app-auth-footer-line a`).

## Regression test
Written first and run on the unfixed code on 2026-09-19:
- `UiKitBoundaryTests.Razor_OutsideKit_UsesAppLinkNotALibraryLink` (architecture, every page and layout) failed, listing the library links starting with `Components\Layout\AuthLayout.razor: <MudLink Href="/terms" Color="Color.Primary" Typo="Typo.body2">`. A first version passed by mistake: a backspace character had replaced `\b` in its pattern, so it matched nothing; fixed before any code change and seen failing.
- `AuthLayoutLinksTests.Footer_IsNamedLegal_AndItsLinksAreKitLinks` (bUnit, the real layout) failed: the footer was named "Language".
- One check per occurrence: `AppLinkTests` (the kit link), `AuthLayoutLinksTests` (footer), `AccountPageTests.Load_ShowsTheEmailReadOnly_…` ("Change password"), `VerifyEmailTests.InvalidToken_LinksBackToSignUp` ("Back to sign-up"); the gallery link is covered by the architecture test.

Measured on screen through the app host (computed colour against the first opaque background, WCAG formula):

| Link | Dark | Light | Before (computed) |
|---|---|---|---|
| Footer: terms, privacy | 14.46:1 | 14.11:1 | 3.87 / 4.21 |
| My account: Change password | 13.19:1 | 15.40:1 | 3.53 / 4.60 |
| Verify email: Back to sign-up | 13.19:1 | 15.40:1 | 3.53 / 4.60 |
| Gallery: leave link | 14.46:1 | 14.11:1 | 3.87 / 4.21 |

All are underlined; the footer is announced as "Documentos legais" (pt-PT) on screen.

## Open questions
- (none)

## Change notes

## Validation script
1. Close any IDE build, run `dotnet run --project src/Hosts/Simulab.AppHost`, and open https://localhost:7125/sign-in signed out.
2. In the light theme, look at "Terms of use" and "Privacy policy" at the bottom. → Underlined, in the dark text colour, clearly readable; not blue.
3. Switch to the dark theme (sun/moon button). → Same links, light text colour, underlined, clearly readable on the dark background.
4. Keyboard only: press Tab until the focus reaches "Terms of use", then "Privacy policy". → A visible focus ring on each; Enter opens the page.
5. Switch the language with the globe. With a screen reader (Windows Narrator: Ctrl+Win+Enter) move to the footer. → It is announced as "Documentos legais" (pt) or "Legal" (en), not "Language".
6. Open https://localhost:7125/verify-email?token=invalid. → "Back to sign-up" is underlined in the text colour, in both themes.
7. Sign in (your account, or `f8.check@example.com` / `Estudar#2026!`), open "My account". → "Change password" is underlined in the text colour, in both themes.

## Delivery
- Branch: bug/B-6 (removed; never pushed)
- Merge: f1e66a1 (--no-ff, AB#722)
- Validation: passed by the owner, 2026-09-19
- Regression: the architecture test and the footer test seen failing before the fix; contrast measured on screen, 13.19:1 to 15.40:1 in both themes
- Tests: full suite 432 passed, 0 failed; build 10 s, tests 27 s; 0 warnings, baseline empty; agile gate GREEN
- Manual pages: none changed (the manual describes tasks, not link colours; no visible text changed)
