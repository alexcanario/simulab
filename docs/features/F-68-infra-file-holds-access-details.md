---
feature: F-68
epic: Cloud hosting and operations
status: building
board: 107
version: 2
---
# infra.md holds the access details of the infrastructure

Technical terms: [glossary](../glossary.md)

## Summary
`docs/infra.md` gets one `## Access` table with every URI of the project (board, repository, releases, pipelines, Azure, Key Vault, cloud and local hosts, external consoles) and, for each one, how Claude signs in to it and where its credential is kept — never the credential's value. The owner's goal is that Claude can publish on the board and run pipelines without hunting for access. It also gets the `## Cloud accounts` table of the agile@canary template (one row per non-local environment), without which `/agile:publish` stops on any environment that declares a deploy command. Raised by the owner on 2026-10-03; `## Cloud accounts` added by change note v2.

## Start
- Depends on: nothing. The rows for the Key Vault, the deploy pipeline and the cloud hosts are written `planned`, and the cloud account ids are left empty; F-64 and F-65 fill them when those exist.
- Waits on (to start): nothing.
- Needed to validate: nothing beyond the owner reading `docs/infra.md`.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-50. The branch is 74 commits behind `main` (F-62 and F-67 shipped since the refinement): the build brings it up to date first.

## Goal
Claude reaches the board, the pipelines, the Key Vault and the hosts through CLI sessions the owner opened once, and one table says how — so no password has to be written anywhere, in a repository that is public.

## Users and use cases
- UC1 Claude, before a board or pipeline operation, reads the `## Access` row, runs its check command and, when it passes, uses the CLI session.
- UC2 Claude, when the check fails (expired or missing session), stops and asks the owner to run the row's sign-in command, pasting the real error.
- UC3 The owner or a developer opens `docs/infra.md` and finds every URI of the project in one table.
- UC4 `/agile:publish <environment>` reads the environment's `## Cloud accounts` row; while its ids are empty it stops naming the missing id instead of saying the row does not exist.

## Business rules
- BR1 `docs/infra.md` has a section `## Access` with one table, columns in this order: `What`, `Status`, `URI`, `Claude signs in with`, `Check`, `Credential kept in`.
- BR2 The table has every row of `## Approved list`, in that order. `Status` is `provisioned`, `planned` or `retired`. A `planned` row has `—` as URI until its item (F-64, F-65) fills it; a URI is never invented.
- BR3 `Credential kept in` holds a pointer, never a value. Allowed forms, exactly: `none`, `gh keyring`, `az login`, `Key Vault: <secret name>`, `user secrets: <key>`, `app host`, `owner only`. Anything else fails the test (AC3).
- BR4 `Claude signs in with` is the command the owner runs once (`gh auth login`, `az login`) or `none` / `owner only`; `Check` is the read-only command Claude runs before use (`gh auth status`, `az account show`) or `—`.
- BR5 When a check fails, Claude stops the step that needs the access, quotes the error and asks the owner to run the sign-in command; it does not try another path. This becomes a line in `.claude/rules/agile/project.md`.
- BR6 `.claude/rules/agile/project.md` also gets the line: `docs/infra.md` never holds a credential value (the repository is public); it points to where the credential is kept.
- BR7 The local hosts keep their existing details in `## Run locally`; the `## Access` rows point to them and do not repeat the local Postgres credentials.
- BR8 `docs/infra.md` has a section `## Cloud accounts` (template `docs/agile/templates/infra.md`, agile@canary 0.2.0 and later) with one table, columns in this order: `Environment`, `Client`, `Cloud`, `Tenant`, `Subscription or account`, `Resource group`, `Region`; exactly one row per non-local environment of `## Environments` (today `staging` and `production`), as in `## Approved list`, "Cloud accounts rows".
- BR9 Tenant, subscription and resource group come from the administrator of the client's cloud account (the owner, for Simulab) and are never invented: until F-64 brings them the cells stay empty, so `/agile:publish` stops with "has no Tenant: fill it…" rather than a placeholder failing as "not a GUID". A filled Tenant or Subscription is a GUID. These ids are not secrets; ADR-0002 #6 and the deploy paragraph of `## Environments` stop saying the subscription never comes from a committed file.

