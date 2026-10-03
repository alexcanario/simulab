---
feature: F-68
epic: Foundation and identity
status: refining
board: 107
version: 1
---
# infra.md holds the access details of the infrastructure

## Summary
`docs/infra.md` gets one `## Access` table with every URI of the project (board, repository, releases, pipelines, Azure, Key Vault, cloud and local hosts, external consoles) and, for each one, how Claude signs in to it and where its credential is kept — never the credential's value. The owner's goal is that Claude can publish on the board and run pipelines without hunting for access. Raised by the owner on 2026-10-03.

## Start
- Depends on: nothing. The rows for the Key Vault, the pipelines and the cloud hosts are written `planned` and filled by F-64 and F-65 when those exist.
- Waits on (to start): nothing.
- Needed to validate: nothing beyond the owner reading `docs/infra.md`.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-50, F-62 (F-62 edits the `## Environments` table of the same file: a small merge conflict is possible, resolved by bringing the branch up to date before the full suite).

## Goal
Claude reaches the board, the pipelines, the Key Vault and the hosts through CLI sessions the owner opened once, and one table says how — so no password has to be written anywhere, in a repository that is public.

## Users and use cases
- UC1 Claude, before a board or pipeline operation, reads the `## Access` row, runs its check command and, when it passes, uses the CLI session.
- UC2 Claude, when the check fails (expired or missing session), stops and asks the owner to run the row's sign-in command, pasting the real error.
- UC3 The owner or a developer opens `docs/infra.md` and finds every URI of the project in one table.

## Business rules
- BR1 `docs/infra.md` has a section `## Access` with one table, columns in this order: `What`, `Status`, `URI`, `Claude signs in with`, `Check`, `Credential kept in`.
- BR2 The table has every row of `## Approved list`, in that order. `Status` is `provisioned`, `planned` or `retired`. A `planned` row has `—` as URI until its item (F-64, F-65) fills it; a URI is never invented.
- BR3 `Credential kept in` holds a pointer, never a value. Allowed forms, exactly: `none`, `gh keyring`, `az login`, `Key Vault: <secret name>`, `user secrets: <key>`, `app host`, `owner only`. Anything else fails the test (AC3).
- BR4 `Claude signs in with` is the command the owner runs once (`gh auth login`, `az login`) or `none` / `owner only`; `Check` is the read-only command Claude runs before use (`gh auth status`, `az account show`) or `—`.
- BR5 When a check fails, Claude stops the step that needs the access, quotes the error and asks the owner to run the sign-in command; it does not try another path. This becomes a line in `.claude/rules/agile/project.md`.
- BR6 `.claude/rules/agile/project.md` also gets the line: `docs/infra.md` never holds a credential value (the repository is public); it points to where the credential is kept.
- BR7 The local hosts keep their existing details in `## Run locally`; the `## Access` rows point to them and do not repeat the local Postgres credentials.

## Screens and API
- No screen, no endpoint. Documentation, two project rules and one architecture test.

## Acceptance criteria
- AC1 Given `docs/infra.md`, when the test reads it, then it finds the `## Access` section and its table with the six columns of BR1 in order.
- AC2 Given the `## Access` table, when the test reads its `What` column, then every row of `## Approved list` is there, in order, and every `Status` is `provisioned`, `planned` or `retired`.
- AC3 Given the `## Access` table, when the test reads `Credential kept in`, then every cell matches one of the forms of BR3; a cell holding anything else (for example a literal password) fails the test and names the row.
- AC4 Given a `planned` row, when the test reads it, then its URI is `—`.
- AC5 Given `.claude/rules/agile/project.md`, when the test reads it, then it contains the rules of BR5 and BR6.
- AC6 Localization: no UI text changes, so there is nothing to translate (the missing-key test stays green).

