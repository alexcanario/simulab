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
A `Grader` system role (a person who signs in and grades essays and open answers) and an assignable tenant on the user, for the curator and the grader. A grader with no tenant sees every essay to grade; with a tenant, only that tenant's essays, not the global ones. Until the first partner tenant exists, the one grader sees every essay. Source: discussion D-5 (owner's model, 2026-10-24); it reverts ADR-0001 row 18 ("Teacher / grader roles: Not in v1"), whose text says RBAC lets roles be added as data.
Added 2026-10-25 (D-7), correcting the first version of this item: `User.TenantId` already exists and the token already carries the claim `tenant_id` (empty today); what is missing is a way to assign the tenant, the role and a visibility rule. Scope settled in D-7: the role and its permission (any role may hold it; the seed gives it to `Grader` and `Admin`); an administrator assigns the role and the user's `TenantId` in the users screen (F-9); the claim carries the tenant; `ICurrentTenant` stays `NoTenant` (wiring it is F-162); the grader's own visibility rule is applied by the correction queue (F-151), because the global filter would also show the global rows. A curator with a tenant gains the field only. No `Tenant` entity yet.

## Start
- Depends on: F-9 (roles and permissions, done).
- Waits on (to start): nothing — scope settled in D-7 (2026-10-25).
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
