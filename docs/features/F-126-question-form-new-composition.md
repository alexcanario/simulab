---
feature: F-126
epic: Foundation and identity
status: refining
board: 177
version: 1
---
# Question form in the new composition

Technical terms: [glossary](../glossary.md)

## Summary
Cut on 2026-10-23 into two parts: this is part 1 (part 2 is F-149, option rows as keys). The question form (`QuestionBank/QuestionForm.razor`) takes the frame of the Stitch reference screen approved on 2026-10-17 (D-4): numbered section cards, and the summary aside that now also holds the publication actions, with **Publish** as the screen's single amber action. The preview and status-reason dialogs keep their composition and only wear the F-148 look. The colours and controls come from F-125 and F-148; the option and key rows (keycap letters, the correct option as a green key with "Correct") and the student preview content are F-149.

## Start
- Depends on: F-125 (done), F-148 (done)
- Waits on (to start): nothing
- Needed to validate: the owner, on screen, in both themes, at desktop and at 1280px and below; the local app host with Docker Desktop running; a `Curator` account and an `Admin` account (rule: a screen that calls a permission-guarded route is validated with the least-privileged role) — owner
- Model: sonnet (no signal: no door, no public contract, no schema, size M after the cut)
- Suggested path: `/agile:screen F-126` (the mockup with every state) → approval → `/agile:build F-126`
- Parallel with: F-127, F-128, F-129 (different pages). Not with F-122, F-123, F-135, F-136, F-137, F-138 (ideas that change the same form): they come after this item.

## Goal
Give the question form the frame of the new identity: a numbered path through its sections and one place, the aside, where the state of the question and every publication action live, with the one action that matters most in amber.

## What exists (verified 2026-10-23)
- `QuestionForm.razor` is 2664 lines. Its form card holds six `AppSectionCard` sections in this order: Classification (`question-section-classification`), Origin (`-origin`), Statement (`-statement`), Base text (`-base-text`), then one of five variants (Options `-options`; True/false key `-true-false-key`; `MatchingKeySection`; `FillInTheBlanksKeySection`), then Explanation (`-explanation`). `AppSectionCard` (F-43) has no number.
- Save is `AppFormActions` at the bottom right of the form card; a frozen question (annulled or in review) has none (F-101 BR5, F-105 BR3).
- The aside is `AppFormAside` (F-43): summary rows (`AsideRows`) and the "Before publishing" checklist (`AsideChecklist`). It is sticky, 20rem wide beside the form and stacked under it below 1280px (`app.css` `.app-form-layout`). It carries no button (F-43 BR4, comment in `AppFormAside.razor`).
- Publication is a separate `MudPaper` card below the form (`question-section-publication`, visible when not transcribing): status chip and text, annulment details, the rejection alert (F-105), Preview, and per state Send for review + Publish (draft), Withdraw (in review), Unpublish + Annul (published), Reinstate (annulled), the visible disabled reason (`question-publication-reason`, `question-annul-reason-hint`) and the change-reason field a publish may ask for (F-98). Below it come the review history and version history cards.
- Every publication button is an outlined `MudButton` today; none is amber. `AppActionButton` (F-148) is the amber key: it fixes its icon to `AppIcons.Restart` (`AppActionButton.razor`, `AppIcons.cs:17`), has no id, no busy state and no `aria-describedby`.
- `ActionColourScopeTests.RoutedPages_PutNoTertiaryColourAndNoActionShortcut` fails on any routed page that uses `Color.Tertiary` on a button or `AppActionButton` (F-148 AC5); this item is the first screen allowed to.
- 19 test files in `tests/Hosts/Simulab.Web.Tests/QuestionBank/` render the form; 8 of them use the publication ids.
- Items that touched this code since its creation: F-95, F-96, F-97, F-98, F-99, F-101, F-102, F-104, F-105, F-106, F-107, F-148.

