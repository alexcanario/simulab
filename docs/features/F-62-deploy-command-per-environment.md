---
feature: F-62
epic: Foundation and identity
status: idea
board: 101
version: 1
---
# Each environment declares how it is deployed

## Summary
`docs/infra.md` has no Deploy command, Check URL or Version (deployed on) column in its environments table; `templates/infra.md` of agile@canary 0.0.101 has all three, and `/agile:publish <environment>` reads them. The item adds the columns, with the command each environment is deployed by or `not declared`. Raised by `/agile:sync` to 0.0.102; the owner chose to capture it on 2026-10-02.

## Start
- Depends on: nothing.
- Waits on (to start): the owner says which environments exist beyond local and how each is deployed — unknown, settled at `/agile:refine`.
- Needed to validate: unknown — settled at `/agile:refine`.
- Suggested path: `/agile:refine` first — unknown, settled at `/agile:refine`.
- Parallel with: unknown — settled at `/agile:refine`.
