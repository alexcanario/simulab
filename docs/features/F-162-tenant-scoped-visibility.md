---
feature: F-162
epic: Partner tenants
status: idea
board: 228
version: 1
---
# Tenant-scoped visibility for curators and graders

Technical terms: [glossary](../glossary.md)

## Summary
Wire `ICurrentTenant` to the `tenant_id` claim so the global tenant filter of `ModuleDbContext` applies in every module: a curator linked to a tenant sees global rows and that tenant's, not other tenants' rows. Today the only implementation is `NoTenant` (always null) and the filter has no effect. Source: discussion D-7 (2026-10-25); F-150 gives the user an assignable `TenantId` and fills the claim but leaves this open. Needs a test per module that the filter holds, as ADR-0001 #7 asks ("Tenant filters are tested from the start").

## Start
- Depends on: F-150.
- Waits on (to start): the first partner tenant and what its curators may see — owner.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:discuss` first, then `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
