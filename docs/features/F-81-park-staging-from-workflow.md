---
feature: F-81
epic: Cloud hosting and operations
status: done
board: 124
version: 1
---
# Park and start staging from a GitHub Actions workflow

Technical terms: [glossary](../glossary.md)

## Summary
One manual GitHub Actions workflow, `staging-power.yml`, that parks or starts the staging environment with the commands of `docs/infra.md` ("Staging start and stop"), so a test window is opened and closed from GitHub (Actions page or `gh workflow run`) instead of a local `az` session. Staging exists (F-64, `rg-simulab-staging`, Central US) and the OIDC sign-in and the GitHub environment `staging` exist (F-65). Raised as out of scope by the F-65 refinement, 2026-10-04.

## Start
- Depends on: F-64 (done: the environment and its resource group); F-65 (done: the OIDC sign-in and the GitHub environment `staging`).
- Waits on (to start): nothing beyond the dependencies.
- Needed to validate: the provisioned staging environment, and the workflow file on `main` (GitHub lists a manual workflow only from the default branch) — owner.
- Model: sonnet (no signal)
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-116 and F-117 are possible (this item adds a new workflow file and a test, and edits only "Staging start and stop" and one table row of `docs/infra.md`); do not run them side by side with another item that edits `docs/infra.md`.

## Goal
Open and close a staging test window from GitHub with one command, so a forgotten window does not keep paying for the apps and the PostgreSQL server, and no local `az` session is needed.

## Users and use cases
- UC1 The owner starts staging for a test window: Actions → `staging-power` → Run workflow → `action` = `start`, or `gh workflow run staging-power.yml --ref main -f action=start`. When the run is green, the site answers on its Check URL.
- UC2 The owner parks staging when the window ends: the same with `action` = `park`. When the run is green, the apps are at zero replicas and the PostgreSQL server is `Stopped`.
- UC3 The owner runs `park` on a staging that is already parked (or `start` on one that is already running): the run is green and changes nothing.

## Business rules
- BR1 The workflow starts only by hand (`workflow_dispatch`), with one required input `action` (choice: `park`, `start`). It has no `environment` input: the job runs in the GitHub environment `staging` and nothing else, so production can never be parked or started by it (ADR-0002).
- BR2 The version of the workflow that runs is the ref it was started on. `main` or a tag `v*` is accepted, as for the staging deploy (F-65 BR2); any other ref fails in the first step, before any Azure sign-in, and the GitHub environment's own ref policy says the same.
- BR3 The Azure sign-in is `azure/login` with OIDC federation, as in `deploy.yml` (F-65 BR3): `id-token: write` and `contents: read` only, the client, tenant and subscription ids and the resource group from the GitHub environment variables. The workflow reads no secret at all.
- BR4 The commands are the ones `docs/infra.md` declares in "Staging start and stop", unchanged, character for character (F-65 BR5 and AC4): the doc holds them as fenced command blocks that use the shell variables `RG` and `SERVER`; the workflow runs those same lines. Park: `az containerapp update` to zero minimum replicas for `api`, `web` and `redis`, then `az postgres flexible-server stop`. Start: `az postgres flexible-server start`, then `az containerapp update` for `redis` (1), `api` (1) and `web` (minimum 0, maximum 1).
- BR5 No resource name is hard-coded. `RG` is the variable `AZURE_RESOURCE_GROUP` of the environment; `SERVER` is the PostgreSQL server found in that resource group (`az postgres flexible-server list`). When the group holds no server or more than one, the run fails before changing anything and says so.
- BR6 Idempotent: before the commands the workflow reads the server state. `park` on a `Stopped` server and `start` on a `Ready` server skip the server command and still make the apps match (the `az containerapp update` calls are repeatable); a state that is neither `Ready` nor `Stopped` (for example `Starting`) fails the run with the state in the message.
- BR7 The workflow checks its own result. After `park`: the server state is `Stopped` and the three apps have a minimum of zero replicas, else the run fails. After `start`: the server is `Ready`, and the run waits for the Check URL (`vars.CHECK_URL`, the same variable the deploy uses) to answer HTTP 200 for at most 10 minutes (a cold `web` needs the first request to wake), else it fails.
- BR8 One run per environment at a time, and never one together with a deploy: the job uses the concurrency group of the staging deploy (`deploy-staging`) and never cancels a run in progress. GitHub keeps one pending run per group, so a newer pending run replaces an older pending one, a deploy included; `docs/infra.md` says so.
- BR9 The run summary names the action, the ref, the commit and the resulting state; after `park` it also gives the date Azure will start the server by itself (7 days after the stop) and says a longer park needs the `park` run again.
- BR10 `docs/infra.md` becomes the one place of the commands and of the how-to: "Staging start and stop" no longer says "declared, not yet run", gives the commands as the blocks of BR4, says the workflow runs them (and the `gh workflow run` form), and keeps the 7-day limit. The table "Code hosting, CI and board" gets a row for the workflow.

