---
feature: F-159
epic: Interface and screen composition
status: idea
board: 218
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
Remove these comments when the file leaves `idea`.
-->
# Review screens in the new composition

Technical terms: [glossary](../glossary.md)

## Summary
The review queue (`QuestionBank/ReviewQueue.razor`, route `/admin/questions/review`) and the review page of a question (`QuestionBank/ReviewQuestion.razor`) take the composition of the Control Alt Systems identity (D-4): page header, table card on the queue, section cards and the summary aside on the review page, one amber action per page. The colours and controls come from F-125 and F-148; the question form (F-126) already has the frame. Found in the E-18 breakdown (2026-10-24): no other composition item names these two screens.

## Start
- Depends on: F-125
- Waits on (to start): nothing
- Needed to validate: nothing
- Suggested path: `/agile:autopilot F-159`
- Parallel with: F-130