## Users and use cases
- UC1 A curator opens a question and reads its sections as numbered steps, 1 to 6, whatever the type.
- UC2 A curator sees the state of the question and every publication action together in the aside, next to the summary and the "Before publishing" checklist, without scrolling to a card below.
- UC3 A curator publishes a complete, saved draft with the one amber action of the screen.
- UC4 A curator sees, in words, why Publish is not available (unsaved changes, incomplete checklist).
- UC5 A curator uses the other publication actions (preview, send for review, withdraw, unpublish, annul, reinstate) exactly as before, from the aside.

## Business rules
- BR1 The sections of the form card are numbered in the order shown: 1 Classification, 2 Origin, 3 Statement, 4 Base text, 5 the key section (Options, True/false key, Matching or Blanks), 6 Explanation. Every type has the same six numbers. The number is decorative (`aria-hidden`); the heading text and the section ids do not change. The publication block and the history cards are not numbered.
- BR2 The numbered card is a kit pattern (`AppSectionCard` gets an optional `Number`), shown in the gallery; the two key-section components receive the number from the page.
- BR3 The publication block lives in the aside (`AppFormAside` gets a content slot after the checklist, so the aside is no longer buttonless: F-43 BR4 is changed here). The Publication card below the form no longer exists. The review history and version history cards stay below, in the same order.
- BR4 The block holds exactly what the card held, with the same ids, texts, order of buttons, enabling rules and visible reasons: the status chip and text; the annulment date and reason; the rejection alert; Preview; per state Send for review and Publish (draft), Withdraw (in review), Unpublish and Annul (published), Reinstate (annulled); the disabled reason; the publish change-reason field; "Save the question first" for an unsaved question. No rule, route, permission or error code changes.
- BR5 Publish is the screen's single amber action: it appears only for a saved draft, as the compact `AppActionButton` (the aside is narrower than 360px). When Publish is not available it is shown disabled (sunken, a real `disabled`) with the reason as visible text under it, as today. In every other state of the question there is no amber action. Send for review, Withdraw, Unpublish, Reinstate, Preview and Annul stay outlined (Annul keeps the error colour).
- BR6 `AppActionButton` gets what the form needs: its icon is a required parameter (the right value depends on what the action means: Publish passes `AppIcons.Publish`, the gallery passes `AppIcons.Restart`), an `Id` on its button, a busy state (progress in place of the icon, button disabled) and an `aria-describedby` for the visible reason. The tooltip around it stays as today. A disabled amber key sits on the page or surface colour, never on a sunken or tint background (F-148 BR8): the aside's background is checked for it.
- BR7 Save never publishes and Publish never saves; unsaved changes block Publish (F-95 BR12, confirmed).
- BR8 Save stays the only navy primary button of the page, at the bottom right of the form card; an annulled or in-review question has no Save (F-101 BR5, F-105 BR3, confirmed). In transcription mode (F-99) there is no publication block and no amber action; "Save and continue" is the primary, as today.
- BR9 Below 1280px the aside, with the publication block, stacks under the form card and before the history cards, in that DOM order (so keyboard order is form, Save, aside, histories).
- BR10 The preview and status-reason dialogs keep their composition; they carry no amber action. Their content with keycap letters is F-149.
- BR11 Only `QuestionForm.razor` among the routed pages may use `AppActionButton` or `Color.Tertiary` on a button until another screen item (F-127 to F-134) picks its own; the scan names the allowed page.

## Screens and API
- No route, endpoint, permission or error code changes. Routes `/admin/questions/new`, `/admin/questions/{id}` and `/admin/questions/transcribe` stay.
- Kit (all shown in the dev gallery `/dev/ui`): `AppSectionCard` + `Number`; `AppFormAside` + content slot; `AppActionButton` + `Icon`, `Id`, `Busy`, `DescribedBy`.
- Files: `QuestionForm.razor`, `MatchingKeySection.razor` and `FillInTheBlanksKeySection.razor` (the number only), `Components/Ui/AppSectionCard.razor`, `AppFormAside.razor`, `AppActionButton.razor`, `Components/Pages/Dev/UiGallery.razor`, `wwwroot/app.css`, the resource files of the shared texts if the gallery needs new keys, `docs/agile/ui-map.md`.
- Left to `/agile:screen F-126`: the order and width of the buttons inside the aside; whether the "Status" summary row stays beside the chip; the fallback when the tallest state of the block (draft with a rejection alert and the change-reason field) is taller than a 1280×720 viewport.

