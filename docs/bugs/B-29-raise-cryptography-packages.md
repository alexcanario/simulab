---
bug: B-29
feature: -
status: idea
board: 170
severity: high
epic: foundation-and-identity
---
# Raise System.Security.Cryptography.Xml and Pkcs to patched versions

Technical terms: [glossary](../glossary.md)

## What happens
The security scan (`scan.js run`, 2026-10-13, found while building F-101) reports nine `high` and three `medium` advisories on two transitive packages, in 20 projects: `System.Security.Cryptography.Xml` 10.0.10 and 10.0.11 (GHSA-c399-9853-6jmj, GHSA-r955-8q5j-3h83, GHSA-7hqp-7c5m-4vq6) and `System.Security.Cryptography.Pkcs` 10.0.0, 10.0.10 and 10.0.11 (GHSA-846f-f764-76mx high, GHSA-2fmv-rv2c-59x7 medium). The advisories are new: the scan of 2026-10-08 did not list them.

Evidence: `dotnet list Simulab.slnx package --vulnerable --include-transitive` prints, for example, `Project Simulab.Identity.Infrastructure ... > System.Security.Cryptography.Pkcs 10.0.11 High https://github.com/advisories/GHSA-846f-f764-76mx` and `> System.Security.Cryptography.Xml 10.0.11 High .../GHSA-r955-8q5j-3h83`; the ids are in `docs/security/findings.md` (`21bb0b0c`, `2b55bc7b`, `3ef8ce9d`, `461e6c39`, `7406ccaa`, `b6b849d5`, `be92a768`, `d76b8d31`, `e96cdd1a`, `5f226af9`, `b0ca391a`, `d484175a`).

The owner accepted them on 2026-10-13 with the reason "transitive, not introduced by F-101, bumped in its own item" so F-101 could go on; this bug is that item. Until it is delivered the scan of every item ends accepted findings and not `GREEN`.

## Start
- Depends on: nothing.
- Waits on (to start): the patched versions being published — checked against the registry at refinement (the advisories name the fixed ranges) — who provides it: unknown, settled at `/agile:refine`.
- Needed to validate: nothing beyond the scan and the full suite.
- Suggested path: `/agile:refine` → `/agile:build` (a pin in `Directory.Packages.props` is the likely fix; the owner's yes for the package is asked at refinement).
- Parallel with: none that edits `Directory.Packages.props`.

## Expected
`scan.js run` ends `agile scan GREEN` for these packages: no open `high` or `medium` advisory on `System.Security.Cryptography.Xml` or `Pkcs`, and the accepted rows are marked `fixed` in `docs/security/findings.md`.

## Cause
Cause not verified: measure it at `/agile:refine` (which package brings `Xml` and `Pkcs` transitively, and whether a central pin or a parent bump removes them).

## Fix
Unknown — settled at `/agile:refine`.

## Docs
- none — no technical page describes these packages.

## Regression test
- A guard that fails while a vulnerable version of these packages resolves — to be named at refinement.

## Decisions
- 2026-10-13 — Captured as its own item and the nine findings accepted in F-101 with its id as the reason — owner chose "accept with a reason and open an item" over bumping inside F-101 — asked.

## Open questions
- (none)

## Validation script
1. Run the scan of the item → `agile scan GREEN`, and the findings above are `fixed`.

## Events
