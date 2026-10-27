---
feature: F-169
epic: Exam Simulator
status: building
board: 235
version: 2
---
# Take an exam edition

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: building (2026-10-27)
- Stopped at: Build started; design passes next.
- Next action: Run system-design and architect, then plan.
- Waiting on: nothing

## Summary
The student's simulator screen: pick a published edition, start an attempt, answer question by question with base texts and images, jump through an answer sheet, see the notice subjects as the sections of the paper in paper order, and submit after a warning about blank questions. No timer here (F-170). It runs on the attempt of F-166.
Added 2026-10-27 (refinement): split in two; this item is part 1 and covers the option types (`SingleChoice`, `TrueFalse`, `MultipleAnswer`). The other five types and the essay with its line limit moved to F-178 (issue 246).

## Start
- Depends on: F-166.
- Waits on (to start): nothing (the mockup of `/agile:screen F-169` was approved with the item, 2026-10-27).
- Needed to validate: the seeded Manaus 2026 edition (exists: 60 single-choice questions, question 47 annulled, base texts and two images); a signed-in `Student` account with a verified email (the owner); the guard sample edition of F-110 for a question of another type (exists).
- Model: opus (a door: a new read of question content and images gated by the owner of an attempt; a public API contract in `ExamEngine`).
- Suggested path: `/agile:screen F-169` → approval → `/agile:build`.
- Parallel with: none.

## Goal
Give the student the first screen where a real paper is taken: the questions of the edition as printed, grouped by the notice subjects, with an answer sheet to move around and a submit that warns about blanks. It is what makes the attempt of F-166 usable by a person, and the screen the timer (F-170) and the result (F-171) build on.

## What already exists
Checked in the code on 2026-10-27, on `main` at `1bc9d69`.
- F-166 stores the attempt: `POST/GET /api/v1/exam-engine/attempts`, `PUT/DELETE .../answers/{questionId}`, `POST .../submit`. `AttemptResponse` carries ids, paper numbers, version numbers, types and the student's answers, never a statement, option text or base text (F-166 BR8). An annulled question is left out of the attempt (F-166 BR2).
- No student-facing read of question content exists. `QuestionVersionResponse` (Question bank contracts) holds the statement, base text and options of a version, plus the key and the explanation, which a student must not get. `IPublishedQuestionDirectory` gives answer shapes only.
- Images: the Web route `/question-images/{id}` (`QuestionImageEndpoints`) proxies the Api and requires `questions.manage` or `questions.review`; its comment says the student route arrives with the simulator. The sanitizer (`QuestionHtmlSanitizer`) admits only that address in a question's HTML.
- A question's origin has an optional `NoticeSubjectId` (F-96, F-75); the Manaus 2026 questions all have one.
- `CatalogExam.razor` (`/catalog/exams/{id}`, F-36) lists the published editions of an exam; it is the student's way to an edition.
- The Web host has no Api client for `ExamEngine`; it references `Simulab.ExamEngine.Contracts` for `WebPermissions` only. `QuestionPreviewContent.razor` renders a question from sanitized HTML for the back office and shows the key; it is a reference, not the screen.
- `AnswerSheet` is already in `docs/glossary.md`.

## Users and use cases
- UC1 A `Student` opens a published edition in the catalog and starts the simulation; when an attempt of theirs is in progress on that edition, the same action resumes it.
- UC2 A `Student` reads one question at a time, with its base text and images, and moves to the previous or the next one.
- UC3 A `Student` answers an option question (`SingleChoice`, `TrueFalse`, `MultipleAnswer`), changes the answer or clears it.
- UC4 A `Student` opens the answer sheet, sees which questions are answered or blank, grouped by notice subject, and jumps to one.
- UC5 A `Student` submits the attempt after a warning that lists the blank questions, and sees the page that confirms it was submitted.

## Business rules
- BR1 The start action shows on each published edition of `/catalog/exams/{id}` only for a user holding `attempts.take`. It calls the start of F-166: a new attempt, or the one in progress on that edition (F-166 BR3), and opens it.
- BR2 The screen shows the version of each question the attempt fixed (F-166 BR2), never the current one: statement, base text, option texts and images as stored, in the question's own content language. It never shows an answer key, an explanation or a rubric (F-166 BR8).
- BR3 Sections are the notice subjects of the questions (epic decision of 2026-10-26). Each section holds its questions in paper-number order; sections are ordered by the lowest paper number of their questions. Questions with no notice subject form a last section, "Other". A section shows its notice subject label.
- BR4 The answer sheet lists every question of the attempt by its paper number, grouped as BR3, each marked answered or blank. A paper number left out of the attempt (an annulled question) is not shown: the numbering jumps (Manaus: 46, 48).
- BR5 One question at a time, with previous and next, which cross sections; the first question opens when the attempt opens, or, on a resumed attempt, the first blank one.
- BR6 An answer is saved when the student marks it, with no confirm button: a single choice or true/false saves on click and clicking the marked one again clears it; a multiple answer saves the set on each tick and clearing the last tick clears the answer. The answer sheet updates after the save succeeds.
- BR7 A save that fails keeps the question as it was before the click and says so; a save refused because the attempt is already submitted (409) opens the submitted page.
- BR8 A question of another type (`Matching`, `FillInTheBlanks`, `ShortAnswer`, `OpenAnswer`, `Essay`) shows its statement and base text and a notice that this kind of answer is not yet in the simulator; it stays blank and counts as blank. F-178 replaces the notice.
- BR9 Submit asks for confirmation: the dialog says how many questions are blank and lists their numbers (or says none is blank) and that a submitted attempt cannot be changed. Confirming calls the submit of F-166 and opens the submitted page.
- BR10 The submitted page shows the edition, the submission date and time in the user's time zone, how many questions were answered and how many left blank, and a link back to the edition. Opening the screen of a submitted attempt opens this page. F-171 replaces it with the result.
- BR11 Only the student who owns the attempt opens its screen, its submitted page, its question content and its images; anyone else, an `Admin` included, gets "not found", as for an id that does not exist. The `Admin` reading of F-166 (`attempts.read`, Api only) is unchanged.
- BR12 An image opens through the attempt only when it is referenced by a question version fixed in that attempt; any other image id answers 404. The image keeps the headers of F-103 (`nosniff`, `sandbox`, `private`).
- BR13 Leaving the screen loses nothing: answers are already saved, and starting again from the edition resumes the attempt (BR1). The simulator's bar has a link "Leave and continue later" to `/catalog/exams/{examId}`. There is no list of attempts (F-173).

