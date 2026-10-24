---
feature: F-150
epic: Partner tenants
status: idea
board: 208
version: 1
---
# Grader role and user-to-tenant link

Technical terms: [glossary](../glossary.md)

## Summary
A `Grader` system role (a person who signs in and grades essays and open answers) and a link from a user to a tenant, for the curator and the grader. A grader or curator with no tenant sees everything; with a tenant, only that tenant's data. Until the first partner tenant exists, the one grader sees every essay to grade. Source: discussion D-5 (owner's model, 2026-10-24); it reverts ADR-0001 row 18 ("Teacher / grader roles: Not in v1"), whose text says RBAC lets roles be added as data. Checked in the code on 2026-10-24: `User` inherits `IdentityUser<Guid>` and is a declared exception to `TenantEntity`, so no user-to-tenant link exists today.

## Start
- Depends on: F-9 (roles and permissions, done).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
