---
feature: F-124
epic: Foundation and identity
status: building
board: 175
version: 1
---
# Freeze the pt-PT and en app manual

Technical terms: [glossary](../glossary.md)

## Summary
Stop updating the pt-PT and en app manual (`docs/manual/pt-PT/`, `docs/manual/en/`) when a feature ships; pt-BR stays the only maintained manual. Today every ship writes each page in three languages (`Manual locales:` in `CLAUDE.md`), which triples the manual work of every item while Simulab has no real pt-PT or en user. Captured from the owner's question of 2026-10-17. Nothing is deleted: each frozen page keeps its content and gets a notice with the freeze date and the app version, pointing to the pt-BR page. The UI locales (`pt-BR, pt-PT, en`) and the legal documents are not touched.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing beyond the repository; the owner reads the frozen pages and the rule lines — owner.
- Suggested path: `/agile:build` (small and clear: one line, one rule line, 46 notices and one test).
- Parallel with: F-105 (refining). No shared files, except that F-105's ship writes manual pages: after this item merges, it writes pt-BR only.

## Goal
Cut the manual work of every item to one language, without losing the translated pages and without leaving a reader of a frozen page believing it is current.

## Users and use cases
- UC1 A developer ships a feature and writes the manual page in pt-BR only (the ship obeys `Manual locales:`).
- UC2 A reader opens a frozen pt-PT or en page and sees at once that it is frozen, since when and for which app version, with a link to the maintained pt-BR page.
- UC3 A maintainer who finds the first real pt-PT or en user can tell which pages are frozen (the `frozen:` key) and unfreezes by the rule line of `project.md` (BR8).

## Business rules
- BR1 `CLAUDE.md` lists the manual locales as `- Manual locales: pt-BR`, alone on its line; the sentence about the manual folder moves to its own line. The list ends the line: a list followed by a full stop and more text has been read without its last locale (`docs/agile/retro-log.md:1179`).
- BR2 `docs/manual/pt-PT/` and `docs/manual/en/` keep all 23 pages each (index included); no page is deleted, renamed or retranslated, and `updated:` and `features:` in their front matter stay as they are (they record the last content update).
- BR3 Every page of both frozen folders carries the front matter key `frozen: <YYYY-MM-DD> (v<version>)` and, right under its `# ` title, one notice line in the page's own language that repeats the date and version and links the pt-BR page of the same slug. The date is the day the build writes the notices and the version is `<Version>` in `Directory.Build.props` that day. The pt-BR pages never carry `frozen:`.
- BR4 The notice, English: `> Frozen on <date> (v<version>). This page may be out of date; see the [pt-BR version](../pt-BR/<slug>.md).` The pt-PT twin: `> Congelada em <date> (v<version>). Esta página pode estar desatualizada; consulte a [versão em pt-BR](../pt-BR/<slug>.md).`
- BR5 The `create-account` page is frozen like the others. `PrivacyManualTests` keeps checking `create-account.md` in pt-BR only; the pt-PT and en cases are removed.
- BR6 `.claude/rules/agile/project.md` changes in three places: line 61 says the manual is pt-BR only while `Manual locales:` lists pt-BR alone; line 131 asks for the `create-account` manual line in pt-BR; one new line says the definition of done's "three languages" reads as the locales of `Manual locales:` and that a new flow page links `../manual/pt-BR/<page>.md` (the links already written to `../manual/en/` stay). `definition-of-done.md` (a copy of the plugin's rule) is not edited.
- BR7 `UI locales:`, the resource files, the missing-key test and the legal documents (terms, privacy policy) stay as they are.
- BR8 The unfreezing rule, one line in `project.md`: when the first real user of a locale exists, add the locale back to `Manual locales:`, translate every page from pt-BR and remove its `frozen:` key and notice; the plugin's catch-up only adds pages a folder lacks and never refreshes a stale one.

## Screens and API
No screen and no API: a configuration line, rule lines, 46 notices and one test.

## Docs
- none — the item changes a configuration line, rule text and manual notices; the unfreezing rule lives in `project.md` (BR8), and no flow, how-to, reference or architecture page is touched.

## Acceptance criteria
- AC1 Given `CLAUDE.md`, when the `Manual locales:` line is read, then it is exactly `- Manual locales: pt-BR`, and the `UI locales:` line still lists `pt-BR, pt-PT, en`.
- AC2 Given the 23 pages of `docs/manual/pt-BR/`, when the pt-PT and en folders are listed, then each holds the same 23 slugs.
- AC3 Given any page of `docs/manual/pt-PT/` or `docs/manual/en/`, when its front matter and first lines are read, then it has `frozen: <YYYY-MM-DD> (v<version>)` and, right under the title, the notice of its language with that date and version, linking `../pt-BR/<slug>.md`, and that pt-BR page exists.
- AC4 Given any page of `docs/manual/pt-BR/`, when its front matter is read, then it has no `frozen:` key.
- AC5 Given `PrivacyManualTests`, when it runs, then it checks `create-account.md` in pt-BR only and passes.
- AC6 Given `project.md`, when the manual lines are read, then they carry the three changes of BR6 and the unfreezing rule of BR8, and `definition-of-done.md` is unchanged.
- AC7 No UI text is added: the resource files and the `UI locales:` set are unchanged, and the missing-key test stays green.

