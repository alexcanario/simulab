---
feature: F-74
epic: Subject taxonomy
status: done
board: 118
version: 1
---
# Notice subjects per edition

Technical terms: [glossary](../glossary.md)

## Summary
On an exam edition, an admin records the `NoticeSubject` rows exactly as the notice states them: the label, the grouping and the number of questions (ADR-0001 #44). Weight and minimum score per notice subject are not part of it: they move to epic Exam Simulator (E-6) with the other scoring rules (epic decision, 2026-10-04).

## Start
- Depends on: nothing (F-79 removed, owner, 2026-10-04: F-74 does not reference `Subject` or `Topic`).
- Waits on (to start): nothing. Not built at the same time as F-79: both change the `Catalog` migration snapshot.
- Needed to validate: an exam with an edition in the local database — the F-37 municipal guard seed provides it (Claude).
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: F-51, F-77 (refinement); not F-79 at build.

## Goal
Give each edition its syllabus in the notice's own words, so the Exam Simulator (E-6) can show the real paper and F-75 can map each row to the canonical taxonomy.

## What exists (verified 2026-10-04)
- `ExamEdition` (F-35) in the `Catalog` module, edited on its own page `/admin/exams/{examId}/editions/{id}` (`ExamEditionForm.razor`) with `AppSectionCard` sections; soft delete; `Draft`/`Published`; a published edition stays editable.
- Endpoints under `/api/v1/catalog/exams/{examId}/editions` (`ExamEndpoints.cs`), guarded by `catalog.manage`.
- No `NoticeSubject` in `src/`. Simulae has no notice subject (only `ExamNoticeTopic`, notice → topic): nothing to import.
- `docs/glossary.md` row `NoticeSubject` still mentions weight and minimum: corrected by this item.

## Users and use cases
- UC1 An admin with `catalog.manage` opens an edition and sees its notice subjects, grouped and in the notice's order, with the sum of the stated question counts.
- UC2 The admin adds a notice subject: group (optional), label, number of questions (optional).
- UC3 The admin edits a notice subject's group, label or number of questions.
- UC4 The admin moves a notice subject up or down inside its group.
- UC5 The admin deletes a notice subject.
- UC6 The admin deletes an edition; its notice subjects leave with it.

## Business rules
- BR1 A notice subject belongs to one edition, fixed at creation. Global data: `TenantId` is null (ADR-0001 #8).
- BR2 Label: required, trimmed, 2 to 200 characters.
- BR3 Group: optional free text, trimmed, at most 100 characters; blank is stored as null ("no group"). The dialog offers the groups already used in the edition.
- BR4 Number of questions: optional; when given, an integer from 1 to 500. Blank means "the notice does not say".
- BR5 Within one edition, the label is unique inside its group, ignoring case and accents (same normalization as the edition's position); "no group" is one group. Deleted rows do not count.
- BR6 Order: every notice subject has a position inside its edition. A new one goes last in its group (last in the edition when its group is new). Moving up or down swaps with the neighbour in the same group; the first row of a group cannot move up and the last cannot move down. Groups are shown in the order of their first row.
- BR7 Changing the group of a notice subject moves it to the end of the new group.
- BR8 The sum shown is the sum of the stated numbers; rows without a number are left out of it and the screen says how many have no number. It is not stored and not checked against anything.
- BR9 Notice subjects can be added, edited, moved and deleted on a Draft or a Published edition, like the edition's other fields.
- BR10 Deleting a notice subject is a soft delete. Deleting an edition soft-deletes its notice subjects in the same save; the edition's delete confirmation says so.
- BR11 Every endpoint requires `catalog.manage`; the section and its actions are shown only to who has it.
- BR12 When adding, the group field starts with the group of the last notice subject added during the same page visit; empty on the first add. A convenience of the screen, not stored.

## Screens and API
- `/admin/exams/{examId}/editions/{id}` — new section card "Notice subjects" below the edition's fields (only for a saved edition): rows grouped under their group heading, each row with label, number of questions (or "—"), move up, move down, edit, delete; an "Add notice subject" button; footer with the sum (BR8). Add and edit in a dialog (rule `ui-project`: simple entity). Detailed in `## Screen` by `/agile:screen`.
- GET `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects` — the list in display order.
- POST `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects` — add (UC2).
- PUT `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/{id}` — edit (UC3).
- POST `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/{id}/move` — body `{ "direction": "up" | "down" }` (UC4).
- DELETE `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/{id}` — soft delete (UC5).
- Error codes: `exam_edition.not_found` (existing; also answers a missing exam); `notice_subject.not_found`, `notice_subject.label_required`, `notice_subject.label_too_short`, `notice_subject.label_too_long`, `notice_subject.group_too_long`, `notice_subject.question_count_invalid`, `notice_subject.duplicate`, `notice_subject.move_invalid`.
- Table `catalog.notice_subjects`: `id`, `tenant_id`, `exam_edition_id`, `group_label`, `label`, `normalized_group`, `normalized_label`, `question_count`, `display_order`, audit and soft-delete columns, column comments; unique index `ux_notice_subjects_tenant_edition_group_label` over (`tenant_id`, `exam_edition_id`, `normalized_group`, `normalized_label`) `NULLS NOT DISTINCT`, filtered to rows not deleted.

## Screen
Mockup: [`mockups/F-74-notice-subjects-per-edition.html`](mockups/F-74-notice-subjects-per-edition.html) (states, languages, light and dark).

### Screen 1 — "Notice subjects" section on the edition page
- Place: `/admin/exams/{examId}/editions/{id}` (`ExamEditionForm.razor`). A new component `NoticeSubjectsSection.razor` (same shape as `ExamEditionsSection.razor`, F-35) in its own `MudPaper Class="app-form-card"`, full width, **below** `app-form-layout` and outside the edition form: the edition's Save stays the page's only primary button and never saves a notice subject; each notice subject is saved by its own dialog or row action.
- Card: `AppSectionCard Id="edition-section-notice-subjects"`, `Title` `NoticeSubjects.Section.Title`, `Icon` `AppIcons.NoticeSubjects` (new constant, Material Outlined `ListAlt`), `Subtitle` `NoticeSubjects.Section.Subtitle`.
- Body, top to bottom:
  1. `AppAlert` (error) for a refused move or delete, or a server error on them (`Errors.For(code)`).
  2. The groups, in the order the Api returns the rows (BR6). Each group is a heading (`h3`, `MudText Typo="Typo.subtitle2"`, the group label; "no group" reads `NoticeSubjects.Group.None`) followed by one `AppItemRows` with the group's rows; the list is labelled by its heading (`aria-labelledby`). When **every** row has no group, no heading is drawn: one `AppItemRows`, no "No group" title.
  3. Each row (`RowTemplate`): the label (truncated with tooltip by `AppTruncatedText` when long), then, right-aligned, the number of questions in the user's culture (`NoticeSubjects.Row.Questions.One` / `.Many`) or "—" with the visually hidden text `NoticeSubjects.Row.NoCount.Spoken`.
  4. Row actions (`Actions`): move up, move down, edit, delete, each an icon button with tooltip and accessible name `Common.ActionOnItem` ("Move up: Língua Portuguesa, Conhecimentos Básicos"). Move up is disabled on the first row of its group (tooltip `NoticeSubjects.MoveUp.DisabledFirst`), move down on the last (`NoticeSubjects.MoveDown.DisabledLast`); a group of one row has both disabled. Icons `AppIcons.MoveUp` (`ArrowUpward`) and `AppIcons.MoveDown` (`ArrowDownward`), new constants. Four actions fit by extending `AppRowActions` (option A of the mockup): an optional move-up/move-down pair placed before Edit and Delete, not counted in its three visible actions, each with its disabled reason, and a `/dev/ui` gallery entry. Option B of the mockup is not built.
  5. Footer line, below the lists: on the left the outlined button `NoticeSubjects.Form.AddTitle` (`AppIcons.Add`), which opens the dialog (a button, not a link: it does not navigate); on the right the sum (BR8): `NoticeSubjects.Total.One` / `.Many` with the sum of the stated numbers, followed, when some rows have no number, by `NoticeSubjects.Total.Unstated.One` / `.Many` with how many. With no stated number at all, the sum is replaced by `NoticeSubjects.Total.None`. The footer is not shown on an empty list. The sum is computed on the screen from the loaded list; nothing is stored.
- Permissions (BR11): the page already requires `catalog.manage` (`[Authorize(Policy = ... CatalogPermissions.Manage)]`); the section is rendered only on that page, so who sees it can use every action. No action is hidden per row.
- Actions and their effects:
  - Add → dialog (Screen 2) in add mode. On success: snackbar `NoticeSubjects.Saved`, list reloaded, focus on the new row's Edit button.
  - Edit → dialog in edit mode with the row's values. On success: snackbar `NoticeSubjects.Saved`, list reloaded, focus on that row's Edit button (the row may have moved group, BR7).
  - Move up / down → `POST .../move` at once, no confirmation, no snackbar. The list is reloaded; focus stays on the same button of the moved row, or on the other move button when the pressed one became disabled. A polite live region (visually hidden) says `NoticeSubjects.Moved` ("Língua Portuguesa moved to position 1 of 4 in Conhecimentos Básicos") or `NoticeSubjects.Moved.NoGroup`. Both buttons of the row are disabled while the call runs. A refusal (`notice_subject.move_invalid`, `notice_subject.not_found`, server error) shows the alert at the top of the section and reloads the list.
  - Delete → `IConfirmService.ConfirmDeleteAsync`: title `Common.Delete.Title`, message `NoticeSubjects.Delete.Message`, confirm button (error color) `Common.Delete.Confirm` with `NoticeSubjects.Delete.Object`. On success: snackbar `NoticeSubjects.Deleted`, list reloaded, focus on the next row's Edit button, else the previous row's, else the Add button. A refusal shows the section alert and reloads.

### Screen 2 — Add / edit dialog (`NoticeSubjectDialog.razor`)
- `MudDialog`, like `OrganizerDialog.razor`: title `NoticeSubjects.Form.AddTitle` or `NoticeSubjects.Form.EditTitle`; `AppAlert` at the top for business and server errors; `AppFormActions` at the bottom (Save filled, Cancel text).
- Fields, in this order (label above, required marked):

| Id | Label key | Type | Required | Limits | Hint / placeholder keys |
|---|---|---|---|---|---|
| `notice-subject-group` | `NoticeSubjects.Field.Group` | free text that suggests the groups already used in the edition (combobox, contains-match ignoring case and accents) | no | 100 characters (`MaxLength`) | `NoticeSubjects.Field.Group.Hint`, `.Group.Placeholder`; listbox label `.Group.Suggestions` |
| `notice-subject-label` | `NoticeSubjects.Field.Label` | text | yes | 2 to 200 characters | `NoticeSubjects.Field.Label.Hint`, `.Label.Placeholder` |
| `notice-subject-question-count` | `NoticeSubjects.Field.QuestionCount` | number (`int?`, `InputType.Number`, `inputmode="numeric"`) | no | 1 to 500 | `NoticeSubjects.Field.QuestionCount.Hint`, `.QuestionCount.Placeholder` |

- The group suggestions come from the loaded list (distinct non-null groups, in display order); no extra endpoint. Typing a new text is allowed.
- The group field is a new kit component (`AppSuggestField` or similar, built on `MudAutocomplete`): free text, suggestions matched ignoring case and accents, with a `/dev/ui` gallery entry.
- Add mode: the group comes pre-filled with the group of the last notice subject added on this page visit (BR12); the admin can clear it.
- Edit mode, group changed from the saved one: the group's hint is replaced by `NoticeSubjects.Field.Group.MoveNote` (BR7).
- Checks on leaving the field and on Save, for comfort only (the Api is the authority): label blank → `notice_subject.label_required`, 1 character → `.label_too_short`, over 200 → `.label_too_long`; group over 100 → `.group_too_long`; number not an integer from 1 to 500 → `.question_count_invalid`. Each error sits under its field and replaces its hint.
- Api refusal: a code of one field goes under that field (the five above); `notice_subject.duplicate` (409), `exam_edition.not_found`, `notice_subject.not_found` and anything else go to the alert at the top of the dialog (rule `ui`: business and server errors). The dialog stays open with what was typed.
- Saving: Save disabled with progress (`AppFormActions Saving`), fields keep their values.
- Cancel, Esc or closing with changes → `ConfirmDiscardAsync` (`Common.Discard.*`); without changes the dialog closes at once.

### Screen 3 — Edition delete confirmation on the exam page (BR10)
- `ExamEditionsSection.razor` keeps its flow; only the message changes: `ExamEditions.Delete.Message` now says the notice subjects leave with the edition (values below). Same keys for title and confirm button.

### States
| State | What shows |
|---|---|
| Unsaved edition (`/editions/new`) | Section card with `NoticeSubjects.Section.SaveEditionFirst`; no list, no Add, no footer (AC17). After the first save the page becomes the edit route and the section loads (empty). |
| Loading | `AppLoadingState` inside the card. |
| Load error | `AppErrorState` with "try again" (`OnRetry` reloads). |
| Empty | `AppItemRows` empty message `NoticeSubjects.Empty`, then the Add button; no sum. |
| Ready, grouped | Group headings in the order of their first row; rows in order; sum footer. |
| Ready, no group at all | One list, no heading. |
| Ready, mixed | Groups and a "No group" heading where its first row falls. |
| Row with no number | "—" in the number column; counted in `Total.Unstated`. |
| First / last row | Move up / move down disabled with their tooltip. |
| Moving | The row's move buttons disabled; then announcement and focus as above. |
| Move or delete refused / server error | Section alert, list reloaded. |
| Delete confirmation | Kit confirmation dialog, error-colored confirm button naming the subject. |
| Deleted | Snackbar `NoticeSubjects.Deleted`; row gone. |
| Dialog: add, edit, group suggestions open, group changed (BR7 note), saving, validation errors, duplicate, server error, unsaved changes | As in Screen 2. |
| Saved | Snackbar `NoticeSubjects.Saved`. |
| Permission denied | The page is not reachable without `catalog.manage` (existing guard, "page not found"); the section never renders for that user. |
| Edition not found | Existing page state (`exam_edition.not_found`); the section is not rendered. |

### Accessibility
- Landmarks and headings: the card heading is `h2` (`AppSectionCard`), each group heading `h3`; every group's `ul` is labelled by its heading, so a screen reader announces "Conhecimentos Básicos, list, 4 items".
- Tab order: the edition form and its Save, the aside, then the section: alert (when present) → per row: move up, move down, edit, delete → Add button. Disabled move buttons are skipped (kit behavior) and keep their tooltip on hover.
- Every icon button has a tooltip and an accessible name from resources, with the row's name (label and group).
- The "—" is `aria-hidden`; its visually hidden text says the number is not stated. Numbers are right-aligned and formatted in the user's culture.
- The sum footer is plain text (not live); the move result is announced by a polite live region; add, edit and delete are announced by the snackbar.
- Dialog: focus goes to the first field on open and back to the button that opened it on close; Esc closes (with discard confirmation when changed); errors are linked with `aria-describedby` and `aria-invalid`. The group field is a `combobox` with `aria-autocomplete="list"`: Arrow down enters the suggestions, Enter picks, Esc closes the list without closing the dialog.
- Real elements: Add, the row actions and the dialog actions are buttons; no link on this screen navigates.
- Targets: icon buttons 32 px; buttons 40 px high.
- Colors: only existing `SimulabTheme` tokens (no identity tokens file yet); no new color.

### UI texts
New keys unless marked "changed". `—` is a symbol, not a resource.

| Key | pt-BR | pt-PT | en |
|---|---|---|---|
| `Common.MoveUp` | Mover para cima | Mover para cima | Move up |
| `Common.MoveDown` | Mover para baixo | Mover para baixo | Move down |
| `NoticeSubjects.Section.Title` | Disciplinas do edital | Disciplinas do aviso | Notice subjects |
| `NoticeSubjects.Section.Subtitle` | O conteúdo como o edital o apresenta: cada disciplina sob o seu grupo, na ordem do edital, com o número de questões. | O conteúdo tal como o aviso o apresenta: cada disciplina sob o seu grupo, pela ordem do aviso, com o número de questões. | The syllabus as the notice states it: each subject under its group, in the notice's order, with its number of questions. |
| `NoticeSubjects.Section.SaveEditionFirst` | Salve a edição primeiro. As disciplinas do edital são adicionadas aqui. | Guarde primeiro a edição. As disciplinas do aviso são adicionadas aqui. | Save the edition first. Its notice subjects are added here. |
| `NoticeSubjects.Empty` | Esta edição ainda não tem disciplinas do edital. | Esta edição ainda não tem disciplinas do aviso. | This edition has no notice subject yet. |
| `NoticeSubjects.Group.None` | Sem grupo | Sem grupo | No group |
| `NoticeSubjects.Row.Questions.One` | 1 questão | 1 questão | 1 question |
| `NoticeSubjects.Row.Questions.Many` | {0} questões | {0} questões | {0} questions |
| `NoticeSubjects.Row.NoCount.Spoken` | número de questões não informado | número de questões não indicado | number of questions not stated |
| `NoticeSubjects.MoveUp.DisabledFirst` | Já é a primeira do grupo | Já é a primeira do grupo | Already first in its group |
| `NoticeSubjects.MoveDown.DisabledLast` | Já é a última do grupo | Já é a última do grupo | Already last in its group |
| `NoticeSubjects.Moved` | {0} foi para a posição {1} de {2} em {3}. | {0} passou para a posição {1} de {2} em {3}. | {0} moved to position {1} of {2} in {3}. |
| `NoticeSubjects.Moved.NoGroup` | {0} foi para a posição {1} de {2}. | {0} passou para a posição {1} de {2}. | {0} moved to position {1} of {2}. |
| `NoticeSubjects.Total.One` | Total informado no edital: 1 questão | Total indicado no aviso: 1 questão | Stated in the notice: 1 question in total |
| `NoticeSubjects.Total.Many` | Total informado no edital: {0} questões | Total indicado no aviso: {0} questões | Stated in the notice: {0} questions in total |
| `NoticeSubjects.Total.None` | Nenhuma disciplina informa o número de questões. | Nenhuma disciplina indica o número de questões. | No subject states its number of questions. |
| `NoticeSubjects.Total.Unstated.One` | 1 disciplina sem número fica fora da soma. | 1 disciplina sem número fica fora da soma. | 1 subject has no number and is left out of the sum. |
| `NoticeSubjects.Total.Unstated.Many` | {0} disciplinas sem número ficam fora da soma. | {0} disciplinas sem número ficam fora da soma. | {0} subjects have no number and are left out of the sum. |
| `NoticeSubjects.Form.AddTitle` | Adicionar disciplina do edital | Adicionar disciplina do aviso | Add notice subject |
| `NoticeSubjects.Form.EditTitle` | Editar disciplina do edital | Editar disciplina do aviso | Edit notice subject |
| `NoticeSubjects.Field.Group` | Grupo | Grupo | Group |
| `NoticeSubjects.Field.Group.Placeholder` | Ex.: Conhecimentos Básicos | Ex.: Conhecimentos gerais | E.g.: Basic knowledge |
| `NoticeSubjects.Field.Group.Hint` | Como o edital agrupa as disciplinas. Escolha um grupo já usado ou digite um novo; deixe vazio se o edital não agrupa. Até 100 caracteres. | Como o aviso agrupa as disciplinas. Escolha um grupo já usado ou escreva um novo; deixe vazio se o aviso não agrupa. No máximo 100 caracteres. | How the notice groups its subjects. Pick a group already used or type a new one; leave it empty when the notice has no groups. At most 100 characters. |
| `NoticeSubjects.Field.Group.MoveNote` | Ao trocar de grupo, a disciplina vai para o fim do novo grupo. | Ao mudar de grupo, a disciplina passa para o fim do novo grupo. | Changing the group moves the subject to the end of the new group. |
| `NoticeSubjects.Field.Group.Suggestions` | Grupos já usados nesta edição | Grupos já usados nesta edição | Groups already used in this edition |
| `NoticeSubjects.Field.Label` | Disciplina | Disciplina | Subject |
| `NoticeSubjects.Field.Label.Placeholder` | Ex.: Língua Portuguesa | Ex.: Língua Portuguesa | E.g.: Portuguese language |
| `NoticeSubjects.Field.Label.Hint` | Como o edital a nomeia. De 2 a 200 caracteres. | Tal como o aviso a designa. De 2 a 200 caracteres. | As the notice names it. From 2 to 200 characters. |
| `NoticeSubjects.Field.QuestionCount` | Número de questões | Número de questões | Number of questions |
| `NoticeSubjects.Field.QuestionCount.Placeholder` | Ex.: 10 | Ex.: 10 | E.g.: 10 |
| `NoticeSubjects.Field.QuestionCount.Hint` | De 1 a 500. Deixe vazio se o edital não informa. | De 1 a 500. Deixe vazio se o aviso não o indica. | From 1 to 500. Leave it empty when the notice does not say. |
| `NoticeSubjects.Saved` | Disciplina do edital salva. | Disciplina do aviso guardada. | Notice subject saved. |
| `NoticeSubjects.Deleted` | Disciplina do edital excluída. | Disciplina do aviso eliminada. | Notice subject deleted. |
| `NoticeSubjects.Delete.Object` | disciplina {0} | disciplina {0} | subject {0} |
| `NoticeSubjects.Delete.Message` | A disciplina {0} sai do edital desta edição. Ela pode ser adicionada de novo depois. | A disciplina {0} sai do aviso desta edição. Pode voltar a ser adicionada mais tarde. | The subject {0} leaves this edition's notice. It can be added again later. |
| `ExamEditions.Delete.Message` (changed) | A edição {0} sai do exame {1}, com as disciplinas do edital dela. O mesmo ano, cargo e banca não poderão ser adicionados de novo a este exame. | A edição {0} sai do exame {1}, com as disciplinas do aviso. O mesmo ano, posto de trabalho e entidade organizadora não poderão voltar a ser adicionados a este exame. | The edition {0} leaves the exam {1}, with its notice subjects. The same year, position and board cannot be added to this exam again. |
| `notice_subject.not_found` | Esta disciplina não está mais na edição. A lista foi recarregada. | Esta disciplina já não está na edição. A lista foi recarregada. | This subject is no longer in the edition. The list was reloaded. |
| `notice_subject.label_required` | Informe a disciplina como o edital a nomeia. | Indique a disciplina tal como o aviso a designa. | Type the subject as the notice names it. |
| `notice_subject.label_too_short` | A disciplina precisa de ao menos 2 caracteres. | A disciplina precisa de pelo menos 2 caracteres. | The subject needs at least 2 characters. |
| `notice_subject.label_too_long` | A disciplina é longa demais: no máximo 200 caracteres. | A disciplina é demasiado longa: no máximo 200 caracteres. | The subject is too long: at most 200 characters. |
| `notice_subject.group_too_long` | O grupo é longo demais: no máximo 100 caracteres. | O grupo é demasiado longo: no máximo 100 caracteres. | The group is too long: at most 100 characters. |
| `notice_subject.question_count_invalid` | Informe um número inteiro de 1 a 500, ou deixe vazio. | Indique um número inteiro de 1 a 500, ou deixe vazio. | Type a whole number from 1 to 500, or leave it empty. |
| `notice_subject.duplicate` | Este grupo já tem uma disciplina com este nome (maiúsculas e acentos não contam). | Este grupo já tem uma disciplina com este nome (maiúsculas e acentos não contam). | This group already has a subject with this name (capitals and accents do not count). |
| `notice_subject.move_invalid` | Esta disciplina não pode ir nessa direção. A lista foi recarregada. | Esta disciplina não pode ir nesse sentido. A lista foi recarregada. | This subject cannot move that way. The list was reloaded. |

## Acceptance criteria
- AC1 Given an edition with notice subjects in two groups, when the admin opens the edition page, then the section lists the groups in the order of their first row and the rows in their order, each with its number of questions or "—" (UC1, BR6).
- AC2 Given rows with 10, 20 and one without a number, when the section loads, then the footer shows 30 and says one row has no number (BR8).
- AC3 Given a saved edition, when the admin adds group "Basic knowledge", label "Portuguese", 10 questions, then the row is stored and appears last in that group (UC2, BR6).
- AC4 Given a blank label, a 1-character label or a 201-character label, when saved, then the API answers 400 with `notice_subject.label_required`, `notice_subject.label_too_short` or `notice_subject.label_too_long` and nothing is stored (BR2).
- AC5 Given a 101-character group, when saved, then 400 `notice_subject.group_too_long`; given a blank group, then it is stored as null (BR3).
- AC6 Given a number of questions of 0, 501 or a negative number, when saved, then 400 `notice_subject.question_count_invalid`; given none, then it is stored as null (BR4).
- AC7 Given "Português" in group "Básicos", when the admin adds "portugues" in "básicos", then 409 `notice_subject.duplicate`; when added in group "Específicos", then it is stored (BR5).
- AC8 Given a deleted "Portuguese" in a group, when the admin adds "Portuguese" to the same group again, then it is stored (BR5).
- AC9 Given an existing row, when the admin edits its label and number, then the new values are stored and its position is unchanged (UC3).
- AC10 Given a row moved to another group, when saved, then it appears last in the new group (BR7).
- AC11 Given three rows A, B, C in one group, when the admin moves B up, then the order is B, A, C; when the admin moves the first row up or the last row down, then 400 `notice_subject.move_invalid` and the order is unchanged (UC4, BR6).
- AC12 Given a row, when the admin deletes it after confirming, then it disappears from the list and stays in the table marked deleted (UC5, BR10).
- AC13 Given a Draft edition with notice subjects, when the admin deletes the edition, then its notice subjects are marked deleted in the same save, and the confirmation text says the notice subjects leave with it (UC6, BR10).
- AC14 Given a Published edition, when the admin adds, edits, moves or deletes a notice subject, then it succeeds (BR9).
- AC15 Given an edition id that does not exist or belongs to another exam, when any endpoint is called, then 404 `exam_edition.not_found`; given a notice subject id of another edition, then 404 `notice_subject.not_found` (BR1).
- AC16 Given a user without `catalog.manage`, when any endpoint is called, then 403; and the edition page is not reachable (BR11).
- AC17 Given a new edition not saved yet, when the page shows, then the section says to save the edition first and offers no add (UC2).
- AC18 Given the admin added "Portuguese" in group "Basic knowledge", when the admin opens the add dialog again on the same page visit, then the group field shows "Basic knowledge"; on a fresh page visit it is empty (BR12).
- AC19 Given a row that is first or last in its group, when the section shows, then its move-up or move-down button is visible, disabled and explains why (BR6, screen).
- AC20 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-10-04 — Group is optional free text on each row, no entity of its own — faithful to the notice and less code; the dialog suggests groups already used to avoid typos (owner, question 1).
- 2026-10-04 — Number of questions is optional, 1 to 500 when given — some notices give only the paper's total; blank means "the notice does not say", E-6 decides how to use it (owner, question 2; upper bound by Claude: no real paper has 500 questions for one subject).
- 2026-10-04 — No total-questions field on the edition; the screen shows the sum — no rule that could block a notice with annulled questions (owner, question 3).
- 2026-10-04 — Label unique per edition and group, ignoring case and accents — the same name under two headings happens in real notices (owner, question 4).
- 2026-10-04 — Notice subjects are editable on a Published edition — the edition's other fields already are; students only see them from F-76 (owner, question 5).
- 2026-10-04 — Deleting an edition soft-deletes its notice subjects, and the confirmation says so — rule `ui-project` (owner, question 6).
- 2026-10-04 — A section card on the edition page with an add/edit dialog — a notice subject is a simple entity; one way to edit (owner, question 7).
- 2026-10-04 — Order by move up/down buttons inside the group — keyboard and screen-reader friendly, no new kit component (owner, question 8).
- 2026-10-04 — Dependency on F-79 removed; F-74 and F-79 are not built at the same time — no data link between them, but both change the `Catalog` migration snapshot (owner, question 9).
- 2026-10-04 — Copying notice subjects from another edition is out of scope, captured as F-86 (owner, question 10).
- 2026-10-04 — No new packages, production or tests — the kit, EF Core and the existing test hosts cover it (Claude).
- 2026-10-04 — The unique index excludes deleted rows (a filtered index), unlike `exam_editions` — an admin who deletes a row by mistake must be able to add it again; a notice subject has no history worth keeping its name reserved (Claude).
- 2026-10-04 — Move is its own endpoint with a direction, not a position in the PUT — two rows change in one save, and the server, not the screen, knows the neighbours (Claude).
- 2026-10-04 — Extend `AppRowActions` with a move-up/move-down pair outside its three visible actions (mockup option A) — a menu item cannot show a disabled reason; any sortable list reuses it (owner, screen question 1).
- 2026-10-04 — New kit field for free text with suggestions — `AppLookupField` only picks options with an id; BR3 needs suggestions (owner, screen question 2).
- 2026-10-04 — The add dialog pre-fills the group of the last subject added on the same page visit (BR12) — notices list ten subjects per block (owner, screen question 3).
- 2026-10-04 — pt-PT uses "aviso" for the notice; the glossary loses its "(?)" — the Portuguese term for the document that opens a public exam (owner, screen question 4).
- 2026-10-04 — `NoticeSubject` lives in the `Catalog` module under `ExamEdition`, as the epic decided (Claude, epic decision).
- 2026-10-04 — Build design pass (`system-design`, then `architect`; verified against `CatalogEndpoints.cs`, `glossary.md`, `Area.cs`): accepted — routes under `/api/v1/catalog/...` (the module's real prefix; the item said `/api/v1/exams/...`), column and property `display_order` / `DisplayOrder` instead of `position` (`Position` is the glossary word for Cargo; `Area` already uses `DisplayOrder`), a missing exam answers `exam_edition.not_found` (one lookup of the edition under its exam; `exam.not_found` dropped from the list), `Group` mapped to column `group_label` explicitly (`group` is a reserved word), `CatalogText.Normalize` for both normalized columns, an empty `normalized_group` for "no group", the filter text as a constant, no concurrency token (last writer wins, positions renumbered on every write), no unique index on `display_order` (a swap changes two rows in one save), the edition delete removes its notice subjects through the second store in the same `SaveChanges` (both share the scoped `CatalogModuleDbContext`) (Claude).
- 2026-10-04 — Dropped from the design: storing the spelling of an existing group on save. It is a rule the item does not state (BR3: stored as typed). Rows of one normalized group stay together and the screen draws one heading per run of rows with the same normalized group, using the first row's text (Claude; owner may ask for the other rule with `/agile:change`).
- 2026-10-04 — Not touched, captured as an observation: `DeleteExamEditionHandler` ignores the result of `TrySaveChangesAsync`; F-74 returns that error on the line it changes anyway (Claude).
- 2026-10-04 — Independent review (`reviewer`, Opus, branch diff against `b8890d3`): 0 blockers, 2 majors, 6 minors. Fixed: the number field is bound as text and parsed in the dialog, so `1.5` or `abc` is refused with `notice_subject.question_count_invalid` instead of being dropped silently (major; test cases added); focus after Add, Edit, Move and Delete is now asserted through the calls to the shell focus helper (major; 3 tests); the group key reuses `ComparableText.Normalize` instead of a copy (minor); no focus call when the list failed to load (minor). Accepted: concurrent writes on one edition can briefly split a group on screen until the next write (the last-writer-wins decision above); the live text of the suggestion count is assembled in code and the suggestion list has no accessible name of its own (minor, left for a kit follow-up); the number field has no `inputmode="numeric"` attribute because `AppTextField` has no hook for it, `type="number"` already brings the numeric keyboard (minor); a `notice_subject.not_found` in the dialog says the list was reloaded while the section reloads only after the dialog closes (minor) (Claude).

## Out of scope
- Weight and minimum score per notice subject — epic E-6.
- Mapping to canonical subjects and topics — F-75.
- The student's view of the syllabus — F-76.
- Copying notice subjects from another edition — F-86.
- The notice subjects of the F-37 municipal guard editions — F-78.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: the app host started from this worktree and an Admin account signed in by the owner, with an exam that has an edition. Both are yours: Claude did not start the app host (it starts the local PostgreSQL, Redis and Mailpit containers, which needs the owner's yes) and does not enter credentials. The F-37 municipal guard seed should already give an exam with editions; this was checked only in its test, not in your local database, so if the Exams list is empty, add an exam and an edition at step 1. Close any app host running from another checkout first: two hosts fight for the same ports. The migration is applied at start (Development), in a database of this item.

Git Bash, from `D:/wt/simulab/f-74-notice-subjects-per`:

```bash
Database__Name=simulab_f74 dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

PowerShell 7, from the same folder:

```powershell
$env:Database__Name = "simulab_f74"; dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

Expected: the Aspire dashboard URL is printed and the Web is at https://localhost:7125, with no `fail` line while the migrations apply. To repeat, stop it (Ctrl+C) and run it again.

1. Sign in as `admin@simulab.local`. Open **Exams**, open an exam, then one of its editions (or add one and save it). Below the edition fields there is a **Notice subjects** card. On **Add edition** (a new, unsaved edition) the same card says to save the edition first and has no Add button.
2. Choose **Add notice subject**. Group `Basic knowledge`, subject `Portuguese`, number `10`, Save: the snackbar says "Notice subject saved." and the row shows under the heading **Basic knowledge** with "10 questions". Add `Math` in the same group (the group field already starts with `Basic knowledge`) with no number: the row shows "—" and the footer says the sum is 10 and 1 subject has no number. Add `Law` under a new group `Specific`.
3. Try to add `portugues` in group `basic knowledge`: the dialog says this group already has a subject with this name. Try subject `A`, number `0`, `501` and `1.5`: each shows its message under its own field and nothing is saved. In the group field type `bas`: `Basic knowledge` is suggested.
4. Move **Math** up: it goes above Portuguese and a screen reader announcement is made (visible in the page, hidden). **Math** is now first, so its Move up button is disabled and its tooltip says why; the last row of a group has Move down disabled. A row never crosses to another group.
5. Edit **Law**: change its group to `Basic knowledge`. The hint says it goes to the end of the new group; after Save it is last there. Delete **Law** (confirm): it leaves the list, then add `Law` again in the same group: allowed.
6. Open the edition list of the exam, delete a **Draft** edition that has notice subjects: the confirmation says its notice subjects leave with it. Edit a **Published** edition and add, move and delete one notice subject: all allowed.
7. Switch the language to **pt-PT** and then **pt-BR** (your profile): the card, the dialog, the messages and the tooltips are in that language (pt-BR says "disciplina do edital", pt-PT "disciplina do aviso"). Light and dark theme: the card, the group headings and the disabled buttons stay readable. Keyboard only: Tab through a row (Move up, Move down, Edit, Delete) to **Add notice subject**, Enter, fill the fields with Tab, Tab to Save, Enter; Esc on a dialog with typed text asks before discarding; Esc inside the open group suggestions closes only the list.
8. Permission check: sign in as a Student. `/admin/exams` and the edition page show Page not found, so the card is never reachable.

## Delivery
- Shipped 2026-10-04, version 0.9.0. Branch `feature/F-74`, merged into `main` with `--no-ff` as `a2a3dd5` (board #118); branch and worktree removed, the branch was never pushed.
- Full suite: 2187 tests, 0 failed, in 99 s (build 22 s); slowest project Identity, 1 m 34 s. Catalog 453, Web 1044, architecture 164. Warnings baseline: 0 entries.
- Migration `20261004210926_AddNoticeSubjects`: table `catalog.notice_subjects`, unique index filtered to live rows and `NULLS NOT DISTINCT`.
- App manual: `exam-editions.md` and `index.md` in pt-BR, pt-PT and en. Technical docs regenerated (`docs/architecture/Catalog/`, `docs/api/Simulab.Api.json`).
- Independent review: 0 blockers, 2 majors fixed, minors in `## Decisions`. The owner validated on screen and authorized the merge, 2026-10-04.
- First full run of the ship gate was red on one test, `DocSetTests.Generate_TwiceWithNoCodeChange_GivesTheSameText`; it passed alone and the full run passed on the second try.