## Screens and API
- No screen, no endpoint. Documentation, two project rules, a note on ADR-0002 and architecture tests.

## Acceptance criteria
- AC1 Given `docs/infra.md`, when the test reads it, then it finds the `## Access` section and its table with the six columns of BR1 in order.
- AC2 Given the `## Access` table, when the test reads its `What` column, then every row of `## Approved list` is there, in order, and every `Status` is `provisioned`, `planned` or `retired`.
- AC3 Given the `## Access` table, when the test reads `Credential kept in`, then every cell matches one of the forms of BR3; a cell holding anything else (for example a literal password) fails the test and names the row.
- AC4 Given a `planned` row, when the test reads it, then its URI is `—`.
- AC5 Given `.claude/rules/agile/project.md`, when the test reads it, then it contains the rules of BR5 and BR6.
- AC6 Localization: no UI text changes, so there is nothing to translate (the missing-key test stays green).
- AC7 Given `docs/infra.md`, when the test reads it, then it finds `## Cloud accounts` with the seven columns of BR8 in order and exactly one row for each non-local environment listed in `## Environments`.
- AC8 Given a `## Cloud accounts` row, when the test reads it, then `Cloud` is one of `azure`, `aws`, `gcp`, `other`, `none`, and `Tenant` and `Subscription or account` are each empty or a GUID; a name or a placeholder (`TBD`, `—`) fails the test and names the row.

## Criterion → test
| Criterion | Test |
|---|---|
| AC1 | `InfraAccessTests.Infra_AccessSection_HasTheSixColumnsInOrder` |
| AC2 | `InfraAccessTests.Infra_AccessTable_HasEveryApprovedRowInOrderWithAKnownStatus` |
| AC3 | `InfraAccessTests.Infra_AccessTable_CredentialCellsAreOnlyPointers`; guard proven by `CredentialProblems_ALiteralPassword_NamesTheRowAndNotTheValue` and `CredentialProblems_EachAllowedPointer_IsAccepted` |
| AC4 | `InfraAccessTests.Infra_AccessTable_APlannedRowHasNoUri` |
| AC5 | `InfraAccessTests.ProjectRules_StateTheFailedCheckRuleAndTheNoCredentialRule` |
| AC6 | no UI text changed; the missing-key tests of the full suite stay green (gate) |
| AC7 | `InfraAccessTests.Infra_CloudAccounts_HasTheSevenColumnsAndOneRowPerNonLocalEnvironment` |
| AC8 | `InfraAccessTests.Infra_CloudAccounts_CloudIsKnownAndIdsAreEmptyOrGuids`; guard proven by `CloudAccountProblems_APlaceholderOrNameAsTenant_NamesTheRow`, `CloudAccountProblems_AnEmptyIdAndAGuid_AreAccepted`, `CloudAccountProblems_AnUnknownCloud_NamesTheRow` |

