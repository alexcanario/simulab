---
paths:
  - "tests/Modules/QuestionBank/**"
---
# Project rules

- Raw SQL in a QuestionBank test names columns in snake_case, as the EF convention maps them (QuestionImageApiTests.RowAsync aliased them in PascalCase and was unlocked to fix), and a test that moves the clock signs in again afterwards (QuestionImageSweepTests did not and was unlocked to fix) (F-103, 2026-10-21)
