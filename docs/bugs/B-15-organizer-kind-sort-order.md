---
bug: B-15
feature: F-33
status: idea
board: 759
severity: low
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# Sorting organizers by kind uses the English name, not the label on screen

## What happens
`OrganizerQueries` orders the kind column by the value stored in the database, which is the English enum name:
`CertifyingBody`, `ExamBoard`, `University`. On screen the reader sees the translated labels, so in pt-BR clicking
the **Tipo** header gives "Certificadora, Banca, Universidade" — an order that looks arbitrary. Found by the
independent review of F-33 (2026-09-23); accepted there because the default sort is by name and the kind column is
secondary.

## Expected
- Sorting by kind follows the labels the reader sees, in the language they are reading, or the column says what it
  sorts by. The choice is the owner's: a culture-aware order needs the sort to move to where the labels live.
