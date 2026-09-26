---
feature: F-30
epic: Foundation and identity
status: refining
board: 749
version: 1
---
# Sign-up writes in one transaction

## Summary
Both sign-ups write the account in several separate saves: the password sign-up (`RegisterUserHandler`: user, role, consent record, verification token) and the Google sign-up (`RegisterGoogleUserHandler`: user, role, Google link, consent record). A failure between two saves leaves an account without its consent record or its link. Found by the independent review of F-20 (finding 4) on 2026-09-23; accepted there because the password sign-up has had the same shape since F-4.

## Start
- Depends on: nothing that is not done. F-4 (password sign-up), F-13 (jobs on the module's unit of work) and
  F-20 (Google sign-up) are all `done` and on `main`.
- Waits on: **the merge of F-29** (link and unlink Google, `building` in the worktree `feature-29` on
  2026-09-26). F-29 changes `GoogleSignInHandler`, `GoogleAccountEndpoints` and the link row, and stores the
  Google address where nothing stores it today; `RegisterGoogleUserHandler` calls
  `GoogleSignInHandler.Login(google)` to build that row. Refining is safe now; the build of the Google half
  starts after F-29 lands. The owner's session that runs F-29 provides it.
- Suggested path: `/agile:refine` → `/agile:build` once F-29 is merged. No screen, no new route.
- Parallel with: anything outside `Simulab.Identity`.

## What exists (verified 2026-09-26, on `main` at `9125cdd`)
- `RegisterUserHandler` (`src/Modules/Identity/Simulab.Identity.Application/Registration/RegisterUserHandler.cs`)
  writes four times, each one its own committed transaction: `userManager.CreateAsync` (user), `AddToRoleAsync`
  (the Student role), `consentStore.AddAsync` (consent record, which calls `SaveChangesAsync` itself) and
  `tokenStore.AddAsync` (verification token, plus the staged email job).
- `RegisterGoogleUserHandler` (`.../GoogleSignIn/RegisterGoogleUserHandler.cs`) writes up to five times:
  `CreateAsync` **or** `AddLoginAsync` + `RemovePasswordAsync` on take-over, `AddToRoleAsync`, `AddLoginAsync`,
  `consentStore.AddAsync`.
- So the premise of the summary holds in today's code: a failure between two of those saves leaves an account
  without its role, its consent record or its Google link, and nothing rolls it back.
- **One transaction can cover all of it.** `IdentityModule` registers
  `UserStore<User, Role, IdentityModuleDbContext, Guid>` and `AddJobQueueFor<IdentityModuleDbContext>`
  (`IdentityModule.cs:78,95`), so `UserManager`, the stores and the email job all write through the same scoped
  `IdentityModuleDbContext`. An explicit transaction on that context enlists every `SaveChangesAsync` under it.
- **The pattern already exists in this module**: `RoleAdministrationStore.RunExclusiveAsync`
  (`.../Persistence/RoleAdministrationStore.cs:21-39`) opens a transaction, commits on a successful `Result`,
  rolls back and clears the change tracker otherwise. The Application layer sees only the abstraction, never EF.
- `IIdentityUnitOfWork` (`.../Abstractions/IIdentityUnitOfWork.cs`) exposes `SaveChangesAsync` only; four
  handlers use it today.
- Two results are **not checked** in the sign-up path: `AddToRoleAsync` in both handlers. A role that fails to
  attach is silent today, and the visitor gets a successful sign-up with no role.
- The same multi-write shape is not only here: `grep` finds 13 handlers in this module with more than one write
  in a request, and `ResetPasswordHandler` was read to confirm it (five writes: consume the token, reset the
  password, reset the failure count, clear the lockout, update the user). The other ten are candidates, not
  verified.
- Nothing about this is visible on screen: no route, no field, no message changes. `POST /api/v1/identity/registrations`
  and `POST /api/v1/identity/google-registrations` keep their contracts and their error codes.

## Goal
An account that is created is created whole: user, role, consent record, verification token or Google link, and
the email job, all committed together or not at all. A failure halfway leaves no trace instead of an account
nobody can explain.

## Users and use cases
- UC1 A visitor signs up with a password and gets an account with its role, its consent record and its
  verification email, or no account at all — never a half one.
- UC2 A visitor signs up with Google and gets an account with its role, its consent record and its Google link,
  or no account at all; a pending account taken over keeps every change of the take-over or none.
- UC3 An administrator looking at an account never finds one without a role or without a consent record, so the
  data export (F-16) and the audit trail (F-21) have the rows they promise.

## Business rules
- BR1 The password sign-up commits once: the user, the Student role, the consent record, the verification token
  and the staged verification email are written in one transaction, or nothing is.
- BR2 The Google sign-up commits once, both ways: creating an account (user, role, link, consent record) and
  taking over a pending one (link, verified email, name, 18+ declaration, password removal, consent record).
- BR3 A failure anywhere inside the sign-up rolls the transaction back and leaves the change tracker clean, so a
  later write in the same request cannot resurrect a staged row.
- BR4 The result of every write in the sign-up path is checked, `AddToRoleAsync` included: a role that does not
  attach fails the sign-up instead of committing an account without a role.
- BR5 The answers the caller sees do not change. An address already registered still gets the same success as a
  new account (F-4 BR4) and still writes nothing; a subject already linked still gets
  `identity.google_account_exists`; a weak password still gets `identity.password_too_weak`.
- BR6 A duplicate address that appears between the lookup and the insert rolls back and answers with that same
  success: there was no account of this visitor's before and there is none after.
- BR7 The email job goes with the account. A verification link never exists without the email that carries it
  (F-13 BR2), and now the reverse too: no email is queued for an account that was not committed.
- BR8 The Application layer never sees EF: the transaction is opened through an abstraction of
  `Simulab.Identity.Application.Abstractions`, as `IRoleAdministrationStore.RunExclusiveAsync` already does.

## Screens and API
No screen, no route, no error code changes. The two endpoints keep their contracts:
- `POST /api/v1/identity/registrations` — password sign-up (F-4).
- `POST /api/v1/identity/google-registrations` — Google sign-up confirmation (F-20).
- Error codes: unchanged — `identity.email_invalid`, `identity.password_too_weak`,
  `identity.google_account_exists` and the terms codes of `RegistrationTerms`.

## Acceptance criteria
- AC1 Given a password sign-up whose consent record write fails, when the request ends, then no user, no role,
  no consent record, no verification token and no email job exist for that address.
- AC2 Given a password sign-up whose verification token write fails, when the request ends, then nothing exists
  for that address — in particular no user Identity had already created.
- AC3 Given a successful password sign-up, when the request ends, then the user, the Student role, the consent
  record, the verification token and one email job are all present, and the caller gets the same success as today.
- AC4 Given a Google sign-up that creates an account and whose consent record write fails, when the request ends,
  then no user, no role and no Google link exist.
- AC5 Given a Google sign-up that takes over a pending account and fails after the link, when the request ends,
  then the account is still pending, still has its password, its old name and its old 18+ declaration, and no link.
- AC6 Given a successful Google sign-up of each kind, when the request ends, then the account is active, without a
  password, linked, with its role and its consent record, and no verification email was queued.
- AC7 Given `AddToRoleAsync` failing, when the sign-up runs, then the request fails, nothing is committed, and the
  failure is not silent.
- AC8 Given an address registered by someone else between the lookup and the insert, when the sign-up runs, then
  the caller gets the same success as for a new account and nothing was written.
- AC9 No new UI text: the localization criterion does not apply, and the missing-key test stays green.

## Decisions
- 2026-09-26 — both sign-ups are in this item, and the build of the Google half waits for the merge of F-29
  (owner) — the guarantee and its tests are the same on both paths; F-29 is `building` and changes
  `GoogleSignInHandler.Login`, which `RegisterGoogleUserHandler` calls.
- 2026-09-26 — the ignored `AddToRoleAsync` results are checked in this item (owner) — a transaction that commits
  an account without a role would meet BR1 and still break the promise.
- 2026-09-26 — the other handlers with the same shape stay out and go to their own item (owner) — 13 handlers in
  this module write more than once per request; `ResetPasswordHandler` was read and confirmed (five writes).
  Captured as `docs/features/F-47-one-transaction-per-identity-handler.md`.
- 2026-09-26 — the transaction is exposed as `IIdentityUnitOfWork.BeginAsync` returning an
  `IIdentityTransaction : IAsyncDisposable` with `CommitAsync`, and disposal without a commit rolls back and
  clears the change tracker — the handler decides, which the duplicate-race path (BR6) needs: it returns a
  success while committing nothing. A `RunInTransactionAsync(Func<Task<Result>>)` wrapper like
  `RunExclusiveAsync` was rejected for exactly that case, where the result is success and the write must not land.
- 2026-09-26 — no execution-strategy work is needed: nothing in `src/` calls `EnableRetryOnFailure`, so a
  user-initiated transaction is allowed; `RoleAdministrationStore` already opens one in production.
- 2026-09-26 — the failure of each write is injected in the tests through the module's own abstractions (a store
  double that throws), not by breaking the database, so AC1, AC2, AC4 and AC5 run on the same Postgres container
  as the rest of the Identity tests.
- 2026-09-26 — no new package.

## Out of scope
- The other 11 handlers with more than one write per request (F-47).
- Any change to an endpoint contract, an error code, a screen or a UI text.
- Retries, idempotency keys or an outbox: the job table already carries the email (F-13).
- Anything F-29 owns: the Google link row, `GoogleSignInHandler`, `GoogleAccountEndpoints`.

## Open questions
<!-- Approval is blocked while any line here is not answered or marked "deferred (owner, YYYY-MM-DD)". -->
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
