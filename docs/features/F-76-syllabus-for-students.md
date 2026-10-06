---
feature: F-76
epic: Subject taxonomy
status: refining
board: 120
version: 1
---
# Syllabus for students

Technical terms: [glossary](../glossary.md)

## Summary
A signed-in student opening a published exam (the page of F-36, `/catalog/exams/{id}`) can expand any of its published editions and see that edition's syllabus: the notice subjects with their group and number of questions, and the canonical topics each one covers.

## Start
- Depends on: F-74 (notice subjects, `approved`), F-75 (mapping to the taxonomy, `idea`), F-79 (subjects and topics, `validating`).
- Waits on (to start): the refinement of F-75 — the refinement of this item is paused until F-75 is `approved` (owner, 2026-10-06).
- Needed to validate: unknown — settled when the refinement resumes.
- Suggested path: `/agile:refine F-75` → resume `/agile:refine F-76` → `/agile:build` (change to an existing page).
- Parallel with: F-77, F-78.

## What exists (verified 2026-10-06)
- Student side (F-36, later touched by F-42 and F-44): `/catalog` (`CatalogSearch.razor`) and `/catalog/exams/{id}` (`CatalogExam.razor`), gated by `catalog.browse`. The exam page lists the published editions as rows of `AppItemRows`, read-only. There is **no edition page** for students: the original summary's "page of a published exam edition" was a false premise.
- Read side: `IPublishedExamQueries` (`Simulab.Catalog.Contracts`) with `ListAsync`, `FindAsync` (exam and its published editions) and `FiltersAsync`. Nothing about notice subjects or topics.
- `NoticeSubject` does not exist in `main`: F-74 is `approved`, not built.
- `Subject`, `Topic` and `Area` exist only on the F-79 branch (`validating`).
- The mapping from a notice subject to subjects and topics is not defined: F-75 is an `idea`. Its shape (to topics only, to whole subjects, or both) decides what this item shows.

## Decisions
- 2026-10-06 — Pause the refinement until F-75 is approved — this item only shows what F-75 defines; refining first would risk a change note (owner, question 1).
- 2026-10-06 — The syllabus opens inside the edition's row on the exam page, no new route — the edition is already a row there (owner, question 2).
- 2026-10-06 — Each notice subject shows its group, label and number of questions as the notice states them, and below it the canonical topics it covers (owner, question 3).
- 2026-10-06 — A notice subject not mapped yet is shown with a "topics not mapped yet" note; an edition without notice subjects says its syllabus is not registered yet (owner, question 4).
- 2026-10-06 — Reuse `catalog.browse`; draft editions never show (F-36 BR1); topic and subject names are content and are not translated; no new packages (Claude).

## Open questions
- What the student sees per topic and how the topics are grouped depends on F-75's mapping shape — answered when the refinement resumes.
