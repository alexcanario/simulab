---
feature: F-68
epic: Foundation and identity
status: refining
board: 107
version: 1
---
# infra.md holds the access details of the infrastructure

## Summary
The owner wants `docs/infra.md` to be the one place that holds the URI and the password of the board, the pipelines, the Key Vault and the hosts, so nobody (human or agent) has to hunt for them. Raised by the owner on 2026-10-03.

The URIs fit the file as it is. The passwords collide with the rule "never commit secrets" (`.claude/rules/agile/git.md`; `docs/infra.md` is committed). How to reconcile the two is settled at `/agile:refine`.

## Start
- Depends on: nothing for the URIs of what exists today (the GitHub board and repository); the Key Vault, pipelines and cloud hosts are `planned` in `docs/infra.md` and arrive with F-64 and F-65.
- Waits on (to start): the owner says where the passwords may live (a committed file, a git-ignored local file, or only a pointer to the vault entry) — owner.
- Needed to validate: nothing beyond what refinement settles.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-62, F-64, F-65.
