---
feature: F-176
epic: Exam Simulator
status: idea
board: 242
version: 1
---
# End-to-end test of a timed exam

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: Captured by the E-06 breakdown; nothing refined yet.
- Next action: Refine it after F-170 and F-171 are built (owner).
- Waiting on: F-170, F-171

## Summary
A browser test takes one full timed attempt of the Manaus 2026 edition: start, answer, submit (and the automatic submit when time runs out) and read the result. ADR-0001 #36 names a full timed Exam Simulator session as the first end-to-end candidate.

## Start
- Depends on: F-170, F-171.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:autopilot F-176`.
- Parallel with: any.
