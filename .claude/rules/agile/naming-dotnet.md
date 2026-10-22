---
paths:
  - "**/*.cs"
  - "**/*.razor"
  - "**/*.sql"
---
# Naming (.NET)

- C#: `PascalCase` types and members, `camelCase` locals and parameters, `_camelCase` private fields, `I` prefix for interfaces, `Async` suffix for async methods.
- One public type per file; the file has the type's name. Namespaces follow folders.
- Name by role, in business words: `ExamBoard`, `RegisterExamBoard`, `ExamBoardResponse`. Avoid `Manager`, `Helper`, `Util`, `Data`, `Info`.
- A feature folder is a verb phrase or a resource (`Features/RegisterExamBoard/` or `Features/ExamBoards/`). Pick one style per project.
- Requests end in `Request`, responses in `Response`, integration events are past tense (`ExamBoardDeactivated`).
- Tests: class `<Subject>Tests`, method `Method_Scenario_Expected`.
- Database: `snake_case` tables and columns, plural tables, one schema per module. Index names say what they protect (`ux_exam_boards_tenant_acronym`).