## Screens and API
- `/catalog/exams/{id}` [changed] — a start action on each published edition row (BR1).
- `/attempts/{id}` [new] — the simulator: the question, previous and next, the answer sheet and submit (UC2 to UC5).
- `/attempts/{id}/submitted` [new] — the submitted page (BR10).
- Web `/attempts/{id}/images/{imageId}` [new] — streams an image of the attempt from the Api with the signed-in user's token (BR12); the content read gives the question HTML with image addresses pointing here.
- `GET /api/v1/exam-engine/attempts/{id}/content` [new] — the questions of the attempt at their fixed versions, for the owner: paper number, type, content language, statement, base text, options (position and text), notice subject (id, label) and the student's answer, in paper-number order; no key, explanation or rubric (BR2, BR3, BR11).
- `GET /api/v1/exam-engine/attempts/{id}/images/{imageId}` [new] — one image of the attempt, for the owner (BR12).
- Existing and unchanged: start, read, save, clear and submit of F-166.
- Error codes: the existing `exam_attempt.not_found` for both new routes (another student's attempt, an unknown id, an image not in the attempt); no new code.
- Contracts: `Simulab.ExamEngine.Contracts` gains the content response; `Simulab.QuestionBank.Contracts` gains a read of the student-safe content of given question versions (statement, base text, options, notice subject, image ids) and of one image; `Simulab.Catalog.Contracts` is read for the notice subject labels when the question bank does not carry them. The design pass settles the exact shape.
- Web: a typed `ExamEngineApiClient`, as `CatalogApiClient` is.

### Wireframe: simulator
```text
+------------------------------------------------------------+
| Guarda Municipal de Manaus 2026 · Consulplan     [new]     |
| Language and literature (questions 1-15)        [Sheet]    |
+------------------------------------------------------------+
| Question 3 of 59                                           |
| +--------------------------------------------------------+ |
| | Base text (when the question has one)                  | |
| +--------------------------------------------------------+ |
| Statement, with images inline                              |
|  ( ) A  option text                                        |
|  (o) B  option text                                        |
|  ( ) C  option text                                        |
|  ( ) D  option text                                        |
|                                                            |
| [< Previous]                              [Next >]         |
|                                    [Submit exam]           |
+------------------------------------------------------------+
| Answer sheet (side panel; a drawer on a phone)   [new]     |
| Language and literature                                    |
|  [1✓] [2✓] [3 ] [4 ] ...                                   |
| Legislation                                                |
|  ... [46✓] [48 ] ...                                       |
| Other                                                      |
|  12 of 59 answered                                         |
+------------------------------------------------------------+
```

### Wireframe: submit dialog and submitted page
```text
+------------------------------------------+
| Submit the exam?                 [new]   |
| 3 questions are blank: 12, 30, 58.       |
| After submitting you cannot change it.   |
|             [Keep answering] [Submit]    |
+------------------------------------------+

+------------------------------------------+
| Exam submitted                   [new]   |
| Guarda Municipal de Manaus 2026          |
| Submitted on 27/10/2026 at 15:42         |
| Answered: 56 · Blank: 3                  |
| [Back to the edition]                    |
+------------------------------------------+
```

### Screen design
Stitch: projects/1446701988332564029/screens/6b9e606274624a55a5d1201352720d30 (2026-10-27) — reference only, simulator default state: [HTML](mockups/F-169-take-exam-edition.stitch.html), [PNG](mockups/F-169-take-exam-edition.stitch.png); the approved mockup below rules.
Mockup: [F-169-take-exam-edition.html](mockups/F-169-take-exam-edition.html) (every state below, light and dark, the three UI locales, desktop and phone). Colors, fonts and radius are the identity tokens (`docs/design/identity.tokens.json`); one new colour since v2, `primarySelected` (answer options below).

#### Screen 1 — `/catalog/exams/{id}` [changed]
- Layout: unchanged (`MainLayout`, `AppPageHeader`, `AppSectionCard` "Editions" with `AppItemRows`, `AppFormAside`). Each edition row gains, at its end (wrapping under the row's text on a phone), one button: `MudButton` `Variant.Outlined`, `StartIcon="@AppIcons.StartAttempt"` (new icon, Material Outlined `PlayCircle`), text `Catalog.Editions.Take`. Outlined, not filled: the page holds one button per edition and the rule allows one primary per page.
- Permission: rendered only when the user holds `attempts.take` (BR1, AC17); a `Curator` (`catalog.browse` only) sees the rows as today. The button is not merely disabled: it is absent.
- Action: calls the start of F-166 for that edition and navigates to `/attempts/{id}` with the returned id (new or in progress, BR1). One label for both cases: the catalog read does not say whether an attempt is in progress.
- States of the action:
  - starting: the clicked button is disabled, shows a `MudProgressCircular` (small) in place of its icon, wrapped in `aria-hidden="true"` (it renders an unnamed `role="progressbar"`, gallery DOM 2026-10-27), and carries `aria-busy="true"`; the other rows' buttons stay enabled.
  - start failed: an `AppAlert` `Severity.Error` at the top of the Editions card, text from `ErrorText.For(code)` for a known code (`exam_attempt.edition_not_found`, `exam_attempt.edition_without_questions`), else `Catalog.Editions.TakeFailed`; the button returns to its normal state.
- Accessible name: `Catalog.Editions.Take.Name` — the visible text followed by the edition's spoken label (year, position, organizer), as the notice link does (F-36 BR9), so five "Take this exam" buttons are told apart.

#### Screen 2 — `/attempts/{id}`, the simulator [new]
- Layout: a new layout `ExamSessionLayout` (the declared exam-session exception of `ui-project.md`): no app menu, no breadcrumb, no page header. Its bar (`MudAppBar`, the app bar tokens) holds, left to right: the edition label as the page's `h1` (`AppTruncatedText`, one line, full text in its tooltip, in the exam's content language), a spacer, the link `Attempt.Leave` (`AppLink` to `/catalog/exams/{examId}`, BR13; on a phone the icon `AppIcons.Back` with `Attempt.Leave` as its name and tooltip), the answer-sheet button (phone only), `ThemeSwitch`, `LanguageSwitch`. Skip link `Layout.SkipToContent` to the question.
- Body, from `md` (960 px) up: two columns — the question card (`MudPaper` `app-form-card`, max 760 px) and the answer sheet as a side panel (300 px, sticky, scrolls on its own). Below `md`: one column; the answer sheet is a `MudDrawer` `Variant.Temporary`, `Anchor.End`, opened by the bar's answer-sheet button.
- The question card, top to bottom:
  1. Section line (BR3): the notice subject label (content language) and `Attempt.Section.Range` with the lowest and highest paper number of the section ("Questions 31 to 50"; an annulled number inside the range is simply absent, BR4).
  2. Heading `h2` `Attempt.Question.Title` ("Question 48") with `tabindex="-1"`, and beside it the caption `Attempt.Question.Position` ("47 of 59", the position in the attempt, not the paper number, because the numbering jumps).
  3. Alert slot (`AppAlert`): save failed (BR7) or submit failed.
  4. Base text, when the question has one (BR2, AC12): the existing `app-question-base` box above the statement, preceded by the caption `Attempt.Question.BaseText`.
  5. Statement (`app-question-content`, sanitized HTML; images inline from `/attempts/{id}/images/{imageId}`, scaled to the column, never up, F-103 BR15).
  6. Answer area, by type:
     - `SingleChoice`: the options A, B, C… in position order, each a row with a radio indicator, the letter (text) and the option text; hint `Attempt.Question.SingleHint`.
     - `TrueFalse`: two rows, `Questions.TrueFalse.True` and `Questions.TrueFalse.False` (existing keys), with a radio indicator; hint `Attempt.Question.TrueFalseHint`.
     - `MultipleAnswer`: the options with a checkbox indicator; hint `Attempt.Question.MultipleHint`.
     - Any other type (BR8): no answer area; an `AppAlert` `Severity.Info` with `Attempt.Question.NotYetSupported`. The question stays blank.
     The answer area is a new kit component, `AppAnswerOptions` (modes `Single`, `TrueFalse`, `Multiple`), in the gallery; a selected row has the new `primarySelected` fill (`#D1E4FE` light, `#1A4583` dark), a 2 px `primary` border and a filled `primary` indicator, so the state is never colour alone (v2). Contrast: ink `#0B1B2E` on `#D1E4FE` 13.4:1, border `#0F3266` on it 9.7:1; dark: text `#F2F5FB` on `#1A4583` 8.6:1, border `#47A0EB` on it 3.4:1. The build adds `primarySelected` to `SimulabTheme` (as the CSS variable `--app-primary-selected`, the way `link` is written), to `docs/design/identity.tokens.json` and `DESIGN.md` (Primary role: "the fill of a marked answer") and to the approved list of ADR-0007, keeping `IdentityTokensTests` and `ApprovedPaletteTests` green.
  7. Save status: one line, `role="status"`: `Attempt.Save.Saving` with a small `MudProgressCircular` (`aria-hidden="true"`, as above) while a save is in flight, then `Attempt.Save.Saved` or `Attempt.Save.Cleared`; empty before the first click on that question.
  8. Navigation row: `Attempt.Nav.Previous` (`Variant.Outlined`, `StartIcon` `AppIcons.ChevronLeft`) at the left; `Attempt.Nav.Next` (`Variant.Filled`, `Color.Primary`, `EndIcon` `AppIcons.ChevronRight`) at the right — the screen's one primary. Previous is `disabled` on the first question, Next on the last (AC5). They cross sections (BR5).
  9. Submit row: `Attempt.Submit.Action` (`Variant.Outlined`, `StartIcon` `AppIcons.Send`), right-aligned, under the navigation, on every question.
- The answer sheet (new kit component `AppAnswerSheet`), BR4 and AC4:
  - Heading `h2` `Attempt.AnswerSheet.Title`; under it `Attempt.AnswerSheet.Progress` ("12 of 59 answered") and a legend (`Attempt.AnswerSheet.Answered`, `Attempt.AnswerSheet.Blank`, `Attempt.AnswerSheet.Current`; v2).
  - One group per section in BR3 order: an `h3` with the notice subject label (content language) and, at its right, the section's range `Attempt.AnswerSheet.SectionRange` ("31–50": the lowest and highest paper number of the section; an annulled number inside it is still absent, BR4; v2), then a grid of number buttons in paper order (36 × 36 px, mono grid font). Questions with no notice subject: last group `Attempt.Section.Other`.
  - A cell is blank (surface, `linesInputs` border), answered (`primarySelected` fill, 2 px `primary` border, a check icon after the number; v2) or current (2 px ink border and bold, `aria-current="step"`), and answered + current together. Answered is marked by the icon, not by colour alone.
  - A click shows that question (and, on a phone, closes the drawer). The sheet changes only after a save succeeds (BR6).
  - On a phone the drawer has a close icon button (`AppIcons.Close`, tooltip and name `Attempt.AnswerSheet.Close`) and closes on Esc and on the overlay.
- Answering (BR6, BR7), no confirm button:
  - single choice and true/false: a click on an unmarked row marks it and saves; a click on the marked row clears the answer (`DELETE`); marking another row replaces the answer.
  - multiple answer: each tick saves the whole set; unticking the last one clears the answer.
  - The row shows the click at once; while the save is in flight the group carries `aria-busy="true"` and ignores further clicks. On success the save status and the sheet update. On failure the group returns to the answer it had before the click, and the alert slot shows `Attempt.Save.Failed` (or `ErrorText.For(code)` for a 4xx code of F-166); on 409 `exam_attempt.already_submitted` the page navigates to `/attempts/{id}/submitted`.
- Opening (BR5, BR10, BR11): on a new attempt the first question in paper order; on a resumed attempt the first blank one (the first question when none is blank); a submitted attempt navigates to its submitted page; an attempt that is not the user's, or unknown, renders `NotFoundContent` (the app's not-found page, F-134), identical in both cases (AC16).
- Submit (BR9, AC10): the kit `AppConfirmDialog` (not destructive, `Color.Primary`), title `Attempt.Submit.Title`, message `Attempt.Submit.Blanks` / `Attempt.Submit.OneBlank` / `Attempt.Submit.NoBlank` followed by `Attempt.Submit.Final`; the blank numbers in paper order separated by ", "; confirm `Attempt.Submit.Action`, cancel `Common.Cancel`. Confirm: the Submit button turns disabled with progress (`Attempt.Submit.Submitting` as its accessible name), Previous, Next and the options are inert; success navigates to the submitted page; failure shows `Attempt.Submit.Failed` in the alert slot with the action `Common.TryAgain`, which reopens the dialog.
- Document title: `Attempt.PageTitle` ("Question 48 · {edition}"), updated on every move.

