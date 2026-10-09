---
bug: B-25
feature: F-65
epic: Cloud hosting and operations
status: idea
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
- Needed to validate: nothing; the Azure side is already correct.
- Cause: `gh api repos/alexcanario/simulab/actions/oidc/customization/sub` answers `{"use_default":true,"use_immutable_subject":true,"sub_claim_prefix":"repo:alexcanario@3664703/simulab@1397573907"}` (2026-10-09): the repository uses the immutable subject, with the owner id 3664703 (`gh api users/alexcanario --jq .id`) and the repository id 1397573907 (`gh api repos/alexcanario/simulab --jq .id`).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none that touches `docs/infra.md` or `tests/Hosts/Simulab.AppHost.Tests/DeployWorkflowTests.cs`.

## Expected
The setup command builds the subject the repository really presents (for instance from `gh api repos/alexcanario/simulab --jq .id` and the owner's id), and the test that checks `docs/infra.md` follows the command instead of the plain text.
