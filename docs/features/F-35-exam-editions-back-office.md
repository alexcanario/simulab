---
feature: F-35
epic: Assessment catalog
status: refining
board: 754
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title and summary only (/agile:idea).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Exam editions back office

## Summary
Admin screens to manage the editions of an exam. An edition is one paper actually applied: the year, the job it selects for (named in the edition itself), the application date, the official link to its notice, and whether it is a draft or published. An edital that opens several jobs with different papers becomes one edition per paper (owner, 2026-09-20). The closest Simulae source is `ExamNotice` and `ExamNoticeFormDialog`, but the model is new work. Needs /agile:screen.

The edition is also **where the exam board lives** — a required link to `Organizer`, not a field on the exam (owner, 2026-09-26). Two reasons, both checked:

- The Manaus municipal guard edital (`Edital nº 01, de 23 de março de 2026`, read 2026-09-26) names the contracting body on its cover ("A PREFEITURA DE MANAUS, por meio da SEMAD, torna pública…") and the board in item 1.1 ("sendo sua execução de responsabilidade do Instituto Consulplan"). The board is declared inside the edital, and the edital is the edition.
- The simulator needs it there. The owner's use case: *"select 20 Mathematics questions from board Consulplan, from the editais of 2021 to 2026"* — a student practising a board's style (epic Exam Simulator, 695). That query walks question → paper → edition → board, and only works if each edition carries its own board: the same exam changes board between years, so a board on the exam would return the wrong questions for every year it changed.

So the edition carries the board and the notice year, and both have to be filterable. Recorded here because the data has to exist before the engine can ask for it.

## Goal
<!-- Why this feature exists, in one or two sentences. -->

## Users and use cases
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <Rule>.

## Screens and API
<!-- Routes, main components, endpoints (always /api/v1/...), error codes. -->
- <Route> — <purpose>
- <METHOD> /api/v1/<resource> — <purpose>
- Error codes: `<area>.<error>`

## Acceptance criteria
<!-- Given / When / Then. Each one is covered by a test. Always include the localization criterion. -->
- AC1 Given <context>, when <action>, then <result>.
- AC<n> All new texts appear in pt-BR, pt-PT and en.

## Decisions
<!-- date — decision — reason. Technical decisions made by Claude are recorded here too. -->
- <YYYY-MM-DD> — <decision> — <reason>

## Out of scope
- The notice document itself: only the official link is stored; the upload arrives with epic 698 (owner, 2026-09-23).
- Scoring rules, wrong-answer penalty and cut-off: epic 695 (owner, 2026-09-23).
- The notice's syllabus (`NoticeSubject`): epic 692.

## Open questions
<!-- Approval is blocked while any line here is not answered or marked "deferred (owner, YYYY-MM-DD)". -->
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-35
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
