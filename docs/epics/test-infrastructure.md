---
epic: test-infrastructure
status: draft
board: 199
---
# Test infrastructure

Technical terms: [glossary](../glossary.md)

## Goal
Keep the test suite reliable and fast: test waits that do not fail only because the machine is loaded, timing measured per project, and the shared infrastructure of the screen and integration tests.

## Start
- Depends on: nothing.
- Waits on: nothing.

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|
| B-32 | Plain WaitForAssertion waits in bUnit page tests time out under heavy load | The plain waits stop failing for the machine's load | Could | S | B-31 | - | no | idea |
| F-160 | Seed the development admin with the legal documents accepted | A signed-in screen can be checked on the app host without accepting terms | Could | S | - | - | no | idea |
| F-177 | Bring the full test suite back under its five-minute budget | The full suite stops running 174 s over the budget of CLAUDE.md | Should | M | - | - | no | idea |

## Execution plan
1. B-32 — the plain waits, after B-31 measured the load — path: `/agile:refine` → `/agile:build` — parallel with: none.
2. F-160 — the seeded admin starts with the documents accepted — path: `/agile:refine` → `/agile:build` — parallel with: B-32.

## Waiting outside the epic
| What | For | Who provides it | Who asks |
|---|---|---|---|
| nothing | - | - | - |

## First release cut

## Out of scope
- Production code: every item of this epic is test-only.

## Decisions
- 2026-10-20 — Epic created from the refinement of B-31, which found a second bug (B-32) in the same area — asked

## Retro notes

## Related
- Related bugs, all done: B-19, B-22, B-23, B-24, B-26, B-30. In refinement: B-31 (question bank epic).
