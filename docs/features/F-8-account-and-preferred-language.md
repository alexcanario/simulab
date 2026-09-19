---
feature: F-8
epic: Foundation and identity
status: refining
board: 712
version: 1
---
# My account and preferred language

## Summary
My account page with the profile. The preferred language in the profile becomes the first culture source, before the cookie and the browser (ADR-0001 #28).

## Goal
A signed-in user sees and edits their own profile (display name and preferred language) in one place, and the app and its emails follow the same language on every device they sign in from.

## What already exists (verified 2026-09-19)
- `User.FullName` (optional, max 120, blank stored as null; set at sign-up, F-4 BR1) and `User.PreferredLanguage` (required, max 10, set at sign-up from the request culture, F-4 BR7). No migration is needed.
- `PreferredLanguage` already chooses the language of the verification, password reset and password changed emails (F-4, F-7). It is not a culture source for the screens yet.
- Web culture today: `CookieRequestCultureProvider` then `AcceptLanguageHeaderRequestCultureProvider` then `en` (`src/Hosts/Simulab.Web/Program.cs`). The header `LanguageSwitch` writes the cookie through `GET /culture/set` with a full page load.
- No profile endpoint and no My account page. The user menu has "Change password" (`/account/password`, F-7) and "Sign out".
- The Web auth cookie carries `ClaimTypes.Name` (display name) from sign-in (`AccountEndpoints.CompleteSignInAsync`); it is not refreshed until the next sign-in.
- Supported locales: `en`, `pt-BR`, `pt-PT` (`SupportedCultures` in the Web, `RequestLocale.Supported` in the Api).

## Users and use cases
- UC1 A signed-in user opens My account from the user menu and sees their email (read only), display name and preferred language, and a link to change the password.
- UC2 A signed-in user changes the display name and/or the preferred language, saves, and the page reloads in the chosen language with the new name in the user menu.
- UC3 A signed-in user picks a language in the header switch; the screen changes and the choice is saved as the preferred language.
- UC4 A user signs in on any device and the app opens in their preferred language, whatever that browser's cookie or `Accept-Language` says.
- UC5 An anonymous visitor keeps today's order: cookie, then browser, then `en`.

## Business rules
- BR1 A user reads and changes only their own profile. The user is the token subject; no id is taken from the route or the body.
- BR2 The display name is optional: blank or whitespace is stored as null; it is trimmed; at most 120 characters.
- BR3 The preferred language is required and must be one of `en`, `pt-BR`, `pt-PT` (exact, case-insensitive match stored in canonical form). Anything else is refused.
- BR4 The email is shown but not editable.
- BR5 Culture order for a signed-in user: the preferred language from the profile, applied at sign-in and after each change (written into the culture cookie), then the cookie, then the browser, then `en`. For an anonymous visitor: cookie, then browser, then `en`.
- BR6 When a signed-in user picks a language in the header switch, the preferred language is saved to the profile. If the save fails, the screen still changes for this browser and the failure is logged; the next sign-in applies the profile value.
- BR7 A change to the preferred language changes the language of the next emails (existing behavior of F-4 and F-7, now reachable).
- BR8 After a save, the page reloads: the new culture applies to the whole circuit and the display name in the user menu is the saved one.

## Screens and API
- `/account` — My account page (`[Authorize]`), reached from the user menu item "My account". One `MudPaper` form card in the style of `/account/password` (F-7), kit components only:
  - Email (read-only text field).
  - Display name (text field, max 120, helper text "optional").
  - Preferred language (select with the three languages, each in its native name, as in `LanguageSwitch`).
  - Actions: "Save" (primary) and "Cancel" (restores the loaded values).
  - Link "Change password" to `/account/password`.
  - States: loading (skeleton), load error (`AppAlert` with Try again), saving (button disabled with progress), validation error under the field, save error (`AppAlert` with the translated error code). Success shows after the reload as a success alert.
- User menu: "My account" + "Sign out". "Change password" leaves the menu and is reached from `/account`.
- `GET /api/v1/identity/profile` — `ProfileResponse(Email, FullName, PreferredLanguage)`. Requires authentication.
- `PUT /api/v1/identity/profile` — `UpdateProfileRequest(FullName, PreferredLanguage)`; 204 on success, 400 with an error code on validation. Requires authentication.
- `PUT /api/v1/identity/profile/preferred-language` — `UpdatePreferredLanguageRequest(PreferredLanguage)`; used by the header switch so it does not overwrite the name. 204 / 400. Requires authentication.
- `GET /api/v1/identity/session` — `SessionInfoResponse` gains `PreferredLanguage`, so the Web can write the culture cookie at sign-in.
- Web endpoint (non-Blazor, like F-5): `GET /account/profile-applied?redirectUri=...` re-issues the auth cookie with the current display name and writes the culture cookie from the profile, then redirects to a local path only.
- `GET /culture/set` — when the visitor is signed in, also calls `PUT .../profile/preferred-language` (BR6).
- Error codes: `profile.full_name_too_long`, `profile.language_not_supported`.

## Acceptance criteria
- AC1 Given a signed-in user, when they call `GET /api/v1/identity/profile`, then they get their own email, display name and preferred language; without a token the answer is 401. (UC1, BR1)
- AC2 Given a signed-in user, when they `PUT /api/v1/identity/profile` with a name and `pt-PT`, then their `User` row has the trimmed name and `pt-PT`, and no other user's row changed. (UC2, BR1, BR2, BR3)
- AC3 Given a blank or whitespace display name, when the profile is saved, then `FullName` is null. (BR2)
- AC4 Given a display name of 121 characters, when the profile is saved, then the answer is 400 `profile.full_name_too_long` and nothing changed. (BR2)
- AC5 Given a preferred language `fr` or empty, when the profile or the preferred language is saved, then the answer is 400 `profile.language_not_supported` and nothing changed; `PT-br` is stored as `pt-BR`. (BR3)
- AC6 Given a signed-in user with name "Ana", when they `PUT .../profile/preferred-language` with `en`, then the language is `en` and the name is still "Ana". (UC3, BR6)
- AC7 Given a user whose preferred language is `pt-PT` and a browser with the culture cookie `en` and `Accept-Language: pt-BR`, when they sign in, then the culture cookie is `pt-PT` and the first page renders in pt-PT. (UC4, BR5)
- AC8 Given an anonymous visitor, when a page is requested, then the culture comes from the cookie, then `Accept-Language`, then `en`, as before. (UC5, BR5)
- AC9 Given a signed-in user, when they pick pt-BR in the header switch, then the profile is `pt-BR` and the page reloads in pt-BR; if the profile call fails, the page still reloads in pt-BR. (UC3, BR6)
- AC10 Given a user whose preferred language was changed to `pt-PT`, when a password reset email is requested, then it is written in pt-PT. (BR7)
- AC11 Given the My account page, when the user saves a new name and language, then the page reloads in the new language and the user menu label shows the new name. (UC2, BR8)
- AC12 Given the My account page, then the email field is read only, the menu shows "My account" and "Sign out" only, and the page links to `/account/password`. (UC1, BR4)
- AC13 Given the My account page with a 121-character name, then the field shows the length error and Save is disabled; with an API error, the translated error alert is shown. (Screens)
- AC14 All new texts (page, menu item, fields, errors `profile.*`) appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-19 — The page edits display name and preferred language; the email is read only; it links to Change password — owner's choice; the name exists since F-4 and had nowhere to be changed.
- 2026-09-19 — The header language switch also saves the preferred language when signed in — owner's choice; one source of truth, otherwise the next sign-in would undo the switch and emails would go out in another language.
- 2026-09-19 — The profile language applies at sign-in and right after a save (written into the culture cookie); anonymous visitors keep cookie → browser → en — owner's choice; the user sees the change at once and on every device.
- 2026-09-19 — User menu: "My account" + "Sign out"; Change password is reached from My account — owner's choice; one entry for the account keeps the menu short.
- 2026-09-19 — Out of scope: email change, active sessions, avatar; erasure is F-10 and TOTP is F-11 — owner's choice.
- 2026-09-19 — No mockup; the page follows the F-7 form card with kit components only — owner's choice; simple form.
- 2026-09-19 — No new permission: any authenticated user, own profile only (subject from the token) — self-service data needs no RBAC entry; permissions gate other people's data.
- 2026-09-19 — A separate `PUT .../profile/preferred-language` for the header switch — the switch knows only the language and must not overwrite the name.
- 2026-09-19 — The culture source stays the cookie in the Web: the profile value is written into the cookie at sign-in (from `SessionInfoResponse.PreferredLanguage`) and on save, instead of a new `RequestCultureProvider` that calls the Api per request — no Api call per page, same effect. Another device already signed in picks up the change at its next sign-in.
- 2026-09-19 — Save goes through a non-Blazor Web endpoint (`/account/profile-applied`) with a full page load — a Blazor circuit cannot write cookies, and its culture is fixed when the circuit starts (same reason as `LanguageSwitch`, F-5).
- 2026-09-19 — Handlers live in `Simulab.Identity.Application/Profile/`; validation reuses `RequestLocale.Supported` for the language list; the rules are in the handler (a thin update, no domain method needed beyond the existing properties).
- 2026-09-19 — No new packages, for code or tests.

## Out of scope
- Changing the email address.
- Active sessions list and remote sign-out.
- Avatar or photo.
- Account erasure (F-10).
- TOTP and Google sign-in (F-11).
- Pushing a language change to other devices already signed in (they apply it at the next sign-in).

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->

## Delivery
<!-- Filled by /agile:ship. -->
