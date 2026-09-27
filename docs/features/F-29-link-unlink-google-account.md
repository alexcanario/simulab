---
feature: F-29
epic: Foundation and identity
status: validating
board: 748
version: 2
Autopilot: built
---
# Link and unlink Google on the account page

## Summary
A section on the Security page to link a Google account to an existing account, and to unlink it while keeping at least one way to sign in. Left out of F-20 by the owner on 2026-09-23: in F-20 the link happens only on the first Google sign-in whose verified email matches an account.

Refined on 2026-09-26. The independent review found the first draft's link flow unsafe; the rules below are the corrected ones, and `## Decisions` keeps what was wrong and why.

## Start
- Depends on: nothing. F-20 (`done`) built the Google round trip, the implicit link, the switch and
  `PasswordNotSetAlert`; F-11 (`done`) built the Security page; F-21 (`done`) built the account events. All on
  `main`.
- Waits on: nothing. Six answers were given by the owner on 2026-09-26, in two rounds.
- Suggested path: `/agile:refine` → `/agile:build`. It touches authentication: the independent review already
  ran on this file and runs again on the diff before validation.
- Parallel with: anything outside `Simulab.Identity` and the Security page. It **changes a rule of F-20 and one
  of F-11**, so neither may be in flight at the same time; both are `done`.

## What exists
- **Linking happens today in exactly one place**: `GoogleSignInHandler.SignInAsync:62` calls
  `userManager.AddLoginAsync` when a checked Google identity's address matches an **active** account not
  already linked to another subject. The address must also be *authoritative* —
  `GoogleIdentity.IsAuthoritative:15` requires `@gmail.com` or a hosted domain, not merely `email_verified`.
- The round trip belongs to the Web host: `GoogleAccountEndpoints.StartPath` (`/account/google/start`) and
  `CompletePath` (`/account/google/complete`). **`Start()` is an unauthenticated GET that carries no intent and
  no per-session state** (`GoogleAccountEndpoints.cs:33`), and `CompleteAsync` always calls
  `SignInWithGoogleAsync` (`:56`). This is the fact the first draft got wrong; BR2 below is written against it.
- F-20 already has the pattern this feature needs: `GoogleSignUpTickets`, a one-time ticket handed to the
  confirmation page in the query (`GoogleAccountEndpoints.cs:72-82`).
- The switch is `Identity:GoogleSignInEnabled`, read once at start on both hosts (`GoogleSignInSettings`).
  `CLAUDE.md` says Google is off in v1.
- The link row is `user_logins`: provider `GoogleSignInProtocol.LoginProvider`, key = Google's subject,
  display name currently the literal `"Google"` (`GoogleSignInHandler.Login:104`). **The Google address is
  stored nowhere**, which is why BR12 below has to store it.
- **An account may have no password**: `UserManager.HasPasswordAsync`, which `EraseAccountHandler.cs:37`
  checks before asking for one, answering `identity.password_not_set`.
- Every password-confirmed action counts failures and locks out: `EraseAccountHandler.cs:43-59`. The codes are
  per action — `account_erasure.current_password_invalid`, `data_export.current_password_invalid`,
  `totp.current_password_invalid` (`IdentityErrorCodes.cs:40,43,80`). **There is no
  `identity.incorrect_password`.**
- **`/account/security` does not exist while TOTP is off**: `Security.razor:261` calls `Navigation.NotFound()`
  on the TOTP switched-off code (F-11 BR12). The danger zone renders only while two-factor is on
  (`Security.razor:151`), and the `PasswordNotSetAlert` slot sits inside the two-factor card (`:42`).
- Account events are **F-21**: `AccountEventTypes` (`AccountEventTypes.cs:10-34`) with `All` pinned to the
  domain enum by a test, and a resource key per value.
- The Api's Google surface is one route today: `POST /api/v1/identity/google-registrations`.
  Password-confirmed actions are `POST /<plural noun>`: `account-erasures`, `data-exports`,
  `password-changes` (`IdentityEndpoints.cs:56,70,75`).

## Goal
Let someone who already has an account decide for themselves whether Google is a way into it — and never let
that decision lock them out, nor let anyone else make it for them.