## Screens and API
No screen and no API route. The GitHub Actions page and `gh workflow run` are the entry points (UC1 and UC2). No Wireframe: the item adds no page, dialog or window of the app.

## Docs
- none — the commands and the how-to live in docs/infra.md (a project document, outside the technical-page folders), updated by AC10

## Acceptance criteria
- AC1 Given `staging-power.yml`, when it is parsed, then its only trigger is `workflow_dispatch` with a required choice input `action` of exactly `park` and `start`, it has no input named `environment`, and the job's `environment` is the literal `staging`. (BR1)
- AC2 Given the workflow, when parsed, then `permissions` is exactly `id-token: write` and `contents: read`, the sign-in uses `azure/login` with `client-id`, `tenant-id` and `subscription-id` from `vars.*`, and the file has no `secrets.*` reference. (BR3)
- AC3 Given the workflow, when parsed, then the job's `concurrency` group is `deploy-staging` and `cancel-in-progress` is false. (BR8)
- AC4 Given the workflow, when parsed, then its first step compares the ref with the rule of BR2 and comes before the Azure sign-in step, and a ref other than `main` or a tag `v*` fails it. (BR2)
- AC5 Given `docs/infra.md` and the workflow, when the park and start command blocks are read from the doc, then each line is contained character for character in the matching step of the workflow, the park step runs only when `action` is `park` and the start step only when it is `start`, and the apps are changed before the server on park and the server before the apps on start. (BR4)
- AC6 Given the workflow, when parsed, then it contains no literal resource group, server or storage name (`rg-`, `postgres-`), takes `RG` from `vars.AZURE_RESOURCE_GROUP`, and finds `SERVER` with a list of the resource group that fails when the count is not one. (BR5)
- AC7 Given the workflow, when parsed, then the state guard exists: the server command of `park` is skipped when the state is `Stopped`, the one of `start` when it is `Ready`, and any other state fails the step. (BR6)
- AC8 Given the workflow, when parsed, then a step after `park` fails unless the server state is `Stopped` and the three apps have zero minimum replicas, and a step after `start` waits on `vars.CHECK_URL` for at most 10 minutes and fails when it never answers 200. (BR7)
- AC9 Given the workflow, when parsed, then the summary step writes the action, ref, commit and state, and after `park` the 7-day restart date. (BR9)
- AC10 Given `docs/infra.md`, when read, then "Staging start and stop" holds the command blocks of BR4, names `staging-power.yml`, no longer contains "declared, not yet run", and the table "Code hosting, CI and board" has the workflow row. (BR10)
- AC11 Given the real staging running, when the owner runs `park`, then the run is green, `api`, `web` and `redis` have zero minimum replicas and the PostgreSQL server is `Stopped`; and given it parked, when the owner runs `start`, then the run is green and the Check URL answers 200. Running either one again is green and changes nothing. (UC1, UC2, UC3, BR6, BR7; validation script)
- AC12 No UI text is added and no resource file is touched.

## Criterion → test

Tests locked: 36a9338604136a522310fa38778c6990504f650c (2 files); unlocks: 1

