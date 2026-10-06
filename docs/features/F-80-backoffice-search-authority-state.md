---
feature: F-80
epic: Assessment catalog
status: idea
board: 123
version: 1
---
# Back-office exam search matches authority and state

Technical terms: [glossary](../glossary.md)

## Summary
The back-office exam list search matches only the exam name (`ExamQueries.cs:24-28`), while the student catalog search also matches the issuing authority and the state (`NormalizedScopeDetail`, `PublishedExamQueries.cs:23-30`). Make the back-office search match the same fields. Raised while refining F-57 (owner, 2026-10-04): kept out so F-57 stays one filter.

## Start
- Depends on: nothing (F-42, done, already fills `NormalizedScopeDetail`).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: unknown — settled at /agile:refine.
- Parallel with: not with F-57 while it is open — both change `ExamQueries` and `Exams.razor`.