## Users and use cases
- UC1 A signed-in user with no Google link sees the section on the Security page, chooses "Connect Google",
  comes back from Google and finds the account linked, with the Google address shown.
- UC2 A signed-in user whose account is linked sees which Google address it is and can disconnect it.
- UC3 A user whose account has no password tries to disconnect and is told why it is refused, with the way to
  create a password first.
- UC4 A user tries to link a Google account that already belongs to another Simulab account and is told, with
  nothing changed on either account.
- UC5 A user who disconnected signs in with Google again and is told the account exists and that Google is
  connected in the settings — the disconnection holds.

## Business rules
- BR1 The section appears on `/account/security` only while `Identity:GoogleSignInEnabled` is on. Off, the
  section is absent and every new endpoint answers 404.
- BR2 **The link is an intent, carried and proved, never inferred.** `/account/google/start` takes an explicit
  `intent=link` **on a new POST of the same path** — `/account/google/start` keeps its GET exactly as it is,
  because the sign-in and sign-up buttons reach it that way (`AppGoogleButton.razor:29`) — requires an
  authenticated caller and antiforgery, and mints a one-time ticket bound to that user's id, over the generic
  `SingleUseTickets<T>`. The ticket id and a `link-intent` marker travel in the challenge's
  `AuthenticationProperties.Items`, so they come back inside the encrypted external cookie and never appear in
  a URL a page can read. `/account/google/complete` links only when the marker is there, the ticket is still
  unused, and the session finishing the round trip is still the user the ticket names. (v2)
  - No marker → the callback behaves exactly as F-20 does today, and nothing is linked.
  - Marker, ticket gone or expired → `google_link.expired`, a calm "the attempt expired, try again", not a
    security refusal: a Web restart empties the in-memory tickets.
  - Marker, ticket valid, different or anonymous session → `google_link.session_changed`, nothing linked.
  - **`google_link.session_changed` and `google_link.expired` are refusals of the Web host**, produced in
    `CompleteAsync` and shown by the page from the query string. The Api cannot produce them: it knows nothing
    of the ticket and would be called with the current session's own token. (v2)
  *Inferring the intent from "a session exists" would link the Google identity of whoever finishes the round
  trip to whatever account is signed in on that browser.*
- BR3 The ID token is checked by the same `GoogleSignInHandler.CheckTokenAsync`: issuer, audience, signature,
  lifetime and `email_verified`.
- BR4 **The Google address does not have to be the account's address**, and it does not have to be
  authoritative in the F-20 sense either: the user is already authenticated, so nothing is being inferred from
  the address.
- BR5 Linking is refused when that subject is already a login of another account, with
  `google_sign_in.account_exists`. Nothing changes on either account.
- BR6 Linking this account to the subject it is already linked to succeeds and changes nothing.
- BR7 Linking when this account is already linked to a **different** subject is refused with
  `google_link.already_linked`. One account has at most one Google link.
- BR8 Unlinking asks for the current password and is refused with `google_link.current_password_invalid`. A
  wrong password counts as a failed attempt and can lock the account, exactly as
  `EraseAccountHandler.cs:43-59` does; a locked account is refused with `identity.account_locked` (423).
- BR9 **Unlinking is refused when the account has no password**, with `identity.password_not_set`: there was
  at least one way to sign in before and this would leave none. It never blocks a case where there was nothing
  to lose, because of BR10's order.
- BR10 The checks run in this order: no link → 204, nothing else is read; no password → `password_not_set`;
  locked → `account_locked`; wrong password → `current_password_invalid`; otherwise the row goes.
- BR11 **F-20's implicit link is switched off** (supersedes F-20 BR4): a Google sign-in whose address matches
  an active account that is not linked no longer links it, and answers `google_sign_in.account_exists` with
  the text telling the reader to connect Google in the account settings. One door, one decision, and
  disconnecting means what it says.
- BR12 The Google address is stored with the link, in the `user_logins` row's display name, which today holds
  the literal `"Google"`. No migration: the column exists. Erasing the account already removes the row, so the
  address goes with it.
- BR13 Linking and unlinking are account events (F-21): two values added to the domain enum and to
  `AccountEventTypes.All`, with their resource key in the three languages, so the pinning test stays green and
  `/admin/account-events` shows who did it and when.
