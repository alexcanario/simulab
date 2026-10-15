---
paths:
  - "src/Modules/QuestionBank/**"
---
# Project rules

- questions.status is an EF concurrency token (F-101): a handler that writes a question saves through IQuestionStore.TrySaveChangesAsync, which answers a lost race as 409 question.version_conflict; SaveChangesAsync would answer it as a 500. (F-101, 2026-10-14)
- A new QuestionType value is one more card in QuestionForm.TypeCards and is offered by the transcription screen too (F-99, QuestionTranscriptionTests counts the cards); the two type column comments (questions, question_versions) need a comment migration or EnumColumnCommentTests goes red. (F-104, 2026-10-15)
