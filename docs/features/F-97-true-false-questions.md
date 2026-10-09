---
feature: F-97
epic: Question bank
status: idea
board: 143
version: 1
---
# True/false questions

Technical terms: [glossary](../glossary.md)

## Summary
The true/false question type (Cebraspe style): the curator writes the statement and sets the answer key as true or false. The question stores no wrong-answer penalty: the penalty is the edition's scoring rule, in epic E-6 (owner, 2026-10-09). Source: epic Question bank (E-4); Simulae `ConfigurationBase.cs` (T3) as the import source, without its penalty fields.

## Start
- Depends on: F-95.
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:autopilot` (small and clear once F-95 sets the type pattern).
- Parallel with: F-96 or F-98.