#### Screen 3 — `/attempts/{id}/submitted` [new]
- Layout: `MainLayout` (the session is over; menu and switches return), no breadcrumb, as `NotFound.razor`. The body is the kit `AppPageState`: icon `AppIcons.Verified` (`Color.Success`), title `h1` `Attempt.Submitted.Title`, message `Attempt.Submitted.Message` (edition label, date `d` and time `t` in the user's culture and time zone, BR10), child content a two-row summary (`Attempt.Submitted.Answered`, `Attempt.Submitted.Blank`, numbers right after their label, in the user's culture), action `Attempt.Submitted.BackToEdition`: `MudButton` `Variant.Filled` `Color.Primary` with `Href` to `/catalog/exams/{examId}` (a link, not a button) and `StartIcon` `AppIcons.Back`.
- States: loading (`AppLoadingState`), load failed (`AppErrorState` with `Attempt.Submitted.LoadFailed` and `Common.TryAgain`), not found (`NotFoundContent`, BR11), and an attempt still in progress opens `/attempts/{id}`.

#### States
| State | Screen | What shows |
| --- | --- | --- |
| Loading | 2, 3 | Session bar without the edition label; `AppLoadingState` in the question card; the sheet panel empty (2). `AppLoadingState` (3). |
| Ready, new attempt | 2 | Question 1, sheet all blank, Previous disabled. |
| Resumed | 2 | The first blank question; the sheet shows the stored answers. |
| Saving | 2 | The clicked row marked, group `aria-busy`, status "Saving…"; sheet unchanged. |
| Saved / cleared | 2 | Status "Answer saved" / "Answer cleared"; sheet cell answered / blank. No snackbar. |
| Save failed (BR7) | 2 | Row back as before the click; error alert at the top of the card. |
| Already submitted (409) | 2 → 3 | Navigates to the submitted page. |
| Other type (BR8) | 2 | Statement and base text, info alert, no answer area; the cell stays blank. |
| Last question | 2 | Next disabled; Submit always present. |
| Submit dialog | 2 | Count and numbers of blanks, or "No question is blank", and the final sentence. |
| Submitting / submit failed | 2 | Submit disabled with progress; then the error alert with "Try again". |
| Load failed | 2, 3 | `AppErrorState` with "Try again" in the card. |
| Not found / permission denied (BR11) | 2, 3 | `NotFoundContent`; nothing of the attempt (the bar shows no edition). |
| Long text | 1, 2, 3 | A long edition label truncates in the bar with a tooltip and wraps on the submitted page; long section labels, statements, options and a long blank list in the dialog wrap (`overflow-wrap: anywhere`); a wide table in a statement scrolls inside its column. |
| Starting / start failed | 1 | Button disabled with progress; error alert at the top of the Editions card. |
| Empty | — | Not reachable: F-166 refuses to start an edition without questions (`exam_attempt.edition_without_questions`), shown as "start failed". |
| Validation error | — | No form field; a refused save (422 codes of F-166) is the save-failed state with its code's text. |

