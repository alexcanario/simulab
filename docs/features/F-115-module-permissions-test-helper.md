---
feature: F-115
epic: Foundation and identity
status: idea
board: 163
version: 1
---
# One test helper for every module's permissions

Technical terms: [glossary](../glossary.md)

## Summary
When a module adds a permission, tests that spell out "every permission of every module" by hand go red, and only the full gate finds them. F-95 added `questions.manage` and broke five Identity tests: `SystemRoleSeedTests`, `SeedAdminTests` and `RoleAdministrationTests` (two sites) listed `IdentityPermissions.All` and `CatalogPermissions.All` by hand, and `FirstStartLogTests` counted the migrated contexts (4, now 5). F-95 fixed them one by one (each now also lists `QuestionBankPermissions.All`); what is left is the cause: a test helper, in `Simulab.Testing`, that returns every permission the registered modules declare, so the next module changes nothing in those tests, and the same for the count of contexts the host migrates on start.

## Start
- Depends on: nothing (F-95 is done and left the tests passing).
- Waits on (to start): unknown — settled at `/agile:refine` (whether the helper reads the `PermissionCatalog` instances from the host's container, as Identity does at start, or the contracts of each module, which keeps the tests independent of the host).
- Needed to validate: nothing beyond the full suite.
- Suggested path: `/agile:refine` → `/agile:build` (a test-only change, small; `/agile:autopilot` fits once refined).
- Parallel with: none known; it touches only test projects, so it can run beside a feature that adds a module.
- Evidence of the cost: the full gate of F-95, `Failed: 5` in `Simulab.Identity.Tests.dll`, e.g. `Expected grants to be a collection with 3 item(s), but {"catalog.browse", "catalog.manage", "identity.roles.manage", "questions.manage"} contains 1 item(s) more`.
