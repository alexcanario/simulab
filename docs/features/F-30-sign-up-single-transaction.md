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
- `RegisterGoogleUserHandler` (`.../GoogleSignIn/RegisterGoogleUserHandler.cs`) writes four times when it creates
  an account (`CreateAsync`, `AddToRoleAsync`, `AddLoginAsync`, `consentStore.AddAsync`) and three times when it
  takes over a pending one (`AddLoginAsync`, `RemovePasswordAsync`, `consentStore.AddAsync`).
- So the premise of the summary holds in today's code: a failure between two of those saves leaves an account
  without its role, its consent record or its Google link, and nothing rolls it back.
- **One transaction can cover all of it.** `IdentityModule` registers
  `UserStore<User, Role, IdentityModuleDbContext, Guid>` and `AddJobQueueFor<IdentityModuleDbContext>`
  (`IdentityModule.cs:78,96`), so `UserManager`, the stores and the email job all write through the same scoped
  `IdentityModuleDbContext`. An explicit transaction on that context enlists every `SaveChangesAsync` under it.
- **The pattern already exists in this module**: `RoleAdministrationStore.RunExclusiveAsync`
  (`.../Persistence/RoleAdministrationStore.cs:21-40`) opens a transaction, commits on a successful `Result`,
  rolls back and clears the change tracker otherwise. The Application layer sees only the abstraction, never EF.
- `IIdentityUnitOfWork` (`.../Abstractions/IIdentityUnitOfWork.cs`) exposes `SaveChangesAsync` only; three
  handlers use it today (`ExportDataHandler`, `ChangePasswordHandler`, `ResetPasswordHandler`) plus the helper
  `PasswordNotice`.
- Two results are **not checked** in the sign-up path: `AddToRoleAsync` in both handlers. A role that fails to
  attach is silent today, and the visitor gets a successful sign-up with no role. Checking the result is not the
  whole story: `UserManager.AddToRoleAsync` **throws** `InvalidOperationException` when the role does not exist and
  only returns a failed `IdentityResult` for a case a new account cannot reach (already in the role).
- **A duplicate the lookup misses does not arrive as an `IdentityResult` today.** `UserManager` validates
  uniqueness with its own query, so `FromIdentityErrors` (`RegisterUserHandler.cs:116-126`) only ever sees a
  `Duplicate*` code for an address that existed at validation time. An address inserted after that check hits
  `ux_users_tenant_normalized_email` (`UserConfiguration.cs:64-71`) and surfaces as `DbUpdateException` /
  PostgreSQL 23505. **Nothing in `Simulab.Identity` catches it** — the visitor gets the host's 500, not the F-4 BR4
  success. Same for a Google subject linked between the lookup and the insert: the `user_logins` key.
- **The translation this needs already exists in another module**: `OrganizerUniqueViolations.Translate`
  (`src/Modules/Catalog/Simulab.Catalog.Infrastructure/Persistence/OrganizerUniqueViolations.cs`, from B-14) turns
  a named unique index into the handler's own error and leaves every other database error to propagate.
- The real error codes are `registration.email_invalid`, `registration.password_too_weak`
  (`IdentityErrorCodes.cs:12-13`) and `google_sign_in.account_exists` (`:98`) — not `identity.*`.
- The sign-up records no account event (F-21), so nothing audit-related enters the transaction. The audit and
  soft-delete interceptor runs per `SaveChangesAsync` and is unaffected; `TenantId` is dormant and its unique index
  is `NULLS NOT DISTINCT`.
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
- BR4 No write in the sign-up path fails silently. `AddToRoleAsync` is the one that does today: its result is
  checked, and because a missing role makes it throw instead, either way the transaction rolls back and no account
  is committed. The caller sees what the host already answers for an unexpected failure (problem details, 500); no
  new error code is invented for it.
- BR5 The answers the caller sees do not change. An address already registered still gets the same success as a
  new account (F-4 BR4) and still writes nothing; a subject already linked still gets
  `google_sign_in.account_exists`; a weak password still gets `registration.password_too_weak`.
- BR6 A duplicate that the lookup misses is answered the same way as one it finds. The transaction rolls back and
  the unique violation is translated by index name, the way B-14 does it for the Catalog: the users index gives the
  F-4 BR4 success, the `user_logins` key gives `google_sign_in.account_exists`. Any other database error propagates.
