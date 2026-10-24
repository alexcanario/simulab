---
feature: F-109
epic: Question bank
status: idea
board: 155
version: 1
---
# Open-answer and essay questions

Technical terms: [glossary](../glossary.md)

## Summary
The open-answer and essay question types with their rubric (criteria and maximum points). They are useful to students only once someone grades them. Source: epic Question bank (E-4), brief capability 3 and its open note "Essays and open answers"; Simulae `ConfigurationBase.cs` (T7, T8) as the import source.
Added 2026-10-24 (D-5): this item delivers only the types and the rubric. Grading is a person, by a new `Grader` role (F-150); the correction queue is F-151 and the AI-suggested grade is F-152. The two types share one rubric of criteria (name, description, maximum points) plus an optional model answer; the essay adds a theme and a size limit. The rubric is part of `QuestionVersion`. A curator may publish these types before grading exists; the simulators decide whether to offer them.

## Start
- Depends on: F-95.
- Waits on (to start): nothing — the grading decision is settled in D-5 (2026-10-24).
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
