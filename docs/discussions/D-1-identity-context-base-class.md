---
discussion: D-1
status: decided
date: 2026-09-22
---
# Identity context base class

## Idea
While refining B-13 (Identity tables have no foreign keys), the owner asked whether `ModuleDbContext` should inherit
ASP.NET Identity's `IdentityDbContext` instead, solving the bug without writing foreign keys by hand.

## What we already know
- `ModuleDbContext` (`src/BuildingBlocks/Simulab.Persistence/ModuleDbContext.cs`) is the base of every module context:
  `JobsDbContext` and `IdentityModuleDbContext` today. The profile requires it and an architecture test checks it.
- `IdentityModuleDbContext.cs:13-17` chose `ModuleDbContext` so the schema, snake_case names, tenant and soft-delete
  filters and audit apply to the identity tables.
- `IdentityDbContext` (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.12, read from the package) declares in
  `OnModelCreating`: tables `AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`,
  `AspNetUserTokens`, `AspNetRoleClaims`, and `AspNetUserPasskeys` in schema versions 2 and 3; indexes `UserNameIndex`
  (unique, global, over `NormalizedUserName`), `EmailIndex`, `RoleNameIndex`; and the relationships of those tables,
  which become foreign keys. Its default schema version in .NET 10 without configuration: not verified.
- Inheriting it does not avoid foreign keys: it declares the same ones, and the migration creates them. It covers 6 of
  the 9 columns of B-13; `consent_records`, `email_verification_tokens` and `password_reset_tokens` are Simulab's own.
- Its global `UserNameIndex` conflicts with `ux_users_tenant_normalized_user_name` (F-4 BR3, unique per tenant).

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — Keep B-13: nine explicit relationships | No visible change; the database refuses orphan rows | S | None new; the relationships live in our configurations |
| B — `IdentityModuleDbContext` inherits `IdentityDbContext<User, Role, Guid>` | Same | M | C# has one base class: the `ModuleDbContext` conventions are copied into it, the architecture test gets an exception, the base's tables and indexes are undone (global `UserNameIndex` breaks F-4 BR3); the migration renames and drops indexes that sign-in and sign-up rely on; 3 of 9 keys still by hand |
| C — `ModuleDbContext` inherits `IdentityDbContext` | Same | M | Every module (Jobs and later) gets user tables in its own schema, against ADR-0001 #6 |

Recommendation: A — the database ends the same as with B, without changing the base class or the F-4 indexes.

## Decisions
- 2026-09-22 — Keep B-13 as refined: nine explicit relationships, `IdentityModuleDbContext` stays on `ModuleDbContext`
  — inheriting `IdentityDbContext` creates the same foreign keys, covers only six of them, and brings a table and index
  scheme that conflicts with the module conventions and F-4 BR3.

## Parked
- (none)

## Outcome
- Epics: none
- Features: none
- Bugs: B-13 unchanged
