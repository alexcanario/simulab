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

## Run locally
- Prerequisites: <SDK version, container runtime, tools>
- Start: `<app host command>` → <URL>. Sign in as: <seed user and role; password kept in <where>>
- Reset local data: `<command>`

## Environments
| Environment | Status (`provisioned` / `planned`) | URL | How it is deployed | Configuration and secrets live in | Deploy command | Check URL | Version (deployed on) |
|---|---|---|---|---|---|---|---|
| local | provisioned | | app host | user secrets | not declared | | |

## Expected secrets
| Name | Used by | Kept in (per environment) |
|---|---|---|

## Store signing (a mobile app only)
- `/agile:publish` signs the Android `.aab` with the upload key named by four environment variables: `ANDROID_SIGNING_KEYSTORE` (absolute path, outside the repository), `ANDROID_SIGNING_ALIAS`, `ANDROID_SIGNING_STORE_PASS`, `ANDROID_SIGNING_KEY_PASS`. Their values are never written here.
- The keystore is kept in: <where, and where its backup is>. Passwords kept in: <where>.

## Shared-screens package (a Hybrid app and its site only)
- The site's `/agile:publish` pushes `<App>.Contracts` and `<App>.Shared` to the GitHub Packages feed of the `origin` owner; the app restores them. Both sides read the token from the environment variable `GITHUB_PACKAGES_TOKEN`, a classic personal access token (`write:packages` on the site, `read:packages` in the app). Its value is never written here.
- Kept in: <where the token lives on each machine and in CI>.

## Release steps
1. <step, or "no release process yet">

## Measured times
| What | Budget | Last measured (date) |
|---|---|---|
| Full build | | |
| Full test suite | < 5 min | |
