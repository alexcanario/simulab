---
feature: F-95
epic: Question bank
status: idea
board: 141
version: 1
---
# Single-choice questions in the back office

Technical terms: [glossary](../glossary.md)

## Summary
A curator writes a single-choice question in the back office: statement, an inline base text, options, answer key, explanation, difficulty, topic and content language; saves it as `Draft`, publishes it, lists it and previews it as a student will see it. This item creates the new `QuestionBank` module (schema `question_bank`, five-project shape, references `Topic` by id through `Catalog.Contracts`), the safe content format for statements and options, the question permission and the guard that blocks deleting a topic in use (F-79 BR9). Source: epic Question bank (E-4), decisions of 2026-10-09; Simulae `Question.cs` and `ConfigurationBase.cs` (T1) as the import source.

## Start
- Depends on: F-79 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`. The content format (sanitized HTML or Markdown), the difficulty scale and the permission name are settled at `/agile:refine`.
- Parallel with: none; every other E-4 feature needs it.
