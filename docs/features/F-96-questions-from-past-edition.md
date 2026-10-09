---
feature: F-96
epic: Question bank
status: idea
board: 142
version: 1
---
# Questions from a past exam edition

Technical terms: [glossary](../glossary.md)

## Summary
A question can come from a past exam edition: it records the `ExamEdition`, its number in the paper and the `NoticeSubject` it belongs to. The edition screen lists its questions, and the question shows a source line (organizer, exam, year). A question without an edition is an `AuthoredQuestion`. Source: epic Question bank (E-4), brief capability 3 "Origin".

## Start
- Depends on: F-95.
- Waits on (to start): what the source line must show, and which sources may be reproduced (brief open note "Rights to reproduce past exams") — owner.
- Needed to validate: the local app host and one published edition with notice subjects — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-97 or F-98.
