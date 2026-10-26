---
epic: exam-simulator
status: agreed
board: 6
---
# Exam Simulator

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: agreed (2026-10-26)
- Stopped at: Breakdown agreed; F-168 to F-176 captured as ideas.
- Next action: Answer the F-166 question round (owner).
- Waiting on: nothing

## Goal
A student takes a real exam edition as it was applied: the same questions, order and sections, the same time limit, and a score and cut-off calculated the way that organizer calculates them, so the student says it felt like the real exam. Source: `product/brief.md` capability 5 and the success line "felt like the real exam"; ADR-0001 #9, #10, #21, #44.

## What exists
Verified on 2026-10-26, on `main` at `8d0059d`:
- The question bank holds the eight types, versions, annulment, base texts and images (epic Question bank, F-95 to F-110 done). The Manaus 2026 paper (Instituto Consulplan, Edital nº 01/2026) is seeded: 60 single-choice questions of booklet type 1, question 47 annulled (D-6, F-110).
- `ExamEdition` has no time limit, scoring rule or cut-off; `NoticeSubject` has the label, group, question count and display order, and no weight or minimum (moved here by the Subject taxonomy decision of 2026-10-04).
- No module stores an attempt: F-166 (refining, worktree `D:/wt/simulab/f-166-store-student`) creates the `ExamEngine` module with the attempt and its answers, no screen; F-167 erases and exports them.
- Simulae (read only, `D:\dev\_control-alt-systems\simulae\repo\src\Modules\ExamEngine`) has the attempt aggregate, a Redis timer and lock, an auto-submit background service and a SignalR hub, and `ExamSessionSettings` (duration, pauses, anti-fraud, reopen policy) in `ContentCatalog`. No student screen and no scoring.
- The Manaus 2026 notice (owner's folder `Simulab/Guarda Municipal/Manaus/2026`): objective paper of 60 questions, 1.50 or 2.00 points per question by notice subject, 100 points, pass at 60 points; essay of 20 points, 20 to 30 lines, pass at 10 points; duration 5 hours; no wrong-answer penalty.

## Start
- Depends on: epic Question bank (delivered for this epic's needs); F-166 for every student-facing feature.
- Waits on: nothing to start. F-174 waits on a Cebraspe edition with true/false questions in the bank (owner) for its validation.

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|
| F-166 | Store a student's answers for an attempt | The `ExamEngine` module: attempt and answers, fixed questions with versions, no screen | Must | L | - | - | no | refining |
| F-167 | Erase and export a student's attempt data | Erasure anonymizes attempts; "download my data" includes them | Must | S | F-166 | - | no | idea |
| F-168 | Exam rules of an edition | Duration, points per question for each notice subject, objective and essay cut-off, in the edition form; the Manaus 2026 rules seeded | Must | M | - | - | no | idea |
| F-169 | Take an exam edition | The student picks a published edition, answers question by question with base texts and images, uses the answer sheet, sees the notice subjects as sections, writes the essay and submits | Must | L | F-166 | - | yes | idea |
| F-170 | Exam timer | Server countdown from the edition's duration, warning near the end, automatic submit, resume after closing the browser | Must | M | F-169, F-168 | - | no | idea |
| F-171 | Score and result of an attempt | Score by the edition's rules: total, per notice subject, passed or not at the cut-off, essay awaiting grade | Must | M | F-166, F-168, F-169 | - | yes | idea |
| F-172 | Answer review after an attempt | Each question with the student's answer, the key, the explanation and the annulled ones | Should | M | F-171 | - | no | idea |
| F-173 | My exam attempts | History of attempts, resume the one in progress, open a result | Should | S | F-169, F-171 | - | no | idea |
| F-174 | Wrong-answer penalty and other scoring models | One wrong cancels one right (Cebraspe), partial credit on multiple answer, annulled question counts for everyone | Should | M | F-168, F-171 | a Cebraspe edition with true/false questions in the bank — owner (validation) | no | idea |
| F-175 | Essay grade in the result | The grader's essay score joins the result with the essay cut-off | Could | S | F-171, F-151 | - | no | idea |
| F-176 | End-to-end test of a timed exam | One full timed Manaus 2026 attempt in a browser test (ADR-0001 #36) | Could | S | F-170, F-171 | - | no | idea |

## Execution plan
1. F-166 — every student-facing feature needs the attempt — path: `/agile:refine` (in progress) → `/agile:build` — parallel with: F-168.
2. F-168 — the rules the timer and the score read; `Catalog` module, no overlap with `ExamEngine` — path: `/agile:refine` → `/agile:build` — parallel with: F-166, F-167.
3. F-167 — F-166 is not released without it (F-166 decision of 2026-10-26) — path: `/agile:refine` → `/agile:build` — parallel with: F-168.
4. F-169 — the first screen a tester uses — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: none.
5. F-170 — the time limit that makes it feel real; imports the Simulae timer, lock and auto-submit — path: `/agile:refine` → `/agile:build` — parallel with: F-171.
6. F-171 — the score and cut-off; closes the first release cut — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: F-170.
7. F-172 — path: `/agile:autopilot` — parallel with: F-173.
8. F-173 — path: `/agile:autopilot` — parallel with: F-172.
9. F-174 — when a Cebraspe edition is in the bank — path: `/agile:refine` → `/agile:build` — parallel with: F-172, F-173.
10. F-175 — after F-151 — path: `/agile:autopilot` — parallel with: any.
11. F-176 — path: `/agile:autopilot` — parallel with: any.

## Waiting outside the epic
| What | For | Who provides it | Who asks |
|---|---|---|---|
| A Cebraspe edition with true/false questions in the bank, with its notice rules | F-174 (validation) | owner | Claude, at F-174 refinement |
| F-151 delivered (essay correction queue, epic Question bank) | F-175 | F-151 build | Claude |

## First release cut
F-166, F-167, F-168, F-169, F-170, F-171

## Out of scope
- Pauses, study mode and reopening an abandoned attempt — epic E-05 Question Bank Simulator, where practice is not a real exam.
- Anti-fraud by focus loss (Simulae `AntiFraudEnabled`).
- Booklet types with shuffled order (Manaus types 2 to 4): the seed holds type 1 only (D-6).
- A `Section` entity: the notice subjects are the sections (decision of 2026-10-26).
- Attempts limited by plan — epics E-07 and E-08 (`IEntitlementService`).
- ENEM score by Item Response Theory — brief open note "ENEM scoring".
- Ranking between students — brief, out of scope of the first version.
- Statistics across attempts over time — epic E-10 Performance analytics.

## Decisions
- 2026-10-26 — The owner agreed the breakdown and the four recommendations of the question round — owner.
- 2026-10-26 — The first release cut is F-166, F-167, F-168, F-169, F-170, F-171: Manaus 2026 complete, timed and scored — the brief's success line "felt like the real exam" needs the time limit (owner, question 1).
- 2026-10-26 — The sections of a paper are its notice subjects in paper order; no `Section` entity — `NoticeSubject` already carries label, group and question count, and Manaus needs no more; the glossary row `Section` stays "not decided" until a paper needs it (owner, question 2).
- 2026-10-26 — E-06 comes before E-05 Question Bank Simulator; `docs/epics/README.md` execution order is updated — the Manaus paper is ready and F-166 is already here; E-05 reuses `ExamEngine` later (owner, question 3).
- 2026-10-26 — Other scoring models (F-174) are Should, out of the first cut — Manaus (Consulplan) has no penalty, and without a Cebraspe edition F-174 cannot be validated (owner, question 4).
- 2026-10-26 — Scoring rules live on the edition (`Catalog`), the score is computed in `ExamEngine` — the question keeps only its answer key (Question bank decision of 2026-10-09) and the weight per notice subject moved here (Subject taxonomy decision of 2026-10-04); module placement is confirmed at F-168 refinement — Claude (technical).

## Retro notes

## Related
- Discussions: D-5, D-6, D-9
