---
feature: F-53
epic: Foundation and identity
status: refining
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
- BR3 A correct password on a marked account issues no tokens: the token endpoint answers `identity.password_change_required` with a single-purpose challenge that lives 5 minutes. All the checks that come before tokens today still come first, in the same order: rate limit, lockout, password, email verified, two-factor step (when on).
- BR4 The change step takes the challenge and the new password. It sets the new password, clears the mark, renews the security stamp, revokes any other session of the account and issues tokens in the same answer: the user does not type the password again.
- BR5 The new password follows the same password policy as every account (12+ characters, upper case, digit, symbol) and must differ from the current one (`identity.password_unchanged`).
- BR6 A refused new password (policy or unchanged) keeps the challenge, so the user can try again until it expires. An expired, unknown or already used challenge answers `identity.password_change_challenge_invalid` and the page goes back to the password step.
- BR7 The change step counts on the per-address sign-in rate limit (F-38), like the two-factor code step.
- BR8 Any successful password set clears the mark: the change step, the password reset by email (F-7) and the change on the account page.
- BR9 Account events: the change step records `PasswordChanged`, then `SignInSucceeded` with method `Password`. No password-change notice email is sent for this step: the user is doing it in the same sign-in, and the only marked account's address cannot receive mail.

## Screens and API
- `/sign-in` — a third state of the existing page, after the password (and two-factor) step: title "Choose a new password", one line saying why, "New password" and "Confirm new password" fields (`autocomplete="new-password"`), the existing password-policy hint, and one primary button. Errors appear in the page's existing error area. Same layout and kit components as the two-factor state and the reset password page; no separate mockup.
- `POST /connect/token`, `grant_type=password` — on a marked account answers 403 with `error=identity.password_change_required`, the challenge and `expires_in`.
- `POST /connect/token`, `grant_type=password_change` (new custom grant), parameters `challenge` and `new_password` — tokens on success.
- Error codes: `identity.password_change_required`, `identity.password_change_challenge_invalid`, `identity.password_unchanged`; policy failures use the codes the password reset already returns.
- Configuration: `Identity:SeedAdmin:RequirePasswordChange` (bool, default true).
- Schema: column `must_change_password boolean not null default false` on the Identity users table, with its column description.

## Acceptance criteria
- AC1 Given the migration runs on a database with accounts, then every account has `must_change_password = false`. (BR1)
- AC2 Given an empty database, the seed password configured and the switch true, when the host starts, then `admin@simulab.local` is created marked. (BR2)
- AC3 Given the switch false, when the seed creates the account, then it is not marked. (BR2, UC4)
- AC4 Given the seeded account already exists unmarked, when the host starts with the switch true, then it stays unmarked. (BR2)
- AC5 Given the Development environment settings, when the configuration is read, then `Identity:SeedAdmin:RequirePasswordChange` is false. (BR2)
- AC6 Given a marked account, when it signs in with the right password, then the token endpoint answers 403 `identity.password_change_required` with a challenge and no tokens. (BR3)
- AC7 Given a marked account that is locked out, or not verified, when it signs in with the right password, then it gets the same answer as an unmarked account (lockout, not verified) and no challenge. (BR3)
- AC8 Given a marked account with two-factor on, when it passes the password and the code steps, then it gets the password-change challenge, not tokens. (BR3)
- AC9 Given a valid challenge and a valid new password, when the change step runs, then tokens are issued, the old password no longer signs in, the new one does, and the mark is cleared. (BR4)
- AC10 Given the change step succeeded, then the account's other sessions are revoked and the security stamp changed. (BR4)
- AC11 Given a new password equal to the current one, then the answer is `identity.password_unchanged` and the same challenge still works for a valid password. (BR5, BR6)
- AC12 Given a new password that breaks the policy, then the answer carries the policy error and the same challenge still works. (BR5, BR6)
- AC13 Given an expired, unknown or used challenge, then the answer is `identity.password_change_challenge_invalid`. (BR6)
- AC14 Given an address at its sign-in limit, when the change step runs, then it answers `sign_in_rate_limited` and the password is not changed. (BR7)
- AC15 Given a marked account, when the password is reset by email or changed on the account page, then the mark is cleared. (BR8)
- AC16 Given a successful change step, then the account events are `PasswordChanged` then `SignInSucceeded` (method `Password`), and no notice email is queued. (BR9)
- AC17 Given a marked account on the sign-in page, when the right password is typed, then the "Choose a new password" state appears; after a valid new password the user lands signed in on the home page; an expired challenge brings back the password step with its message. (UC2, screen; also in the validation script)
- AC18 All new texts appear in pt-BR, pt-PT and en, and the missing-key and error-code-text tests are green.

## Decisions
- 2026-10-06 — Only the seeded administrator is marked in v1; an admin action to mark any user in the back office is a separate idea — owner, question 1. It is the only account created by someone other than its owner today.
- 2026-10-06 — The seed does not mark the account in development (switch off in `appsettings.Development.json`) — owner, question 2. Otherwise the development password stops working after the first sign-in and every validation script breaks.
- 2026-10-06 — Only an account created by the seed after this item is marked; an existing one is left alone, no data migration — owner, question 3. Keeps F-52 BR5; no staging or production installation exists yet.
- 2026-10-06 — After the new password the user is signed in directly; the new password must differ from the current one; a password reset by email also clears the mark — owner, question 4.
- 2026-10-06 — Technical: the step follows the two-factor pattern of F-11 (403 with a challenge, a custom grant on `/connect/token`, challenge kept in Redis like the two-factor one). Tokens are never issued before the change, so no other endpoint or page needs a guard. `/connect/token` stays outside `/api/v1/` as every grant of F-5 and F-11.
- 2026-10-06 — Technical: the challenge is spent only on success (BR6): it proves the password step already passed, and spending it on a policy failure would make the user type the old password again for a typo in the new one. The rate limit (BR7) bounds the retries.
- 2026-10-06 — Technical: the step comes after the two-factor step, so a marked account with two-factor still proves both factors first.
- 2026-10-06 — Technical: Google sign-in is unchanged. The only marked account is `admin@simulab.local`, which cannot have Google linked before its first sign-in; the back-office idea must revisit this.
- 2026-10-06 — Technical: no notice email for the forced change (BR9).
- 2026-10-06 — Technical: the screen is a state of the existing sign-in page built from the same kit components as the two-factor state; no `/agile:screen` mockup.
- 2026-10-06 — New packages: none (production and tests).
- 2026-10-06 — The board id in this file was 779; the real issue is #90 — corrected.

## Out of scope
- An admin action in the back office to mark any user "must change password" (separate idea).
- Marking an existing seeded account, or any account other than the seeded administrator.
- Password expiry or periodic rotation.
- Showing the mark in the back office user list.

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