#### Accessibility
- Landmarks: the bar is the `banner` with the edition as `h1`; the question card is `main` (`#main-content`, the skip link's target); the answer sheet is a `nav` labelled by its heading (it moves between questions).
- Moving to another question (Previous, Next, a sheet cell) moves focus to the question heading, so a screen reader reads "Question 48, heading level 2"; the position caption follows it.
- Single choice and true/false: a `role="radiogroup"` labelled by the question heading and described by the hint; each row `role="radio"` with `aria-checked`, the letter as text ("A") before the option text. One tab stop (the marked row, else the first); arrow keys move focus between rows without marking (marking saves, so selection does not follow focus); Space or Enter marks the focused row, or clears it when already marked. This departs from the APG radio pattern on purpose and is written in the kit component.
- Multiple answer: a `role="group"` labelled by the heading; each row `role="checkbox"` with `aria-checked`, its own tab stop, Space toggles.
- Rows, cells and buttons are at least 36 px high (cells 36 × 36 px), above the 24 px minimum; every one has the pointer cursor and the kit's focus ring (3 px info, 2 px offset).
- The save status is a polite live region ("Saving…", "Answer saved", "Answer cleared"); a failed save and a failed submit are `AppAlert` `Severity.Error` (`role="alert"`). The answer-sheet progress is not a live region (the status already speaks).
- Sheet cells are `button`s named `Attempt.AnswerSheet.CellName` ("Question 48: blank"), the current one with `aria-current="step"`; each section's cells sit in a list under its `h3`.
- Question content carries `lang` = the question's content language; UI text carries the UI locale. Images keep their stored `alt`; an image with an empty `alt` is decorative.
- Previous and Next at the ends are really `disabled`. The edition label in the bar is text, not a link.
- The back action of the submitted page is a link (`a href`), named by its text.
- Checked in the gallery DOM (`/dev/ui`, 2026-10-27): `MudAppBar` renders a `<header>` (the banner); `MudButton` with `Href` renders `<a href>`; `MudProgressCircular` renders an unnamed `role="progressbar"` with `aria-busy`, so inside a button or the save status it is `aria-hidden`; the kit's `MudDialog` renders `role="dialog"` labelled by its title, moves focus inside and closes on Esc, but carries no `aria-modal` (the build adds it, or the screen check accepts MudBlazor's focus trap); `MudTooltip` adds no `aria-describedby`, so every icon button keeps its own `aria-label`. Still unverified (gallery DOM): `MudDrawer` `Variant.Temporary` `Anchor.End` (focus in, Esc, overlay, focus back to the opener) — the gallery has no temporary drawer; the build checks it at its screen check.
- Tab order (desktop): skip link → bar switches → options → Previous → Next → Submit → answer-sheet cells in paper order. Phone: skip link → answer-sheet button → switches → options → Previous → Next → Submit; inside the drawer: close → cells.

