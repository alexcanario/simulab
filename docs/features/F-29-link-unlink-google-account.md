---
feature: F-29
epic: Foundation and identity
status: refining
board: 748
version: 1
---
# Link and unlink Google on the account page

## Summary
A section on the Security page to link a Google account to an existing account, and to unlink it while keeping at least one way to sign in. Left out of F-20 by the owner on 2026-09-23: in F-20 the link happens only on the first Google sign-in whose verified email matches an account.

## Start
- Depends on: nothing. F-20 (`done`) built the Google round trip, the link on first sign-in, the switch and
  `PasswordNotSetAlert`; F-11 (`done`) built the Security page this section joins. Both are on `main`.
- Waits on: nothing. The three answers were given by the owner on 2026-09-26.
- Suggested path: `/agile:refine` → `/agile:build`. It touches authentication, so the independent review runs
  on this file before approval and again on the diff before validation.
- Parallel with: anything outside `Simulab.Identity` and the Security page. B-18 is in its own worktree and
  touches only a theme test.

## What exists
- **Linking happens today in exactly one place**: `GoogleSignInHandler.SignInAsync` calls
  `userManager.AddLoginAsync` when a checked Google identity's verified address matches an **active** account
  that is not already linked to another Google subject (F-20 BR4). There is nowhere a signed-in user can link
  or unlink by choice — which is what this feature adds.
- The Google round trip belongs to the Web host: `GoogleAccountEndpoints` at `/account/google/start` and
  `/account/google/complete`, because a Blazor circuit can neither redirect to Google nor read the answer.
  They are mapped only while the feature is on.
- The switch is `Identity:GoogleSignInEnabled`, read once at start on both hosts (`GoogleSignInSettings`).
  `CLAUDE.md` says Google is off in v1, so this section is invisible by default.
- The link row is `user_logins`, provider `GoogleSignInProtocol.LoginProvider`, key = Google's subject
  (`GoogleSignInHandler.Login`). `IUserDirectory.FindLoginKeyAsync(userId, provider)` already answers whether
  an account is linked — nothing new is needed to read the state.
- **An account may have no password**: `UserManager.HasPasswordAsync` is what `EraseAccountHandler` checks
  before asking for one, answering `identity.password_not_set`. `PasswordNotSetAlert` is the shared component
  that says so and points at `/forgot-password`, which gives a first password.
- `/account/security` (F-11) is one card with the two-factor block and a danger zone. It already renders
  `PasswordNotSetAlert` for `identity.password_not_set` and a lockout alert, so this section inherits both.
- The Api's Google surface today is one route: `POST /api/v1/identity/google-registrations`. There is no
  endpoint for linking or unlinking; both are new.

## Goal
Let someone who already has an account decide for themselves whether Google is a way into it — and never let
that decision lock them out.

## Users and use cases
- UC1 A signed-in user with no Google link sees the section on the Security page, chooses "Connect Google",
  comes back from Google and finds the account linked, with the Google address shown.
- UC2 A signed-in user whose account is linked sees which Google address it is and can disconnect it.
- UC3 A user whose account has no password tries to disconnect and is told why it is refused, with the way to
  create a password first.
- UC4 A user tries to link a Google account that already belongs to another Simulab account and is told, with
  nothing changed on either account.

## Business rules
- BR1 The section appears on `/account/security` only while `Identity:GoogleSignInEnabled` is on, the same
  switch that maps the round trip. Off, the section is absent and both new endpoints answer 404 — a switched
  off feature is not reachable by URL either.
- BR2 Linking reuses the F-20 round trip: the page navigates to `/account/google/start` with a full load, and
  the callback finishes on `/account/google/complete`, which knows it is a link because the caller is already
  signed in. The ID token is checked by the same `GoogleSignInHandler.CheckTokenAsync` — issuer, audience,
  signature, lifetime and `email_verified`.
- BR3 **The Google address does not have to be the account's address.** The user is already authenticated, and
  proving control of a Google account is what the link means. The address is stored nowhere new: it is shown
  from the token at link time and read from Google on later sign-ins.
- BR4 Linking is refused when that Google subject is already a login of another account, with
  `google_sign_in.account_exists` — the code F-20 already uses for the same situation. Nothing changes on
  either account.
- BR5 Linking this account to the subject it is already linked to succeeds and changes nothing: coming back
  from Google twice must not turn into an error.
- BR6 Linking when this account is already linked to a **different** subject replaces nothing: it is refused
  with `google_sign_in.already_linked`, and the user disconnects first. One account has at most one Google
  link.
- BR7 Unlinking asks for the current password, as erasing the account and downloading the data already do, and
  is refused with `identity.incorrect_password` when it is wrong.
