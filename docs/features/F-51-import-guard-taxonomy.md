---
feature: F-51
epic: Subject taxonomy
status: idea
board: 777
version: 1
---
# Import the municipal guard taxonomy

## Summary
Bring the subject taxonomy of Simulae's `GuardaMunicipalContentSeed` (9 subjects, 23 knowledge domains, 70 topics) into the Subject taxonomy epic (692) as seed data, once its model exists (ADR-0001 #43 to #46). It was left out of F-37 (BR13) because the taxonomy entities do not exist yet. The source of truth for the names and the counts is Simulae's decision record `docs/1. Product Owner/[TK #662] Decisoes confirmadas - Taxonomia Guarda Municipal.md`; Simulae has no `NoticeSubject`, so the link between an edition and its topics is new work.

## Start
- Depends on: the taxonomy model of epic 692 (no feature of it exists yet); F-37 (its data migration is the precedent).
- Waits on: nothing.
- Suggested path: `/agile:refine` after the first feature of epic 692 is done.
- Parallel with: none known.
