---
updated: <YYYY-MM-DD>
---
<!--
How the app runs, where, and with which secrets. Filled by /agile:bootstrap from quiz round 6; updated by /agile:ship when any of it changes.
Save as: docs/infra.md. Write what is true today, not what is planned: an environment that does not exist yet is `planned`.
Never write a secret's value here, only its name and where it is kept.
-->
# Infra

## Run locally
- Prerequisites: <SDK version, container runtime, tools>
- Start: `<app host command>` → <URL>. Sign in as: <seed user and role; password kept in <where>>
- Reset local data: `<command>`

## Environments
| Environment | Status (`provisioned` / `planned`) | URL | How it is deployed | Configuration and secrets live in |
|---|---|---|---|---|
| local | provisioned | | app host | user secrets |

## Expected secrets
| Name | Used by | Kept in (per environment) |
|---|---|---|

## Release steps
1. <step, or "no release process yet">

## Measured times
| What | Budget | Last measured (date) |
|---|---|---|
| Full build | | |
| Full test suite | < 5 min | |
