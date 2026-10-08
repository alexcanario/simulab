---
updated: <YYYY-MM-DD>
---
<!--
How the app runs, where, and with which secrets. Filled by /agile:bootstrap from quiz round 6; updated by /agile:ship when any of it changes.
Save as: docs/infra.md. Write what is true today, not what is planned: an environment that does not exist yet is `planned`.
Never write a secret's value here, only its name and where it is kept.
`/agile:publish <environment>` runs the Deploy command of that row (`not declared` when there is none; the profile has the recipe for containers + Aspire) with the environment variables AGILE_ENVIRONMENT, AGILE_VERSION, AGILE_TAG and AGILE_ARTIFACTS, in its own worktree detached at the release tag. The command runs in the machine's default shell: cmd.exe on Windows (%AGILE_TAG%), /bin/sh elsewhere ($AGILE_TAG), so keep it to one portable command line.
Check URL (optional): polled for HTTP 200 for up to 60 s after the command exits 0. Version (deployed on): written only by /agile:publish, as `v<x.y.z> (YYYY-MM-DD)`.
Expected secrets: a name the deploy needs from the environment of the machine that deploys is listed with "environment variable" in its last column and the environment it belongs to; /agile:publish checks that it is set (the value is never read), and a recipe secret is named Parameters__<name>.
The deploy pipeline (.github/workflows/deploy.yml, written by /agile:bootstrap when origin is on github.com and a non-local environment declares a command) runs the same Deploy command from this table and passes the same secrets, by name, from the GitHub environment of the same name: add a secret here and to that environment, then offer the workflow's new `secrets.<name>` line to /agile:sync.
-->
# Infra

Technical terms: [glossary](glossary.md)

## Run locally
- Prerequisites: <SDK version, container runtime, tools>
- Start: `<app host command>` → <URL>. Sign in as: <seed user and role; password kept in <where>>
- Reset local data: `<command>`

## Environments
| Environment | Status (`provisioned` / `planned`) | URL | How it is deployed | Configuration and secrets live in | Deploy command | Check URL | Version (deployed on) |
|---|---|---|---|---|---|---|---|
| local | provisioned | | app host | user secrets | not declared | | |

