---
paths:
  - "tests/Modules/QuestionBank/**"
---
# Project rules

- Raw SQL in a QuestionBank test names columns in snake_case, as the EF convention maps them (QuestionImageApiTests.RowAsync aliased them in PascalCase and was unlocked to fix), and a test that moves the clock signs in again afterwards (QuestionImageSweepTests did not and was unlocked to fix) (F-103, 2026-10-21)
- Seed JSON for question_versions columns (options, match_items, blanks) in raw SQL uses the PascalCase property names EF ToJson reads ("AcceptedAnswers"), and never compare a tuple that holds a list with Be (it compares the list by reference): assert each member (F-108, 2026-10-24)