## Approved list
### Access rows
Rows of the `## Access` table, in order (the owner asked for every URI of the project, 2026-10-03). Status verified on disk and with `gh`/`az` on 2026-10-03; rows 5 and 6 corrected by change note v2 (2026-10-04).
1. Repository — provisioned — `https://github.com/alexcanario/simulab` — `gh auth login` — `gh auth status` — `gh keyring`
2. Board: issues — provisioned — `https://github.com/alexcanario/simulab/issues` — `gh auth login` — `gh auth status` — `gh keyring`
3. Board: project — provisioned — `https://github.com/users/alexcanario/projects/11` — `gh auth login` — `gh auth status` — `gh keyring`
4. Releases — provisioned — `https://github.com/alexcanario/simulab/releases` — `gh auth login` — `gh auth status` — `gh keyring`
5. CI pipeline (GitHub Actions) — provisioned (`.github/workflows/ci.yml`, F-62) — `https://github.com/alexcanario/simulab/actions/workflows/ci.yml` — `gh auth login` — `gh auth status` — `gh keyring`
6. Deploy pipeline (GitHub Actions) — planned (F-65) — `—` — `gh auth login` — `gh auth status` — `gh keyring`
7. Azure portal and subscription — provisioned — `https://portal.azure.com` — `az login` — `az account show` — `az login`
8. Key Vault — planned (F-64) — `—` — `az login` — `az account show` — `az login`
9. Staging Web — planned (F-64) — `—` — `none` — `—` — `none`
10. Staging Api — planned (F-64) — `—` — `none` — `—` — `none`
11. Production Web — planned — `—` — `none` — `—` — `none`
12. Production Api — planned — `—` — `none` — `—` — `none`
13. Local Web — provisioned — `https://localhost:7125` — `none` — `—` — `none`
14. Local UI kit gallery — provisioned — `https://localhost:7125/dev/ui` — `none` — `—` — `none`
15. Local Api — provisioned — `https://localhost:7287` — `none` — `—` — `none`
16. Local OpenAPI document — provisioned — `https://localhost:7287/openapi/v1.json` — `none` — `—` — `none`
17. Local token endpoint — provisioned — `https://localhost:7287/connect/token` — `none` — `—` — `none`
18. Local Aspire dashboard — provisioned — `https://localhost:17162` — `none` — `—` — `app host`
19. Local Mailpit — provisioned — linked from the dashboard (its port changes on each start) — `none` — `—` — `none`
20. Local PostgreSQL — provisioned — `127.0.0.1:5432` (see `## Run locally`) — `none` — `—` — `app host`
21. Local Redis — provisioned — connection string generated by the app host — `none` — `—` — `app host`
22. Claude API console — provisioned — `https://console.anthropic.com` — `owner only` — `—` — `user secrets: Ai:ApiKey`
23. Google Cloud console (OAuth client, F-20) — provisioned — `https://console.cloud.google.com/apis/credentials` — `owner only` — `—` — `user secrets: Google:ClientSecret`
24. Former board (Azure Boards) — retired (items migrated to GitHub) — `https://dev.azure.com/acanariopt/simulab` — `none` — `—` — `none`

### Cloud accounts rows
Rows of the `## Cloud accounts` table, in order (change note v2, 2026-10-04). Empty cells stay empty until F-64.
1. staging — Simulab — azure — (empty) — (empty) — (empty) — brazilsouth
2. production — Simulab — azure — (empty) — (empty) — (empty) — brazilsouth