## Cloud accounts
<!--
One row per non-local environment: the client's cloud account its deploy lands in. The repository, the tag, the GitHub Release and the board stay in the owner's GitHub; only the deploy moves.
Cloud: `azure`, `aws`, `gcp`, `other` or `none` (a host without a cloud account, such as the compose recipe). An environment with a Deploy command and no row stops /agile:publish.
Azure: Tenant and Subscription are GUIDs (not a name or a domain), written here (ids are not secrets; no secret goes in this file). /agile:publish keeps one `az` login per subscription in
`<home>/.agile/azure/<tenant>/<subscription>`, checks that it sees that subscription before the deploy command and runs the command with Azure__TenantId, Azure__SubscriptionId, Azure__CredentialSource=AzureCli
(and Azure__ResourceGroup and Azure__Location when filled), after removing every inherited AZURE_*, ARM_* and Azure__* variable. Log in once, in Git Bash: AZURE_CONFIG_DIR="<folder>" az login --tenant <tenant>.
AWS: Subscription or account is the 12-digit account id (aws sts get-caller-identity prints it) and Region an AWS region code such as us-east-1; Tenant and Resource group are ignored. /agile:publish keeps one `aws` login per account in
`<home>/.agile/aws/<account>` (AWS_CONFIG_FILE and AWS_SHARED_CREDENTIALS_FILE point there), checks with `aws sts get-caller-identity` that it is that account before the deploy command and runs the command with AWS_CONFIG_FILE,
AWS_SHARED_CREDENTIALS_FILE, AWS_REGION and AWS_DEFAULT_REGION, after removing every inherited AWS_* variable. Log in once, in Git Bash: AWS_CONFIG_FILE="<folder>/config" AWS_SHARED_CREDENTIALS_FILE="<folder>/credentials" aws configure sso.
Google Cloud: Subscription or account is the project id (not its name or its number; gcloud projects list prints it); Tenant and Resource group are ignored. One `gcloud` login per project in `<home>/.agile/gcp/<project>` (CLOUDSDK_CONFIG), checked with
`gcloud projects describe <project>`: the login needs resourcemanager.projects.get on it (Browser, Viewer, Editor or Owner). The command runs with CLOUDSDK_CONFIG, CLOUDSDK_CORE_PROJECT and GOOGLE_CLOUD_PROJECT, after removing every inherited CLOUDSDK_*,
GOOGLE_APPLICATION_CREDENTIALS, GOOGLE_CLOUD_PROJECT, GCLOUD_PROJECT and GCP_PROJECT. Log in once, in Git Bash: CLOUDSDK_CONFIG="<folder>" gcloud auth login, then CLOUDSDK_CONFIG="<folder>" gcloud auth application-default login (a deploy command that uses a Google SDK reads the second).
`other` is shown in the plan and not checked.
An environment whose AppHost says `Deploy:Target` `aca` (Azure Container Apps, the profile's second recipe) needs Resource group, Region and Monthly budget filled (Azure__ResourceGroup, Azure__Location): the plan and the pipeline stop without them. Its URL and Check URL stay blank until the first deploy prints the generated address; fill them by hand then, or, for the client's own domain, write it as the URL (see "Custom domain" below).
Monthly budget: the ceiling of one month's spend, a whole number in the billing currency of the subscription, or `none`; only an `aca` environment uses it (filled anywhere else it is reported as ignored). After the deploy command, whatever its exit code while the resource group exists, the plugin writes the budget `agile-monthly-ceiling` on that group with `az rest`: Owner and Contributor of the group get Azure's e-mail at 80 % and 100 % of the spend and at 100 % of the forecast (no e-mail address is written here), and nothing is stopped when it fires. A budget starts on the first day of the month it was created and keeps that date; `none` writes and deletes nothing, and the plan names a budget left from an earlier ceiling.
Parking an `aca` environment (`/agile:publish <environment> --park`, `--resume` to bring it back; each shows its plan and waits for the owner's yes) sets every container app to minimum 0 and stops its PostgreSQL server, marked `agile-parked`; the registry, the log workspace and the storage keep costing (about 8.7 a month, measured 2026-10-06), an Azure SQL database pauses itself, and Azure starts a stopped server by itself after 7 days (park again). A deploy of a parked environment starts its database first.

What an `aca` environment has cost this month is read on demand: `/agile:publish <environment> --cost` prints `Spend this month: <amount> <currency> of <ceiling> (<n> %)` from Azure Cost Management (the resource group, in the subscription's billing currency; the figures lag 8 to 24 hours), and `--plan` shows the same as `Spend:` after `Budget:`. A deploy, the pipeline and `/agile:status` never read it.
Deleting an `aca` environment is deleting its resource group and then its budget (the budget outlives its group), both by hand in the client's subscription: `az rest --method delete --url "/subscriptions/<subscription>/resourceGroups/<group>/providers/Microsoft.Consumption/budgets/agile-monthly-ceiling?api-version=2023-11-01"`.
Custom domain: an `aca` environment answers on the client's own domain when its URL in `## Environments` is that domain (`https://app.client.com` or `https://client.com`); a blank URL or a generated `azurecontainerapps.io` address means none. `/agile:publish <environment>` (`--plan` shows it) and the pipeline read what Azure expects, resolve the client's DNS records and list the ones that are missing or wrong, each with its expected value (a subdomain: `CNAME <sub>` straight to the app's generated address, no proxy in between, and `TXT asuid.<sub>`; an apex: `A @` to the environment's static IP and `TXT asuid`; a CAA record on the root must allow `0 issue "digicert.com"`). The client creates them at their DNS provider, then the next deploy adds the domain, has Azure issue a free managed certificate and binds it, with no step in the Azure portal. Check URL may be the domain once it is bound.
What to ask the client's administrator: locally, the owner as a guest in their tenant with the role Contributor on the subscription (plus Role Based Access Control Administrator limited to the roles the app's managed
identities need, when the deploy creates role assignments; for an `aca` environment, which always creates them, Contributor plus Role Based Access Control Administrator on the subscription); for the pipeline, an app registration in their tenant with the same roles and a federated credential for `repo:<owner>/<repo>:environment:<environment>`.
GitHub: set the variables AZURE_CLIENT_ID, AZURE_TENANT_ID and AZURE_SUBSCRIPTION_ID on the GitHub environment (variables, not secrets), and give each Azure environment a deployment protection rule: "Selected branches and tags"
with the tag pattern `v*` only, and required reviewers on production. A manual run can start from any branch and gets the same federated subject: the protection rule is the boundary, the plugin's check only catches a mistake.
What to ask the administrator for the pipeline on AWS: an IAM role that trusts GitHub's OIDC provider for `repo:<owner>/<repo>:environment:<environment>`; set AWS_ROLE_ARN and AWS_REGION on the GitHub environment. On Google Cloud: a Workload Identity pool and provider and a service account
(with resourcemanager.projects.get on the project); set GCP_WORKLOAD_IDENTITY_PROVIDER and GCP_SERVICE_ACCOUNT (and GCP_PROJECT_ID when the table has more than one project) on the GitHub environment. All variables, never secrets.
-->
| Environment | Client | Cloud | Tenant | Subscription or account | Resource group | Region | Monthly budget |
|---|---|---|---|---|---|---|---|

## Expected secrets
| Name | Used by | Kept in (per environment) |
|---|---|---|

## Store signing (a mobile app only)
- `/agile:publish` signs the Android `.aab` with the upload key named by four environment variables: `ANDROID_SIGNING_KEYSTORE` (absolute path, outside the repository), `ANDROID_SIGNING_ALIAS`, `ANDROID_SIGNING_STORE_PASS`, `ANDROID_SIGNING_KEY_PASS`. Their values are never written here.
- The keystore is kept in: <where, and where its backup is>. Passwords kept in: <where>.

## Shared-screens package (a Hybrid app and its site only)
- The site's `/agile:publish` pushes `<App>.Contracts` and `<App>.Shared` to the GitHub Packages feed of the `origin` owner; the app restores them. Both sides read the token from the environment variable `GITHUB_PACKAGES_TOKEN`, a classic personal access token (`write:packages` on the site, `read:packages` in the app). Its value is never written here.
- Kept in: <where the token lives on each machine and in CI>.

## Desktop updates (a desktop app only)
- Update source: <\\server\share\<app> | https://updates.example.com/<app> | https://github.com/<owner>/<repo> | not declared>
- Update source (linux): <only with linux-x64 in the head: the share's path as mounted on Linux (/mnt/updates/<app>) | https://... | not declared (an https main line is used)>
- Beta channel: <every merge | off>
- With a share and `every merge`, `/agile:ship` packs each merge to the main branch as `<Version>-beta` on Velopack's beta channel and sends it to the share (the five newest betas stay); testers install `<App>-beta-Setup.exe` from there, and the stable Setup.exe takes a machine back to stable. `off`, or an https source, sends no beta.
- `/agile:publish` packs the Windows head with Velopack (and an Avalonia head's `linux-x64` as an AppImage, which reads the Linux line) and sends the feed to a share; an https URL receives nothing from the plugin (the output lists the files to copy). A public GitHub repository (`https://github.com/<owner>/<repo>`, the app's own or one that holds only the releases) gets the GitHub Release and the feed attached to it (the apps see a release up to 60 s after the upload, and a repository without a token allows 60 checks per hour per IP address); `/agile:publish` stops when the repository is not public, and a GitHub source takes no beta channel and one `win-*` runtime per head. The app reads the source from `Updates:Source` in its `appsettings.json`. Never a token or a private repository's GitHub Releases address here.

## Code signing (a desktop app only)
- Code signing: none
- Signing account: <artifact-signing only: <tenant id>/<subscription id> of the Azure account that owns the signing account>
- `none` (or no line) ships the Windows installer unsigned and Windows warns "Unknown publisher". A mode makes `vpk` sign Setup.exe, the app's files and Update.exe of every Windows release and beta, from one environment variable of the shell that runs `/agile:publish` (and `/agile:ship`, for the beta); the plugin passes only that one to `vpk`, so a variable left for another project never signs this one. The publish stops when the variable is unset. An app with no update source is signed too: the files of its `win-*` zips are signed by the same `vpk` (packed into a scratch folder, only the signed files are kept), so the project needs `vpk` as a local tool (`dotnet tool install vpk --version 1.2.161`; `/agile:publish` asks before installing it), and every `.exe` and `.dll` of the zip is checked. The Linux AppImage and the zips of non-Windows runtimes are never signed.
  - `artifact-signing` (Microsoft's Artifact Signing, organizations in the USA, Canada, EU and UK, individuals in the USA and Canada): `VPK_AZURE_TRUSTED_SIGN_FILE` = absolute path of its `metadata.json`, outside the repository (`Endpoint`, `CodeSigningAccountName`, `CertificateProfileName`, and `"ExcludeCredentials": ["ManagedIdentityCredential", "SharedTokenCacheCredential", "VisualStudioCredential", "VisualStudioCodeCredential"]` so the signing uses the Azure CLI login below). The login lives in `<home>/.agile/azure/<tenant>/<subscription>`; log in once, in Git Bash: AZURE_CONFIG_DIR="<that folder>" az login --tenant <tenant>.
  - `signtool` (a certificate from a CA, on a token or a cloud HSM): `VPK_SIGN_PARAMS` = signtool's parameters, the certificate chosen by `/sha1 <thumbprint>` (or the token's CSP), with a timestamp: `/fd SHA256 /tr <the CA's timestamp URL> /td SHA256 /sha1 <thumbprint>`. Never `/p`, nor a token PIN in `/kc` (`[{{...}}]`): the publish refuses a secret on a command line.
  - `template` (a vendor's own signing tool): `VPK_SIGN_TEMPLATE` = its command with `{{file}}` (one file) or `{{file...}}` (several) where the file goes.
- The certificate or the Artifact Signing account is kept in: <where; who renews it and when it expires>. The variable's value is never written here.

## Release steps
1. <step, or "no release process yet">

## Measured times
| What | Budget | Last measured (date) |
|---|---|---|
| Full build | | |
| Full test suite | < 5 min | |
