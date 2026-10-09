---
bug: B-25
feature: F-65
epic: Cloud hosting and operations
status: done
board: 158
severity: medium
---
# The deploy setup command creates a federated credential the sign-in rejects

Technical terms: [glossary](../glossary.md)

## What happens
The one-time setup of `docs/infra.md` ("Deploy workflow (F-65)", step 1) creates the Azure federated credentials with the subject `repo:alexcanario/simulab:environment:<environment>`. GitHub presents this repository with numeric ids, so the first deploy run failed at "Sign in to Azure".
1. Create the credentials with the setup command of `docs/infra.md` (done 2026-10-09).
2. Run `gh workflow run deploy.yml --ref main -f environment=staging` (run 37951011234).
3. The step "Sign in to Azure" fails: `AADSTS700213: No matching federated identity record found for presented assertion subject 'repo:alexcanario@3664703/simulab@1397573907:environment:staging'`.

Worked around by hand on 2026-10-09: both credentials (`github-staging`, `github-production`) were updated with `az ad app federated-credential update` to the subject with ids, and the next run (37951156063) was green. A note about it sits under step 1 of the setup in `docs/infra.md` and in Part 5 of the owner's staging commands file.

## Start
- Depends on: nothing. Related: F-65 (done), whose setup command and test this changes.
- Waits on (to start): nothing.
- Needed to validate: the owner signed in with `gh auth login` (the check reads one GitHub endpoint, read-only); the Azure side is already correct and is not touched.
- Cause: `gh api repos/alexcanario/simulab/actions/oidc/customization/sub` answers `{"use_default":true,"use_immutable_subject":true,"sub_claim_prefix":"repo:alexcanario@3664703/simulab@1397573907"}` (2026-10-09, read again at refinement): the repository uses the immutable subject, with the owner id 3664703 and the repository id 1397573907.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none that touches `docs/infra.md` or `tests/Hosts/Simulab.AppHost.Tests/DeployWorkflowTests.cs`.

## Expected
The setup command builds the subject the repository really presents, asking GitHub for it instead of writing it by hand, and the test that checks `docs/infra.md` follows the command instead of the plain text.

## Cause
- `docs/infra.md:181`: the setup command writes the subject as the literal `repo:alexcanario/simulab:environment:$environment`. The repository presents `sub_claim_prefix` = `repo:alexcanario@3664703/simulab@1397573907` (endpoint above, verified today), so the credential never matches.
- `docs/infra.md:186`: the note under the command says the written subject "is the plain form, which this repository's runs do not use" and tells the reader to copy the subject from an error after a failed run.
- `tests/Hosts/Simulab.AppHost.Tests/DeployWorkflowTests.cs:225` (`Infra_HasTheSetupCommands_AndTheDeployWorkflowRowIsProvisioned`, AC8 of F-65): asserts the plain literal, so it protects the bug.
- Duplicates (the same subject written elsewhere): `artifacts/staging-commands.md:369` (Part 5.4, the owner's staging commands, already holds the ids form as a manual fix) and `docs/agile/templates/infra.md` (the plugin's template copy). No other implementation of the rule in code or in `.github/workflows/deploy.yml` (the workflow only presents the token).
- Items that touched this code since F-65: none (F-81 reads the same sign-in but writes no setup command).

## Fix
- `docs/infra.md` step 1: before the loop, `$prefix = gh api repos/alexcanario/simulab/actions/oidc/customization/sub --jq .sub_claim_prefix`; if it comes back empty (a repository without the immutable subject), `$prefix = "repo:alexcanario/simulab"`; the subject becomes `"${prefix}:environment:$environment"`.
- Rewrite the note under the command: why the prefix is asked from GitHub (this repository uses numeric ids), and keep the hint for `AADSTS700213` (compare the subject in the error with `az ad app federated-credential list`).
- `DeployWorkflowTests.cs:225`: assert the command reads `oidc/customization/sub` and `sub_claim_prefix`, builds the subject from the prefix with `:environment:$environment`, and no longer contains the plain literal as the subject.
- Duplicates: `artifacts/staging-commands.md` Part 5.4 is updated here, so its fix uses the same prefix command (the owner's file, kept current for the staging). The plugin template `docs/agile/templates/infra.md` is a plugin copy: not edited; the plugin note goes to the retro-log only.

## Regression test
- `Infra_HasTheSetupCommands_AndTheDeployWorkflowRowIsProvisioned` (changed assertion; seen failing against today's `docs/infra.md` before the edit) — `tests/Hosts/Simulab.AppHost.Tests`

## Criterion → test
| Criterion | Test |
|---|---|
| AC1 | `Infra_HasTheSetupCommands_AndTheDeployWorkflowRowIsProvisioned` |
| AC2 | `Infra_HasTheSetupCommands_AndTheDeployWorkflowRowIsProvisioned` (the fallback line) |
| AC3 | validation script step 2 (read on screen) |
| AC4 | validation script step 3 (read on screen) |

## Acceptance criteria
- AC1. Given the setup command of `docs/infra.md` step 1, when it runs, then the subject of each credential is the `sub_claim_prefix` that GitHub answers for the repository followed by `:environment:<environment>`.
- AC2. Given a repository whose answer has no `sub_claim_prefix`, when the command runs, then the prefix falls back to `repo:alexcanario/simulab`.
- AC3. Given the note under step 1, when read, then it says the prefix is asked from GitHub and keeps the `AADSTS700213` hint, without the sentence that calls the plain form the one written above.
- AC4. Given Part 5.4 of the staging commands file, when read, then its fix builds the subject from the same prefix command.

## Decisions
- 2026-10-09: the fix asks GitHub for `sub_claim_prefix` instead of writing the two ids in the file — the ids belong to this repository only and the endpoint is the source the sign-in itself follows. Reason: the file stays correct if the repository is renamed or recreated.
- 2026-10-09: fallback to the plain prefix when the answer is empty — the endpoint's documented answer for a repository without the immutable subject is not settled by the docs read; the fallback costs one line. Not reproduced against such a repository.
- 2026-10-09: the test stays a text check of `docs/infra.md`, as F-65 built it; it does not call GitHub or Azure.
- 2026-10-09: no question to the owner: the cause is confirmed, the fix is one command and its test, no new package, no schema, no screen, no permission.
- 2026-10-09: owner approved B-25 ("aprovo B-25").

## Out of scope
- Changing the two live credentials: already correct since 2026-10-09.
- Editing the plugin template `docs/agile/templates/infra.md`.
- Any change to `.github/workflows/deploy.yml`.

## Open questions
- (none)

## Validation script
1. In `docs/infra.md` step 1, read the command → the subject is built from `gh api repos/alexcanario/simulab/actions/oidc/customization/sub --jq .sub_claim_prefix`, with the plain prefix as fallback.
2. Read the note under the command → it no longer calls the plain form the one written above and still names `AADSTS700213`.
3. Read Part 5.4 of `artifacts/staging-commands.md` → the fix uses the same prefix command.
4. Run only the first line of the command in PowerShell 7 → it prints `repo:alexcanario@3664703/simulab@1397573907`, the same text as the subject in `az ad app federated-credential list --id <appId> --query "[].subject"`.

## Delivery
- Branch: bug/B-25
- Merge: 6fee541 (app version 0.23.1)
- Tests: full suite green, 2,635 passed, 0 failed (`gate.js ship`, 2026-10-09); `DeployWorkflowTests` 21 passed. Scan: `agile scan GREEN`.
- Manual: no visible behavior changed, no page updated. Changed docs: `docs/infra.md` (deploy setup, step 1) and `artifacts/staging-commands.md` (Part 5.4).