### Wireframe: Question form (saved draft, desktop)
```text
+----------------------------+-----------------------+
| Questions > Edit question  |                       |
+----------------------------+-----------------------+
| (1) Classification  [chg]  | SUMMARY       [chg]   |
| (2) Origin                 | Status    Draft       |
| (3) Statement              | Subject   Law         |
| (4) Base text              | Topic     Contracts   |
| (5) Options                | Before publishing     |
| (6) Explanation            |  [x] Topic            |
|                            |  [x] Statement        |
|        Cancel  [ Save ]    |  [ ] Key              |
|                            | PUBLICATION   [new]   |
| Review history  (below)    | (Draft)               |
| Version history (below)    | [Preview]             |
|                            | [Send for review]     |
|                            | [(P) Publish] amber   |
|                            | Why disabled: ...     |
+----------------------------+-----------------------+
```

### Wireframe: Publication block of the aside, by state
```text
 Draft (above)          Published            In review
+-----------------+  +-----------------+  +-----------------+
| (Draft)         |  | (Published)     |  | (In review)     |
| [Preview]       |  | [Preview]       |  | [Preview]       |
| [Send review]   |  | [Unpublish]     |  | [Withdraw]      |
| [(P) Publish]   |  | [Annul]         |  |                 |
+-----------------+  +-----------------+  +-----------------+

 Annulled                 New (not saved)
+-----------------+  +---------------------+
| (Annulled)      |  | Save the question   |
| date + reason   |  | first.              |
| [Preview]       |  |                     |
| [Reinstate]     |  |                     |
+-----------------+  +---------------------+
```

## Docs
- flow: new docs/flows/question-form-composition.md — the frame of the form (numbered sections, the aside with the publication block, the one amber action) with the wireframes above.
- flow: update docs/flows/review-before-publishing.md — the "Publication card" wireframes become the aside's publication block.
- flow: update docs/flows/annul-question.md — the same for the annul and reinstate wireframes.

## Acceptance criteria
- AC1 Given the form for each of the five types (single choice, true/false, multiple answer, matching, fill in the blanks), when it renders, then its six sections show the numbers 1 to 6 in order, the number is `aria-hidden`, and each heading text and section id is the one it had (BR1).
- AC2 Given the gallery, when `/dev/ui` opens, then it shows a numbered section card, an aside with a content slot holding actions, and the action key with another icon, with an id, busy and disabled (BR2, BR3, BR6).
- AC3 Given a saved draft, when the form renders, then the aside holds the status chip, Preview, Send for review and Publish with the ids they had, and no element with the id `question-section-publication` exists outside the aside (BR3, BR4).
- AC4 Given a saved published, in-review or annulled question, when the form renders, then the aside holds the actions of that state (published: Preview, Unpublish, Annul; in review: Preview, Withdraw; annulled: the annulment date and reason, Preview, Reinstate) and nothing amber (BR4, BR5).
- AC5 Given a new unsaved question, when the form renders, then the aside shows "Save the question first" and no publication button (BR4).
- AC6 Given a complete saved draft with no unsaved change, when Publish is clicked, then the question is published as before, the chip and the block change to the published state and no amber action remains (BR5, BR7).
- AC7 Given a saved draft with unsaved changes or an incomplete checklist, when the form renders, then Publish is a real `disabled` button, sunken, with the reason as visible text referenced by `aria-describedby`, and Send for review is disabled for the same reason (BR5, BR6).
- AC8 Given Publish is running, when the call has not returned, then the amber key is disabled and shows progress (BR6).
- AC9 Given the routed pages, when they are scanned, then `QuestionForm.razor` is the only one with `AppActionButton` or a tertiary button, at most one amber action is rendered in any state of the form, and it is the compact variant (BR5, BR11).
- AC10 Given the stylesheet resolved against the theme, when the aside background is read in both themes, then it is the page or the surface colour (BR6).
- AC11 Given a draft with changes, when Save is clicked, then the question is saved and not published, and the form has exactly one navy primary button (BR7, BR8).
- AC12 Given an annulled or in-review question, when the form renders, then there is no Save and the way out is in the aside (BR8).
- AC13 Given transcription mode, when the form renders, then there is no publication block, no amber action, and "Save and continue" is the primary (BR8).
- AC14 Given the rendered page, when the DOM order is read, then the form card comes first, the aside next and the history cards last (BR9).
- AC15 Given the review history and version history cards, when the form renders a saved question, then they are still below and unchanged (BR3).
- AC16 Given the preview and status-reason dialogs, when they open, then their structure is unchanged and they contain no amber action (BR10).
- AC17 All new texts appear in every locale of `UI locales:` and the missing-key test is green.

