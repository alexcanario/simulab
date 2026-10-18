---
epic: ai-assisted-exam-import
status: draft
board: 9
---
<!--
One file per epic. Save as: docs/epics/<slug>.md
Status: draft -> agreed (owner said so) -> done (every feature done or dropped).
Created by /agile:epic. Features are refined one at a time with /agile:refine.
-->
# AI-assisted exam import

Technical terms: [glossary](../glossary.md)

## Goal
A curator uploads the PDF of a past exam paper and its answer key, and Simulab turns them into draft questions (statement, options, shared base text, paper number, answer key, suggested topic) that the curator reviews before publishing, instead of typing every question by hand (F-99). Source: `product/brief.md` capability 4; ADR-0001 #20, #22, #23, #42, #45 and the deferred "dedicated OCR service" (ADR-0001 line 127). Board epic migrated from Azure Boards AB#698.

## What exists
Verified in `src/` on 2026-10-18: nothing of the import is built.
- `IAiGateway` in `src/BuildingBlocks/Simulab.Ai`, one implementation, `AnthropicAiGateway`, text-only: the request is one text prompt (`AnthropicAiGateway.cs:128`), no PDF or image input. `AiOptions.ModelFor(purpose)` already routes a model per purpose (`AiOptions.cs:36`).
- Job table and worker in the `Api` host (ADR-0001 #20, F-13).
- `QuestionBank` module: questions with options, shared base text (F-102), versions (F-98), annulment (F-101), `Draft` / `InReview` / `Published` and the review queue (F-105, done).
- No `IFileStorage` (F-89 is an idea), no eval suite (F-40 is an idea), no vector or full-text extension in PostgreSQL.

## Candidate approach
Recorded on 2026-10-18 from a discussion with the owner about RAG over legal PDFs. A candidate, not a decision: the first E-9 item settles it in an ADR.
1. **Deterministic extraction first.** A small worker using Docling (Python, open source: layout, two-column pages, tables, OCR) turns the exam PDF and the answer-key PDF into text plus layout. Docling comes before the model because the gateway takes text only today.
2. **Structured extraction through `IAiGateway`.** A new purpose (for example `exam_import`) finds question boundaries, statement, options, shared base text, paper number and answer key. `AiOptions` routes that purpose to a local Ollama model on a dedicated machine (no per-token cost), through a new `OllamaAiGateway` beside `AnthropicAiGateway`. Usage and cost are still recorded by the gateway.
3. **Deterministic validation of the JSON.** 2 to 5 options, sequential paper numbers, `QuestionLimits`; a failure goes to the curator's review queue. The output becomes `Draft` / `InReview` questions; the topic is suggested through the aliases (ADR-0001 #45) and the curator confirms it.
4. **Runs as a job** (ADR-0001 #20) and keeps the original file through `IFileStorage` (ADR-0001 #23), so the curator compares the draft with the original.
5. **ADR.** This is the "dedicated OCR service" decision ADR-0001 deferred; it becomes an ADR in the first E-9 item, together with the embedding model choice of F-142.

## Start
- Depends on: F-40 (eval suite: required before any E-9 item, see `docs/epics/README.md`, step 12), F-89 or another item that creates `Simulab.Storage` / `IFileStorage`.
- Waits on: the open questions below — owner.

## Open questions
1. Is a Python worker (Docling) accepted as a second language in the solution? The owner allows other languages for tools when needed; adoption is confirmed at refinement.
2. Ollama hardware: a GPU with 16 to 24 GB for 8B to 14B models — not verified; who provides the dedicated machine and where it runs (dev only, or staging too).
3. Quality of a local model against Claude on scanned papers — measured by the F-40 golden exams before choosing.
4. Preliminary versus final answer key, and annulments after appeals: which one an import reads and how a later key updates it (F-98, F-101).
5. Reproduction rights per source (`product/brief.md:79`, still open).

## Features
| Id | Feature | Value | Priority | Size | Depends on | Waits on | Screen design | Status |
|---|---|---|---|---|---|---|---|---|

## Execution plan
Not broken down yet: `/agile:epic ai-assisted-exam-import` details it.

## Waiting outside the epic
| What | For | Who provides it | Who asks |
|---|---|---|---|
| Python worker yes or no | first E-9 item | owner | Claude |
| Dedicated machine with a GPU for Ollama | first E-9 item | owner | Claude |
| Rights to reproduce each source | every E-9 item | owner | Claude |

## First release cut

## Out of scope
- Translating question content (each exam keeps its own language).

## Decisions

## Retro notes

## Related
- F-142 Question similarity search (question-bank epic): shares the Ollama machine and the embedding model choice.
