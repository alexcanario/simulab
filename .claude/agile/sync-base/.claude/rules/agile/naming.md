# Naming

- Everything in the repository is English: identifiers, files, folders, routes, database objects, commits, docs, code comments.
- Only the conversation with the owner and the pt-BR resource and manual files are in Portuguese; the one exception is `### pt-BR` of `## Validation script`.
- A business term gets its English identifier in `docs/glossary.md` before its first use in code. Never invent a second translation.
- An English class name does not excuse Portuguese members: properties, methods, parameters, exceptions and enum values are English too.
- Existing code that breaks this is debt, not a reference. Do not copy its names; register the rename as an `idea`.
- A rename in the ubiquitous language updates the glossary, code, routes, resource keys and docs in the same item.
- Ids in docs and branches: `F-<n>`, `B-<n>`, `D-<n>` or a ticket id (`PRJ-123`, "Ticket ids" in `conventions.md`). Slugs are lowercase English words joined by `-`, at most 5.
- The vocabulary architecture test enforces this in every assembly, UI and API included. A new Portuguese term found twice goes into its forbidden list.
