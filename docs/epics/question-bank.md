---
epic: question-bank
status: agreed
board: 4
---
# Question bank

Technical terms: [glossary](../glossary.md)

## Goal
Give Simulab a bank of reviewed questions, from past exam editions and authored by curators, in the types the brief lists, with versions and annulment, so the Question Bank Simulator (E-5) and the Exam Simulator (E-6) have something real to practice. Source: `product/brief.md` capability 3; ADR-0001 #8, #10, #27, #43.

## What exists
Verified in `src/` on 2026-10-09: no question code in Simulab (no entity, module, migration or endpoint). The `Catalog` module has `Exam`, `ExamEdition`, `NoticeSubject`, `NoticeSubjectMapping`, `Subject` and `Topic` (F-33 to F-37, F-74, F-75, F-79). No `IFileStorage` yet: F-89 (approved, paused) creates the `Simulab.Storage` building block. The UI kit has `AppMultiPickField` and `AppChip` (F-75), meant for question filters.

Simulae (read-only source, `src/Modules/ContentCatalog`): one `Questions` table with a polymorphic JSON type configuration (`ConfigurationBase.cs`), `AnswerOptions`, `QuestionReviewDecisions`; 8 types (T1 to T8, T5 Hotspot is not in the brief); register, transcription, edit and review-queue pages; about 360 question tests. Known gaps not to import: statement and base text are raw HTML typed in a text field with no sanitizer; `EmRevisao` and `Rascunho` are never assigned; no question list page; no file storage (images only as inline HTML); the wrong-answer penalty is stored on each true/false question.

