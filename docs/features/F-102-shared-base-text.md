---
feature: F-102
epic: Question bank
status: idea
board: 148
version: 1
---
# Shared base text

Technical terms: [glossary](../glossary.md)

## Summary
One `BaseText` (a reading passage, a table, a case) serves several questions, as in reading comprehension blocks: the curator writes it once and links the questions to it; the simulators show it beside each of them. F-95 keeps only an inline base text per question. Source: epic Question bank (E-4), glossary `BaseText`.

## Start
- Depends on: F-95.
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: F-101, F-104.