- BR14 `/account/security` exists while **either** TOTP or Google is on (supersedes F-11 BR12); each block
  appears by its own switch. With both off, the page is still Not Found. **The "Security" link on
  `/account` follows the same either-switch rule** (`Account.razor:145` reads the TOTP status alone today):
  otherwise, in v1's own configuration, the page exists and nothing leads to it, which is the hole this rule
  was written to close. (v2)
- BR15 Every new text exists in pt-BR, pt-PT and en, the three new error codes and the two event types
  included.

## Screens and API
- `/account/security` — a new card, after the two-factor card and before the danger zone; when two-factor is
  off, it follows the two-factor card's "off" state, which stays on the page (BR14).
  - Not linked: the explanation and `Security.Google.Connect`, which posts to `/account/google/start` with the
    antiforgery token and `intent=link` (a form post, not a link: BR2 needs antiforgery).
  - Linked: the stored Google address and `Security.Google.Disconnect`, opening the kit confirmation dialog
    with the current-password field, the shape `EraseAccountDialog` uses.
  - Linked with no password: `PasswordNotSetAlert` in the card, and the disconnect button disabled with a
    tooltip that says why.
- `POST /account/google/start` (Web host) — the link intent. Form-bound `intent=link`, authenticated,
  antiforgery. The **GET of the same path is untouched**: it is how sign-in and sign-up reach Google. (v2)
- `GET /api/v1/identity/google-links` — the state for the screen: `{ linked, email }`, `email` null when not
  linked or when the stored display name is still the literal provider name of the rows F-20 wrote. 200
  always for an authenticated caller. Plural, as `api-contracts.md` requires. (v2)
- `POST /api/v1/identity/google-links` — links the caller to the identity of the ID token in the body. 204.
  Errors: `google_sign_in.invalid_token` (400), `google_sign_in.email_not_verified` (422),
  `google_sign_in.account_exists` (409), `google_link.already_linked` (409). **Four, not five**:
  `google_link.session_changed` never comes from here (v2, BR2).
- `POST /api/v1/identity/google-link-removals` — unlinks the caller, current password in the body. 204, and
  204 when there was no link. Errors: `identity.password_not_set` (422),
  `google_link.current_password_invalid` (422), `identity.account_locked` (423). A POST and not a DELETE: it
  carries a password, and the module's password-confirmed actions are all `POST /<plural noun>`.
- All three require an authenticated caller and act only on that caller's own account; none takes a user id.
- New error codes (v2 adds `google_link.expired`; all four are constants of `IdentityErrorCodes`, one place
  per module, even the two the Web host produces): `google_link.expired`, `google_link.already_linked`,
  `google_link.session_changed`,
  `google_link.current_password_invalid`.

## Acceptance criteria
- AC1 Given a signed-in user with no link, when the round trip comes back with a checked identity and a valid
  ticket, then `user_logins` has one Google row and `GET google-link` answers `linked` with the address.
- AC2 Given the account is already linked to that subject, when the link runs again, then it succeeds and
  there is still exactly one row.
- AC3 Given the account is linked to a different subject, when a link is attempted, then
  `google_link.already_linked` and the existing row is untouched.
- AC4 Given the subject belongs to another account, when a link is attempted, then
  `google_sign_in.account_exists` and neither account changes.
- AC5 Given a Google address that differs from the account's, when it is linked, then it is accepted (BR4).
- AC6 Given `email_verified` false, when a link is attempted, then `google_sign_in.email_not_verified`.
- AC7 **Given the callback arrives with no link marker, when it completes, then nothing is linked** — it
  behaves as an F-20 sign-in (BR2). (v2: the marker, not the ticket, is what tells the two apart.)
- AC7b **Given the callback arrives with the marker and a ticket that is gone or expired, when it completes,
  then `google_link.expired` and nothing is linked** — a Web restart is not a security refusal. (v2)
- AC8 **Given the callback arrives with a ticket minted for another user, when it completes, then
  `google_link.session_changed` and nothing is linked** (BR2).
- AC9 **Given a link ticket already used once, when it is presented again, then it is refused and nothing is
  linked** (BR2).
- AC10 Given a request to `/account/google/start` with `intent=link` and no antiforgery token, when it is
  posted, then it is refused before Google is reached (BR2).
