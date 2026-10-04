---
feature: F-77
epic: Subject taxonomy
status: refining
board: 121
version: 1
---
# Subject and topic aliases

Technical terms: [glossary](../glossary.md)

## Summary
`SubjectAlias` and `TopicAlias` (ADR-0001 #45): an admin records the other names organizers use for a canonical subject or topic, and a lookup by alias is ready for the AI-assisted exam import (E-9), which turns each curator confirmation into a new alias. After the first release cut (epic decision, 2026-10-04).

## Start
- Depends on: F-79 (`Subject`, `Topic` and their dialogs) — `validating` on 2026-10-04, not merged. The build waits for its merge and brings this branch up to date with `main` first.
- Waits on (to start): the F-79 merge — owner (`/agile:ship F-79`).
- Needed to validate: the app host started from this worktree and an Admin account signed in — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-51, F-74, F-75, F-76 (they touch other tables; shared conflict magnets: the `Catalog` migration snapshot and `SharedResources`).

## What exists
Verified on 2026-10-04 in `main` and in the F-79 branch (`D:/wt/simulab/f-79-subjects-topics`, commit `cb2611e`):
- No alias exists anywhere in `src/` of either branch.
- `Subject` and `Topic` (`Simulab.Catalog.Domain/Entities`) exist only on the F-79 branch: `TenantEntity`, name trimmed and normalized with `CatalogText.Normalize`, unique indexes `NULLS NOT DISTINCT` with deleted rows included (F-79 BR5, BR7), a topic may move to another subject (F-79 BR10), create and edit in dialogs (`SubjectDialog.razor`, `TopicDialog.razor`), subject list searchable by name (`SubjectQueries`).
- Modules read each other only through `Contracts` query interfaces (profile); `IPublishedExamQueries` in `Simulab.Catalog.Contracts` is the precedent for the lookup this item exposes.
- The E-9 import module does not exist yet: nothing calls the lookup in this item except its tests and the subject list search (BR10).

## Goal
Let the catalog remember the other names a subject or topic goes by, so the AI import maps a notice's wording to the canonical taxonomy without creating duplicates, and a curator finds a subject by any of its names.

## Users and use cases
- UC1 A curator (holder of `catalog.manage`) adds, edits and removes the other names of a subject in the subject dialog.
- UC2 A curator adds, edits and removes the other names of a topic in the topic dialog.
- UC3 A curator searches the subject list by an alias and finds the subject.
- UC4 Another module (the future import, E-9) looks a name up and gets the subjects and topics it matches, by canonical name or by alias.
- UC5 A user without `catalog.manage` sees and changes none of it.

## Business rules
- BR1 `SubjectAlias : TenantEntity` maps to `catalog.subject_aliases` (`SubjectId`, `Name`, `NormalizedName`); `TopicAlias : TenantEntity` maps to `catalog.topic_aliases` (`TopicId`, `Name`, `NormalizedName`). Global data (`TenantId` null), soft delete. An alias is only text: no organizer, no language (owner, refinement).
- BR2 An alias is trimmed and normalized with `CatalogText.Normalize`, like the names. A subject alias is 2 to 150 characters, a topic alias 2 to 200 (the name limits of F-79). Errors: `subject.alias_too_short`, `subject.alias_too_long`, `topic.alias_too_short`, `topic.alias_too_long`.
- BR3 There is no limit on the number of aliases per subject or topic.
- BR4 Aliases are saved with their subject or topic: `SaveSubjectRequest` and `SaveTopicRequest` carry an `Aliases` list that replaces the current set. A null list leaves the set unchanged. Entries with the same normalized form inside one request count once (the first as typed is kept).
- BR5 An alias equal (normalized) to its own subject's or topic's name is refused: 400 `subject.alias_same_as_name` / `topic.alias_same_as_name`.
- BR6 A subject alias is unique across the whole subject vocabulary: its normalized form must not match another subject's name, nor a live alias of another subject. Else 409 `subject.alias_taken`. The reverse holds too: a subject created or renamed with a name equal to another subject's live alias is 409 `subject.name_taken` (F-79's code, so the lookup never finds two subjects for one name).
- BR7 A topic alias is unique inside its subject: its normalized form must not match the name of another topic of the same subject, nor a live alias of another topic of the same subject. Else 409 `topic.alias_taken`. The same alias under two subjects is allowed (F-79 BR7). The reverse holds too: a topic created, renamed or moved with a name equal to a live alias of another topic of the target subject is 409 `topic.name_taken`.
- BR8 A removed alias frees its text: the alias uniqueness counts live aliases only, so an alias removed by mistake can be added again, to the same or another item. This departs from the Catalog rule that a deleted name stays taken (F-33, F-79 BR5) because an alias is a pointer, not an item (owner, refinement).
- BR9 Deleting a subject or a topic removes its aliases with it and is never blocked by them (F-79 BR8 still blocks a subject with topics). Moving a topic to another subject (F-79 BR10) takes its aliases along; an alias that clashes in the target subject (BR7) refuses the move with 409 `topic.alias_taken` and the topic stays where it was.
- BR10 The subject list search (F-79 BR12) matches the subject's name or any of its live aliases; the row shows the subject's name. Topics have no list search, so there is no topic search to extend.
- BR11 `Simulab.Catalog.Contracts` exposes a query interface for the lookup: given a name, it returns every live subject whose name or alias matches its normalized form (zero or one, by BR6) and every live topic whose name or alias matches (zero or more, each with its subject, by BR7). The lookup is read-only and has no HTTP endpoint in this item.
- BR12 Domain rules return `Result`, never exceptions.
- BR13 `catalog.manage` gates the alias field in both dialogs and the endpoints that save them (F-79 BR15); `SubjectResponse` and `TopicResponse` carry the aliases.
- BR14 Every new UI text and error code exists in pt-BR, pt-PT and en in `SharedResources`.

## Screens and API
No new screen or route. Both dialogs of F-79 gain one field.
- Subject dialog (`/admin/subjects`, create and edit): an "Other names" field of chips under the name, with a hint ("Names organizers use for this subject"). Typing a name and pressing Enter adds a chip; each chip has a remove button. A refused alias shows its message on the field; nothing is saved.
- Topic dialog (`/admin/subjects/{id}`, create and edit): the same "Other names" field.
- Subject list: unchanged columns; the search also finds by alias (BR10).
- POST /api/v1/catalog/subjects, PUT /api/v1/catalog/subjects/{id} — body gains `aliases: string[] | null`.
- POST /api/v1/catalog/subjects/{subjectId}/topics, PUT /api/v1/catalog/topics/{id} — body gains `aliases: string[] | null`.
- GET /api/v1/catalog/subjects/{id}, GET /api/v1/catalog/subjects/{subjectId}/topics — responses gain `aliases: string[]`, ordered by normalized form.
- GET /api/v1/catalog/subjects?search= — `search` also matches aliases.
- New error codes: `subject.alias_too_short`, `subject.alias_too_long`, `subject.alias_same_as_name`, `subject.alias_taken`, `topic.alias_too_short`, `topic.alias_too_long`, `topic.alias_same_as_name`, `topic.alias_taken`.

## Acceptance criteria
- AC1 Given the previous migration, when the new migration runs, then `catalog.subject_aliases` and `catalog.topic_aliases` exist with restricted foreign keys to subjects and topics, and their unique indexes are `NULLS NOT DISTINCT` and cover live rows only. (BR1, BR8)
- AC2 Given subject "Língua Portuguesa", when the curator saves it with the other names "Português" and "Portugues", then one alias "Português" is stored and the dialog shows one chip. (UC1, BR2, BR4)
- AC3 Given an alias of 1 or of 151 characters on a subject (201 on a topic), then 400 `subject.alias_too_short` / `subject.alias_too_long` (`topic.…`); nothing is written. (BR2)
- AC4 Given subject "Língua Portuguesa", when its alias is "lingua portuguesa", then 400 `subject.alias_same_as_name`; the same for a topic. (BR5)
- AC5 Given subjects "Língua Portuguesa" (alias "Português") and "Matemática", when "Matemática" gets the alias "portugues" or "lingua portuguesa", then 409 `subject.alias_taken`; when a new subject named "Português" is created, then 409 `subject.name_taken`. (BR6)
- AC6 Given two subjects each with a topic, when both topics get the alias "Interpretação", then both are saved; when a second topic of the same subject gets it, or a topic of that subject is renamed "interpretacao", then 409 `topic.alias_taken` / `topic.name_taken`. (BR7)
- AC7 Given a subject with alias "Português", when the curator removes the alias and saves, then another subject can take "Português" as an alias or name; its old row stays with `IsDeleted` true. (BR8)
- AC8 Given two alias rows inserted directly with `TenantId` null, the same subject and the same normalized form, both live, then PostgreSQL rejects the second; with the first deleted, it accepts it. (BR8)
- AC9 Given a topic with alias "Crase" moved to a subject that has a topic named "Crase", then 409 `topic.alias_taken` and the topic and its alias stay in the first subject; moved to a subject without the clash, the alias goes with it. (BR9)
- AC10 Given a topic with two aliases, when it is deleted, then its aliases are deleted too; given a subject with aliases and no topics, it can be deleted and its aliases go with it. (BR9)
- AC11 Given "Língua Portuguesa" with alias "Português", when the curator searches the subject list for "portugues", then "Língua Portuguesa" shows once. (UC3, BR10)
- AC12 Given "Língua Portuguesa" (alias "Português") with topic "Interpretação de texto" (alias "Compreensão"), and "Inglês" with topic "Interpretação de texto", when the lookup runs for "portugues", then it returns that subject and no topic; for "compreensao", that one topic with its subject; for "interpretacao de texto", both topics with their subjects; for an unknown name, nothing. Deleted subjects, topics and aliases are never returned. (UC4, BR11)
- AC13 Given a call with `aliases` null on an update, then the stored aliases are unchanged. (BR4)
- AC14 Given `SubjectAlias` or `TopicAlias` creation with an empty text, then a failed `Result` with the too-short code, and nothing throws. (BR12)
- AC15 Given a signed-in Student, then the endpoints that save aliases answer 403 `identity.forbidden` and the pages stay Not Found (F-79 AC15 extended to the new fields). (UC5, BR13)
- AC16 All new texts and error codes appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR14)

## Decisions
- 2026-10-04 — A subject alias is unique across every subject's name and alias; equal to its own name is refused — the lookup must point to one subject (owner, refinement).
- 2026-10-04 — A topic alias is unique inside its subject, like topic names (F-79 BR7) — "Interpretação de texto" exists under Portuguese and English (owner, refinement).
- 2026-10-04 — A removed alias frees its text (live-only unique index), unlike the Catalog's deleted names — an alias is a pointer and a wrong one must be fixable (owner, refinement).
- 2026-10-04 — Aliases follow their item: deleted with it, moved with a topic, a clash refuses the move — never blocks a delete (owner, refinement).
- 2026-10-04 — Aliases are edited as chips in the existing subject and topic dialogs — one way to edit an item (`ui-project`), no new screen (owner, refinement).
- 2026-10-04 — The subject list search matches aliases — the curator finds "Língua Portuguesa" by "Português" before E-9 exists (owner, refinement).
- 2026-10-04 — An alias is text only, no organizer — the audit records who and when; E-9 may add the organizer if the import needs it (owner, refinement).
- 2026-10-04 — No limit on aliases per item; the length limits are the name limits (owner, refinement).
- 2026-10-04 — No new package: xunit, bunit and Testcontainers.PostgreSql are in `Directory.Packages.props` (Claude, verified).
- 2026-10-04 — The lookup is an in-process query interface in `Simulab.Catalog.Contracts` (precedent `IPublishedExamQueries`), with no HTTP endpoint — its only future caller (E-9) runs in the same process; an endpoint with no caller would be unused surface (Claude, technical).
- 2026-10-04 — The lookup matches canonical names as well as aliases — ADR-0001 #45 looks aliases up first and the import needs one call for both; the result says nothing about which matched (Claude, technical).
- 2026-10-04 — Cross-table uniqueness (an alias against the other subjects' names, a name against the other subjects' aliases) is checked in the handlers; the database guarantees only uniqueness inside each alias table. Two curators racing the same text into a name and an alias at the same instant can both succeed; accepted, as F-79 accepts its own delete race (Claude, technical).
- 2026-10-04 — Aliases travel in the save requests as a whole set (`null` = unchanged), not through their own endpoints — the dialog saves once (Claude, technical).
- 2026-10-04 — `topic_aliases` carries no `subject_id`; the per-subject check joins through `topics`, so a move needs no alias row update — one source of truth for the subject (Claude, technical; the build's design pass may revisit it with `EXPLAIN` if the index cannot serve the check).

## Out of scope
- The import calling the lookup, suggesting the closest topic and turning a curator's confirmation into an alias — epic E-9 (owner, refinement).
- A limit on the number of aliases per item (owner, refinement).
- Aliases in the municipal guard seed (F-51, F-78) (owner, refinement).
- The organizer that uses an alias (owner, refinement).
- An HTTP endpoint for the lookup.
- Translating aliases: they are content, like the names (F-79).

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
