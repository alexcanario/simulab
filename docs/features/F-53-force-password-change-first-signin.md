---
feature: F-53
epic: Foundation and identity
status: validating
board: 90
version: 1
---
# Force a password change at first sign-in

Technical terms: [glossary](../glossary.md)

## Summary
Let an account be marked "must change password", and block sign-in from doing anything else until the owner of the account picks a new password. Split out of F-52 (seed admin user). In v1 only the seeded administrator is marked, when the seed creates it outside development: whoever configured the seed secret knows that password, and `admin@simulab.local` cannot receive the password reset email. It needs a flag on the user, one more step in the sign-in flow, and a migration. No such flag exists today (checked in the code on 2026-10-06).

## Start
- Depends on: F-5 (sign-in, done); F-52 (seed admin, done); F-11 (the two-factor step is the pattern this step follows, done).
- Waits on (to start): nothing.
- Needed to validate: the worktree's own database (fresh, so the seed creates the account) and the switch `Identity:SeedAdmin:RequirePasswordChange` set to `true` for that run — Claude prepares both; the owner follows the script.
- Suggested path: `/agile:refine` → `/agile:build F-53`.
- Parallel with: any item that does not touch the Identity module or the sign-in page.

## Goal
Nobody keeps using the seeded administrator with the password an operator typed into a secret: the first person who signs in with it picks a password only they know before reaching anything else.

## Users and use cases
- UC1 An operator deploys a fresh installation with the seed password in a secret; the seed creates `admin@simulab.local` marked "must change password".
- UC2 The administrator signs in with the seed password; instead of entering the app, the sign-in page asks for a new password. After choosing one, the administrator is signed in and the mark is gone.
- UC3 The administrator signs in later with the new password and enters directly, with no extra step.
- UC4 A developer starts the app in development; the seeded account is not marked and the development password keeps working.

