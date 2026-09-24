---
bug: B-14
feature: F-33
status: idea
board: 758
severity: low
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# A concurrent duplicate organizer answers 500 instead of 409

## What happens
Two requests creating organizers with the same normalized name (or acronym) at the same time both pass
`SaveOrganizerHandler.TakenAsync`, because each reads the table before the other writes. The unique index does its
job and refuses the second insert, but the handler does not translate the violation, so `SaveChangesAsync` throws
`DbUpdateException` and the caller gets an uncoded 500. F-33 BR9 promises 409 `organizer.name_taken` /
`organizer.acronym_taken`. Found by the independent review of F-33 (2026-09-23); accepted there because the back
office has one Admin at a time, so the race is unlikely in v1.

## Expected
- A duplicate refused by the database answers with the same 409 and the same error code as a duplicate caught by
  the handler's own check, whichever of the two wins the race.
