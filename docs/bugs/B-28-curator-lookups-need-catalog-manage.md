---
bug: B-28
feature: F-95
status: idea
board: 165
severity: high
epic: Question bank
---
# A curator without catalog.manage cannot search subjects and topics in the question form

Technical terms: [glossary](../glossary.md)

## What happens
The question form picks the topic through two lookups: `CatalogApi.ListSubjectsAsync` (`src/Hosts/Simulab.Web/Components/Pages/QuestionBank/QuestionForm.razor:587`) and `CatalogApi.ListTopicsAsync` (`QuestionForm.razor:609`). Both call routes of the catalog that require `catalog.manage` (`src/Modules/Catalog/Simulab.Catalog.Api/SubjectEndpoints.cs:25`, `:37`, `:46`). The role `Curator` is granted `questions.manage` (`QuestionBankModule.cs:33`) and `catalog.browse` (`CatalogModule.cs:37`), not `catalog.manage`. So a curator who is not an admin most likely gets 403 on both lookups, cannot pick a topic, and cannot save a question (a topic is required, F-95 BR3).

Found while refining F-96 (2026-10-12), by reading the grants and the routes. Not reproduced on a running host: F-95 was validated with the seeded administrator, who holds every permission.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the local app host and an account with the `Curator` role and no other grant — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-96 touches the same form (a new "Origin" section) and adds lookups under `questions.manage` for editions; agree the order to avoid a conflict in `QuestionForm.razor` and `QuestionBankApiClient.cs`.

## Expected
A user with `questions.manage` and no `catalog.manage` picks a subject and a topic in the question form. The lookups for the question form belong to the question bank (guarded by `questions.manage`, fed by `ITopicDirectory`-style contracts of the catalog), the same way F-96 does for editions; granting `catalog.manage` to `Curator` is a roles decision for the owner, not the default fix.

## Cause
Not verified at runtime: measure it at `/agile:refine` with a curator account. Evidence in the code is the five lines above.

## Fix
Decided at refinement.

## Regression test
- A curator holding only `questions.manage` and `catalog.browse` gets 200 from the question form's subject and topic lookups — to be named at refinement.

## Open questions
- (none)

## Validation script

## Events

## Delivery