## Decisions
- 2026-10-23 — Size L judged; cut in two: this item keeps the frame (numbered cards, aside with publication, amber action, dialogs untouched); the option and key rows as keys are F-149 — the form has 2664 lines, 19 test files and five types, and the two halves are read separately — asked
- 2026-10-23 — Keep both F-126 and F-137: F-126 moves the publication actions into the aside, F-137 (idea) later adds only the readiness meter; F-137's summary is updated — similar item, asked
- 2026-10-23 — Save never publishes and Publish never saves; unsaved changes block Publish — confirmed: docs/features/F-95-single-choice-questions.md
- 2026-10-23 — An annulled or in-review question is frozen: no Save, only the way out — confirmed: docs/features/F-101-annul-question.md and docs/features/F-105-review-before-publishing.md
- 2026-10-23 — Save stays the only navy primary button; the amber action is a second button of another role, at most one — confirmed: docs/features/F-148-control-alt-palette-v2.md (BR7) and ui.md
- 2026-10-23 — The amber action is Publish, only on a saved draft; every other action is outlined and in other states there is no amber — asked
- 2026-10-23 — All publication actions and texts move to the aside; the Publication card below disappears; the histories stay below — asked; changed: the aside carries no button (docs/features/F-43-exam-form.md BR4)
- 2026-10-23 — The preview and status-reason dialogs do not change composition in this item — asked
- 2026-10-23 — The amber key is the compact variant in the aside (the aside is narrower than 360px, F-148 BR8) — Claude
- 2026-10-23 — `AppActionButton.Icon` is required, with no default: the right icon depends on the action (ui-project rule on kit parameters); `Id`, `Busy` and `DescribedBy` are added for the form — Claude
- 2026-10-23 — The number of a section is decorative and the heading keeps its text and id, so the 19 existing test files still find their selectors; a screen reader reads the section name as before — Claude
- 2026-10-23 — Every publication id and text is kept (`question-publish`, `question-send-for-review`, `question-unpublish`, `question-withdraw`, `question-reinstate`, `question-annul`, `question-publication-reason`, `question-annul-reason-hint`, the preview button id, the rejection and annulment ids); tests that depended on the card's position are changed, not the ids — Claude
- 2026-10-23 — No new package; no migration; no data changes — Claude
- 2026-10-23 — The mockup of `/agile:screen` shows every state at 1280×720; the tallest state decides the fallback if the sticky aside is taller than the viewport — Claude
- 2026-10-23 — The pt-BR manual (`docs/manual/pt-BR/questions.md`, "Publicar e despublicar", "cartão Publicação, abaixo do formulário") is updated at the ship; pt-PT and en are frozen (F-124) — Claude

## Out of scope
- Option rows as keys, the "Correct" green key, the student preview content: F-149.
- Sticky section index F-135, inline student preview F-136, readiness meter F-137, keyboard shortcuts F-138: ideas that come after this item.
- Assigning a reviewer (F-122) and review of published edits (F-123): ideas on the same form, after this item.
- The exam, exam edition and base text forms: F-127, F-128, F-129. The question list: F-132.
- Short answer and open answer types (F-108, F-109): they take the numbered frame for free when built.

## Open questions
- (none)

## Events

- 2026-10-23 question — asked 8, from context 3