| Criterion | Test |
|---|---|
| AC1 | `StagingPowerWorkflowTests.Trigger_IsOnlyWorkflowDispatchWithARequiredActionChoice_AndNoEnvironmentInput`, `StagingPowerWorkflowTests.Job_RunsInTheLiteralEnvironmentStaging` |
| AC2 | `StagingPowerWorkflowTests.Permissions_AreExactlyIdTokenWriteAndContentsRead`, `StagingPowerWorkflowTests.AzureLogin_UsesOidcWithIdsFromVariables`, `StagingPowerWorkflowTests.Workflow_ReadsNoSecretAtAll` |
| AC3 | `StagingPowerWorkflowTests.Concurrency_IsTheGroupOfTheStagingDeploy_AndNeverCancelsARunningRun` |
| AC4 | `StagingPowerWorkflowTests.RefCheck_IsTheFirstStep_BeforeTheAzureSignIn`, `StagingPowerWorkflowTests.RefCheck_AcceptsOnlyMainOrATagV` |
| AC5 | `StagingPowerWorkflowTests.ParkStep_RunsTheLinesOfInfraCharacterForCharacter_OnlyOnPark_AppsBeforeTheServer`, `StagingPowerWorkflowTests.StartStep_RunsTheLinesOfInfraCharacterForCharacter_OnlyOnStart_ServerBeforeTheApps` |
| AC6 | `StagingPowerWorkflowTests.Workflow_HardCodesNoResourceName_AndTakesTheGroupFromTheEnvironmentVariable`, `StagingPowerWorkflowTests.FindServerStep_FailsUnlessTheGroupHoldsExactlyOneServer` |
| AC7 | `StagingPowerWorkflowTests.FindServerStep_AcceptsOnlyReadyOrStopped_AndNamesTheOtherState`, `StagingPowerWorkflowTests.ParkStep_StopsTheServerOnlyWhenItIsNotStopped_AndAlwaysUpdatesTheApps`, `StagingPowerWorkflowTests.StartStep_StartsTheServerOnlyWhenItIsNotReady_AndAlwaysUpdatesTheApps` |
| AC8 | `StagingPowerWorkflowTests.ParkCheck_FailsUnlessTheServerIsStoppedAndTheThreeAppsHaveZeroMinimumReplicas`, `StagingPowerWorkflowTests.WaitStep_WaitsOnTheCheckUrlForAtMostTenMinutes_AfterAStart_AndFailsOtherwise`, `StagingPowerWorkflowTests.WaitStep_PassesWhenTheCheckUrlAnswers200` |
| AC9 | `StagingPowerWorkflowTests.Summary_NamesTheActionTheRefTheCommitTheState_AndTheSevenDayRestartAfterAPark` |
| AC10 | `StagingPowerWorkflowTests.Infra_StartAndStopSection_HoldsTheBlocksAndNamesTheWorkflow`, `StagingPowerWorkflowTests.Infra_CodeHostingTable_HasTheWorkflowRow` |
| AC11 | validation step 2 to 5 |
| AC12 | validation step 1 (no resource file in the diff) |