## Criterion → test

Tests locked: 4f08bcca3f34dba2a8a4faf5f8665e3bbbdd0d40 (1 file); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `FrozenManualTests.ManualLocales_ListsPtBrAlone_AndUiLocalesAreUntouched` |
| AC2 | `FrozenManualTests.FrozenFolder_HoldsTheSameSlugsAsPtBr`, `FrozenManualTests.ReferenceFolder_Holds23Pages` |
| AC3 | `FrozenManualTests.FrozenPage_CarriesTheFrozenKeyAndTheNotice` |
| AC4 | `FrozenManualTests.PtBrPage_HasNoFrozenKey` |
| AC5 | `PrivacyManualTests.CreateAccount_NamesTheRealRegionAndProcessors` |
| AC6 | validation step 1 |
| AC7 | `FrozenManualTests.ManualLocales_ListsPtBrAlone_AndUiLocalesAreUntouched` (the `UI locales:` line), the existing missing-key tests of each module |

## Decisions
- 2026-10-17 — Only stop updating; nothing is deleted — reversible and cheap — asked
- 2026-10-17 — The freeze is the `Manual locales:` line listing pt-BR alone: the ship and `i18n.md:26` already obey that line, so no plugin change is needed — `references/conventions.md` "App manual languages" — confirmed: .claude/rules/agile/i18n.md
- 2026-10-17 — The manual is not served or published by anything: no file under `src/`, `tests/`, `tools/` or `.github/` reads `docs/manual` except `PrivacyManualTests`. So there is no language picker to worry about, the frozen locales are labelled and not hidden, and the notice is the only signal a reader gets — verified in the code, answers the two open points of the idea — confirmed: tests/Simulab.ArchitectureTests/PrivacyManualTests.cs
- 2026-10-17 — Notice on every frozen page (46), as front matter key plus a visible line in the page's language — flow pages link straight to manual pages, so an index-only notice would be missed — asked
- 2026-10-17: Does `create-account` also freeze? — owner chose "Congela também" over the recommended "Só ela fica nos 3 idiomas" (average 5.8 vs 8.2); kept after the follow-up card. Risk the owner accepted: after a change of data region or processor, the frozen pt-PT and en `create-account` pages can name the old ones, and only the notice warns the reader — asked
- 2026-10-17 — `definition-of-done.md:18` is a copy of the plugin's rule, so it is not edited; `project.md` (not touched by `/agile:sync`) says "three languages" reads as the locales of `Manual locales:`, and the wording fix goes to the plugin as a retro note — asked
- 2026-10-17 — `Manual locales:` alone on its line, the folder sentence on the next line — the template writes the list alone (`docs/agile/templates/project-claude.md:38`) and a trailing sentence cost a locale in `sync.js plan` (`retro-log.md:1179`) — confirmed: docs/agile/templates/project-claude.md
- 2026-10-17 — The notice's version is `<Version>` of `Directory.Build.props` (1.1.0 today) and its date the day of the build, both written once — the idea asked for "date and version" — Claude decided, no product impact
- 2026-10-17 — New flow pages link the pt-BR manual page and old links to `../manual/en/` stay — the maintained page is the current one, and the en page still exists with its notice — Claude decided, no product impact
- 2026-10-17 — One architecture test class `FrozenManualTests` reads the files as `PrivacyManualTests` does; no other code — AC1 to AC4 are file facts that would drift unseen — Claude decided

- 2026-10-17 — Parallel build with F-105 (validating) authorized by the owner in chat; no shared files — owner
- 2026-10-17: plan — 1 `FrozenManualTests` (AC1-AC4) and `PrivacyManualTests` in pt-BR only (AC5), seen failing → commit `test(F-124)` and lock; 2 `CLAUDE.md` `Manual locales:` line (AC1); 3 the 46 pages: `frozen:` key and notice written by one throwaway script run from the scratchpad (AC2-AC4); 4 `project.md` three edits and the unfreezing rule (AC6); 5 gate, coverage table, scan, validation script → `FrozenManualTests`, `PrivacyManualTests` green
- 2026-10-17 — Design passes skipped: no project, contract, message or schema change (configuration, rule text, markdown and one test) — Claude decided

## Out of scope
- Unfreezing and translating the pages again (a later item, when a real pt-PT or en user exists).
- Hiding or deleting the frozen folders.
- The UI locales, resource files and missing-key test; the legal documents.
- The wording of the plugin's definition of done (a plugin note at the retro).
- The plugin's copies in `docs/agile/` (`workflow.pt-BR.md`, `workflow-examples.pt-BR.md`).

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written by /agile:build at its end. -->

## Events

- 2026-10-17 question — round 1: asked 3, from context 2
- 2026-10-17 override — create-account freeze: owner chose Congela tambem over recommended (5.8 vs 8.2); kept

## Delivery
- Branch: feature/F-124
- Merge: -
- Tests: -
- Manual pages: pt-PT and en pages frozen (46 notices); pt-BR unchanged
