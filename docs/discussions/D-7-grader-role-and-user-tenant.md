---
discussion: D-7
status: decided
date: 2026-10-25
---
# Grader role and the user's tenant

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: decided (2026-10-25)
- Stopped at: Decided: option A, the Grader role, an assignable TenantId and the grader's own visibility rule; F-150 updated and F-162 captured.
- Next action: Run /agile:refine F-150.
- Waiting on: nothing

## Idea
F-150 creates the `Grader` system role and lets a user (curator and grader) belong to a tenant. With no tenant a grader sees every essay to grade; with a tenant, only that tenant's. Follows D-5.

## What we already know
- `IdentityRoles` holds three seed roles (`Student`, `Curator`, `Admin`); they are system roles (`Role.IsSystem`, F-9), changed only in their permissions. Authorization is by permission, never by role name (ADR-0001 #14).
- Correction of D-5, which said no user-to-tenant link exists: `User.TenantId` exists (nullable) and `TokenEndpoints.cs:394` writes the claim `tenant_id`, always empty in v1 (`SessionClaims.cs`). What is missing: a `Tenant` entity, a way to assign a user to a tenant, and an `ICurrentTenant` that reads the claim (the only implementation is `NoTenant`).
- The global filter of `ModuleDbContext` shows rows whose `TenantId` is null or equals the current tenant, so it would show a tenant's grader the global essays too; the owner wants a grader with a tenant to see only that tenant's.
- ADR-0001 #7 keeps multi-tenancy dormant ("every user is B2C"); row 18 puts grader roles out of v1 and says RBAC can add them as data.
- Epic Partner tenants holds F-139 (brand) and F-150; no item wakes the tenant filter.

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — Role, field and own rule | The `Grader` role and its permission; an administrator assigns the role and the user's `TenantId`; the claim carries it; the queue (F-151) filters by the grader's tenant with its own rule; `ICurrentTenant` stays `NoTenant` | M | A curator with a tenant sees everything until another item wires the global filter |
| B — A plus `ICurrentTenant` from the claim | Same, and the global tenant filter applies to every module | L | Wakes multi-tenancy everywhere; still needs the own rule (the filter also shows global rows); belongs to epic Partner tenants |
| C — Role only | The role and permission, no tenant | S | A grader would always see everything |

Recommendation: A — it delivers the owner's model without waking multi-tenancy in every module, and the field and the claim already exist.

## Decisions
- 2026-10-25 — F-150 delivers the `Grader` system role, an assignable `TenantId` on the user and the grader's visibility rule (option A) — it covers the owner's model without waking multi-tenancy in the whole app.
- 2026-10-25 — The user's tenant is only an id; no `Tenant` entity yet — the field and the claim exist, and the partner record comes with the first partner.
- 2026-10-25 — An administrator assigns the role and the tenant in the users screen (F-9) — it reuses the role management that exists.
- 2026-10-25 — The claim `tenant_id` carries the user's tenant; `ICurrentTenant` stays `NoTenant` — wiring the global filter is a decision of epic Partner tenants.
- 2026-10-25 — A grader with no tenant sees every essay to grade; a grader with a tenant sees only that tenant's, not the global ones — the owner's model; the global filter would also show the global rows, so the rule lives in the queue (F-151).
- 2026-10-25 — A curator linked to a tenant gains the field only; what the curator sees does not change in F-150 — there is no partner content to filter yet.
- 2026-10-25 — The permission to grade can be given to any role; the seed gives it to `Grader` and `Admin` (ADR-0001 #14) — the administrator must be able to grade while no grader exists.
- 2026-10-25 — Reverting ADR-0001 row 18 is noted in the ADR when F-150 is refined — the ADR text says RBAC adds roles as data.

## Parked
- A `Tenant` entity and the partner registration screen — wait for the first partner.
- Wiring `ICurrentTenant` to the claim (global filter in every module) — captured as F-162 in epic Partner tenants.

## Outcome
- Epics: Partner tenants
- Features: F-150 (scope settled), F-162
- Bugs: none