## Start
- Depends on: epic Subject taxonomy (E-3): F-79 done (questions point at `Topic`); F-75 done (notice subjects).
- Waits on: nothing to start. Content rights (which sources may be reproduced and what the source line shows) waits on the owner, for F-96 and F-110.

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|
| F-95 | Single-choice questions in the back office | New `QuestionBank` module: a curator writes a single-choice question (statement, inline base text, options, answer key, explanation, difficulty, topic, content language), saves it as draft, publishes it, lists and previews it; topic delete guard | Must | L | - | - | yes | idea |
| F-96 | Questions from a past exam edition | A question records its edition, its number in the paper and its notice subject; the edition lists its questions; a source line shows organizer, exam and year | Must | M | F-95 | what the source line must show — owner | no | idea |
| F-97 | True/false questions | The true/false type (answer key only; the penalty is the edition's scoring rule, E-6) | Must | M | F-95 | - | no | idea |
| F-98 | Question versions and answer key changes | Editing a published question creates a version; an answer key change records its reason and date; an attempt can name the version that graded it | Must | M | F-95, F-97 | - | no | done |
| F-99 | Transcribe the paper of an edition | Fast sequential entry of an edition's questions: edition and notice subject kept, number auto-incremented | Should | M | F-96, F-97 | - | yes | idea |
| F-100 | Find questions in the back office | Filters by subject, topic, organizer, edition, type, status, difficulty and text | Should | S | F-95 | - | no | idea |
| F-101 | Annul a question | A curator annuls a published question with a reason; the simulators leave it out or score it by the organizer's rule (E-6) | Should | S | F-98 | - | no | done |
| F-102 | Shared base text | One base text (reading passage) serves several questions | Should | M | F-95 | - | yes | done |
| F-103 | Images in questions | Images in the statement, base text and options, stored through `IFileStorage` | Should | M | F-95, F-89 | - | no | idea |
| F-104 | Multiple-answer questions | The multiple-answer type | Should | M | F-95 | - | no | done |
| F-105 | Review before publishing | `InReview` status, review queue, approve or reject with a reason, a review permission; may move to epic E-9 | Should | M | F-95 | - | yes | idea |
| F-106 | Matching questions | The matching type | Could | M | F-95 | - | no | idea |
| F-107 | Fill-in-the-blanks questions | The fill-in-the-blanks type | Could | M | F-95 | - | no | idea |
| F-108 | Short-answer questions | The short-answer type with accepted answers | Could | S | F-95 | - | no | idea |
| F-109 | Open-answer and essay questions | The open-answer and essay types with their rubric; grading moves to F-150 to F-152 (D-5) | Could | M | F-95 | - | no | idea |
| F-110 | Sample questions of the municipal guard editions | Seeded questions for the F-37 editions, for local work and testers | Could | S | F-96, F-97 | source questions and the right to use them — owner | no | idea |
| F-151 | Essay and open-answer correction queue | A grader scores the essays and open answers waiting for a grade, by rubric criterion (D-5) | Could | M | F-109, F-150 | the item that stores a student's answers (simulator epics) — owner | unknown | idea |
| F-152 | AI-suggested essay grade | The AI proposes a score per criterion and a grader confirms (D-5) | Could | M | F-151 | AI cost and per-plan limit — owner | unknown | idea |
| F-154 | Question form field polish | Subject list on click, difficulty labels, character counters (D-6) | Could | S | - | - | unknown | idea |
| F-155 | Autosave of the question draft | Unsaved edits kept without a click on Save (D-6) | Could | M | - | - | unknown | idea |
| F-158 | Review queue count in the menu | How many questions wait for review, beside the menu item (D-6) | Could | S | - | - | no | idea |
| F-142 | Question similarity search | Duplicate and reused questions across editions, similar questions, topic suggestion by neighbourhood; full-text first, vectors later | Could | M | - | questions in volume; embedding model choice (E-9 ADR) — owner | no | idea |

## Execution plan
1. F-95 — creates the module, the model and the safe content format (sanitized HTML or Markdown, settled at its refinement); every other feature needs it — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: none.
2. F-96 — exam questions are the product's core content — path: `/agile:refine` → `/agile:build` — parallel with: F-97 or F-98.
3. F-97 — true/false covers Cebraspe exams — path: `/agile:autopilot` (small, clear once F-95 sets the type pattern) — parallel with: F-96 or F-98.
4. F-98 — versions must exist before E-5 records any attempt — path: `/agile:refine` → `/agile:build` — parallel with: F-96 or F-97.
5. F-99 — volume of real questions for testers; closes the first release cut — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: F-100.
6. F-100 — the list grows with transcription — path: `/agile:autopilot` — parallel with: F-99, F-101.
7. F-101 — path: `/agile:autopilot` — parallel with: F-100, F-102.
8. F-102 — reading-comprehension questions — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: F-101, F-104.
9. F-104 — path: `/agile:autopilot` — parallel with: F-102.
10. F-103 — after F-89 is built — path: `/agile:refine` → `/agile:build` — parallel with: F-104.
11. F-105 — before or with epic E-9, where every AI-extracted item needs human review — path: `/agile:refine` with `/agile:screen` → `/agile:build` — parallel with: none.
12. F-106, F-107, F-108 — remaining objective types, by demand — path: `/agile:autopilot` each — parallel with: each other.
13. F-109 — the grading decision is settled in D-5 (2026-10-24) — path: `/agile:refine` → `/agile:build` — parallel with: none. Then F-151 (after F-150 and the item that stores a student's answers) and F-152.
14. F-110 — once the source questions are in hand — path: `/agile:refine` → `/agile:build` — parallel with: any.

## Waiting outside the epic
| What | For | Who provides it | Who asks |
|---|---|---|---|
| Which past-exam sources may be reproduced and what the source line must show (brief open note "Rights to reproduce past exams") | F-96, F-110 | owner | Claude, at F-96 refinement |
| ~~How open answers and essays are graded~~ — settled in D-5 (2026-10-24): a person grades; AI only suggests, later (F-152) | F-109 | owner | done |
| The questions of the municipal guard editions and the right to use them | F-110 | owner | Claude, at F-110 refinement |
| `IFileStorage` built (F-89) | F-103 | F-89 build | Claude |

## First release cut: mvp-cadastro-provas
F-95, F-96, F-97, F-98, F-99

## First release cut: mockup-stitch-3-barra-app
Rank: 3
F-158, F-156, F-157

## Out of scope
- Hotspot questions (Simulae T5) — not in the brief.
- Wrong-answer penalty, weight and scoring of annulled questions — epic Exam Simulator (E-6), with the edition's scoring rules (owner, 2026-10-09).
- Question visibility by plan — epics E-7 and E-8.
- Private questions of a student or an institution — epic Institutions (multi-tenancy).
- AI-assisted import of past exams — epic E-9.
- Translating question content — ADR-0001 #27.

## Decisions
- 2026-10-24 — Essays and open answers are graded by a person (a new `Grader` role, F-150); F-109 delivers only the types and the rubric, the correction queue is F-151 and the AI-suggested grade F-152 — no student attempt exists to produce answers to grade (D-5, owner).
- 2026-10-09 — Questions live in a new module `QuestionBank` (schema `question_bank`), five-project shape (imported from Simulae, ADR-0001 #2b); it references `Topic` and `ExamEdition` by id through `Catalog.Contracts` — own lifecycle (versions, review) and consumed by E-5, E-6 and E-9 through contracts; the topic delete guard becomes a cross-module query (owner, question 1).
- 2026-10-09 — The wrong-answer penalty belongs to the edition's scoring rules (E-6), not to each question; the question keeps only its answer key — same reasoning as the weight per notice subject (decision of 2026-10-04); E-5 applies the origin edition's rule when there is one (owner, question 2).
- 2026-10-09 — F-95 has only `Draft` and `Published`, like editions; `InReview` and the review queue arrive with F-105, which may move to E-9 — shortest path to testers; the glossary already says `InReview` joins with the AI import (owner, question 3).
- 2026-10-09 — First release cut is F-95 to F-99 — single choice and true/false cover most public service exams; versions before any attempt exists avoid a data migration; transcription gives volume (owner, question 4).
- 2026-10-09 — Do not import Simulae's raw-HTML fields without a sanitizer, nor its unused `EmRevisao`/`Rascunho` paths — known defects of the source (Claude, from the Simulae read).
- 2026-10-20 — first release cut written by /agile:mvp: 5 items (criterion: the goal, dependency closure); label: mvp-cadastro-provas; goal: "A aplicacao deve ser capaz atraves do usuario responsavel cadastrar os tipos de provas elencados no levantamento de requisitos"; gaps left: 0
- 2026-10-24 — milestones of mockup-stitch written by /agile:mvp: 3 milestones (11 items); label: mockup-stitch; goal: "Formulário de questão no visual do mockup do Stitch: F-137, F-153, F-154, F-155, F-156, F-157, F-158, F-135, F-136, F-138, F-149"; gaps left: 0; milestones: mockup-stitch-1-formulario (campos, alternativas e resumo do formulario de questao), mockup-stitch-2-navegacao (indice, previa, atalhos e autosave no formulario), mockup-stitch-3-barra-app (busca global, notificacoes e contador da fila)

## Related
- Discussions: none
- Decisions: `docs/decisions/ADR-0001-foundation.md` (#8 global data, #10 question versions, #23 file storage, #27 content language, #43 topic link)
- Epics that consume this one: E-5 Question Bank Simulator, E-6 Exam Simulator, E-9 AI-assisted exam import, E-10 Performance analytics
