---
adr: 0005
status: accepted
date: 2026-10-10
---
# ADR-0005: The question bank is its own module

Technical terms: [glossary](../glossary.md)

## Context
The profile (2026-09-20) put organizers, exams, editions, taxonomy and the question bank in one `Catalog` module, on the argument that the three epics that fill it share invariants and transactions. When the Question bank epic (E-4) was refined on 2026-10-09 the owner decided otherwise: the bank is large (several question types, versions, review, import), changes at its own pace and is read by the exam engine and the coach, which should not depend on the catalog's exams and editions.

F-95 is the first item that writes questions, so it is the item that creates the module.

## Options
**A. Keep the bank inside `Catalog`.** One schema, joins to topics are free. The module grows with every question type, and every later consumer of questions references the whole catalog.

**B. A `QuestionBank` module, five projects, its own schema** (chosen). The bank grows without touching the catalog; a consumer references `QuestionBank.Contracts` only. Costs a reference to a topic by id (no foreign key) and a contract to ask the catalog for names.

## Decision
1. **`QuestionBank` is a module of five projects** (`Domain`, `Application`, `Infrastructure`, `Api`, `Contracts`) under `src/Modules/QuestionBank/`, with schema `question_bank` and its own `DbContext` and migrations. The profile's table is corrected in the same item.
2. **A question points at its topic by id.** There is no foreign key from `question_bank.questions` to `catalog.topics`: a module never touches another module's tables.
3. **The guard is inverted.** `Catalog.Contracts` declares `ITopicUsageCheck`; `QuestionBank` implements it; `DeleteTopicHandler` and the topic list ask every registered check. The catalog never references `QuestionBank`, no cycle exists, and each later item that adds a reference to a topic registers its own check without editing the catalog.
4. **Names come through a contract.** `Catalog.Contracts` declares `ITopicDirectory` (a topic with its subject, by id); the catalog implements it and `QuestionBank.Infrastructure` consumes it. `QuestionBank.Application` references only its own `Domain` and `Contracts`, as the architecture tests require.
5. **Content is sanitized HTML.** Every text of a question is HTML written in a visual editor and sanitized by the Api against one allow-list on every write; the Web shows what was stored. Markdown was compared and rejected: the parser pipeline in use still emitted `javascript:` links and `data:` images (measured 2026-10-10).
6. **One new package**, `HtmlSanitizer` (MIT), in `QuestionBank.Application`; the editor package belongs to the Web.

## Consequences
- Every later question type (F-97, F-104, F-106 to F-109) is a vertical slice inside this module.
- The student side (E-5, E-6) reads questions through `QuestionBank.Contracts`.
- Deleting a topic that a question uses is refused with `topic.in_use`; the text names both uses.
- A question that outlives a deleted topic cannot exist through the app; a bypass would leave a question with an unknown topic, which the list shows with an empty topic name.
