---
feature: F-8
epic: Foundation and identity
status: done
board: 712
version: 1
---
# My account and preferred language

Technical terms: [glossary](../glossary.md)

## Summary
My account page with the profile. The preferred language in the profile becomes the first culture source, before the cookie and the browser (ADR-0001 #28).

## Goal
A signed-in user sees and edits their own profile (display name and preferred language) in one place, and the app and its emails follow the same language on every device they sign in from.

## What already exists (verified 2026-09-19)
- `User.FullName` (optional, max 120, blank stored as null; set at sign-up, F-4 BR1) and `User.PreferredLanguage` (required, max 10, set at sign-up from the request culture, F-4 BR7). No migration is needed.
- `PreferredLanguage` already chooses the language of the verification, password reset and password changed emails (F-4, F-7). It is not a culture source for the screens yet.
- Web culture today: `CookieRequestCultureProvider` then `AcceptLanguageHeaderRequestCultureProvider` then `en` (`src/Hosts/Simulab.Web/Program.cs`). The header `LanguageSwitch` writes the cookie through `GET /culture/set` with a full page load.
- No profile endpoint and no My account page. The user menu has "Change password" (`/account/password`, F-7) and "Sign out".
- The Web auth cookie can carry `ClaimTypes.Name` (display name) from sign-in (`AccountEndpoints.CompleteSignInAsync`), but the sign-in page always passed `DisplayName: null` (found in build): the menu label showed the email. F-8 fills it from `SessionInfoResponse.DisplayName`.
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
- `/account` — My account page (signed-in only: an anonymous visitor is sent to `/sign-in`, as on `/account/password`), reached from the user menu item "My account". One `MudPaper` form card in the style of `/account/password` (F-7), kit components only:
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
- `GET /culture/set` — when the visitor is signed in and the request comes from the app itself (`Sec-Fetch-Site: same-origin`), also calls `PUT .../profile/preferred-language` (BR6).
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
- 2026-09-19 — One `ProfileHandler` in `Simulab.Identity.Application/Profile/` (get, update, update language); the rules are in the handler (a thin update, no domain method needed beyond the existing properties).
- 2026-09-19 — No new packages, for code or tests.
- 2026-09-19 (build) — The language list moved to `SupportedLanguages` in `Simulab.Identity.Contracts`; `RequestLocale` (Api) and `SupportedCultures` (Web) read it — the handler needs it, and three copies of one list would drift. The name limit is `ProfileLimits.FullNameMaxLength` in the contracts, read by the handler (authority) and the page (comfort).
- 2026-09-19 (build) — `SessionInfoResponse` also gains optional `FullName`, so the sign-in writes the menu label (AC11 needs the name in the menu, and sign-in never set it before). Both new fields are optional: no contract break.
- 2026-09-19 (build) — The kit gains `AppSelectField` (label above, hint or error below, like `AppTextField`), shown in the gallery's fields section; the page uses it for the language.
- 2026-09-19 (build) — After a successful save the page yields one render before navigating, so the unsaved-changes guard sees the saved state and does not ask to discard.
- 2026-09-19 (build) — `IdentityApiFactory` clears its Npgsql pool on dispose: one more test class pushed the idle pooled connections of finished classes past the container limit (`53300: too many clients`).
- 2026-09-19 (build) — Found and captured, not fixed: B-7 (sign-up does not check the name length in the Api).
- 2026-09-19 (review) — major, fixed: no test covered the sign-in page copying the name and language into the ticket; `SignInProfileTests` now does, and `FakeAuthApi` answers both fields.
- 2026-09-19 (review) — minor, fixed: `/account/profile-applied` stored the per-request permission claims in the cookie and restarted its expiry; it now keeps sign-in's claims and the original issue and expiry.
- 2026-09-19 (review) — minor, fixed: a cross-site link to `/culture/set` could change a signed-in user's stored language (CSRF, auth cookie `SameSite=Lax`); the profile is saved only on `Sec-Fetch-Site: same-origin`, the screen still changes.
- 2026-09-19 (review) — minor, fixed: `SessionInfoResponse.DisplayName` renamed `FullName` — one identifier per business term (glossary); not shipped yet.
- 2026-09-19 (review) — minor, fixed: a concurrency failure between the header switch and a save answered 500; the handler retries once on a fresh copy (the profile is last-write-wins). No test: the race cannot be provoked deterministically through HTTP.
- 2026-09-19 (review) — minor, accepted: `GET /identity/session` now reads the account once more per call (primary-key lookup next to the permission query it already runs); a separate sign-in call would add a round trip for the same data.
- 2026-09-19 (review) — minor, fixed in the file: `/account` is signed-in only through a redirect, like `/account/password`, not `[Authorize]`.
- 2026-09-19 (review) — minor, at ship: the manual (three languages) must say Change password moved to My account.
- 2026-09-19 — Retro candidate (owner agreed): a rule that every technical term used with the owner is added to `## Technical terms` in `docs/glossary.md` in the same step.

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
1. Close any open IDE build, run `dotnet run --project src/Hosts/Simulab.AppHost`, open the Web at https://localhost:7125. Sign in with your account (or create one at `/sign-up` and verify it through Mailpit). → The app opens in your account's preferred language, whatever language the browser had before.
2. Open the user menu (top right). → It shows "My account" and "Sign out" only; its accessible name has your display name, or your email when you have none.
3. Click "My account". → `/account` shows your email (read only, with the hint), display name, preferred language and a "Change password" link that opens `/account/password`.
4. Type a display name of 121 characters. → "Use at most 120 characters." under the field and Save disabled. Click Cancel. → The saved values are back.
5. Change the display name and pick another preferred language, then Save. → The page reloads in the new language with "Your profile was saved." (in that language), and the user menu name is the new one.
6. Use the header language switch (globe) to pick a third language, then reload `/account`. → The page is in that language and the preferred language field shows it.
7. Sign out, pick English in the header switch, sign in again. → The app opens in the preferred language from step 6, not in English.
8. Keyboard only, on `/account`: Tab through email, display name, language (open with Space or Alt+Down, choose with the arrows), "Change password", Cancel and Save; change the name and save with Enter on Save. → Every element gets a visible focus ring, in that order, and the save works. Repeat step 3 in the light and dark themes.

## Delivery
- Branch: feature/F-8 (removed; never pushed)
- Merge: b4646de (--no-ff, AB#712)
- Validation: passed by the owner on screen, 2026-09-19
- Review: agile:reviewer, 0 blockers, 1 major and 7 minors; 6 fixed, 1 accepted, 1 done at ship (manual)
- Tests: full suite 399 passed, 0 failed; gate ship run 37 s with the build; 0 warnings, baseline empty
- Manual pages: docs/manual/{pt-BR,pt-PT,en}/my-account.md (new); password.md, getting-around.md, index.md (updated)
- Captured during build: B-7 (AB#724)
