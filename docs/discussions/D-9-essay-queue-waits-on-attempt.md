---
discussion: D-9
status: decided
date: 2026-10-26
---
# Essay correction queue waits on the student attempt

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: decided (2026-10-26)
- Stopped at: Direction chosen for F-151: it waits on the item that stores a student's answers (F-166).
- Next action: Refine F-166 with /agile:refine F-166 (owner).
- Waiting on: nothing

## Idea
F-151 is the screen where a grader scores the essays and open answers waiting for a grade. The owner opened it with no lean; the question was whether to build it now or wait for the student attempt it needs.

## What we already know
- D-5 made the queue its own item, depending on F-109 (types and rubric), F-150 (grader role and user-to-tenant link) and the student attempt.
- The rubric belongs to `QuestionVersion`, so a grade can name the rubric version that produced it.
- `src/Modules` holds only `Catalog`, `Identity`, `Plans` and `QuestionBank`: nothing stores a student's answers, and no feature or bug names it (searched 2026-10-26, no similar item).
- The board has two open epics for it, E-05 Question Bank Simulator (#5) and E-06 Exam Simulator (#6), with no epic file and no feature yet.
- F-152 (AI suggesting a grade, a person confirming) comes after the queue.
- Performance levels per criterion (ENEM style) stay parked until a curator writes a real essay rubric (D-5).

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — Wait for the student attempt | F-151 stays an idea; the item that stores answers is captured first | none now | The queue starts only after the simulator; rules out nothing |
| B — Queue with a contract and sample data | F-151 defines an interface the simulator will implement later; the screen runs on seeded sample answers | M | The shape of a student answer is guessed and may need rework; no grade reaches a student result until the attempt exists |
| C — Queue with its own table of answers to grade | F-151 creates the table in `QuestionBank`; the simulator writes into it | L | Pre-empts the attempt, which has no owner yet; hard to change later |

Recommendation: A — D-5 already placed the queue after the attempt, and without real answers the screen cannot be validated.

## Decisions
- 2026-10-26 — F-151 stays `idea` and waits on the item that stores a student's answers; no queue, contract or table is built before it — D-5 placed the queue after the attempt and, without real answers, the screen cannot be validated.
- 2026-10-26 — The item that stores a student's answers is captured now as F-166 in the epic E-06 Exam Simulator — F-151 and F-152 depend on it and nothing named it; a real exam edition is the case that needs essay correction (contest and entrance-exam essays).

## Parked
- Whether the grader picks a performance level or types a number — settled when F-151 is refined, with the first real essay rubric (D-5).
- Where the grader's screen lives (back office menu or its own area) — settled at the F-151 refinement.
- Whether the answers are shared with E-05 Question Bank Simulator — settled when F-166 is refined.

## Outcome
- Epics: E-06 Exam Simulator (existing, board #6; no epic file yet)
- Features: F-166 (captured); F-151 (waits on F-166)
- Bugs: none
