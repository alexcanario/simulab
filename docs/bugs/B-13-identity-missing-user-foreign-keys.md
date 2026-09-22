---
bug: B-13
feature: F-6
status: validating
board: 745
severity: medium
---
# Identity tables have no foreign keys to users

## What happens
The Identity tables that point to a user or a role have no foreign key in the database. `IdentityModuleDbContext` derives from `ModuleDbContext`, not `IdentityDbContext`, and the configurations (for example `IdentityUserRoleConfiguration.cs`) declare no relationships. `user_roles`, `user_claims`, `user_logins`, `user_tokens`, `consent_records`, `email_verification_tokens` and `password_reset_tokens` keep a `user_id`, and `role_claims` a `role_id`, with no foreign key in the model snapshot, so rows can point to a user or role that does not exist. Found while refining F-26 (2026-09-22): the Identity entity diagram draws `users` with no relation. Whether this was intentional: not verified, no decision found.

## Expected
Every Identity column that holds the id of a user or a role, and exists only to serve that user or role, has a foreign
key in the database, so a row cannot point to a user or role that does not exist. Delete behavior and the columns that
stay without a foreign key: see `## Decisions`.

## Cause
- Not intentional: `IdentityModuleDbContext.cs:13-17` explains why the context inherits `ModuleDbContext` (schema,
  naming, filters, audit), and says "The Identity stores need entity types, not a base class". It does not mention the
  relationships that `IdentityDbContext.OnModelCreating` would have declared; they were lost with the base class, not
  dropped on purpose. No decision in `docs/decisions/` or in F-3, F-4, F-6, F-9, F-10, F-14 mentions them.
- The configurations declare keys and indexes only, no `HasOne`:
  - `Persistence/Configurations/IdentityUserRoleConfiguration.cs:14-16` — `user_roles.user_id` and `user_roles.role_id`.
  - `IdentityUserClaimConfiguration.cs:14-16` — `user_claims.user_id`.
  - `IdentityUserLoginConfiguration.cs:14-17` — `user_logins.user_id` (not even indexed).
  - `IdentityUserTokenConfiguration.cs:14-17` — `user_tokens.user_id` (first column of the key).
  - `IdentityRoleClaimConfiguration.cs:14-16` — `role_claims.role_id`.
  - `ConsentRecordConfiguration.cs:14-21` — `consent_records.user_id`.
  - `EmailVerificationTokenConfiguration.cs:14-20` and `PasswordResetTokenConfiguration.cs:14-20` —
    `email_verification_tokens.user_id`, `password_reset_tokens.user_id` (both `SingleUseToken.UserId`).
- `Migrations/IdentityModuleDbContextModelSnapshot.cs`: the only foreign keys are OpenIddict's (lines 1055-1072), the JSON
  ownership of `role_changes` (1105, 1132) and `role_permissions` → `roles` / `permissions` (1143-1153,
  `RolePermissionConfiguration.cs:17-25`, cascade) — the one Identity relationship that was written by hand.
- Why nothing broke: users and roles are never hard deleted. `AccountErasureStore.cs:30` and
  `RoleAdministrationStore.cs:110` call `Remove`, and the interceptor turns it into a soft delete (F-10 BR6, F-9 BR5).
  `AccountErasureStore.RemoveAccountDataAsync` (`:33-60`) deletes the dependent rows of an erased account by hand, and
  `DeleteRoleHandler.cs:28-31` refuses to delete a role that still has holders. Nothing stops an insert with an id that
  does not exist.
- Not a business rule, so no duplicate to look for. The manual clean-up in `AccountErasureStore` is not a reimplementation
  of a foreign key: a soft delete never fires a cascade, so it stays needed.
- Columns that also hold a user or role id but are not in scope of "serve that user": the audit columns (`created_by`,
  `updated_by`, `deleted_by`, every table), `role_changes.created_by`, `target_user_id`, `role_id`, `role_ids`
  (F-14: the audit outlives the role and keeps the erased id as a pseudonym), and OpenIddict's `subject` (text).

## Fix
- One `HasOne<User>()` / `HasOne<Role>()` per column below, with `.WithMany()` and no navigation (the Identity entity
  types stay as ASP.NET Identity ships them), in the configuration that owns the table:

  | Table | Column | Principal | On delete |
  |---|---|---|---|
  | `user_roles` | `user_id` | `users` | cascade |
  | `user_roles` | `role_id` | `roles` | cascade |
  | `user_claims` | `user_id` | `users` | cascade |
  | `user_logins` | `user_id` | `users` | cascade |
  | `user_tokens` | `user_id` | `users` | cascade |
  | `email_verification_tokens` | `user_id` | `users` | cascade |
  | `password_reset_tokens` | `user_id` | `users` | cascade |
  | `role_claims` | `role_id` | `roles` | cascade |
  | `consent_records` | `user_id` | `users` | restrict |

- One migration `AddIdentityForeignKeys`. Before each `AddForeignKey`, a `DELETE` of the rows whose id has no user or
  role (`WHERE NOT EXISTS`), so the migration runs on any database. `user_logins.user_id` gets the index EF adds for a
  foreign key (`ix_user_logins_user_id`).
- `AccountErasureStore.RemoveAccountDataAsync` and `DeleteRoleHandler` do not change: a soft delete fires no cascade.
- Regenerate `docs/architecture/` with `dotnet run --project tools/Simulab.DocGen` in the same commit (`--check` green);
  the Identity diagram gains the nine relations.

## Regression test
- `IdentitySchemaTests.Migration_CreatesForeignKeyForEveryUserAndRoleColumn` — `Simulab.Identity.Tests`: reads
  `pg_constraint` for the schema and expects exactly the nine rows of the table above plus the existing
  `role_permissions.role_id`, with their delete rule. Seen failing before the fix (only `role_permissions` found).
