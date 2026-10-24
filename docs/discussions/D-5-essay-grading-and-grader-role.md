---
discussion: D-5
status: decided
date: 2026-10-24
---
# Essay grading and the grader role

Technical terms: [glossary](../glossary.md)

## Idea
F-109 adds the open-answer and essay question types with a rubric, but they only help a student once someone grades them. The owner proposed a grader (`Grader`) role: a person who signs in and grades the essays waiting for a grade, linked to a tenant like the curator; with no tenant the grader sees every essay, which matters until the first partner (a prep-course tenant) exists.

## What we already know
- The glossary already lists `OpenAnswer` and `Essay` in `QuestionType`; the brief (capability 3) lists both types, and its open note asks who grades them: a person, AI, or out of v1.
- ADR-0001 row 18 puts teacher and grader roles out of v1 ("RBAC lets them be added as data"); line 124 lists essay and open-answer grading as out of scope. This discussion reverts row 18 for the grader role.
- `User` inherits `IdentityUser<Guid>` and is a declared exception to `TenantEntity` (glossary): no user-to-tenant link exists in the code; `TenantId` lives on data rows and is dormant.
- No student attempt module exists yet (`Catalog`, `Identity`, `Plans`, `QuestionBank` only), so nothing produces answers to grade.
- `IAiGateway` exists (`Simulab.Ai`); it checks `IEntitlementService` and records usage and cost. The brief requires a person to review every AI output.
- Each new question type is one more `TypeCards` card, appears in the transcription screen (F-99) and needs a comment migration for the two `type` columns.

## Options
| Option | How it works for the user | Effort | Risks / what it rules out |
|---|---|---|---|
| A — Types and rubric only | The curator writes open-answer and essay questions with a rubric; grading comes in later items | M | Questions exist before anyone can answer or grade them; rules out nothing |
| B — Types, grader role, queue (people grade) | A grader signs in and grades essays, scoped by tenant | L | Nothing to grade until the student attempt exists; pulls Identity and RBAC work |
| C — B plus AI suggesting the grade | AI proposes a grade per criterion and the grader confirms | L+ | AI cost per student and grade quality; needs a person to confirm |

Recommendation: A now, B and C as separate items — F-109 has nothing to grade until the student attempt exists, and the user-to-tenant link is an Identity decision of its own.

## Decisions
- 2026-10-24 — F-109 delivers only the `OpenAnswer` and `Essay` types with a rubric — no student attempt exists to produce answers to grade.
- 2026-10-24 — `OpenAnswer` and `Essay` are two types sharing one rubric; the essay adds a theme and a size limit (lines or words) — contest and entrance-exam essays need them, and both types already exist in the glossary.
- 2026-10-24 — The rubric is a list of criteria (name, description, maximum points); the question's score is their sum; an optional model answer helps the grader — it is what the item description asks for; performance levels per criterion can be added later as data.
- 2026-10-24 — The rubric is part of `QuestionVersion`: changing it creates a new version with a reason, like the answer key — a grade must name the rubric that graded it.
- 2026-10-24 — A curator may publish these types before grading exists; the simulators decide whether to offer them — the bank stores content, the simulator decides what the student sees.
- 2026-10-24 — The grader is a person who signs in and grades; with no tenant the grader sees every essay to grade, with a tenant only that tenant's — owner's model; until the first partner tenant exists, the one grader grades everything.
- 2026-10-24 — The `Grader` role and a user-to-tenant link (for curator and grader) become a new item — no such link exists today (checked in the code), and it reverts ADR-0001 row 18 (RBAC adds roles as data).
- 2026-10-24 — The grading queue and screen are a separate item that depends on F-109, the grader role and the student attempt — it has nothing to show before a student answers.
- 2026-10-24 — AI suggesting a grade, with a person confirming, is a later item after the queue — the brief requires a person to review AI output, and it adds plan limits and cost.

## Parked
- Performance levels per criterion (ENEM style, 0 to 200 per competence) — wait for the first real essay rubric from a curator.
- AI grading with no person confirming — the brief rules it out; reopen only if the brief changes.
- Which epic owns the student attempt (Question Bank Simulator or Exam Simulator) — no epic file exists for either; settled when `/agile:epic` details them.

## Outcome
- Epics: Partner tenants (F-150), Question bank (F-151, F-152)
- Features: F-109 (scope narrowed), F-150, F-151, F-152
- Bugs: none
