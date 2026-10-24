---
discussion: D-6
status: decided
date: 2026-10-24
---
# Manaus 2026 as the only municipal guard seed

Technical terms: [glossary](../glossary.md)

## Idea
The owner proposed clearing the current municipal guard seed and building a new one from a real paper: Instituto Consulplan 2026, Prefeitura de Manaus/AM, Técnico Municipal I - Guarda Municipal, with its final answer key. F-110 (sample questions of the guard editions) gets its source.

## What we already know
- The current seed is catalog data only, three data migrations in `catalog`: `SeedMunicipalGuardCatalog` (F-37: 7 organizers, 6 issuing authorities, 6 exams, 4 editions — Curitiba, Manaus, Salvador, Maceió), `SeedGuardTaxonomy` (F-51: 23 subjects, 70 topics) and `SeedGuardNoticeSubjects` (F-78: the notice subjects of the 4 editions).
- The Manaus 2026 edition is already seeded (`Edital nº 01, de 23 de março de 2026`); the paper belongs to it.
- No question is seeded: the `questionbank` migrations are schema only.
- Each seed migration runs once per database and an admin's edit or delete stays final (F-37 BR1, BR5; F-51 BR1; F-78 BR1). Staging (F-64) has already run them, so clearing it needs a new migration; deleting the old ones does nothing there.
- Catalog tables carry `is_deleted` (soft delete).
- No student attempt exists yet (epic E-6), so no student data points at an edition; questions typed by curators in staging (F-96) may.
- The paper (booklet type 1, white): 60 multiple-choice questions with four options and one essay question. The final answer key covers types 1 to 4; question 47 is annulled (`#`). It uses shared base texts (F-102, e.g. questions 01 to 08), an image (a geography question, F-103) and I/II/III statements inside multiple choice.
- Images live in `IFileStorage` (F-103): an `img` points at `/question-images/<guid>`, and a pending image with no owner is deleted after 24 hours (F-103 BR11). A SQL migration cannot store a blob.
- The essay type depends on F-109 (`refining`).

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — Clean-up migration + questions migration | Every environment, new or staging, shows only Manaus 2026, with its 60 questions published and Q47 annulled; a new migration soft-deletes the rest, the old ones stay | M–L | The transcription of 60 questions is the long, error-prone part; soft-deleted rows stay in the database |
| B — Rewrite the old migrations and recreate the databases | Same result, cleaner migration history | M | Breaks the frozen-seed rule (F-37 BR4), needs a staging database reset and loses what testers typed |
| C — Unpublish the other editions, seed the questions | Students see only Manaus; admins still see the rest | M | A new database still starts with 4 editions: not what the owner asked |

Recommendation: A — the only option that keeps only Manaus without an irreversible delete in staging, and it keeps the seed rules of F-37, F-51 and F-78.

## Decisions
- 2026-10-24 — The municipal guard seed keeps only the Manaus 2026 edition (Instituto Consulplan, Edital nº 01/2026) — owner's choice.
- 2026-10-24 — The paper and its answer key are free to use — owner's answer; it settles F-110's "right to use them".
- 2026-10-24 — The clean-up also applies to staging — owner's answer.
- 2026-10-24 — The clean-up is a new data migration that soft-deletes the other editions, the exams, issuing authorities and organizers only they use, and their notice subjects; the old seed migrations stay untouched and a row an admin already edited is skipped — option A; reversible, and it keeps F-37 BR1 and BR4.
- 2026-10-24 — The taxonomy of F-51 (23 subjects, 70 topics) stays whole — it is a guard vocabulary, not tied to an edition, and the Manaus notice subjects and questions point at it.
- 2026-10-24 — The seed holds the 60 multiple-choice questions of booklet type 1 (white) with its answer key, question 47 annulled (F-101) — types 2 to 4 are the same questions in another order.
- 2026-10-24 — The question with an image comes from the migration with a fixed image id; its file is stored by an idempotent seeder at app start-up — a migration cannot store a blob, and an image with no owner is swept after 24 hours (F-103 BR11).
- 2026-10-24 — Claude proposes the subject and topic of each question from the taxonomy; the owner reviews the table at `/agile:refine` — a question with no topic leaves "what to study next" blind.

## Parked
- The essay question of the Manaus paper — waits for F-109 (open-answer and essay questions); not in this seed.

## Outcome
- Epics: none new.
- Features: F-161 (clean-up of the guard catalog seed); F-110 improved with the Manaus 2026 scope.
- Bugs: none.