## Decisions
- 2026-10-03 — No credential value in `docs/infra.md`; it holds pointers only — the repository is public (`gh repo view --json visibility` → `PUBLIC`), and a committed password stays in the history even after removal. Owner's goal (Claude publishes on the board and runs pipelines) is met by CLI sessions instead: verified 2026-10-03, `gh` signed in as `alexcanario` (keyring, scopes `project`, `workflow`, `repo`) and `az` signed in to "Azure subscription 1". Claude also never types a password to authenticate, so a written password would not serve that goal.
- 2026-10-03 — One `## Access` table, not new columns in the existing tables — owner's choice; it also avoids F-62's edits to `## Environments`.
- 2026-10-03 — Every URI of the project, planned rows included — owner's answer ("põe todas as URIs do projeto lá").
- 2026-10-03 — An architecture test guards the table (columns, rows, credential pointers) — owner's choice; it keeps a password from entering the public file later.
- 2026-10-03 — A failed check stops the step and asks the owner to sign in — owner's choice.
- 2026-10-03 — The test lives in `Simulab.ArchitectureTests` beside `ArchitectureOverviewTests` and reads the file the same way (`SolutionAssemblies.RepositoryRoot()`) — same pattern as the existing document checks. Technical choice.
- 2026-10-03 — The Mailpit and Redis rows carry no port: the app host assigns them per start; the dashboard (fixed at 17162 in `src/Hosts/Simulab.AppHost/Properties/launchSettings.json`) links them. Technical choice.
- 2026-10-03 — The former Azure Boards row is kept as `retired`, from the "Migrado de AB#..." notes on the migrated issues; whether that project still exists is not verified. Technical choice.
- 2026-10-04 — `## Cloud accounts` added (change note v2) — owner's request; with agile@canary 0.4.0 synced on `main` and deploy commands declared by F-62, `/agile:publish staging` stops today on "no row in `## Cloud accounts`" (`scripts/publish.js` `cloudPlan`).
- 2026-10-04 — Unknown ids are left empty, not `—` or `TBD` — the plugin's reader (`templates/dotnet/workflows/agile-deploy.js` `cloudRowProblem`) reports an empty Tenant as "has no Tenant: fill it with the client's tenant id", while any other text fails as "must be a GUID". Technical choice.
- 2026-10-04 — Tenant and subscription ids are committed — they are identifiers, not secrets (template note); ADR-0002 #6 gets a note and the deploy paragraph of `## Environments` is corrected, since both said the subscription never comes from a committed file. Owner's approval of change note v2.
- 2026-10-04 — Client `Simulab`, Cloud `azure`, Region `brazilsouth` — ADR-0002 and `## Environments` (Azure Container Apps, Brazil South); the owner is the account's administrator. Owner's approval of change note v2.
- 2026-10-09 — Build started: the branch was merged with `main` (24 commits) with no conflict before the first edit. The plugin's `## Cloud accounts` template has an eighth column, `Monthly budget` (agile@canary 0.39.0), that BR8 does not list; the table has the seven columns BR8 and AC7 fix. Only an `aca` environment reads it, and F-64's first deploy is what decides a budget — so it is left to that item to add the column. Technical choice, to be confirmed at validation.
- 2026-10-09 — The `## Access` and `## Cloud accounts` sections sit after "Code hosting, CI and board" and before "Expected secrets" in `docs/infra.md`; the two project rules are a new `## Access to the infrastructure` section of `.claude/rules/agile/project.md`. Technical choice.
- 2026-10-09 — Guard tests live in `tests/Simulab.ArchitectureTests/InfraAccessTests.cs`; the parsers (`Section`, `FirstTable`, `CredentialProblems`, `CloudAccountProblems`) are `internal` so negative-control tests prove a literal password, a `TBD` tenant and an unknown cloud are caught, and the real-file tests assert their own inputs are not empty. Technical choice.

## Out of scope
- Creating the Key Vault, the pipelines or the cloud hosts (F-64, F-65).
- Filling the tenant, subscription and resource group ids (F-64).
- The behavior of `/agile:publish` itself: it is the plugin's and already exists.
- Storing any credential value anywhere, in or out of the repository.
- A local git-ignored file with passwords (offered and not chosen).

## Open questions
- (none)

## Change notes
### v2 — 2026-10-04
- What: add `## Cloud accounts` to `docs/infra.md` (BR8, BR9, AC7, AC8, UC4, "Cloud accounts rows"); note on ADR-0002 #6 and correction of the deploy paragraph of `## Environments`; `## Access` rows 5-6 corrected (the CI pipeline is provisioned since F-62; the deploy pipeline is a separate planned row) and the later rows renumbered; Start updated (F-62 shipped).
- Why: the owner asked for it; `/agile:publish` (agile@canary 0.4.0) stops on an environment that declares a deploy command and has no row, and staging and production declare one since F-62.
- Affected: Summary, Start, UC4, BR8, BR9, AC7, AC8, `## Approved list`; other criteria unchanged.
- Re-approved: 2026-10-04
