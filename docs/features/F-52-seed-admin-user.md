---
feature: F-52
epic: Foundation and identity
status: building
board: 778
version: 1
---
# Seed the admin user

## Summary
Seed one administrator account, `admin@simulab.local`, holding the `Admin` role, in every environment (development, test and production), so nobody has to insert the first Admin by SQL (`docs/infra.md`, F-9 BR11). The password always comes from configuration and is never a constant in code: the development password is a generated test value kept in `appsettings.Development.json`; production takes it from a secret or an environment variable, and without one nothing is seeded.

## Start
- Depends on: F-6 (roles, permissions and the Admin seed role are done); F-5 (sign-in); F-4 (email verification).
- Waits on: the owner, for the production password (a secret or environment variable, never the repository) — only when the item is deployed, it does not block the build.
- Suggested path: `/agile:build F-52`.
- Parallel with: none known.

## Goal
A fresh installation has a working Admin account from its first start, with no manual database step, and with no password known in advance in production.

## Users and use cases
- UC1 The owner starts the app in development and signs in as `admin@simulab.local` with the development password, reaching the back office.
- UC2 An operator deploys to production with the admin password in a secret; the first start creates the account with the `Admin` role.
- UC3 An operator deploys without the secret; the app starts normally and no admin account is created.
- UC4 The app restarts; the seed changes nothing (idempotent).

## Business rules
- BR1 The account is `admin@simulab.local`, display name "Administrator", status Active, email verified, two-factor off, holding the `Admin` role.
- BR2 "Unrestricted" means the `Admin` role only: F-6 already grants it every declared permission on each start, including permissions added by later modules. There is no superuser bypass and no check on the role name.
- BR3 The password is read from `Identity:SeedAdmin:Password`. When it is missing or empty, nothing is seeded and no error is raised. It is never logged.
- BR4 The password must satisfy the same password policy as any user (12+ characters, upper case, digit, symbol). A value that does not satisfy it fails the start with a clear message that never includes the value.
- BR5 Idempotent, with a before-and-after rule: if the account already exists, its password, status, email verification and two-factor state are left untouched, and it is only given the `Admin` role when it does not hold it. There was one admin account before and there is one after.
- BR6 The seed runs after the roles and permissions are seeded, and it runs in every environment when the password is configured, independently of `Database:ApplyMigrationsOnStart`.
- BR7 Development: `appsettings.Development.json` carries a generated test password. Production: no password in any committed file.

## Screens and API
- No new screen and no new endpoint. The account signs in through the existing sign-in page and token endpoint (F-5).
- Error codes: none new. A password that fails BR4 stops the start with an exception message, not an API error.
- Configuration keys: `Identity:SeedAdmin:Password` (the email is fixed by BR1).

## Acceptance criteria
- AC1 Given a configured valid password and an empty database, when the host starts, then `admin@simulab.local` exists, Active, email verified, without two-factor, holding the `Admin` role.
- AC2 Given the seeded account, when it signs in with the configured password through the token endpoint, then it receives tokens.
- AC3 Given the seeded account, when it calls an endpoint guarded by `identity.roles.manage`, then it is allowed.
- AC4 Given a permission declared by a module, when the host starts, then the seeded account holds it through the `Admin` role (no separate rule).
- AC5 Given no configured password, when the host starts, then no admin account is created and the host starts normally.
- AC6 Given the account already exists with a different password and without the `Admin` role, when the host starts, then its password is unchanged and it now holds `Admin`.
- AC7 Given the seed ran once, when the host starts again, then there is still exactly one account with that email and no change to it.
- AC8 Given a password that breaks the policy, when the host starts, then the start fails with a message that does not contain the password.
- AC9 Given production settings with `Database:ApplyMigrationsOnStart` off and a configured password, when the host starts, then the roles exist and the admin account is seeded.
- AC10 Given the Development environment, when the host starts with `appsettings.Development.json`, then the seed runs with the password kept there.
- AC11 The password never appears in the log output of the seed.
- AC12 No new UI text is added; the missing-key test stays green in pt-BR, pt-PT and en.

## Decisions
- 2026-09-29 — Production password comes only from a secret or environment variable; without it nothing is seeded — owner, question 1. A fixed, known password in production was rejected.
- 2026-09-29 — Forced password change at first sign-in is not part of this item — owner, question 2. No "must change password" flag exists today (verified in the code); it touches sign-in and the schema. Captured as its own idea.
- 2026-09-29 — Unrestricted access = the `Admin` role, nothing more — owner, question 3. F-6 grants Admin every declared permission on every start; ADR-0001 decision 14 forbids checking role names.
- 2026-09-29 — The account is created with email verified and no two-factor — owner, question 4. The `.local` domain cannot receive the verification email; two-factor is off in v1 (F-11).
- 2026-09-29 — The development password is a value generated by Claude and kept in `appsettings.Development.json`, next to the OpenIddict development secret already there — owner, follow-up. The password given earlier in chat is not available to this session. It is a development-only value; the owner sees it in the validation script.
- 2026-09-29 — Technical: the seed is its own step in the Api start-up, after `EnsureRolesAndPermissionsAsync`, and does not depend on `ApplyMigrationsOnStart` — verified in `src/Hosts/Simulab.Api/Program.cs`: today the roles seed lives inside that block, which is off in a release, so a release would have no `Admin` role to give. The step makes sure roles and permissions are seeded (the call is idempotent) before creating the account, and only when a password is configured.
- 2026-09-29 — Technical: the account is created through `UserManager` and the existing `User` entity (`VerifyEmail`), so the password policy and hashing are the normal ones; no direct SQL and no new project.
- 2026-09-29 — No new package and no migration: the item uses existing tables and services.

## Out of scope
- Forced password change at first sign-in (its own idea).
- Turning on TOTP for the seeded account.
- Rotating or resetting the admin password through the back office (F-7 password reset already covers it once the account has a reachable email).
- Any change to the permission model or to the back office screens.

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
