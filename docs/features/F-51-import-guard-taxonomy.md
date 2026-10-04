---
feature: F-51
epic: Subject taxonomy
status: idea
board: 88
version: 1
---
# Import the municipal guard taxonomy

Technical terms: [glossary](../glossary.md)

## Summary
Bring the subject taxonomy of Simulae's `GuardaMunicipalContentSeed` (9 subjects, 23 knowledge domains, 70 topics) into the Subject taxonomy epic (E-3) as seed data, once its model exists (F-79, ADR-0001 #43 to #46). The remap: Simulae's top-level `Subject` becomes the `Area`, `KnowledgeDomain` becomes `Subject`, `Topic` stays `Topic`; a "Local knowledge" subject gets one topic per city (ADR-0001 consequences). It was left out of F-37 (BR13) because the taxonomy entities do not exist yet. The source of truth for the names and the counts is Simulae's decision record `docs/1. Product Owner/[TK #662] Decisoes confirmadas - Taxonomia Guarda Municipal.md`; Simulae has no `NoticeSubject`, so the link between an edition and its topics is new work.

## Start
- Depends on: F-79 (the taxonomy model); F-37 (its data migration is the precedent, done).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-74, F-77.