- BR6b One transaction widens that window instead of closing it: nothing this request writes is visible to a
  concurrent one until the commit, so the loser of a race now always arrives at the index, never at the lookup.
  BR6 is what makes that safe, and the comment in `RegisterGoogleUserHandler.TakeOverAsync` ("the link comes first,
  so a race lost here changes nothing") stops being true and is rewritten with it.
- BR7 The email job goes with the account. A verification link never exists without the email that carries it
  (F-13 BR2), and now the reverse too: no email is queued for an account that was not committed.
- BR8 The Application layer never sees EF: the transaction is opened through an abstraction of
  `Simulab.Identity.Application.Abstractions`, as `IRoleAdministrationStore.RunExclusiveAsync` already does.
- BR9 The transaction holds writes only. It opens after every validation, every lookup and every outbound call —
  the Google ID-token check especially — so no PostgreSQL transaction stays open across the network. The password
  hashing inside `CreateAsync` is a write and stays in.

## Screens and API
No screen, no route, no error code changes. The two endpoints keep their contracts:
- `POST /api/v1/identity/registrations` — password sign-up (F-4).
- `POST /api/v1/identity/google-registrations` — Google sign-up confirmation (F-20).
- Error codes: unchanged — `registration.email_invalid`, `registration.password_too_weak`,
  `google_sign_in.account_exists` and the terms codes of `RegistrationTerms`.

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
- AC7 Given the Student role missing, when a password sign-up runs, then it fails (the throw is not swallowed), no
  user, consent record, token or email job is committed, and the caller gets the host's problem-details failure.
- AC8 Given a user row inserted at the same address after the handler's lookup, when the insert hits
  `ux_users_tenant_normalized_email`, then the transaction rolls back, nothing of this request is written, and the
  caller gets the same success as for a new account (F-4 BR4).
- AC8b Given a `user_logins` row for the same Google subject inserted after the handler's lookup, when the insert
  hits that key, then the transaction rolls back and the caller gets `google_sign_in.account_exists`.
- AC8c Given any other database error, when it is raised, then it propagates unchanged: the translation answers for
  the two named indexes only.
- AC9 Given a rolled-back sign-up, when the same request writes again afterwards, then nothing staged before the
  rollback is written: the change tracker was cleared.
- AC10 Given the solution, when the architecture tests run, then `Simulab.Identity.Application` still references no
  EF Core type — the transaction reaches it only through its own abstraction (BR8).
- AC11 Given the Google sign-up, when the transaction opens, then the Google ID-token validation has already
  finished: no outbound call happens with a transaction open (BR9).
- AC12 No new UI text: the localization criterion does not apply, and the missing-key test stays green.

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
- 2026-09-26 — the failures are injected through what the test can really reach: a store double that throws for the
  consent record and the token (AC1, AC2, AC4, AC5), the Student role removed for AC7, and a row inserted by a
  second connection between the lookup and the insert for AC8 and AC8b. Never by breaking the database, so
  everything runs on the same Postgres container as the rest of the Identity tests.
- 2026-09-26 — no new package.

### Independent review of this file (2026-09-26, before approval)
The `reviewer` agent read the file against the code; every blocker and major was checked here before being
accepted, and the findings are folded into the rules and criteria above.
- blocker, confirmed — BR6/AC8 as first written was not achievable: a duplicate the lookup misses arrives as
  `DbUpdateException` 23505, never as an `IdentityResult`, and nothing in the module catches it. Now BR6 with the
  B-14 translation by index name, plus AC8, AC8b and AC8c.
- blocker, confirmed — one transaction widens that race instead of closing it, and the Google take-over comment
  that relies on an immediately committed link stops being true. Now BR6b.
- major, confirmed — `AddToRoleAsync` throws for a missing role and the handlers hold the concrete `UserManager`,
  so "check the result" and "a store double that throws" did not reach AC7. Now BR4 and AC7, injected by removing
  the Student role.
- major, confirmed — AC7 did not say what the caller sees, while BR5 and the scope forbid a new error code. Now
  BR4: the host's existing problem-details failure, nothing new.
- major, confirmed — the error codes were written as `identity.*`; they are `registration.*` and
  `google_sign_in.account_exists`. Fixed in "What exists", BR5 and the API section.
- major, confirmed — the transaction boundary was unsaid, which read as holding one open across the Google call.
  Now BR9 and AC11.
- minor, confirmed — BR8 and BR3 had no criterion: now AC10 and AC9.
- minor, confirmed — the write counts (four and three, not "up to five"), "three handlers and one helper", and
  three line references were wrong. Fixed.
- Nothing was rejected. The reviewer did not check the 13 handlers of F-47, the unmerged F-29 branch,
  `RegistrationTerms.CheckAsync`, the worker's polling SQL, or whether AC1-AC5 can share one API factory per test
  class while a scoped store throws — the last one is a build question, not a premise.

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
