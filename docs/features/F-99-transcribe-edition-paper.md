---
feature: F-99
epic: Question bank
status: idea
board: 145
version: 1
---
# Transcribe the paper of an edition

Technical terms: [glossary](../glossary.md)

## Summary
A transcription mode for one edition: the curator types its questions one after the other, the edition and the current notice subject stay selected and the number in the paper increments by itself, so a whole paper is entered without reopening the form. It gives testers a real volume of questions before the AI import (E-9). Source: epic Question bank (E-4); Simulae `QuestionTranscriptionPage` as the reference.

## Start
- Depends on: F-96, F-97.
- Waits on (to start): nothing.
- Needed to validate: the local app host and one real past paper to transcribe a few questions from — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: F-100.
