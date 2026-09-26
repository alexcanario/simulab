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
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <Rule>.

## Screens and API
<!-- Routes, main components, endpoints (always /api/v1/...), error codes. -->
- <Route> — <purpose>
- <METHOD> /api/v1/<resource> — <purpose>
- Error codes: `<area>.<error>`

## Acceptance criteria
<!-- Given / When / Then. Each one is covered by a test. Always include the localization criterion. -->
- AC1 Given <context>, when <action>, then <result>.
- AC<n> All new texts appear in pt-BR, pt-PT and en.

## Decisions
<!-- date — decision — reason. Technical decisions made by Claude are recorded here too. -->
- <YYYY-MM-DD> — <decision> — <reason>

## Out of scope
- <Item>

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