#### Resource keys
New keys in `SharedResources` (en / pt-BR / pt-PT). Reused: `Questions.TrueFalse.True`, `Questions.TrueFalse.False`, `Common.Cancel`, `Common.TryAgain`, `Common.GoHome`, `NotFound.Title`, `NotFound.Message`, `Layout.SkipToContent`, the `exam_attempt.*` codes of F-166.

| Key | en | pt-BR | pt-PT |
| --- | --- | --- | --- |
| `Catalog.Editions.Take` | Take this exam | Fazer este simulado | Fazer esta simulação |
| `Catalog.Editions.Take.Name` | Take this exam: {0} | Fazer este simulado: {0} | Fazer esta simulação: {0} |
| `Catalog.Editions.TakeFailed` | The exam could not be started. Try again. | Não foi possível iniciar o simulado. Tente de novo. | Não foi possível iniciar a simulação. Tente novamente. |
| `Attempt.Leave` | Leave and continue later | Sair e continuar depois | Sair e continuar mais tarde |
| `Attempt.PageTitle` | Question {0} · {1} | Questão {0} · {1} | Questão {0} · {1} |
| `Attempt.Section.Range` | Questions {0} to {1} | Questões {0} a {1} | Questões {0} a {1} |
| `Attempt.Section.Other` | Other | Outras | Outras |
| `Attempt.Question.Title` | Question {0} | Questão {0} | Questão {0} |
| `Attempt.Question.Position` | {0} of {1} | {0} de {1} | {0} de {1} |
| `Attempt.Question.BaseText` | Base text | Texto-base | Texto-base |
| `Attempt.Question.SingleHint` | Mark one option. Click it again to clear it. | Marque uma alternativa. Clique nela de novo para limpar. | Marque uma alternativa. Clique nela novamente para a limpar. |
| `Attempt.Question.TrueFalseHint` | Judge the statement. Click your choice again to clear it. | Julgue o item. Clique na sua escolha de novo para limpar. | Julgue a afirmação. Clique novamente na sua escolha para a limpar. |
| `Attempt.Question.MultipleHint` | Mark every option that applies. Untick them all to clear the answer. | Marque todas as alternativas corretas. Desmarque todas para limpar a resposta. | Assinale todas as alternativas corretas. Desmarque-as todas para limpar a resposta. |
| `Attempt.Question.NotYetSupported` | This kind of answer is not in the simulator yet. The question stays blank and counts as blank. | Este tipo de resposta ainda não está no simulador. A questão fica em branco e conta como em branco. | Este tipo de resposta ainda não está no simulador. A questão fica em branco e conta como em branco. |
| `Attempt.Save.Saving` | Saving… | Salvando… | A guardar… |
| `Attempt.Save.Saved` | Answer saved | Resposta salva | Resposta guardada |
| `Attempt.Save.Cleared` | Answer cleared | Resposta apagada | Resposta apagada |
| `Attempt.Save.Failed` | Your answer was not saved. The question is as it was before; mark it again. | Sua resposta não foi salva. A questão voltou a como estava; marque de novo. | A sua resposta não foi guardada. A questão voltou ao que estava; marque novamente. |
| `Attempt.Nav.Previous` | Previous | Anterior | Anterior |
| `Attempt.Nav.Next` | Next | Próxima | Seguinte |
| `Attempt.AnswerSheet.Title` | Answer sheet | Cartão-resposta | Folha de respostas |
| `Attempt.AnswerSheet.Open` | Answer sheet ({0} of {1} answered) | Cartão-resposta ({0} de {1} respondidas) | Folha de respostas ({0} de {1} respondidas) |
| `Attempt.AnswerSheet.Close` | Close the answer sheet | Fechar o cartão-resposta | Fechar a folha de respostas |
| `Attempt.AnswerSheet.Progress` | {0} of {1} answered | {0} de {1} respondidas | {0} de {1} respondidas |
| `Attempt.AnswerSheet.Answered` | Answered | Respondida | Respondida |
| `Attempt.AnswerSheet.Blank` | Blank | Em branco | Em branco |
| `Attempt.AnswerSheet.Current` | Current | Atual | Atual |
| `Attempt.AnswerSheet.SectionRange` | {0}–{1} | {0}–{1} | {0}–{1} |
| `Attempt.AnswerSheet.CellName` | Question {0}: {1} | Questão {0}: {1} | Questão {0}: {1} |
| `Attempt.Submit.Action` | Submit exam | Enviar prova | Submeter prova |
| `Attempt.Submit.Title` | Submit the exam? | Enviar a prova? | Submeter a prova? |
| `Attempt.Submit.Blanks` | {0} questions are blank: {1}. | {0} questões estão em branco: {1}. | {0} questões estão em branco: {1}. |
| `Attempt.Submit.OneBlank` | 1 question is blank: {0}. | 1 questão está em branco: {0}. | 1 questão está em branco: {0}. |
| `Attempt.Submit.NoBlank` | No question is blank. | Nenhuma questão está em branco. | Nenhuma questão está em branco. |
| `Attempt.Submit.Final` | After submitting you cannot change your answers. | Depois de enviar, você não pode mudar suas respostas. | Depois de submeter, não pode alterar as suas respostas. |
| `Attempt.Submit.Submitting` | Submitting… | Enviando… | A submeter… |
| `Attempt.Submit.Failed` | The exam was not submitted. Your answers are kept; try again. | A prova não foi enviada. Suas respostas continuam salvas; tente de novo. | A prova não foi submetida. As suas respostas continuam guardadas; tente novamente. |
| `Attempt.Load.Failed` | The exam could not be loaded. | Não foi possível carregar a prova. | Não foi possível carregar a prova. |
| `Attempt.Submitted.Title` | Exam submitted | Prova enviada | Prova submetida |
| `Attempt.Submitted.Message` | Your answers to {0} were submitted on {1} at {2}. | Suas respostas de {0} foram enviadas em {1} às {2}. | As suas respostas de {0} foram submetidas em {1} às {2}. |
| `Attempt.Submitted.Answered` | Answered | Respondidas | Respondidas |
| `Attempt.Submitted.Blank` | Blank | Em branco | Em branco |
| `Attempt.Submitted.BackToEdition` | Back to the exam | Voltar à prova | Voltar à prova |
| `Attempt.Submitted.LoadFailed` | The submitted exam could not be loaded. | Não foi possível carregar a prova enviada. | Não foi possível carregar a prova submetida. |

