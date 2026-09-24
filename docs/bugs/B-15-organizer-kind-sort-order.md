---
bug: B-15
feature: F-33
status: building
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

## Business rules
- BR15 (amends F-33 BR12): sorting the kind column follows the order of the reader's own labels, alphabetically in
  the reader's culture (`CultureInfo.CurrentCulture`, the same culture the page already renders in). The Web
  computes that order once (there are only three kinds) and sends it to the Api as `kindOrder`, a comma-separated
  list of the three `OrganizerKind` names, from first to last. The Api sorts by that order when given; when
  `kindOrder` is missing, not exactly the three known kinds, or has a repeat, it falls back to today's behavior
  (stored name order) — sorting by kind stays a secondary, non-critical column, so a bad parameter degrades instead
  of failing the whole list.
- BR16: within the same kind, organizers still sort by name ascending, whichever direction the kind sort runs
  (unchanged from BR12; confirmed by the owner, not reversed with the direction).

## Screens and API
- `GET /api/v1/catalog/organizers` gains one optional query parameter: `kindOrder` (string, comma-separated
  `OrganizerKind` names, e.g. `ExamBoard,CertifyingBody,University`). No new route, no error code: an invalid value
  is ignored, not refused.
- `Organizers.razor` computes `kindOrder` once per render (culture-aware `string.Compare` over the three labels from
  `OrganizerText.KindName`) and passes it on every list call, exactly as it already does for `sortBy`.

## Acceptance criteria
- AC1. Given the reader's culture is pt-BR, when they sort by Tipo ascending, then the order is Banca examinadora,
  Certificadora, Universidade — not the stored-name order.
- AC2. Given the reader's culture is pt-PT, when they sort by Tipo ascending, then the order is Entidade
  certificadora, Júri de exame, Universidade.
- AC3. Given the reader's culture is en, when they sort by Tipo ascending, then the order is Certifying body, Exam
  board, University.
- AC4. Given the same kind twice, when sorted by kind (either direction), then those rows are still ordered by name
  ascending.
- AC5. Given a request with no `kindOrder`, an unknown value in it, or a repeated kind, when the Api sorts by kind,
  then it falls back to the order in place before this item (stored name), and the request still succeeds.
- AC6. The localization criterion: no text is added, so the existing missing-key test stays green.

## Decisions
- 2026-09-24 (owner): sort by the reader's own labels, alphabetically in their culture; the Web computes the order
  and sends it to the Api. Reason: sorting means what the reader sees; three values make the trip cheap, and the
  labels stay only in the Web project (rule: i18n — the Api and the Catalog module carry no resources).
- 2026-09-24 (owner): the name tie-break inside a kind group always stays ascending, never following the kind
  direction. Reason: no product reason to invert it, and it keeps today's behavior for every other case.
- 2026-09-24 (Claude): `kindOrder` is optional and a bad value degrades to the pre-fix order rather than a 400.
  Reason: BR12 already treats kind sorting as non-critical (no dedicated error code exists for a sort parameter);
  an old Web build or a direct Api call must keep working.
- 2026-09-24 (Claude): the Api sort is built as a bounded three-way conditional (`Kind == first ? 0 : Kind == second
  ? 1 : 2`), not a lookup table. Reason: `OrganizerKind` has exactly three values by BR7, and EF Core translates a
  nested conditional to a SQL `CASE WHEN`; a `Dictionary` lookup would not translate and would force loading every
  row into memory to sort.

## Out of scope
- Any other translated-enum sort in the solution (none found).
- Making search or another column culture-aware; only the kind column changes.

## Open questions
None.