## Business rules
- BR1 A user has a "must change password" mark (`MustChangePassword`), false by default. Every existing account is false after the migration.
- BR2 The seed marks the account only when it creates it, and only when `Identity:SeedAdmin:RequirePasswordChange` is true. The key defaults to true; `appsettings.Development.json` sets it to false. An account that already exists is never marked by the seed (F-52 BR5 stays: an existing account's password, status and two-factor state are untouched).
- BR3 A correct password on a marked account issues no tokens: the token endpoint answers `password_change.required` with a password-change challenge that lives 5 minutes. All the checks that come before tokens today still come first, in the same order: rate limit, lockout, password, email verified, two-factor code step (when on). The step that hands out the challenge instead of tokens records no `SignInSucceeded`, does not clear the failure count and does not take the account name out of the rate-limit set: the change step does all three.
- BR4 The change step takes the challenge and the new password. It sets the new password, clears the mark, clears the failure count, renews the security stamp, revokes any other session of the account and issues tokens in the same answer: the user does not type the password again.
- BR5 The new password follows the same password policy as every account (12+ characters, upper case, digit, symbol; `password_change.password_too_weak`) and must differ from the current one (`password_change.same_as_current`) — the codes the account-page change already returns.
- BR6 The password-change challenge is its own kind: a two-factor challenge is never accepted by the change step, and a password-change challenge is never accepted by the code step (separate stores or key prefixes). It is bound to the account and its security stamp. While the input is checked it is read, not spent; a refused new password (policy or same as current) keeps it, so the user can try again until it expires. Before any write it is spent atomically; a request that loses that race, or a challenge that is expired, unknown, already used, of the other kind, or whose account is gone, no longer Active, no longer marked or has a different security stamp (for example after a reset by email) answers `password_change.challenge_invalid`, records `SignInFailed` with reason `ChallengeInvalid`, and the page goes back to the password step.
- BR7 The change step follows the F-38 per-address limit like the code step: an address at its limit is refused (`identity.sign_in_rate_limited`) before the challenge is read; the account name is counted once the challenge is known; success takes it out.
- BR8 Any successful password set clears the mark: the change step, the password reset by email (F-7) and the change on the account page.
- BR9 Account events: the change step records `PasswordChanged`, then `SignInSucceeded` with the method of the last factor the user proved — `Password`, or `TotpCode` / `RecoveryCode` when the two-factor step came before (the challenge carries it). Exactly one `SignInSucceeded` per sign-in. No password-change notice email is sent for this step: the user is doing it in the same sign-in, and the only marked account's address cannot receive mail.
- BR10 The `password_change` grant is registered whatever the two-factor switch says: the seed switch defaults to true outside development, and without the grant the seeded administrator could never sign in.

## Screens and API
- `/sign-in` — a third state of the existing page, after the password (and two-factor) step: title "Choose a new password", one line saying why, "New password" and "Confirm new password" fields (`autocomplete="new-password"`), the existing password-policy hint, and one primary button. The state has its own challenge field (separate from the two-factor one) and its own error area, since today's error area is drawn only in the password state. It is reached from the password step and from the code step (including the code step that follows Google), and the Web auth client gets a "needs password change" result next to the two-factor one. Same layout and kit components as the two-factor state and the reset password page; no separate mockup.
- `POST /connect/token`, `grant_type=password` or `grant_type=totp` — on a marked account answers 400 with `error=password_change.required`, `challenge` and `expires_in` (as `mfa_required` does in F-11).
- `POST /connect/token`, `grant_type=password_change` (new custom grant on the `simulab-web` client, always on — BR10), parameters `challenge` and `new_password` — tokens on success.
- Error codes: new `password_change.required`, `password_change.challenge_invalid`; reused `password_change.password_too_weak`, `password_change.same_as_current`, `identity.sign_in_rate_limited`.
- Configuration: `Identity:SeedAdmin:RequirePasswordChange` (bool, default true).
- Schema: column `must_change_password boolean not null default false` on the Identity users table, with its column description.

## Acceptance criteria
- AC1 Given the migration runs on a database with accounts, then every account has `must_change_password = false`. (BR1)
- AC2 Given an empty database, the seed password configured and the switch set to true explicitly, when the seed runs, then `admin@simulab.local` is created marked. (BR2)
- AC3 Given the switch false, when the seed creates the account, then it is not marked. (BR2, UC4)
- AC4 Given the seeded account already exists unmarked, when the seed runs with the switch true, then it stays unmarked. (BR2)
- AC5 Given the Development environment settings, when the configuration is read, then `Identity:SeedAdmin:RequirePasswordChange` is false; given no value, the seed treats it as true. (BR2) Every seed test sets the switch explicitly: the integration hosts run as Development.
- AC6 Given a marked account, when it signs in with the right password, then the token endpoint answers 400 `password_change.required` with a challenge and no tokens, records no `SignInSucceeded`, and keeps the failure count. (BR3)
- AC7 Given a marked account that is locked out, or not verified, when it signs in with the right password, then it gets the same answer as an unmarked account (lockout, not verified) and no challenge. (BR3)
- AC8 Given a marked account with two-factor on, when it passes the password and the code steps, then the code step answers `password_change.required` with a challenge, not tokens, and records no `SignInSucceeded`. (BR3)
- AC9 Given a valid challenge and a valid new password, when the change step runs, then tokens are issued, the old password no longer signs in, the new one does, the mark and the failure count are cleared. (BR4)
- AC10 Given the change step succeeded, then the account's other sessions are revoked and the security stamp changed. (BR4)
- AC11 Given a new password equal to the current one, then the answer is `password_change.same_as_current` and the same challenge still works for a valid password. (BR5, BR6)
- AC12 Given a new password that breaks the policy, then the answer is `password_change.password_too_weak` and the same challenge still works. (BR5, BR6)
- AC13 Given a challenge that is expired, unknown or already used, or whose account was reset by email, is no longer Active or no longer marked, then the answer is `password_change.challenge_invalid`, a `SignInFailed` with reason `ChallengeInvalid` is recorded and the password is not changed. (BR6)
- AC14 Given a two-factor challenge sent to `password_change`, then the answer is `password_change.challenge_invalid`; given a password-change challenge sent to `totp`, then the answer is `totp.challenge_invalid`. (BR6)
- AC15 Given two concurrent change requests with the same valid challenge, then exactly one succeeds and the other answers `password_change.challenge_invalid`; the account has one password set and one new session. (BR6)
- AC16 Given an address at its sign-in limit, when the change step runs, then it answers `identity.sign_in_rate_limited` before the challenge is read, the challenge is not spent and the password is not changed. (BR7)
- AC17 Given an account marked directly by the test, when the password is reset by email or changed on the account page, then the mark is cleared. (BR8; a marked account cannot reach the account page through sign-in, so the test sets the mark itself)
- AC18 Given a successful change step after the password step, then the account events are `PasswordChanged` then one `SignInSucceeded` with method `Password`; after a code step, one `SignInSucceeded` with method `TotpCode` (or `RecoveryCode`); no notice email is queued. (BR9)
- AC19 Given two-factor off and the seed switch on, when a marked account signs in, then `grant_type=password_change` is accepted (not `unsupported_grant_type`). (BR10)
- AC20 Given a marked account on the sign-in page, when the right password is typed — directly, or followed by a valid two-factor code — then the "Choose a new password" state appears; a weak or unchanged password shows its message in that state; after a valid new password the user lands signed in on the home page; an expired challenge brings back the password step with its message. (UC2, screen; also in the validation script)
- AC21 All new texts appear in pt-BR, pt-PT and en, and the missing-key and error-code-text tests are green.

## Decisions
- 2026-10-06 — Only the seeded administrator is marked in v1; an admin action to mark any user in the back office is F-92 (idea) — owner, question 1. It is the only account created by someone other than its owner today.
- 2026-10-06 — The seed does not mark the account in development (switch off in `appsettings.Development.json`) — owner, question 2. Otherwise the development password stops working after the first sign-in and every validation script breaks.
- 2026-10-06 — Only an account created by the seed after this item is marked; an existing one is left alone, no data migration — owner, question 3. Keeps F-52 BR5; no staging or production installation exists yet.
- 2026-10-06 — After the new password the user is signed in directly; the new password must differ from the current one; a password reset by email also clears the mark — owner, question 4.
- 2026-10-06 — Technical: the step follows the two-factor pattern of F-11 (403 with a challenge, a custom grant on `/connect/token`, challenge kept in Redis like the two-factor one). Tokens are never issued before the change, so no other endpoint or page needs a guard. `/connect/token` stays outside `/api/v1/` as every grant of F-5 and F-11.
- 2026-10-06 — Technical: the challenge is spent only when the new password is accepted (BR6): it proves the password step already passed, and spending it on a policy failure would make the user type the old password again for a typo in the new one. The retries are bounded by the 5-minute lifetime (the per-address limit counts account names, so repeated tries on one account count once); whoever holds the challenge already proved the password.
- 2026-10-06 — Independent review of this file (reviewer agent, before approval), all findings checked in the code: (1) major, a two-factor challenge could stand in for a password-change one → separate kinds, BR6, AC14; (2) major, "spent only on success" raced → read, then atomic spend before writes, BR6, AC15; (3) major, a challenge stayed valid after a reset by email → bound to account state and security stamp, BR6, AC13; (4) major, double `SignInSucceeded` on the two-factor path → BR3, BR9, AC8, AC18; (5) major, error codes broke the per-flow pattern → `password_change.*`, BR5; (6) major, the sign-in page has no error area in the challenge state and drops unknown codes in the code step → Screens and API, AC20; minors fixed: 400 not 403, real rate-limit code and order (BR7, AC16), grant always registered (BR10, AC19), test sets the mark directly (AC17), password step keeps count and events (BR3), seed tests set the switch (AC5); the rate-limit reasoning above corrected.
- 2026-10-06 — Technical: the step comes after the two-factor step, so a marked account with two-factor still proves both factors first.
- 2026-10-06 — Technical: Google sign-in is unchanged. The only marked account is `admin@simulab.local`, which cannot have Google linked before its first sign-in; the back-office idea must revisit this.
- 2026-10-06 — Technical: no notice email for the forced change (BR9).
- 2026-10-06 — Technical: the screen is a state of the existing sign-in page built from the same kit components as the two-factor state; no `/agile:screen` mockup.
- 2026-10-06 — New packages: none (production and tests).
- 2026-10-06 — The board id in this file was 779; the real issue is #90 — corrected.
- 2026-10-06 — Build design passes (`system-design`, then `architect`, both read-only; the main session checked `TokenEndpoints.cs:237`, `SecondFactor.cs:42` and the handlers in the files). Accepted: everything in the Identity module, own Redis key prefix with peek then atomic spend, handler order of BR6/BR7, grant always registered, the mark cleared in `ResetPasswordHandler` and `ChangePasswordHandler` before their password call (one UPDATE, BR8/AC17), the race loser records `SignInFailed(ChallengeInvalid)`, a failure of `ResetPasswordAsync` after the rules passed is a server error (as the reset handler), the column default carried by the migration only, the glossary row for "must change password". Dropped: `HasDefaultValue(false)` in the model (no other bool of `users` has one).
- 2026-10-06 — BR3, name in the rate-limit set: honoured as written. The password step takes the account name out of the set only when the account is not marked (`TokenEndpoints.cs:237`); the change step counts and clears it (BR7).
- 2026-10-06 — Independent review of the built branch (reviewer agent, `/agile:review`), checked in the code: (1) major, confirmed — the "unknown challenge" test asserted nothing about the `ChallengeInvalid` event and the expired / not marked / not active / reset cases only the code: each now asserts exactly one event, with the account or none (an expired or unknown challenge names none); (2) minor, confirmed — a marked account that is not verified kept its name in the address's set: the name is now released whenever the account is not active, with a test; (3) minor, confirmed — the locked-out test now asserts no challenge; (4) minor, recorded — the refresh grant and the Google grant do not look at the mark. Not a bypass in v1 (the only marked account has no session and no Google link) but F-92 makes "session exists and marked" reachable: the guard on both belongs to F-92 before any admin can mark an account; (5) minor — the manual page was updated in three languages.
- 2026-10-06 — BR3, failure count on a marked account with two-factor: NOT as written. `SecondFactor.VerifyAsync` clears the count on a right code (F-11 BR10) and `TotpAccountHandler` shares it; the code step therefore clears it before the challenge is handed out. After that step nothing is left to guess, so the rule behind BR3 does not bite. The password-only path keeps the count (AC6). Change note proposed, waiting for the owner (see `## Change notes`).

## Out of scope
- An admin action in the back office to mark any user "must change password" (F-92, idea).
- Marking an existing seeded account, or any account other than the seeded administrator.
- Password expiry or periodic rotation.
- Showing the mark in the back office user list.

## Open questions
- (none)

## Change notes
- v1 note 1 (accepted by the owner 2026-10-06, with the validation): BR3 says the step that hands out the password-change challenge "does not clear the failure count". On the two-factor path the right code has already cleared it (F-11 BR10, shared `SecondFactor.VerifyAsync`). Proposal: BR3 keeps its wording for the password step and reads "after the two-factor code step the count is already clear". Affects BR3 only; no acceptance criterion changes (AC6 is password-only, AC8 asserts the events).

## Validation script
Needed to validate: a database of its own and the switch `Identity:SeedAdmin:RequirePasswordChange` on for the run — both are in the start command below (Claude prepared them; nothing else is needed). Sign-in is `admin@simulab.local` with the development password of `appsettings.Development.json` (`Admin@Simulab123!`); a new password to type: `Nova#Senha2026!`.

1. **Start the app host** from the F-53 worktree, with every other app host stopped first. The first start creates the database and the seed marks the administrator. Expected: the dashboard opens at `https://localhost:17162` with `api` and `web` Running; the sign-in page is the Web URL the dashboard shows (usually `https://localhost:7125/sign-in`).
   - Git Bash: `cd /d/wt/simulab/f-53-force-password && Database__Name=simulab_f53_val Identity__SeedAdmin__RequirePasswordChange=true dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https`
   - PowerShell 7: `Set-Location D:\wt\simulab\f-53-force-password; $env:Database__Name = "simulab_f53_val"; $env:Identity__SeedAdmin__RequirePasswordChange = "true"; dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https`
   - To repeat the whole script, stop the app host and start it again with another database name (`simulab_f53_val2`): the first run changes the password.
2. Sign in as `admin@simulab.local`. Expected: the page shows **Choose a new password** with the two fields and a hint about 12 characters, not the home page.
3. Language: select **Cancel** (back to the password), switch the language to Português (Brasil) with the globe, sign in again. Expected: **Escolha uma nova senha**, the fields and **Salvar e entrar** in Portuguese. Switch back to English.
4. Keyboard only, from the step: Tab to the first field, type `abc`, Tab, type `abc`, Enter. Expected: the message *This password does not follow the rules above.* in the step, which stays. Then use the current password in both fields. Expected: *Choose a password different from the current one.*
5. Keyboard only: type `Nova#Senha2026!` in both fields and press Enter. Expected: you land on the home page signed in, with no second password prompt.
6. Sign out, then sign in with `Nova#Senha2026!`. Expected: straight to the home page, no extra step. Sign out and try `Admin@Simulab123!`. Expected: *Incorrect email or password.*
7. Stop the app host (Ctrl+C in its terminal).

Not shown on screen, covered by tests: AC1 to AC19 (migration, seed, token endpoint, challenge rules, race, limits, events). Not checked on screen by Claude: the browser pane was hidden (no viewport), so contrast and layout of the new step are for step 2 to 5; Claude ran the flow in the real app host by script and saw the step, the new password and the old one refused.

## Delivery
