---
feature: F-65
epic: Foundation and identity
status: approved
board: 104
version: 1
---
# A GitHub Actions workflow deploys a release

Technical terms: [glossary](../glossary.md)

## Summary
A `deploy.yml` workflow runs the Deploy command of `docs/infra.md` for an environment, signing in to Azure with OIDC federation (no stored cloud secret) and using GitHub environments, with an approval required for production. F-62's ADR evaluates the approach; this item writes the workflow once there is an environment to deploy to. Raised as out of scope by the F-62 refinement, 2026-10-02.

## Start
- Depends on: F-62 (done: ADR-0002, the AppHost publish shape, the declared commands); F-64 (refining on 2026-10-04: creates the staging environment this workflow deploys to).
- Waits on (to start): F-64 provisioned staging (resource group, environment) — owner, through F-64. The item may be approved before; the build waits.
- Needed to validate: the provisioned staging environment; the Azure identity and the GitHub environments created with the commands of BR8 — owner runs them, or authorizes Claude to run them, at validation.
- Suggested path: `/agile:refine` → `/agile:build` after F-64 is done.
- Parallel with: none that touches `.github/workflows/` or `docs/infra.md`; F-82 (CI action versions) also edits `.github/workflows/` and should not run beside it.

## What exists (checked 2026-10-04)
- `.github/workflows/ci.yml`: build and test on pull requests and pushes to `main`, no deploy step (F-62 BR8). Uses `actions/checkout@v4`, `actions/setup-dotnet@v4`.
- `docs/infra.md`: staging and production `planned`, each with an `aspire deploy` Deploy command; the command needs `AZURE__SUBSCRIPTIONID`, `AZURE__LOCATION` (`brazilsouth`), `AZURE__RESOURCEGROUP` and the parameter `Parameters__openiddict-client-secret`.
- `src/Hosts/Simulab.AppHost/AzureDeployment.cs`: the only deploy-time parameter is `openiddict-client-secret` (secret). In publish mode the AppHost ignores `Google:*` and `Ai:ApiKey` from user secrets (F-62).
- `Directory.Packages.props`: every `Aspire.*` package is 13.6.0; the Aspire CLI must match (ADR-0002 consequences).
- GitHub: the repository `alexcanario/simulab` is **public**, so environments with required reviewers work on the free plan. No environment exists yet (`gh api repos/alexcanario/simulab/environments` returned none).
- Azure: nothing exists until F-64.
- Aspire reads the Azure credential source from `Azure:CredentialSource`; `AzureCli` uses the sign-in that `azure/login` leaves on the runner ([aspire.dev/deployment/azure](https://aspire.dev/deployment/azure/)). Not verified against a real subscription: the first staging deploy proves it.

## Use cases
- UC1 — The owner deploys a version to staging: in GitHub, Actions → `deploy` → "Run workflow", picks `main` or a tag `v*` as the workflow's ref and `staging` as the environment; or the same with `gh workflow run deploy.yml --ref <ref> -f environment=staging`.
- UC2 — The owner deploys a release tag to production: same as UC1 with `production`; the run waits until the owner approves it in GitHub, and only then signs in to Azure.
- UC3 — `/agile:publish` with an environment named runs the Deploy command of `docs/infra.md`, which is now the `gh workflow run` of UC1/UC2 for the new tag.

## Business rules
- BR1 — The workflow starts only by hand (`workflow_dispatch`), with one input `environment` (choice: `staging`, `production`). The version deployed is the ref the run was started on; there is no separate version input. No push, tag or release starts a deploy.
- BR2 — Staging accepts `main` or a tag `v*`; production accepts only a tag `v*`. Any other ref fails in the first step, before any Azure sign-in, with a message naming the rule. The same rule is also set on each GitHub environment as its deployment branch and tag policy (BR8), so it holds even if the workflow file is changed on a branch.
- BR3 — The Azure sign-in is `azure/login` with OIDC federation. The workflow asks for `id-token: write` and `contents: read` only. No Azure credential is stored in GitHub: the client id, tenant id, subscription id and resource group are GitHub environment **variables** (not secrets), one set per environment.
- BR4 — One GitHub environment per target, `staging` and `production`. `production` has the owner (`alexcanario`) as required reviewer; `staging` has no reviewer. The deploy job runs inside the environment, so a production run does nothing until approved, and a rejected run never signs in to Azure.
- BR5 — The deploy step runs the `aspire deploy` command declared in `docs/infra.md` for that environment, unchanged, with the Aspire CLI pinned to the version of the `Aspire.*` packages (13.6.0 today) and `Azure__CredentialSource=AzureCli`. The OpenIddict client secret comes from the GitHub environment secret `OPENIDDICT_CLIENT_SECRET` and reaches the command as `Parameters__openiddict-client-secret`. This application secret is the only secret the workflow reads.
- BR6 — At most one deploy per environment runs at a time; a second one waits for the first (`concurrency` per environment, never cancelling a deploy in progress).
- BR7 — After the deploy, when the environment has a check URL (environment variable `CHECK_URL`, filled by F-64 in `docs/infra.md` "Check URL"), the run waits for it to answer HTTP 200, for at most 5 minutes, and fails otherwise. The run summary names the environment, the ref and the commit deployed.
- BR8 — `docs/infra.md` gets the one-time setup as commands the owner runs (or authorizes Claude to run): the Microsoft Entra app registration and its service principal; one federated credential per environment with subject `repo:alexcanario/simulab:environment:<environment>`; the roles Contributor and Role Based Access Control Administrator scoped to that environment's resource group only (the deploy creates role assignments for the apps' managed identities); and the `gh` commands that create the two GitHub environments, the production reviewer, the deployment policies of BR2, the variables of BR3 and the secret of BR5. Production's Azure part is run at the first release, when its resource group exists.
- BR9 — The Deploy command column of `docs/infra.md` becomes, per environment, `gh workflow run deploy.yml --ref <tag> -f environment=<environment>`; the `aspire deploy` command stays in the file as what the workflow runs. The row "Deploy workflow" of "Code hosting, CI and board" becomes `provisioned`.

## Screens and API
None. The only interface is GitHub's "Run workflow" form and its approval button.

## Acceptance criteria
| AC | Rule | Given / When / Then | Proved by |
|---|---|---|---|
| AC1 | BR1, BR4 | Given `deploy.yml`, when it is parsed, then its only trigger is `workflow_dispatch` with a required choice input `environment` of exactly `staging` and `production`, and the deploy job's `environment` is that input. | test |
| AC2 | BR3 | Given `deploy.yml`, when it is parsed, then `permissions` is exactly `id-token: write` and `contents: read`, the sign-in uses `azure/login` with `client-id`, `tenant-id` and `subscription-id` taken from `vars.*`, and no `secrets.*` other than `OPENIDDICT_CLIENT_SECRET` appears anywhere in the file (no `creds`, no client secret). | test |
| AC3 | BR2 | Given `deploy.yml`, when it is parsed, then the ref check is the first step of the job, before `azure/login`, and it accepts `refs/heads/main` and `refs/tags/v*` for staging and only `refs/tags/v*` for production. Given a production run on `main`, when it starts, then it fails in that step and no Azure sign-in happens. | test; validation step |
| AC4 | BR5, BR9 | Given `deploy.yml` and `docs/infra.md`, when the deploy command is resolved for each environment, then it is character for character the `aspire deploy` command `docs/infra.md` declares for that environment, and the Deploy command column holds the `gh workflow run` form. | test |
| AC5 | BR5 | Given `deploy.yml` and `Directory.Packages.props`, when compared, then the Aspire CLI version the workflow installs equals the version of the `Aspire.*` packages. | test |
| AC6 | BR6 | Given `deploy.yml`, when parsed, then the job's `concurrency` group contains the environment input and `cancel-in-progress` is false. | test |
| AC7 | BR7 | Given `deploy.yml`, when parsed, then a step after the deploy waits on `vars.CHECK_URL` for at most 5 minutes when it is set, and the run writes environment, ref and commit to the job summary. Given the staging deploy of AC9, then the check passes. | test; validation step |
| AC8 | BR8, BR9 | Given `docs/infra.md`, then it has the setup commands of BR8, the `gh workflow run` Deploy commands and the Deploy workflow row `provisioned`. | validation step |
| AC9 | UC1, BR3, BR5 | Given staging provisioned and the setup of BR8 done, when the owner runs `deploy` on `main` for `staging`, then the run is green, signs in with no stored Azure secret, and the staging check URL answers 200 with the deployed version. | validation step |
| AC10 | UC2, BR4 | Given the `production` environment, when the owner runs `deploy` on a tag `v*` for `production`, then the run waits for the owner's approval; when the owner rejects it, then it ends without any Azure sign-in. | validation step |
| AC11 | — | Localization: the item adds no UI text; the missing-key test stays green. The app manual does not change (no behavior visible to students). | test (existing missing-key test) |

## Decisions
- 2026-10-04 — Manual trigger only (`workflow_dispatch`) with an `environment` input; `docs/infra.md`'s Deploy command becomes the `gh workflow run`, so `/agile:publish` triggers it with no change of its own — one deploy path, and nothing deploys without being asked (owner, question 1).
- 2026-10-04 — Staging accepts `main` or a tag `v*`; production only a tag `v*` — testing unversioned work on staging is useful, production stays traceable to a release (owner, question 2).
- 2026-10-04 — Parking and starting staging from a workflow is out of scope, captured as F-81 — the park/start commands are confirmed only by F-64 (owner, question 3).
- 2026-10-04 — Production approval: the owner as the only required reviewer, deployments only from tags `v*`; staging has no reviewer — ADR-0002 #8 asks for approval before production (owner, question 4).
- 2026-10-04 — The OpenIddict client secret is a GitHub environment secret per environment, passed as `Parameters__openiddict-client-secret` — ADR-0002's "no stored cloud secret" is about the Azure sign-in, which stays OIDC; reading it from Key Vault first would need the vault before the deploy that creates it (owner, question 5).
- 2026-10-04 — The Azure identity and the GitHub environments are created by commands written in `docs/infra.md`, run by the owner (or by Claude with authorization at that moment); the roles are scoped to each environment's resource group, which F-64 creates — the workflow can touch nothing else in the subscription (owner, question 6).
- 2026-10-04 — Test package approved: `YamlDotNet` 18.1.0, MIT (nuget.org, checked 2026-10-04), test project only — the tests read the workflow as YAML instead of matching strings (owner, question 7). Production code needs no new package.
- 2026-10-04 — `ci.yml` keeps its action versions; moving it to the current majors is F-82 (owner, question 8).
- 2026-10-04 — The build waits for F-64; the validation is a real staging deploy, and production only has its approval gate and ref rule checked (its environment does not exist until the first release) (owner, question 9).
- 2026-10-04 — The version deployed is the ref the run was started on, not a separate input — GitHub then applies the environment's deployment branch and tag policy to that same ref, so the rule of BR2 is enforced by GitHub as well as by the first step (Claude, technical). Consequence: a tag created before F-65 merged has no `deploy.yml` and cannot be deployed by the workflow.
- 2026-10-04 — Action versions in `deploy.yml`: the current majors, checked 2026-10-04 on GitHub releases: `actions/checkout@v7`, `actions/setup-dotnet@v6`, `azure/login@v3` (Claude, technical).
- 2026-10-04 — The Aspire CLI is installed on the runner at the pinned version (`dotnet tool install --global Aspire.Cli --version <version>`), and the version is read from one place the test of AC5 compares with `Directory.Packages.props` (Claude, technical).
- 2026-10-04 — The tests live in `tests/Hosts/Simulab.AppHost.Tests` beside F-62's publish tests (Claude, technical).
- 2026-10-04 — Approved by the owner ("aprovo F-65"). The build waits for F-64.
- 2026-10-04 — Glossary: added the technical terms "federated credential", "GitHub environment", "required reviewer" and "workflow_dispatch" (Claude, technical).

## Out of scope
- Parking and starting staging from a workflow — F-81.
- Updating `ci.yml` to current action majors — F-82.
- Creating the staging environment, its resource group and check URL — F-64.
- Creating the production environment in Azure — the first release.
- Deploying automatically on push, tag or release.
- Rolling back to an earlier version by a separate action (a rollback is a deploy of the earlier tag).

## Open questions
None.