- AC11 Given an anonymous caller, when `/account/google/start` is posted with `intent=link`, then it is
  refused (BR2).
- AC12 Given a linked account with a password, when the user disconnects with the right password, then the row
  is gone and the account still signs in with the password.
- AC13 Given a wrong password, when a disconnect is attempted, then
  `google_link.current_password_invalid`, the row stays, **and the failed-attempt count grew by one**; and
  after the limit the next attempt is `identity.account_locked` (BR8).
- AC14 Given a linked account **with no password**, when a disconnect is attempted, then
  `identity.password_not_set` and the row stays.
- AC15 Given an account with **no link and no password**, when a disconnect is attempted, then it answers 204
  and nothing is read (BR10's order).
- AC16 Given an account that disconnected, when it signs in with Google at the same address, then it is
  refused with `google_sign_in.account_exists` and nothing is linked (BR11).
- AC17 Given the Google switch is off, when the page is opened and when each of the three endpoints is called,
  then the section is absent and each endpoint answers 404.
- AC18 Given TOTP off and Google on, when `/account/security` is opened, then the page loads with the Google
  card and without the two-factor block (BR14).
- AC18b Given TOTP off and Google on, when `/account` is opened, then the "Security" link is there (BR14, v2).
- AC18c Given Google off and TOTP on, when `/account/security` is opened, then the page loads without the
  Google card (BR1, v2).
- AC19 Given both switches off, when `/account/security` is opened, then it is Not Found (BR14).
- AC20 Given an anonymous caller, when any of the three endpoints is called, then 401.
- AC21 Given a link or an unlink succeeded, when the account events are read, then the action is there with
  its instant and the account it belongs to (BR13).
- AC22 Given the event types, when the pinning test runs, then `AccountEventTypes.All` still matches the
  domain enum and every value has a text in the three languages.
- AC23 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-26 — The Google address does not have to match the account's (BR4) — the user is already
  authenticated, and the personal Gmail is often not the address they signed up with (owner, round 1 Q1).
- 2026-09-26 — Unlinking asks for the current password, counts failures and is refused outright when there is
  no password (BR8, BR9) — the same door the other sensitive actions of this page use (owner, round 1 Q2).
- 2026-09-26 — A subject already linked elsewhere is refused, never moved (BR5) — moving it would silently
  take a way in from another account (owner, round 1 Q3).
- 2026-09-26 — **F-20's implicit link is switched off** (BR11) — with an explicit link in the product, an
  implicit one would silently undo a disconnection at the next sign-in. It changes a rule of a shipped
  feature, so F-20 gets a change note pointing here (owner, round 2 Q1).
- 2026-09-26 — `/account/security` exists while either switch is on (BR14) — otherwise, in v1's own
  configuration (Google on, TOTP off), the whole feature would be unreachable. F-11 gets a change note
  (owner, round 2 Q2).
- 2026-09-26 — The Google address is stored and shown (BR12) — without it the disconnect button is blind for
  anyone with two Google accounts. It goes in the existing display-name column, so there is no migration and
  no new place for personal data to outlive the account (owner, round 2 Q3).
- 2026-09-26 — **The first draft's BR2 was wrong and unsafe**, and is replaced: it said the callback could
  infer a link from the caller being signed in. `GoogleAccountEndpoints.cs:33` shows `Start()` is an
  unauthenticated GET with no state, so that inference would link whoever finishes the round trip to whatever
  account is signed in on that browser, and leaves the classic account-linking CSRF open. Found by the
  independent review before approval, which is why this item runs one.
- 2026-09-26 — Unlink is `POST /google-link-removals`, not `DELETE /google-links` — it carries a password, and
  every password-confirmed action in this module is `POST /<plural noun>` (`IdentityEndpoints.cs:56,70,75`).
- 2026-09-26 — One Google link per account (BR7) — `user_logins` allows several rows per provider, but a
  second one means nothing to the user and doubles every message on this screen.
- 2026-09-26 — No new package: three endpoints, one screen section and their tests (rule `build-config`).

### Independent review, 2026-09-27 (`/agile:review`, fresh context)
No blocker. Eleven findings, all verified in the code before acting; every one confirmed.
- **major, fixed** — `Security.razor` announced and cleared the callback's outcome in `OnInitializedAsync`.
  `Routes` is `InteractiveServer` with prerendering on, so that first runs in static prerender, where
  `NavigateTo` becomes a 302: the browser reloaded without the query and the interactive pass saw nothing.
  AC7b and AC8 would have reached no real user. Moved to `OnAfterRender(firstRender)`, which never runs during
  prerender. No bUnit test can catch this class of defect — bUnit does not prerender — so it stays a
  validation-script step on the running app.
- **major, fixed** — the app manual still described F-20's implicit link, which BR11 removes, and had no page
  for the Google card. `google-sign-in.md` corrected and `security-google.md` written, in en, pt-BR and pt-PT,
  with both indexes updated.
- **major, fixed** — the decisions above promised change notes on F-20 (BR4) and F-11 (BR12); neither existed.
  Written as F-20 v4 and F-11 v4.
- **major, fixed** — `GoogleDisconnectDialog` had no test at all, and the fake Api's removal fixtures had no
  reader. `GoogleDisconnectDialogTests` now drives the three refusals and the cancel through the real page.
  It found a second defect: `_passwordNotSet` was never reset, so one `password_not_set` kept its alert over
  every later answer. Fixed and pinned.
- **major, fixed** — nothing exercised `StartLink`'s happy path; every callback test injected the marker by
  hand. A test now posts the real form with the antiforgery token scraped from the page the browser gets.
- **major, fixed** — AC15 ran on an account **with** a password, so it could not tell BR10's order apart. It
  now builds the real case: a Google-only account whose link is removed first.
- **minor, fixed** — an expired web session answered `google_link.session_changed`; it is `google_link.expired`,
  which is the separation change note v2 asked for.
- **minor, fixed** — `GoogleLinkResponse.HasPassword` was `bool = false`, against `api-contracts.md`: now
  `bool?`, so "no password" and "the server did not say" stop reading the same.
- **minor, fixed** — AC11 accepted three statuses and so could not fail for its own reason. It now asserts the
  cookie challenge (authorization runs before antiforgery) and the endpoint's `IAuthorizeData` metadata.
- **minor, fixed** — a `PasswordNotSet` branch on the card was unreachable; removed.
- **minor, fixed** — `LinkIntent` was declared mid-file; moved beside the other two constants.
- Known limit, accepted: no test reads the issued ticket's id, because it lives protected inside the OIDC
  `state` — which is the point of BR2. The id-to-account binding is covered from the other side, by AC8.

## Out of scope
- Any provider other than Google.
- More than one Google account per Simulab account.
- Changing the account's e-mail to the Google one, or the other way round.
- An admin unlinking somebody else's Google account.
- Re-opening how a **first** Google sign-in creates an account (F-20 BR7); only the implicit link of an
  existing account changes here.

## Open questions
- (none)

## Change notes

### v2 — 2026-09-26
- **What:** four corrections, all found by the two design passes before any code was written.
  1. `/account/google/start` **keeps its GET**; the link intent is a new POST on the same path, form-bound so
     the antiforgery middleware actually validates it. The first version said "the start becomes a POST",
     which would have broken three shipped callers (`SignIn.razor:155`, `SignUp.razor:124`, the gallery) and
     two shipped tests with 405.
  2. `google_link.session_changed` is a **Web-host** refusal, not an Api error: the Api knows nothing of the
     ticket and would be called with the current session's own token, so it physically cannot produce it. The
     link route lists four errors, not five.
  3. A new code `google_link.expired` separates "the attempt expired" (a Web restart empties the in-memory
     tickets) from "the session changed", which is a security refusal. The `link-intent` marker in
     `AuthenticationProperties.Items`, not the ticket's presence, is what tells a link from a sign-in.
  4. BR14 also covers the "Security" link on `/account` (`Account.razor:145` reads the TOTP status alone), and
     `GET /google-link` becomes `/google-links` — plural, as `api-contracts.md` requires.
- **Why:** 1, 2 and 4 are false premises of the approved file, each verified in the code before being written
  down; 3 is a gap the review found — as written, an expired attempt read to the user as a security refusal.
- **Affected:** BR2, BR14, `## Screens and API`, AC7; AC7b, AC18b and AC18c added. Every other rule and
  criterion is unchanged.
- **Re-approved:** 2026-09-26 (owner), for BR2, BR14, AC7, AC7b, AC18b and AC18c — the only criteria this
  note touches.


## Coverage
| Criterion | Test(s) |
|---|---|
| AC1 | `GoogleLinkTests.Link_CheckedTokenAndNoLinkYet_LinksItAndTheStateShowsTheAddress`; on screen, `SecurityGooglePageTests.Linked_ShowsTheAddressAndOffersToDisconnect` |
| AC2 | `GoogleLinkTests.Link_SameSubjectTwice_SucceedsAndLeavesOneRow` |
| AC3 | `GoogleLinkTests.Link_AccountLinkedToAnotherSubject_IsRefusedAndKeepsTheFirstLink` |
| AC4 | `GoogleLinkTests.Link_SubjectOwnedByAnotherAccount_IsRefusedAndNeitherAccountChanges` |
| AC5 | `GoogleLinkTests.Link_GoogleAddressDiffersFromTheAccountAddress_IsAccepted` |
| AC6 | `GoogleLinkTests.Link_EmailNotVerified_IsRefusedAndLinksNothing` |
| AC7 | `GoogleLinkEndpointTests.Complete_NoLinkMarker_SignsInAndLinksNothing` |
| AC7b | `GoogleLinkEndpointTests.Complete_MarkerWithNoTicket_SaysExpiredAndLinksNothing`; shown to the reader by `SecurityGooglePageTests.RefusalInTheAddress_IsShownOnTheCardAndThenCleared("google_link.expired")`, and on the running app by validation step 3 — see the note below |
| AC8 | `GoogleLinkEndpointTests.Complete_TicketMintedForAnotherUser_SaysSessionChangedAndLinksNothing`; shown by the same page test with `google_link.session_changed` |
| AC9 | `GoogleLinkEndpointTests.Complete_TicketPresentedTwice_IsRefusedTheSecondTime` |
| AC10 | `GoogleLinkEndpointTests.StartLink_WithoutTheAntiforgeryToken_IsRefusedBeforeGoogle`; the form that carries the token is pinned by `SecurityGooglePageTests.NotLinked_OffersAFormPostWithTheIntentAndTheAntiforgeryToken`, and the happy path by `StartLink_SignedInWithTheToken_ChallengesGoogleWithAMarkerAndATicketForTheCaller` |
| AC11 | `GoogleLinkEndpointTests.StartLink_Anonymous_IsRefusedAndTheEndpointRequiresAuthorization` |
| AC12 | `GoogleLinkTests.Unlink_RightPassword_RemovesTheRowAndThePasswordStillWorks`; on screen, `GoogleDisconnectDialogTests.RightPassword_ClosesTheDialogAndTheCardOffersToConnectAgain` |
| AC13 | `GoogleLinkTests.Unlink_WrongPassword_KeepsTheLinkCountsTheAttemptAndLocksAtTheLimit`; on screen, `GoogleDisconnectDialogTests.WrongPassword_ShowsItOnTheFieldAndKeepsTheDialogOpen` and `Locked_ShowsTheCountdownAlert` |
| AC14 | `GoogleLinkTests.Unlink_AccountWithoutAPassword_AnswersPasswordNotSetAndKeepsTheLink`; on screen, `SecurityGooglePageTests.LinkedWithoutAPassword_DisablesTheDisconnectAndSaysWhy` and `GoogleDisconnectDialogTests.PasswordNotSet_ShowsThatAlertAndThenClearsItOnTheNextAnswer` |
| AC15 | `GoogleLinkTests.Unlink_NoLinkAndNoPassword_Answers204AndReadsNothingElse` |
| AC16 | `GoogleLinkTests.SignIn_AfterDisconnecting_IsRefusedAndLinksNothing`, plus the re-founded `GoogleSignInTests.Grant_ActivePasswordAccountWithTheSameGmailAddress_IsRefusedAndLinksNothing` and `Grant_WorkspaceAccountWithTheSameAddress_IsRefusedAndLinksNothing` |
| AC17 | `GoogleLinkSwitchedOffTests.EveryRoute_SwitchedOff_Is404` (the three routes); the section's absence by `SecurityGooglePageTests.GoogleOffTotpOn_ShowsNoGoogleCard`; the Web start by the existing `GoogleAccountEndpointTests.Start_SwitchedOff_IsNotFound` |
| AC18 | `SecurityGooglePageTests.TotpOffGoogleOn_ShowsTheGoogleCardAndNoTwoFactorBlock` |
| AC18b | `AccountPageTests.Load_TwoFactorOffButGoogleOn_StillShowsTheSecurityLink` |
| AC18c | `SecurityGooglePageTests.GoogleOffTotpOn_ShowsNoGoogleCard` |
| AC19 | `SecurityGooglePageTests.BothOff_IsNotFound` |
| AC20 | `GoogleLinkTests.AnyRoute_Anonymous_Is401` (a case per route) |
| AC21 | `GoogleLinkTests.LinkAndUnlink_AreRecordedAsAccountEventsOfThatAccount` (and that another account's events are not touched) |
| AC22 | `AccountEventNamesTests` (`AccountEventTypes.All` matches the domain enum) and `AccountEventResourcesTests` (a text per value in the three languages); both already existed and now cover the two new values |
| AC23 | `ResourceParityTests` — the missing-key test over the new `Security.Google.*` keys in pt-BR, pt-PT and en |

Beyond the criteria: `State_LinkWrittenBeforeThisFeature_IsLinkedWithNoAddress` and
`SecurityGooglePageTests.LinkedWithoutAnAddress_SaysSoInsteadOfShowingNothing` pin BR12's old rows,
`LinkedInTheAddress_ConfirmsAndClearsTheAddress` the success message, and
`GoogleDisconnectDialogTests.Cancel_ClosesTheDialogAndCallsNothing` that walking away calls nothing.

**One criterion no test can hold.** AC7b and AC8 depend on the callback's outcome surviving a full page load.
bUnit does not prerender, so a bUnit test passes whether or not the page survives the static pass — which is
exactly how the first attempt shipped a version that worked in tests and for no real user. The guard is
validation step 3, run on the app host: the request must answer **200** with the alert on the card, never a
302 to a clean address.

## Validation script
Everything runs against the app host. `Google:ClientId` and `Google:ClientSecret` must be in the AppHost's
user secrets (`docs/infra.md`), otherwise the Google card does not exist and nothing below applies.

1. Start the app: `dotnet run --project src/Hosts/Simulab.AppHost` from
   `D:\dev\_icontrol\wt\simulab\feature-29`. Close any other app host first — the ports collide.
2. Sign in and open **My account → Security**. The page shows two cards: **Two-factor sign-in** and
   **Google**, the second with **Connect Google**. (AC18, AC18b: the **Security** link is on `/account`.)
3. **The one that only the running app can show.** Open
   `https://localhost:7125/account/security?error=google_link.expired` directly. The card must show *The
   attempt took too long. Nothing was connected; try again.* and the address must fall back to
   `/account/security`. Repeat with `?error=google_link.session_changed` (*The account signed in here changed
   while you were at Google.*) and with `?linked=1` (a green *Google connected.*). All three were run here on
   2026-09-27; the request answered 200, not a redirect.
4. Select **Connect Google**, choose a Google account, allow access. You come back to **Security**, which
   confirms and shows the Google address (AC1, AC5: it does not have to be your Simulab address).
5. Press the browser's Back button, then **Connect Google** again with the same Google account. It succeeds
   and nothing is duplicated (AC2). With a *different* Google account it is refused, and the first link stays
   (AC3).
6. Select **Disconnect Google**, type a wrong password: the dialog stays open with *Wrong password.* and the
   link is still there (AC13). Type the right one: the card goes back to **Connect Google** and your password
   still signs you in (AC12).
7. Sign out and select **Continue with Google** with the address you just disconnected: it is refused with
   *An account with this email address already exists.* and nothing is linked (AC16, BR11).
8. Keyboard only, from the top of the Security page: Tab to **Connect Google** and press Enter — the Google
   round trip starts. On a linked account, Tab to **Disconnect Google**, Enter, type the password, Tab to the
   confirm button, Enter. Switch the language in the app bar and check the card, the dialog and the three
   messages in pt-BR and pt-PT.

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-29
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: `docs/manual/{en,pt-BR,pt-PT}/security-google.md` (new) and `google-sign-in.md` (corrected)
