---
bug: B-29
feature: -
status: building
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
- Waits on (to start): nothing — the patched version 10.0.13 of both packages is on nuget.org (checked 2026-10-14).
- Needed to validate: nothing beyond the scan and the full suite.
- Suggested path: `/agile:refine` → `/agile:build` (a pin in `Directory.Packages.props` is the likely fix; the owner's yes for the package is asked at refinement).
- Parallel with: none that edits `Directory.Packages.props`.

## Expected
`scan.js run` ends `agile scan GREEN` for these packages: no open `high` or `medium` advisory on `System.Security.Cryptography.Xml` or `Pkcs`, and the accepted rows are marked `fixed` in `docs/security/findings.md`.

## Cause
Measured 2026-10-14 in this worktree with `dotnet list Simulab.slnx package --vulnerable --include-transitive` and `dotnet nuget why`. Nothing in the repository references either package: both arrive transitively, so there is no source line to change (`Directory.Packages.props` has no entry for them).
- `System.Security.Cryptography.Xml` 10.0.11 comes from `Microsoft.AspNetCore.DataProtection` 10.0.11, itself pulled by `OpenIddict.AspNetCore` 7.7.1 (`OpenIddict.{Client,Server,Validation}.DataProtection`). The 10.0.10 rows of the 2026-10-13 scan were the same chain resolved before the last restore.
- `System.Security.Cryptography.Pkcs` 10.0.11 comes from `Xml` 10.0.11 (above); 10.0.0 comes from `MimeKit` 4.18.0, pulled by `MailKit` 4.18.0 (`Directory.Packages.props:111`, used by `Simulab.Email`).
- Vulnerable ranges (GitHub advisory API, read 2026-10-14): all five advisories name `>= 10.0.0, <= 10.0.12` and the first patched version `10.0.13`; both packages have 10.0.13 on nuget.org.
- `CentralPackageTransitivePinningEnabled` is already `true` (`Directory.Packages.props:12`), so one `PackageVersion` entry per package lifts the resolved version in every project that reaches it, test projects and `tools/Simulab.DocGen` included.
- Duplicates: a package advisory, not a business rule. No duplicate found.

## Fix
- `Directory.Packages.props`: two `PackageVersion` entries, `System.Security.Cryptography.Xml` and `System.Security.Cryptography.Pkcs`, both `10.0.13`, with a comment naming the advisories and when to remove the pin (when `MailKit` and `Microsoft.AspNetCore.DataProtection` resolve both at `>= 10.0.13` by themselves). Transitive pinning (already on) lifts every project that reaches them; no `PackageReference` is added anywhere.
- The nine accepted rows and the three medium ones in `docs/security/findings.md` (`21bb0b0c`, `2b55bc7b`, `3ef8ce9d`, `461e6c39`, `7406ccaa`, `b6b849d5`, `be92a768`, `d76b8d31`, `e96cdd1a`, `5f226af9`, `b0ca391a`, `d484175a`) are marked `fixed` by the scan's `accept --status fixed` once the scan no longer reports them.
- Deferred: nothing. The open semgrep row `298f77f8` (`unsafe-path-combine`) is another item's and is not touched.

## Docs
- none — no technical page describes these packages.

## Regression test
- `CryptographyPackagesTests.XmlAndPkcs_ResolveToPatchedVersion_InEveryProject` — `Simulab.ArchitectureTests`. Reads the `obj/project.assets.json` of every project of `Simulab.slnx` and fails when `System.Security.Cryptography.Xml` or `System.Security.Cryptography.Pkcs` resolves below `10.0.13` (names the project and the version). Seen failing before the pin (resolves 10.0.11 and 10.0.0 today). Scope: the two packages of these advisories; a future advisory is still the scan's job.

## Decisions
- 2026-10-13 — Captured as its own item and the nine findings accepted in F-101 with its id as the reason — owner chose "accept with a reason and open an item" over bumping inside F-101 — asked.
- 2026-10-14 — Fix is a central pin of both packages at 10.0.13, not a bump of `MailKit` or `OpenIddict` — one line each, no behavior change, the transitive pinning is already on, and a parent bump was not shown to carry 10.0.13 — decided by Claude.
- 2026-10-14 — The owner approved the two `PackageVersion` entries (`System.Security.Cryptography.Xml` 10.0.13 and `System.Security.Cryptography.Pkcs` 10.0.13, MIT, Microsoft, latest stable on nuget.org on 2026-10-14, both already in the resolved graph) — asked.
- 2026-10-14 — Regression guard is an architecture test over `obj/project.assets.json` — the definition of done asks for a regression test seen failing; owner first chose "scan only", then switched to the recommended test after the evaluation (average 7.5 vs 5.0) — asked.
- 2026-10-14 — The pin carries a comment saying it is removed once `MailKit` and `Microsoft.AspNetCore.DataProtection` resolve both packages at 10.0.13 or later — avoids a pin nobody remembers — asked.
- 2026-10-14 — Validation of this bug is the scan and the full suite; no screen is involved — the fix changes no behavior — decided by Claude.

## Open questions
- (none)

## Validation script
1. Run `dotnet list Simulab.slnx package --vulnerable --include-transitive` → no line for `System.Security.Cryptography.Xml` or `Pkcs`.
2. Run the scan of the item → `agile scan GREEN`, and the twelve findings above are `fixed` in `docs/security/findings.md`.
3. Run the full suite (`dotnet test Simulab.slnx`) → green, including `CryptographyPackagesTests`.
4. Start the app host and sign in once → the sign-in works (DataProtection and OpenIddict load with the new packages).

## Events