## Decisions
- 2026-10-22 — Only started by hand; no schedule in this item — F-65 D1 (manual trigger only, nothing runs without being asked) still holds — confirmed: F-65
- 2026-10-22 — The workflow runs the commands of `docs/infra.md` unchanged and a test compares both — one source of truth — confirmed: F-65
- 2026-10-22 — Same OIDC identity (`simulab-deploy`) and the GitHub environment `staging`; no new credential, no new role — confirmed: F-65. The role Contributor on `rg-simulab-staging` covers `Microsoft.App/containerApps/write` and the PostgreSQL flexible-server `stop` and `start` actions; not verified without Azure, proven by AC11.
- 2026-10-22 — Production is never parked: the workflow has no `environment` input and the job's environment is the literal `staging` — ADR-0002 and `docs/infra.md` — confirmed: docs/decisions/ADR-0002 (the confirmed question said "any other environment fails before the sign-in"; a missing input is the stronger form of the same rule: nothing to refuse)
- 2026-10-22 — One workflow with an `action` input (`park`, `start`), not two workflows — shared sign-in, ref rule, concurrency group and test — asked
- 2026-10-22 — The scheduled re-park (every 6 days, because Azure restarts a stopped PostgreSQL server after 7 days) is a separate idea that depends on F-117 (the plugin's `agile-park.js` requires its `agile-deploy.js`); this item only documents the limit — asked
- 2026-10-22 — The commands are plain `az` steps in the workflow, not the plugin's `agile-park.js`: that script needs `.github/scripts/agile-deploy.js`, which this repository does not have until F-117, and the plugin's park path is built for a `## Schedule` table this item does not use (Claude, technical)
- 2026-10-22 — The server name is found with `az postgres flexible-server list` in the environment's resource group instead of a new variable, so a re-created server needs no variable change (Claude, technical)
- 2026-10-22 — Same concurrency group as the staging deploy (`deploy-staging`): a park must never run during a deploy. Cost: a newer pending run replaces an older pending one, so a park queued behind a deploy can cancel a deploy waiting in the same queue; documented, never cancelling a run in progress (Claude, technical)
- 2026-10-22 — Start waits at most 10 minutes for the Check URL (the deploy waits 5): a cold start follows a server start that takes about 3 to 4 minutes (Claude, technical; F-65 BR7 is the precedent)
- 2026-10-22 — Tests live in `tests/Hosts/Simulab.AppHost.Tests` as `StagingPowerWorkflowTests`, reading the YAML as F-65's `DeployWorkflowTests` does; no new package (`YamlDotNet` is already referenced by that project) (Claude, technical)
- 2026-10-22 — No security section: no route, screen, upload or inbound call is opened, and the sign-in and its permissions are F-65's (no door by `model-choice.md` signal 1) (Claude, technical)
- 2026-10-22 — A sign-in session lives in Redis, so a park signs every tester out; that is the documented behavior of F-64 (BR4) and is not new here (Claude, technical)
- 2026-10-22: plan — 1 tests of AC1-AC10 (workflow read as YAML, steps run in bash with a fake `az`) plus a stub workflow and a shared test helper, locked → red for missing behavior; 2 `staging-power.yml` (ref check, sign-in, server and state, park, start, checks, summary) → AC1-AC9 green; 3 `docs/infra.md` command blocks, workflow row and how-to → AC5 and AC10 green; 4 gate, scan, validation script → `agile gate GREEN`. No design passes (no project, contract, message or schema). Reuse: `DeployWorkflowTests` pattern; its private bash runner is copied into a new helper (debt, the old file stays untouched).
- 2026-10-22: tests unlocked (the Summary test compared YAML nodes by reference across two parses and could never pass; it now compares the last step by name) - owner chose "Destravar e corrigir" as recommended
- 2026-10-22: the `gh workflow run` forms are inline code in the section, not a fenced block, because the locked tests read the fenced `bash` blocks as the park and start commands (Claude, technical)
- 2026-10-22: `## Docs` set to none: techdocs.js check refuses docs/infra.md as a reference page (not under docs/reference/) and the "none" how-to line; the infra.md edit stays and is proven by AC10 (Claude, technical)
- 2026-10-22: Reuse: `DeployWorkflowTests` pattern (YAML read, bash run of a step), the sign-in and ref-check shape of `deploy.yml`; nothing else found to reuse.
- 2026-10-22: Shortcuts: none

## Out of scope
- A scheduled park, resume or re-park (a new idea, depends on F-117).
- Parking or starting production (never, ADR-0002).
- What `aspire deploy` does to a parked staging: not verified and not covered; the doc says to start staging before a deploy as advice only, and the first real deploy after a park is the proof.
- The cost measurement of F-64 BR8 (done there), and changes to the plugin's `/agile:publish --park`.
- Updating the actions of `ci.yml` (F-82).

## Open questions
- (none)

## Change notes

## Validation script
### English
Needed to validate: the provisioned staging environment (F-64, in place now: `rg-simulab-staging`, Central US, with the F-65 sign-in and the variables `AZURE_RESOURCE_GROUP` and `CHECK_URL` in the GitHub environment `staging`) and `staging-power.yml` on `main`, because GitHub lists a manual workflow only from the default branch. The owner provides both. The workflow is on `main` only after the merge, so steps 2 to 6 run after it; step 1 needs nothing but the worktree. No app host is started and no local data is touched. There is no screen, so no language switch and no keyboard pass.

1. Before the merge, in the worktree, run the tests of the workflow and look for resource files in the diff.
   - [ ] 1.1 Stop the app host if it runs (a running host locks `bin/`), then run `dotnet test tests/Hosts/Simulab.AppHost.Tests --filter "FullyQualifiedName~StagingPowerWorkflowTests"` (the same line in Git Bash and in PowerShell 7) → `Passed!  - Failed: 0, Passed: 36`.
   - [ ] 1.2 Run `git diff --name-only main...HEAD -- "*.resx"` (the same line in both shells) → no output: no UI text was added (AC12).
2. [ ] After the merge, park a running staging: `gh workflow run staging-power.yml --ref main -f action=park` (the same line in both shells), then `gh run watch` and pick the run → the run is green; the run summary lists action, ref, commit, the server `Stopped` and the date Azure restarts it (AC9, AC11).
3. Confirm the park in Azure (the same lines in both shells).
   - [ ] 3.1 `az postgres flexible-server list -g rg-simulab-staging --query "[].state" -o tsv` → `Stopped`.
   - [ ] 3.2 `az containerapp show -n api -g rg-simulab-staging --query properties.template.scale.minReplicas -o tsv`, then the same for `web` and `redis` → `0` each (an empty answer also means zero).
4. [ ] Park again: `gh workflow run staging-power.yml --ref main -f action=park`, then `gh run watch` → the run is green and the log of "Park staging" says `The server is already stopped` (UC3, BR6).
5. [ ] Start: `gh workflow run staging-power.yml --ref main -f action=start`, then `gh run watch` → the run is green; "Wait for the check URL" is green and says it answered 200; the summary shows the server `Ready`. Open the address `gh variable get CHECK_URL --env staging --repo alexcanario/simulab` prints in a browser → the site answers (AC8, AC11).
6. [ ] Start again: `gh workflow run staging-power.yml --ref main -f action=start`, then `gh run watch` → green, and the log of "Start staging" says `The server is already running`. Park staging again if the test window is over (UC2).
7. Optional, a ref that is not allowed (AC4 is already proven by tests): GitHub refuses any run that is not on the default branch or a tag unless the branch exists on the remote.
   - [ ] 7.1 `git push origin main:refs/heads/scratch-f81` (the same line in both shells), then `gh workflow run staging-power.yml --ref scratch-f81 -f action=park` → GitHub refuses the run (the environment policy allows `main` and `v*` tags) or the first step "Check the ref" fails with `cannot park or start staging`; the log has no "Sign in to Azure" step.
   - [ ] 7.2 `git push origin --delete scratch-f81` → the scratch branch is gone.

### pt-BR
Necessário para validar: o ambiente de staging provisionado (F-64, já no lugar: `rg-simulab-staging`, Central US, com o login da F-65 e as variáveis `AZURE_RESOURCE_GROUP` e `CHECK_URL` no ambiente `staging` do GitHub) e o `staging-power.yml` na `main`, porque o GitHub só lista um workflow manual a partir do branch padrão. Você fornece os dois. O workflow só está na `main` depois do merge, então os passos 2 a 6 rodam depois dele; o passo 1 só precisa do worktree. Nenhum app host é iniciado e nenhum dado local é tocado. Não há tela, então não há troca de idioma nem passada só por teclado.

1. Antes do merge, no worktree, rode os testes do workflow e procure arquivos de recursos no diff.
   - [ ] 1.1 Pare o app host se ele estiver rodando (um host rodando trava o `bin/`), depois rode `dotnet test tests/Hosts/Simulab.AppHost.Tests --filter "FullyQualifiedName~StagingPowerWorkflowTests"` (a mesma linha no Git Bash e no PowerShell 7) → `Passed!  - Failed: 0, Passed: 36`.
   - [ ] 1.2 Rode `git diff --name-only main...HEAD -- "*.resx"` (a mesma linha nos dois shells) → nenhuma saída: nenhum texto de UI foi adicionado (AC12).
2. [ ] Depois do merge, estacione um staging que está rodando: `gh workflow run staging-power.yml --ref main -f action=park` (a mesma linha nos dois shells), depois `gh run watch` e escolha a execução → a execução fica verde; o resumo lista ação, ref, commit, o servidor `Stopped` e a data em que o Azure o reinicia (AC9, AC11).
3. Confirme o estacionamento no Azure (as mesmas linhas nos dois shells).
   - [ ] 3.1 `az postgres flexible-server list -g rg-simulab-staging --query "[].state" -o tsv` → `Stopped`.
   - [ ] 3.2 `az containerapp show -n api -g rg-simulab-staging --query properties.template.scale.minReplicas -o tsv`, depois o mesmo para `web` e `redis` → `0` em cada (resposta vazia também significa zero).
4. [ ] Estacione de novo: `gh workflow run staging-power.yml --ref main -f action=park`, depois `gh run watch` → a execução fica verde e o log de "Park staging" diz `The server is already stopped` (UC3, BR6).
5. [ ] Inicie: `gh workflow run staging-power.yml --ref main -f action=start`, depois `gh run watch` → a execução fica verde; "Wait for the check URL" fica verde e diz que respondeu 200; o resumo mostra o servidor `Ready`. Abra no navegador o endereço que `gh variable get CHECK_URL --env staging --repo alexcanario/simulab` imprime → o site responde (AC8, AC11).
6. [ ] Inicie de novo: `gh workflow run staging-power.yml --ref main -f action=start`, depois `gh run watch` → verde, e o log de "Start staging" diz `The server is already running`. Estacione o staging de novo se a janela de testes acabou (UC2).
7. Opcional, um ref não permitido (o AC4 já está provado por testes): o GitHub só roda um workflow em um branch que existe no remoto.
   - [ ] 7.1 `git push origin main:refs/heads/scratch-f81` (a mesma linha nos dois shells), depois `gh workflow run staging-power.yml --ref scratch-f81 -f action=park` → o GitHub recusa a execução (a política do ambiente permite `main` e tags `v*`) ou o primeiro passo "Check the ref" falha com `cannot park or start staging`; o log não tem o passo "Sign in to Azure".
   - [ ] 7.2 `git push origin --delete scratch-f81` → o branch temporário some.

## Events

- 2026-10-22 question — asked 2, from context 4
- 2026-10-22 blocked — test wrong: Summary test compared YAML nodes by reference across separate parses; compare the last step by name

## Delivery

Gate time: 5 passes, 16 min 32 s — turn 2 (56 s), ship 1 (11 min 3 s), scan 2 (4 min 33 s); 0 red
Usage: 1009.9k weighted (estimate) — build 73% · ship 26% · other 1% — context per turn 191k → 235k — 1 session, 57 turns
Built 2026-10-22 on `feature/F-81`, validated by the owner (step 1 run by the owner; steps 2 to 7 need the workflow on `main`) and shipped 2026-10-22 as merge `7e56cfd` (app version 1.15.0, board #124).
- Full suite at the ship: 4924 tests, 0 failures; build 44 s, tests 619 s (over the 5 min budget of the profile: the QuestionBank, Identity and Catalog test projects take about 10 min each in parallel, a finding for the retro); 0 new warnings; `docs` gate green; security scan green (16 false positives and 13 fixed triaged earlier, 2 medium in `tools/Simulab.DocGen` left open). New tests: 36 (`StagingPowerWorkflowTests`). No new package.
- No manual page: the item changes no screen or text (AC12). Glossary: nothing missing.
- Validation evidence: step 1 (1.1 and 1.2) passed, confirmed by the owner in chat; the widget ticks were not used, so the evidence tool records 0 of 10. Steps 2 to 7 (AC11 and the real runs of AC8) are not proven before the merge: they run on `main` against the real staging. The first real run is the proof of the Contributor role on the PostgreSQL stop and start actions.
- Follow-up: F-147 (scheduled staging re-park, depends on F-117).