- BR8 **Unlinking is refused when the account has no password**, with `identity.password_not_set`: there was
  at least one way to sign in before and this would leave none. Written as a before-and-after, so it never
  blocks a case where there was nothing to lose. The screen shows `PasswordNotSetAlert`, whose action already
  leads to `/forgot-password`.
- BR9 Unlinking an account that has no Google link succeeds and changes nothing, so a second tab does not turn
  a finished action into an error.
- BR10 Both actions are recorded as account events (F-14), as the other security actions are, so
  `/admin/account-events` shows who linked or unlinked and when.
- BR11 Every new text exists in pt-BR, pt-PT and en, the new error code included.

## Screens and API
- `/account/security` — a new card section, **after** two-factor and **before** the danger zone: the title, one
  line of explanation, the state, and one action.
  - Not linked: the button `Security.Google.Connect` navigates to `/account/google/start` with a full load.
  - Linked: the Google address, and `Security.Google.Disconnect`, which opens the kit confirmation dialog with
    the current-password field, the shape `EraseAccountDialog` already uses.
  - Linked with no password: `PasswordNotSetAlert` in place of the password field, and the disconnect button
    disabled with a tooltip saying why.
- `POST /api/v1/identity/google-links` — links the signed-in caller to the Google identity of the ID token in
  the body. 204 on success. Errors: `google_sign_in.invalid_token` (400),
  `google_sign_in.email_not_verified` (422), `google_sign_in.account_exists` (409),
  `google_sign_in.already_linked` (409).
- `DELETE /api/v1/identity/google-links` — unlinks the signed-in caller, with the current password in the body.
  204 on success, and on an account that had no link. Errors: `identity.incorrect_password` (422),
  `identity.password_not_set` (422).
- Both require an authenticated caller and act only on that caller's own account: neither takes a user id.
- Error codes: `google_sign_in.already_linked` is new; the other three exist since F-20.

## Acceptance criteria
- AC1 Given a signed-in user with no link, when the Google round trip comes back with a checked identity, then
  `user_logins` has one Google row for that account and the section shows the address.
- AC2 Given the account is already linked to that same subject, when the link runs again, then it succeeds and
  there is still exactly one row.
- AC3 Given the account is linked to a different subject, when a link is attempted, then it is refused with
  `google_sign_in.already_linked` and the existing row is untouched.
- AC4 Given the subject belongs to another account, when a link is attempted, then it is refused with
  `google_sign_in.account_exists` and neither account changes.
- AC5 Given a Google identity whose address differs from the account's, when it is linked, then it is accepted.
- AC6 Given a token Google does not vouch for (`email_verified` false), when a link is attempted, then it is
  refused with `google_sign_in.email_not_verified`.
- AC7 Given a linked account with a password, when the user disconnects with the right password, then the row
  is gone and the account still signs in with the password.
- AC8 Given a linked account with a password, when the password is wrong, then it is refused with
  `identity.incorrect_password` and the row stays.
- AC9 Given a linked account **with no password**, when a disconnect is attempted, then it is refused with
  `identity.password_not_set` and the row stays.
- AC10 Given an account with no Google link, when a disconnect is attempted, then it succeeds and nothing
  changes.
- AC11 Given the switch is off, when the Security page is opened, then the section is absent; and when either
  endpoint is called, then it answers 404.
- AC12 Given an anonymous caller, when either endpoint is called, then it answers 401.
- AC13 Given a link or an unlink succeeded, when the account events are read, then the action is there with
  its instant.
- AC14 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-26 — The Google address does not have to match the account's (BR3) — the user is already
  authenticated, and the personal Gmail is often not the address they signed up with. The consequence, written
  so it is not rediscovered: a later Google sign-in finds the account by the link, which is the branch F-20
  already tries first, and no longer by the address (owner, question 1).
- 2026-09-26 — Unlinking asks for the current password and is refused outright when there is none (BR7, BR8) —
  the same door the other two sensitive actions of this page use, and the refusal is what keeps the account
  reachable (owner, question 2).
- 2026-09-26 — A subject already linked elsewhere is refused, never moved (BR4) — moving it would silently
  take a way in from another account, and leave one with no password locked out (owner, question 3).
- 2026-09-26 — One Google link per account (BR6) — decided here: `user_logins` allows several rows per
  provider, but a second one means nothing to the user and doubles every message on this screen.
- 2026-09-26 — The link goes through the Web host's existing round trip rather than a new one (BR2) — decided
  here: a Blazor circuit cannot redirect to Google, which is why `GoogleAccountEndpoints` exists at all.
- 2026-09-26 — No new package: two endpoints, one screen section and their tests (rule `build-config`).

## Out of scope
- Any provider other than Google.
- More than one Google account per Simulab account.
- Changing the account's e-mail to the Google one, or the other way round.
- Signing in with Google, which is F-20 and already shipped.
- An admin unlinking somebody else's Google account.

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-29
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