## Approved list
Rows of the `## Access` table, in order (the owner asked for every URI of the project, 2026-10-03). Status as of 2026-10-03, verified on disk and with `gh`/`az`.
1. Repository — provisioned — `https://github.com/alexcanario/simulab` — `gh auth login` — `gh auth status` — `gh keyring`
2. Board: issues — provisioned — `https://github.com/alexcanario/simulab/issues` — `gh auth login` — `gh auth status` — `gh keyring`
3. Board: project — provisioned — `https://github.com/users/alexcanario/projects/11` — `gh auth login` — `gh auth status` — `gh keyring`
4. Releases — provisioned — `https://github.com/alexcanario/simulab/releases` — `gh auth login` — `gh auth status` — `gh keyring`
5. Pipelines (GitHub Actions) — planned (no `.github/workflows` yet; F-65) — `—` — `gh auth login` — `gh auth status` — `gh keyring`
6. Azure portal and subscription — provisioned — `https://portal.azure.com` — `az login` — `az account show` — `az login`
7. Key Vault — planned (F-64) — `—` — `az login` — `az account show` — `az login`
8. Staging Web — planned (F-64) — `—` — `none` — `—` — `none`
9. Staging Api — planned (F-64) — `—` — `none` — `—` — `none`
10. Production Web — planned — `—` — `none` — `—` — `none`
11. Production Api — planned — `—` — `none` — `—` — `none`
12. Local Web — provisioned — `https://localhost:7125` — `none` — `—` — `none`
13. Local UI kit gallery — provisioned — `https://localhost:7125/dev/ui` — `none` — `—` — `none`
14. Local Api — provisioned — `https://localhost:7287` — `none` — `—` — `none`
15. Local OpenAPI document — provisioned — `https://localhost:7287/openapi/v1.json` — `none` — `—` — `none`
16. Local token endpoint — provisioned — `https://localhost:7287/connect/token` — `none` — `—` — `none`
17. Local Aspire dashboard — provisioned — `https://localhost:17162` — `none` — `—` — `app host`
18. Local Mailpit — provisioned — linked from the dashboard (its port changes on each start) — `none` — `—` — `none`
19. Local PostgreSQL — provisioned — `127.0.0.1:5432` (see `## Run locally`) — `none` — `—` — `app host`
20. Local Redis — provisioned — connection string generated by the app host — `none` — `—` — `app host`
21. Claude API console — provisioned — `https://console.anthropic.com` — `owner only` — `—` — `user secrets: Ai:ApiKey`
22. Google Cloud console (OAuth client, F-20) — provisioned — `https://console.cloud.google.com/apis/credentials` — `owner only` — `—` — `user secrets: Google:ClientSecret`
23. Former board (Azure Boards) — retired (items migrated to GitHub) — `https://dev.azure.com/acanariopt/simulab` — `none` — `—` — `none`

## Decisions
- 2026-10-03 — No credential value in `docs/infra.md`; it holds pointers only — the repository is public (`gh repo view --json visibility` → `PUBLIC`), and a committed password stays in the history even after removal. Owner's goal (Claude publishes on the board and runs pipelines) is met by CLI sessions instead: verified 2026-10-03, `gh` signed in as `alexcanario` (keyring, scopes `project`, `workflow`, `repo`) and `az` signed in to "Azure subscription 1". Claude also never types a password to authenticate, so a written password would not serve that goal.
- 2026-10-03 — One `## Access` table, not new columns in the existing tables — owner's choice; it also avoids F-62's edits to `## Environments`.
- 2026-10-03 — Every URI of the project, planned rows included — owner's answer ("põe todas as URIs do projeto lá").
- 2026-10-03 — An architecture test guards the table (columns, rows, credential pointers) — owner's choice; it keeps a password from entering the public file later.
- 2026-10-03 — A failed check stops the step and asks the owner to sign in — owner's choice.
- 2026-10-03 — The test lives in `Simulab.ArchitectureTests` beside `ArchitectureOverviewTests` and reads the file the same way (`SolutionAssemblies.RepositoryRoot()`) — same pattern as the existing document checks. Technical choice.
- 2026-10-03 — The Mailpit and Redis rows carry no port: the app host assigns them per start; the dashboard (fixed at 17162 in `src/Hosts/Simulab.AppHost/Properties/launchSettings.json`) links them. Technical choice.
- 2026-10-03 — The former Azure Boards row is kept as `retired`, from the "Migrado de AB#..." notes on the migrated issues; whether that project still exists is not verified. Technical choice.

## Out of scope
- Creating the Key Vault, the pipelines or the cloud hosts (F-64, F-65).
- Storing any credential value anywhere, in or out of the repository.
- A local git-ignored file with passwords (offered and not chosen).

## Open questions
- (none)
