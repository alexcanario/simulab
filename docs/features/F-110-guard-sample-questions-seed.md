---
feature: F-110
epic: Question bank
status: refining
board: 156
version: 1
---
# Sample questions of the municipal guard editions

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: refining (2026-10-25)
- Stopped at: Refinement started: worktree created, premises being checked
- Next action: Question round, then approval
- Waiting on: nothing

## Summary
Seeded questions for the municipal guard editions of F-37, so local work and testers start with real questions without typing them. Simulae has no question seed (its `GuardaMunicipalContentSeed` holds only the taxonomy). Source: epic Question bank (E-4).

Added 2026-10-24: the source is the Manaus 2026 paper (Instituto Consulplan, Técnico Municipal I - Guarda Municipal, booklet type 1, white) and its final answer key, free to use (owner, D-6). The seed holds its 60 multiple-choice questions with their base texts, question 47 annulled (F-101). The question with an image comes from the migration with a fixed image id and its file is stored by an idempotent seeder at app start-up (F-103 BR11). Claude proposes the subject and topic of each question from the F-51 taxonomy; the owner reviews the table at refinement. The essay question waits for F-109. The other editions leave the seed in F-161.

## Start
- Depends on: F-96, F-97.
- Waits on (to start): nothing — the source paper and answer key are with the owner (OneDrive `Simulab/Guarda Municipal/Manaus/2026`), free to use (D-6).
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: any E-4 feature.