## Security
### Surfaces
- S1 `GET /api/v1/exam-engine/attempts/{id}/content` — a signed-in user holding `attempts.take`; only the owner gets the content.
- S2 `GET /api/v1/exam-engine/attempts/{id}/images/{imageId}` and the Web route `/attempts/{id}/images/{imageId}` — the same.
- S3 The pages `/attempts/{id}` and `/attempts/{id}/submitted` — the same.
- S4 The start action on `/catalog/exams/{id}` — shown to holders of `attempts.take`; the catalog itself is read by holders of `catalog.browse` (students and curators).
### Threats
- T1 (S1, I) Another student, or an `Admin` through `attempts.read`, reads the content of an attempt that is not theirs, or tells an existing id apart from an unknown one → AC13 [sec]
- T2 (S1, I) The content leaks the answer key, the true/false key, the explanation or a rubric → AC14 [sec]
- T3 (S2, I) A student opens an image that is not in a version of their own attempt (a draft question's image, another exam's) by guessing its id → AC15 [sec]
- T4 (S2, T) A stored image is served so the browser runs it as a page of the app's origin → AC15 [sec]
- T5 (S3, I) A student opens another student's simulator or submitted page by its address → AC16 [sec]
- T6 (S1, S2, S) An anonymous caller reads content or an image → AC13 [sec], AC15 [sec]
- T7 (S4, E) A user without `attempts.take` (a `Curator`) sees or uses the start action → AC17 [sec]
### Personal data
No new datum. The screen shows the student's own answers (stored by F-166, whose `## Security` holds the row) to that student only (BR11).

## Docs
- flow: new docs/flows/take-exam-edition.md — the student's path: start from the edition, answer, the answer sheet and sections, submit and the submitted page, with the wireframes.
- flow: update docs/flows/exam-attempt.md#steps — the two new owner-only routes (content and image) and their 404 rule.

## Acceptance criteria
- AC1 Given a `Student` on `/catalog/exams/{id}` of an exam with a published edition, when they use the start action of that edition, then an attempt is started and `/attempts/{id}` opens on the first question; when they use it again while that attempt is in progress, then the same attempt opens on its first blank question.
- AC2 Given an attempt started before a question was edited, when the student opens it, then the statement, base text and options shown are those of the version fixed at the start.
- AC3 Given the Manaus 2026 attempt, when the screen opens, then the sections are the notice subjects with their labels, ordered by their lowest paper number, each with its questions in paper-number order; a question with no notice subject is in a last section "Other".
- AC4 Given the Manaus 2026 attempt, when the answer sheet opens, then it lists 59 numbers grouped by section, with 46 followed by 48, each marked answered or blank, the current one marked as current and explained in the legend; each section's heading shows its lowest and highest paper number; a click on a number shows that question.
- AC5 Given a question, when the student uses next and previous, then the next and the previous question in paper order are shown, across sections; previous is unavailable on the first question and next on the last.
- AC6 Given a blank single-choice or true/false question, when the student marks an option, then the answer is saved and the answer sheet shows it answered, and the marked option and the cell use the `primarySelected` fill with a 2 px border; when they mark another, then it replaces the first; when they click the marked one again, then the answer is cleared and the sheet shows it blank.
- AC7 Given a multiple-answer question, when the student ticks two options, then the saved answer holds both; when they untick both, then the answer is cleared.
- AC8 Given a save that fails, when the Api answers an error, then the question shows the answer it had before the click and a message says the answer was not saved; when the Api answers 409 `exam_attempt.already_submitted`, then the submitted page opens.
- AC9 Given a question of another type (the guard sample edition of F-110), when it is shown, then its statement and base text appear with the notice that this answer is not yet in the simulator, and it stays blank.
- AC10 Given an attempt with blank questions, when the student chooses submit, then the dialog shows how many are blank and their numbers; when they cancel, then nothing changes; when they confirm, then the attempt is submitted and the submitted page opens. With no blank question the dialog says none is blank.
- AC11 Given a submitted attempt, when its submitted page opens, then it shows the edition, the submission date and time in the user's time zone, the answered and blank counts and a link to the edition; opening `/attempts/{id}` of that attempt opens this page.
- AC12 Given a question with an image and a base text (Manaus 2026), when it is shown, then the image loads through `/attempts/{id}/images/{imageId}` and the base text appears above the statement.
- AC13 [sec] (T1, T6) Given an attempt of student A, when student B, an `Admin` or an anonymous caller asks for its content, then B and the `Admin` get 404 `exam_attempt.not_found`, identical to an unknown id, and the anonymous caller gets 401.
- AC14 [sec] (T2) Given questions with an answer key, a true/false key and an explanation, when their attempt's content is read, then the response holds none of them.
- AC15 [sec] (T3, T4, T6) Given an image of a question in student A's attempt and an image of a draft question, when student A asks for the second through the attempt, or student B asks for the first through A's attempt, then each answer is 404; the anonymous caller gets 401 (Api) or the sign-in (Web); the image A may open is served with its stored type, `nosniff`, `sandbox` and `private`.
- AC16 [sec] (T5) Given an attempt of student A, when student B or an `Admin` opens `/attempts/{id}` or `/attempts/{id}/submitted`, then the not-found page is shown and nothing of the attempt appears.
- AC17 [sec] (T7) Given a `Curator`, when they open `/catalog/exams/{id}`, then no start action is shown; a `Student` sees it on each published edition.
- AC18 The architecture tests stay green: `ExamEngine` reads the question bank and the catalog only through their `Contracts`, and `/openapi/v1.json` lists the two new routes.
- AC19 Every new text (start action, simulator, leave link, answer sheet, "Other", notice of BR8, submit dialog, submitted page, messages) exists in pt-BR, pt-PT and en, and the missing-key test is green.
- AC20 Given an attempt in progress, when the student uses "Leave and continue later" in the simulator's bar, then `/catalog/exams/{examId}` opens, and starting that edition again opens the same attempt with its answers.

## Criterion → test

