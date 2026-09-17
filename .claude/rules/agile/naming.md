# Naming

- Everything in the repository is English: identifiers, files, folders, routes, database objects, commits, docs, code comments.
- Only the conversation with the owner and the pt-BR / pt-PT resource and manual files are in Portuguese.
- A business term gets its English identifier in `docs/glossary.md` before its first use in code. Never invent a second translation.
- An English class name does not excuse Portuguese members: properties, methods, parameters, exceptions and enum values are English too.
- Existing code that breaks this is debt, not a reference. Do not copy its names; register the rename as an `idea`.
- A rename in the ubiquitous language updates the glossary, code, routes, resource keys and docs in the same item.
- C#: `PascalCase` types and members, `camelCase` locals and parameters, `_camelCase` private fields, `I` prefix for interfaces, `Async` suffix for async methods.
- One public type per file; the file has the type's name. Namespaces follow folders.
- Name by role, in business words: `ExamBoard`, `RegisterExamBoard`, `ExamBoardResponse`. Avoid `Manager`, `Helper`, `Util`, `Data`, `Info`.
- A feature folder is a verb phrase or a resource (`Features/RegisterExamBoard/` or `Features/ExamBoards/`). Pick one style per project.
- Requests end in `Request`, responses in `Response`, integration events are past tense (`ExamBoardDeactivated`).
- Tests: class `<Subject>Tests`, method `Method_Scenario_Expected`.
- Database: `snake_case` tables and columns, plural tables, one schema per module. Index names say what they protect (`ux_exam_boards_tenant_acronym`).
- Routes: lowercase kebab-case plural nouns. Error codes and resource keys: `snake_case` with a dot (`exam_board.acronym_taken`).
- Ids in docs and branches: `F-<n>`, `B-<n>`, `D-<n>`. Slugs are lowercase English words joined by `-`, at most 5.
- Folders and worktrees have short names (`<type>-<n>`): Windows paths break at 260 characters.
- The vocabulary architecture test enforces this in every assembly, UI and API included. A new Portuguese term found twice goes into its forbidden list.
