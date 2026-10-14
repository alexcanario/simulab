---
paths:
  - "src/Modules/QuestionBank/**"
---
# Project rules

- questions.status is an EF concurrency token (F-101): a handler that writes a question saves through IQuestionStore.TrySaveChangesAsync, which answers a lost race as 409 question.version_conflict; SaveChangesAsync would answer it as a 500. (F-101, 2026-10-14)
