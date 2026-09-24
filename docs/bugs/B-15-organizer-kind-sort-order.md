---
bug: B-15
feature: F-33
status: refining
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

## Cause
Verified on `main` (bdefd71). The code was last touched by F-33 and by B-14 (which changed only saving).

- `OrganizerConfiguration.cs:32-34` stores `Kind` with `HasConversion<string>()`, so the column holds `ExamBoard`,
  `CertifyingBody` or `University`.
- `OrganizerQueries.Sort` (`src/Modules/Catalog/Simulab.Catalog.Infrastructure/Persistence/OrganizerQueries.cs:54-55`)
  orders by `organizer.Kind`, that is by the stored English name, then by name.
- The labels live only in the Web project: `SharedResources.{en,pt-BR,pt-PT}.resx` keys `Organizers.Kind.<Kind>`,
  read by `OrganizerText.KindName`. The Api and the Catalog module have no resources and no culture.
- `Organizers.razor:88-93` maps the column to `OrganizerSort.Kind` and sends only `sortBy` and `descending`; the
  reader's language never reaches the query.

What the reader gets, by stored-name order (CertifyingBody, ExamBoard, University) against the alphabetical order of
the labels they read:

| Language | Stored-name order shows | Alphabetical labels |
|---|---|---|
| en | Certifying body, Exam board, University | same (by luck) |
| pt-BR | Certificadora, Banca examinadora, Universidade | Banca examinadora, Certificadora, Universidade |
| pt-PT | Entidade certificadora, Júri de exame, Universidade | Entidade certificadora, Júri de exame, Universidade (same) |

So today only pt-BR is visibly out of order; the defect is structural (the order depends on English names).

Duplicates (the same rule implemented again): none found. The only other `OrderBy` over a list in `src/` is the
users list (Identity), which sorts by names, not by a translated enum, and the UI-kit gallery source.