- `IdentitySchemaTests.UserRole_WithUnknownUser_IsRefused` — `Simulab.Identity.Tests`: an insert into `user_roles` with a
  random `user_id` throws `PostgresException` with `ForeignKeyViolation`. Fails today (the insert passes).

## Acceptance criteria
- AC1 — Given the migrated Identity schema, when its foreign keys are read, then each of the nine columns of the Fix table
  references its principal with the delete rule shown (`CASCADE` or `RESTRICT`), and no audit column (`created_by`,
  `updated_by`, `deleted_by`), no `role_changes` column and no OpenIddict `subject` has a foreign key. (Expected, D1, D2)
  → `Migration_CreatesForeignKeyForEveryUserAndRoleColumn`
- AC2 — Given no user with a given id, when a row with that `user_id` is inserted into `user_roles`, then the database
  refuses it with a foreign key violation. (Expected) → `UserRole_WithUnknownUser_IsRefused`
- AC3 — Given a database with a `user_tokens` row and a `consent_records` row whose `user_id` has no user, and a
  `role_claims` row whose `role_id` has no role, when the migration runs, then those rows are gone, every row that points
  to an existing user or role is still there, and the foreign keys exist. (D3) → `AddIdentityForeignKeysMigrationTests.Up_RemovesOrphansAndKeepsTheRest`
- AC4 — Given a user whose account is erased (F-10), when the erasure runs, then it still succeeds and leaves the user
  soft deleted with its dependent rows removed, as before. (Fix: erasure unchanged) → existing `AccountErasureTests`
- AC5 — Given the Identity model, when DocGen runs with `--check`, then `docs/architecture/` is up to date and
  `Identity/entities.md` draws the nine relations. → `EntityModelsTests` (existing check) plus the regenerated docs
- No UI text changes, so no localization criterion.

## Decisions
- D1 (owner, 2026-09-22) — Foreign keys on the nine columns only. Audit columns, `role_changes` and OpenIddict's
  `subject` stay without one. Reason: the audit must outlive the user or role and keeps an erased id as a pseudonym
  (F-10, F-14).
- D2 (owner, 2026-09-22) — Cascade on every relation except `consent_records` → `users`, which is restrict. Reason: the
  cascade rows exist only to serve the account (same as `role_permissions`); consent is legal evidence and must never
  vanish with a hard delete. Today neither fires: users and roles are only soft deleted.
- D3 (owner, 2026-09-22) — The migration deletes orphan rows before adding each foreign key. Reason: it runs on any
  database without a manual clean-up; only rows that already point to nothing are lost. Only the local database exists
  today (staging and production are `planned` in `docs/infra.md`).
- D4 (owner, 2026-09-22) — Scope as in `## Out of scope`. Reason: keep the bug to foreign keys, migration and tests.
- D5 (Claude, 2026-09-22) — Relationships without navigation properties (`HasOne<User>().WithMany().HasForeignKey(...)`).
  Reason: the Identity store entity types are ASP.NET Identity's own classes and no code needs to navigate.
- D6 (Claude, 2026-09-22) — `users` and `roles` carry query filters and the dependents do not, which EF reports as
  `PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning` for required relationships. `role_permissions`
  already has this shape. Build result (2026-09-22): `dotnet ef migrations add` and
  `dotnet ef migrations has-pending-model-changes --verbose` build the model and print no query filter warning; no
  filter was added to the dependents. Whether the running Api logs it: step 2 of the validation script.
- D7 (Claude, 2026-09-22) — The regression tests go in `IdentitySchemaTests` (real PostgreSQL through
  `Simulab.Testing.PostgresServer`), next to the existing schema checks. The migration test
  (`AddIdentityForeignKeysMigrationTests`) migrates a fresh database to `AddRoleChanges`, inserts kept and orphan rows
  (the kept user soft deleted) and then applies the rest. No new package.
- D8 (Claude, 2026-09-22) — No app-host check by Claude: the item wires nothing through the app host, and starting it
  applies the migration (and its orphan clean-up) to the owner's local database, which only the owner may change
  (`workflow.md`, `project.md`). The owner applies it in the validation script.

## Out of scope
- Changing soft delete, or adding a hard delete of accounts or roles.
- Foreign keys on audit columns, `role_changes` and OpenIddict `subject` (D1).
- Diagram layout and readability: F-26. F-25 (building in `wt/simulab/feature-25`) changes `EntityModels.cs`, not the
  model; the regenerated docs of whichever merges second are regenerated again after its update from `main`.

## Open questions
- (none)

## Validation script
1. Close any app host that is running. In Git Bash or PowerShell 7 (same command in both), from the repository root:
   `dotnet run --project src/Hosts/Simulab.AppHost` → the Aspire dashboard opens; `api` and `web` reach `Running`.
   To repeat: stop with Ctrl+C and run it again (the migration is applied only once).
2. Dashboard → `api` → Console logs → a line `Applying migration '20260922132717_AddIdentityForeignKeys'` and no
   `warn` line mentioning "global query filter".
3. Open the Web, sign in with your own account → the home page opens.
4. Open your account page, switch the language to pt-PT and back to pt-BR → the page reloads in each language, no error.
5. With your admin account, open Roles, open one role and its holders using only the keyboard (Tab / Enter) → the lists
   show as before.
6. Open `docs/architecture/Identity/entities.md` in the VS Code Markdown preview → `users` has lines to
   `consent_records`, `email_verification_tokens`, `password_reset_tokens`, `user_claims`, `user_logins`, `user_roles`,
   `user_tokens`; `roles` to `role_claims`, `user_roles` and `role_permissions`.

## Delivery
- Branch: bug/B-13
- Merge: -
