---
feature: F-120
epic: foundation-and-identity
status: idea
board: 171
version: 1
---
# Architecture test for section id selectors

Technical terms: [glossary](../glossary.md)

## Summary
An architecture test that refuses a screen test whose selector is built on the id of an `AppSectionCard` without the `-title` suffix. The card renders its `Id` only on the heading, as `<Id>-title`, and the section element has no id, so such a selector finds nothing: the test fails for a defect of the test, not of the screen. The written rule (`.claude/rules/agile/project.md`, from F-97) was broken again in F-104, each time with one test unlock (events: F-97 `blocked`, F-104 `blocked`; F-104 unlocked six tests of `MultipleAnswerQuestionFormTests`). The test replaces the written rule once it exists; the rule line is then removed. Source: F-104 retro, lesson 1.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing; the architecture suite is the evidence.
- Suggested path: `/agile:autopilot` (small and clear once the selector pattern is listed).
- Parallel with: any.
