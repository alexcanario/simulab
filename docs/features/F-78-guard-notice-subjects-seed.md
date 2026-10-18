---
feature: F-78
epic: Subject taxonomy
status: refining
board: 122
version: 1
---
# Notice subjects of the municipal guard editions

Technical terms: [glossary](../glossary.md)

## Summary
Seed the `NoticeSubject` rows and their mapping for the municipal guard editions F-37 brought in, so those editions show a real syllabus. Simulae has no notice subjects; the source is the official notices.

## Start
- Depends on: F-74 (the `NoticeSubject` model, `approved`), F-51 (the canonical subjects, `approved`), F-75 (the mapping model, `refining`, worktree `D:\wt\simulab\f-75-map-notice-subjects`). F-51 itself waits for F-79 (`validating`).
- Waits on (to start): F-75 approved — the refinement of F-78 pauses until then (owner, 2026-10-06); the notices of the four editions as the source of labels and question counts — Claude reads the public links, the owner sends what the links do not give.
- Needed to validate: the local app host with the F-74 and F-75 screens, showing the four seeded editions — Claude.
- Suggested path: `/agile:refine` (resumed after F-75 is approved) → `/agile:build` (after F-74, F-51 and F-75 are merged).
- Parallel with: F-76, F-77.

## What exists
Verified on 2026-10-06:
- The F-37 seed (`20260929142114_SeedMunicipalGuardCatalog.cs`) has six municipal guard exams but only four editions: Curitiba 2025 (Instituto AOCP, "Edital nº 02/2025"), Manaus 2026 (Instituto Consulplan, "Edital nº 01, de 23 de março de 2026"), Salvador 2026 (FGV, "Edital nº 02/2026"), Maceió 2026 (Copeve/Ufal, "Edital nº 01/2026"). Recife and Goiânia have an exam and no edition. Each edition stores its notice link (`notice_url`).
- No `NoticeSubject` in `src/` on `main`; the model arrives with F-74 (`approved`, worktree `D:\wt\simulab\f-74-notice-subjects-per`): label, optional group, optional number of questions, position, soft delete, global rows.
- No canonical subject in `src/` on `main`; the 23 subjects and 70 topics arrive with F-51 (`approved`, its `## Approved list`) on top of F-79's model.
- The mapping model (subjects, topics or both) is not decided: F-75 is `refining` with only its summary written.
- Neither Simulab nor Simulae (read-only) holds the text of these notices.

## Decisions
- 2026-10-06 — Source: Claude reads the four notices through the links stored on the editions and transcribes them; the owner sends any notice the link does not give; the owner checks the transcribed list before approval (owner, question 1, "1 e 2").
- 2026-10-06 — Scope: the four existing editions; Recife and Goiânia have no edition and stay out (owner, question 2).
- 2026-10-06 — One `NoticeSubject` per line of the notice's exam table (subject and number of questions); the group is the notice's block ("Conhecimentos Básicos", ...); the number of questions is blank when the notice does not state it (F-74 BR4) (owner, question 3).
- 2026-10-06 — The refinement waits for F-75 to be approved, then maps subjects and topics in one pass — no rework if F-75 picks a different model (owner, question 4).
- 2026-10-06 — A notice subject with no canonical equivalent in F-51 stays unmapped and is listed in this file; a curator creates the canonical subject in F-79's back office and maps it with F-75 (owner, question 5).
- 2026-10-06 — An edition that already has any notice subject when the seed runs is skipped whole — no mixing of the admin's order and labels with the seed's (owner, question 6).
- 2026-10-06 — Delivery is one data migration of the `catalog` schema with fixed GUIDs and frozen literals, never updating a row — the F-37 and F-51 precedent (Claude, technical).
- 2026-10-06 — No new package expected — xunit, AwesomeAssertions and Testcontainers.PostgreSql are already in `Directory.Packages.props` (Claude, verified in F-51).
- 2026-10-06 — Board: GitHub issue #122 set to Ready (Claude).

## Out of scope
- Editions of Recife and Goiânia.
- Creating canonical subjects or topics missing from F-51.
- Weight and minimum score per notice subject — epic E-6.

## Open questions
- Mapping of each notice subject to canonical subjects and topics — waits for F-75's model; resumed when F-75 is approved (owner, 2026-10-06).
- The transcribed notice subjects of the four editions (`## Approved list`) — to be checked by the owner.