Tests locked: d5775699db1a399ae36646e4a8eeb7f212a243c5 (5 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC2 | `AttemptContentEndpointTests.Content_AfterTheQuestionWasEdited_ShowsTheStatementAndOptionsOfTheFixedVersion` |
| AC3 | `AttemptContentEndpointTests.Content_GroupsTheQuestionsInSectionsByNoticeSubject_InPaperOrder_WithTheOthersLast`; `AttemptSectionsTests.Build_OrdersSectionsByTheirLowestPaperNumber_AndPutsTheQuestionsWithNoSubjectLast`; `AttemptSectionsTests.Build_WithNoNoticeSubject_IsOneLastSection_AndWithNoQuestion_IsEmpty` |
| AC4 | `AttemptSectionsTests.Build_GivesEachSectionItsLowestAndHighestPaperNumber_WhenTheNumberingJumps`; screen: pending |
| AC5 | pending |
| AC6 | pending |
| AC7 | pending |
| AC8 | pending |
| AC9 | pending |
| AC10 | pending |
| AC11 | `AttemptContentEndpointTests.Content_OfASubmittedAttempt_IsReadByItsOwner_WithTheTimeAndTheAnswers`; screen: pending |
| AC12 | `AttemptContentEndpointTests.Content_CarriesTheBaseText_AndPointsEveryPictureAtTheAttempt`; `AttemptImageEndpointTests.Image_HeldByAFixedVersion_OpensForTheOwner_WithTheStoredTypeAndTheHardeningHeaders`; Web `AttemptImageEndpointTests.Show_ForAHolderOfTake_StreamsTheApisBytes_WithTheStoredTypeAndTheHardeningHeaders`; screen: pending |
| AC13 | `AttemptContentEndpointTests.Content_OfAnotherStudentsAttempt_AnswersLikeAnUnknownId_EvenForAnAdmin_AndAnonymousGets401` |
| AC14 | `AttemptContentEndpointTests.Content_NeverCarriesAnAnswerKeyATrueFalseKeyAnExplanationOrARubric` |
| AC15 | `AttemptImageEndpointTests.Image_NotHeldByAFixedVersionOfTheAttempt_Answers404`; `AttemptImageEndpointTests.Image_ThroughAnotherStudentsAttempt_Answers404_EvenForAnAdmin_AndAnonymousGets401`; `AttemptImageEndpointTests.Image_HeldByAFixedVersion_OpensForTheOwner_WithTheStoredTypeAndTheHardeningHeaders`; Web `AttemptImageEndpointTests.Show_TheApiAnswers404_TheWebAnswers404`, `Show_NotSignedIn_GoesToSignIn_AndWithoutTake_IsRefused_AndTheApiIsNotAsked` |
| AC16 | pending |
| AC17 | pending |
| AC18 | `ExamEngineContractTests.TheOpenApiDocument_Answers200_AndListsTheSevenAttemptRoutes`; architecture tests: pending |
| AC19 | pending |
| AC20 | pending |

## Decisions
- 2026-10-27 — Size L judged; split in two: this item keeps the option types, the content read, images, answer sheet, sections and submit; F-178 (issue 246) takes `Matching`, `FillInTheBlanks`, `ShortAnswer`, `OpenAnswer` and the essay — asked ("Split as proposed")
- 2026-10-27 — No similar item (213 items read) — Claude (technical)
- 2026-10-27 — The sections are the notice subjects in paper order, no `Section` entity, as decided in the epic Exam Simulator. Still true here? Yes — confirmed: docs/epics/exam-simulator.md
- 2026-10-27 — The student starts and resumes from a start action on each published edition of `/catalog/exams/{id}`; no new page to pick an edition — asked
- 2026-10-27 — One question at a time with previous and next, and the answer sheet beside it (a drawer on a phone) — asked
- 2026-10-27 — An answer is saved when it is marked; clicking the marked option again clears it; no confirm button — asked
- 2026-10-27 — An annulled question is not shown and the numbering jumps; F-166 BR2 is unchanged; "annulled counts for everyone" stays with F-174 — asked
- 2026-10-27 — A question of a type that arrives with F-178 shows its statement with a notice and stays blank; the start is not hidden — asked
- 2026-10-27 — Questions with no notice subject go to a last section "Other" — asked
- 2026-10-27 — After submitting, a "submitted" page with date, time and counts and a link to the edition; F-171 replaces it with the result — asked
- 2026-10-27 — An image opens only when it is in a question version of the student's own attempt — asked
- 2026-10-27 — Only the owner opens the simulator; the `Admin` keeps the Api read of F-166 and gets "not found" on the screen and on the content and image routes — asked
- 2026-10-27 — The screen gets a mockup (`/agile:screen F-169`) before approval — asked
- 2026-10-27 — No new package: MudBlazor, the typed Http clients and bUnit 2.10.3 are in `Directory.Packages.props` — asked
- 2026-10-27 — The content and the images are read through `ExamEngine` routes scoped to the attempt, not through the question bank's back office routes: the attempt decides who may read and which version; the image address in the HTML is rewritten from `/question-images/{id}` to `/attempts/{attemptId}/images/{id}` for the student; the design pass settles where the rewrite happens — Claude (technical)
- 2026-10-27 — Sections are ordered by the lowest paper number of their questions, not by the notice subject's display order: the screen follows the paper, and the two agree on Manaus — Claude (technical)
- 2026-10-27 — A resumed attempt opens on its first blank question — Claude (technical)
- 2026-10-27 — No new error code: both new routes answer `exam_attempt.not_found` as F-166 BR7 does — Claude (technical)
- 2026-10-27 — Screen design (`/agile:screen F-169`): mockup `docs/features/mockups/F-169-take-exam-edition.html` — asked
- 2026-10-27 — A saved answer is told by an inline polite status line, no snackbar per save; the build adds that line to the exam-session exception of `ui-project.md` — asked
- 2026-10-27 — The simulator's bar has a "Leave and continue later" link back to the exam page (BR13, AC20) — asked
- 2026-10-27 — A multiple-answer question does not show how many options to mark: the count is part of the key (BR2) — asked
- 2026-10-27 — The submit dialog uses the kit's `AppConfirmDialog`, so its cancel reads `Common.Cancel`, not the wireframe's "Keep answering" — Claude (technical)
- 2026-10-27 — New kit components `ExamSessionLayout`, `AppAnswerOptions`, `AppAnswerSheet` and the save status, each in the gallery `/dev/ui`; new icons `AppIcons.StartAttempt` (PlayCircle) and `AppIcons.AnswerSheet` (GridView); theme and language switches stay in the session bar; a resumed attempt shows no extra notice — Claude (technical), shown to the owner
- 2026-10-27 — Stitch offered — declined
- 2026-10-27 — Approved by the owner ("aprovo F-169"), with the mockup
- 2026-10-27 — Stitch offered again at the owner's request (a reference beside the approved mockup) — failed (generation of the simulator screen timed out; no new screen in project `Simulab` after 10 polls in 5 minutes; not retried)
- 2026-10-27 — Stitch offered — reference (projects/1446701988332564029/screens/6b9e606274624a55a5d1201352720d30, generated by the owner by hand from the same prompt; downloaded beside the mockup; the approved mockup rules)
- 2026-10-27 — Colour of a marked answer — owner chose "the Stitch blue #D1E4FE (a new token)" over the recommended "2 px border with the existing primaryLighten" (average 5.4 vs 8.0); kept
- 2026-10-27 — The `primarySelected` token goes into `identity.tokens.json`, `DESIGN.md` and ADR-0007 at the build, with the theme: `IdentityTokensTests` fails a token the theme does not set — Claude (technical)
- 2026-10-27 — Design passes (system-design, architect; opus). Accepted: the content read `GET .../attempts/{id}/content` (`AttemptContentResponse` with sections, questions, edition), the image read `GET .../attempts/{id}/images/{imageId}`, both owner only behind `TakePolicy` and never `IAttemptAccess.CanReadAnyAsync`; QuestionBank gains `ListVersionContentAsync` and `OpenVersionImageAsync` on `IPublishedQuestionDirectory` (projection only, no key, explanation or `IsCorrect`); sections built in ExamEngine Application (`AttemptSections`), unit-tested; the `src="/question-images/` rewrite in the ExamEngine content handler; the content read is allowed on a submitted attempt and feeds the submitted page; `Edition` nullable (no label, Leave hidden, back to `/catalog`); a fixed version that no longer resolves fails the read with a logged error; the notice subject is read from `Question` today (a reassignment after the start moves the question to another section; accepted, BR3). From the architect: every step adds its own route to `ExamEngineContractTests` (finding 1); the kit pieces are built with the simulator, not as a step alone (2); the submitted page comes before the simulator (3); ExamEngine gets its own port `IAttemptSource.OpenImageAsync` returning an ExamEngine record, and an `OpenAttemptImageHandler` (4, `ModuleBoundaryTests`); `OpenVersionImageAsync` checks membership then reuses `OpenQuestionImageHandler` to open the file (5); the bar label's language is the exam's `ContentLanguage` from `IPublishedExamQueries.FindAsync`, falling back to the first question's (6, Screen 2 says "the exam's content language"). Dropped: the first question's language as the only source (open point 2), replaced by finding 6 — Claude (technical)
- 2026-10-27 — Design coverage: contracts (route list, `IPublishedQuestionDirectory` and `IAttemptSource` implementers and callers, nullable ids), data (no table, column, index or migration; pair reads on `ix_question_versions_question`), rules (`ModuleBoundaryTests`, `ExamEngineContractTests`), layout (`build-plan.md` slices), scope, simplicity — all read; not checked: none
- 2026-10-27 — traps — T1 reuse `QuestionVersionResponse`/`QuestionOptionResponse` in the student content: leaks the key (AC14); T2 copy `GetAttemptHandler`'s `CanReadAnyAsync`: an `Admin` reads content, images and the submitted page (AC13, AC16); T3 serve images through `OpenQuestionImageHandler` alone or by `OwnerId`: serves draft images and refuses base-text images (AC15)
- 2026-10-27 — design trade-off 7 How the image route proves an image belongs to the attempt — owner chose the alternative (the architect's pick): one `SELECT EXISTS ... LIKE` over the attempt's (question, version) pairs; the F-103 LIKE shortcut gets a second user, written on its marker
- 2026-10-27: plan — 1 content read, QuestionBank projection and owner-only ExamEngine route → AC2, AC13, AC14; 2 sections, edition and labels in the same read → AC3; 3 images through the attempt, Api route and Web proxy → AC12 (Api), AC15; 4 Web `ExamEngineApiClient` and the `primarySelected` token (theme, tokens, DESIGN.md, ADR-0007) → build, `IdentityTokensTests`, `ApprovedPaletteTests`; 5 the three screens by the `frontend` agent (start action, submitted page, simulator with `ExamSessionLayout`, `AppAnswerOptions`, `AppAnswerSheet`, save status and gallery; removes nothing; one fix round budgeted) → AC1, AC4–AC12, AC16, AC17, AC19, AC20; 6 OpenAPI document and the two flow pages → AC18, `DocGen --check`, `techdocs.js check`

## Out of scope
- `Matching`, `FillInTheBlanks`, `ShortAnswer`, `OpenAnswer` and the essay with its line counter (F-178).
- Timer, warning near the end and automatic submit (F-170).
- Score, cut-off and the result screen (F-171); answer review with the key (F-172); the list of attempts (F-173).
- Showing annulled questions in the paper (F-174 decides how they count).
- An `Admin` view of a student's simulator.
- Pause, abandon, anti-fraud and booklet types (epic `## Out of scope`).

## Open questions
- (none)

## Change notes
### v2 — 2026-10-27
- What: the answer sheet's legend gains "Current"; each section heading of the answer sheet shows its range of paper numbers; a marked option and an answered cell use a new colour `primarySelected` (`#D1E4FE` light, `#1A4583` dark) with a 2 px `primary` border. New keys `Attempt.AnswerSheet.Current` and `Attempt.AnswerSheet.SectionRange`; the mockup follows.
- Why: the owner compared the approved mockup with the Stitch reference and took these three from it.
- Affected: AC4, AC6, `### Screen design` (answer options, answer sheet, resource keys), the mockup. Not affected: the business rules, the other criteria, the API, security and data.
- Re-approved by the owner: 2026-10-27 ("aprovo F-169").

## Validation script
Written at the end of the build.

## Events

- 2026-10-27 question — asked 12, from context 1
- 2026-10-27 change — v2: answer sheet legend Current, section ranges, primarySelected colour
- 2026-10-27 override — Colour of a marked answer — chose "Stitch blue #D1E4FE" over "2 px border with primaryLighten"

## Delivery
