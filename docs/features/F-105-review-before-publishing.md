---
feature: F-105
epic: Question bank
status: refining
board: 151
version: 1
---
# Review before publishing

Technical terms: [glossary](../glossary.md)

## Summary
Questions gain the `InReview` status: a curator sends a complete draft for review, a person with the review permission approves it (it is published) or rejects it with a reason (it goes back to `Draft`), from a review queue. Publishing straight from `Draft` stays as it is; review is a second, optional way to publish. Every send and every decision is kept in an append-only history. Source: epic Question bank (E-4), brief capability 3 "Workflow" (draft, review, publish) and the AI rule "every AI-extracted item is reviewed by a person before it is published" (E-9 will create its questions already `InReview`); owner decisions of 2026-10-16.

## Start
- Depends on: F-95 (done), F-98 (done), F-101 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host, a `Curator` account, an `Admin` account and one account whose role holds only `questions.review` (created on the roles screen) — owner.
- Suggested path: `/agile:screen F-105` (the review queue and the review screen) → approval → `/agile:build`.
- Parallel with: none; it changes `QuestionStatus`, the question handlers and the question form, which every other E-4 item touches.

## Goal
Give the question bank the middle step of "draft, review, publish": a question can wait for a second pair of eyes, with a clear queue, a decision and a recorded reason, and the AI import (E-9) gets the state and the permission it needs.

## What exists (verified 2026-10-16)
- `QuestionStatus` is `Draft`, `Published`, `Annulled` (F-101 appended the last one); stored as a string, and `questions.status` is an EF concurrency token (a lost race answers 409 `question.version_conflict` through `IQuestionStore.TrySaveChangesAsync`). The glossary already lists `InReview` for the state (`docs/glossary.md` lines 56 and 72).
- `Question` (`Domain/Entities/Question.cs`) has `Publish`, `Unpublish`, `Annul`, `Reinstate`, `EnsureDeletable` and a private `Frozen()` for annulled questions; publishing makes no version by itself, `NextVersion` does (F-98: a first publication makes version 1).
- One permission, `questions.manage` (`QuestionBankPermissions`), seeded to `Curator` through `PermissionCatalog.InitialGrants`; `Admin` has every permission. Roles are `Student`, `Curator`, `Admin`: there is no reviewer role.
- Routes under `/api/v1/question-bank/questions` all require `questions.manage` (`QuestionEndpoints.cs`). The Web has the list (`Questions.razor`), the form with the Publication and History cards (`QuestionForm.razor`), a preview dialog and a reason dialog (`QuestionStatusReasonDialog.razor`, F-101).
- The audit interceptor fills `CreatedBy` (the signed-in user id) on every `TenantEntity` it saves. The `QuestionBank` module has no way to turn a user id into a name (nothing in `Identity.Contracts` does), and the version history shows no author either.
- E-9 has no epic file yet, so there is nowhere to move this item to (the epic's "may move to E-9" is closed by the owner's answer below).
- The Simulae source (`FilaRevisaoPage`, `QuestionReviewDecision`) is not on this machine (`D:\dev\_icontrol\simulae` does not exist), and the epic notes its `EmRevisao` status is never assigned. Nothing is imported: the design starts from this module.

## Users and use cases
- UC1 A curator sends a complete draft for review.
- UC2 A curator withdraws a question from review; it is a draft again.
- UC3 A reviewer opens the review queue and sees the questions waiting, oldest first.
- UC4 A reviewer opens one question, sees it as a student will, with the answer key and the explanation, and approves it: it is published.
- UC5 A reviewer rejects it with a reason: it goes back to `Draft`.
- UC6 A curator sees why a question was rejected and the history of sends and decisions, fixes the draft and sends it again.
- UC7 A curator still publishes a complete draft directly, and still edits a published question directly (unchanged from F-95 and F-98).

## Business rules
- BR1 `QuestionStatus` gains `InReview`, appended last. A question is `InReview` from a send until a decision or a withdrawal. Nothing that serves questions to students may read it as published: it is a distinct value, like `Annulled`.
- BR2 Sending needs `questions.manage`, a `Draft`, and the same completeness check as publishing (BR4 of F-95 and the type's key rules): an incomplete draft is refused with the codes publishing uses. A send makes no version (versions belong to published content).
- BR3 While `InReview` the question is frozen: save, publish, unpublish and delete answer 409 `question.in_review` and change nothing; reading, listing, previewing and the version history still work. The only ways out are a decision and a withdrawal. Annul keeps its own rule (`question.not_published`).
- BR4 Withdrawing needs `questions.manage` (any holder, not only the one who sent it) and an `InReview` question; it becomes `Draft`. No reason.
- BR5 Approving needs `questions.review` and an `InReview` question. It does exactly what Publish does (status `Published`, and the version F-98 makes) and adds a note, optional, 1 to 500 characters once trimmed. The person who sent the question may approve it (owner, 2026-10-16); the history records who sent and who decided.
- BR6 Rejecting needs `questions.review` and an `InReview` question; the reason is required, 1 to 500 characters once trimmed (the F-101 rule). The question becomes `Draft`; there is no `Rejected` status.
- BR7 Every send, withdrawal, approval and rejection adds one `QuestionReviewEntry` (kind, reason or note, who, when). Entries are never edited or deleted. "Who" is the user id the audit interceptor stores; this item shows no names (see Decisions).
- BR8 The review queue lists the `InReview` questions, oldest send first (the time of the latest `Submitted` entry), paginated (cap 100), never deleted ones. It has no search or filter in this item.
- BR9 The review screen shows the question as a student sees it plus the answer key, the explanation and the classification (subject, topic, language, difficulty, source edition when there is one). The reviewer cannot change content there.
- BR10 `questions.review` is a new permission of the module's `PermissionCatalog`. `Curator` receives it through `InitialGrants` (owner, 2026-10-16); `Admin` has it as every permission. Endpoints and screens check the permission, never the role. The review routes need only `questions.review`: a reviewer without `questions.manage` can do the whole review.
- BR11 Two decisions at once (or a decision against a withdrawal) are one winner and one 409 `question.version_conflict`, through the existing concurrency token.
- BR12 The topic delete guard already counts a question of any status: a topic used only by a question in review is refused with `topic.in_use`.
- BR13 Publishing a `Draft` directly with `questions.manage` is unchanged (owner, 2026-10-16). It is refused on an `InReview` question (BR3).
- BR14 Editing a `Published` question stays direct: no new review (owner, 2026-10-16; F-123 captures the idea).

## Screens and API
### API (`/api/v1/question-bank`)
- `questions.manage`: `POST /questions/{id}/submit` (no body; 200 `QuestionResponse`), `POST /questions/{id}/withdraw` (no body; 200 `QuestionResponse`), `GET /questions/{id}/reviews` (the entries of one question, oldest first: `QuestionReviewEntryResponse(Kind, Reason, At)`).
- `questions.review`: `GET /review-queue?page=&pageSize=` (`ReviewQueuePageResponse(Items, Total)` of `ReviewQueueItemResponse(Id, StatementExcerpt, ContentLanguage, Type, TopicName, SubjectName, Difficulty, SubmittedAt)`), `GET /review-queue/{id}` (a `QuestionResponse` of an `InReview` question), `POST /review-queue/{id}/approve` (`ApproveQuestionRequest(Note)`), `POST /review-queue/{id}/reject` (`RejectQuestionRequest(Reason)`); both 200 `QuestionResponse`.
- `QuestionStatus` gains `InReview`; `QuestionResponse` is otherwise unchanged. Typed client methods in `QuestionBankApiClient`.
- Error codes (new): `question.in_review` (409), `question.already_in_review` (409), `question.not_in_review` (409), `question.review_reason_required` (400), `question.review_reason_too_long` (400). Reused: `question.not_found`, `question.already_published`, `question.annulled`, `question.not_published`, `question.version_conflict`, and the completeness codes of publishing.
- Migration in schema `question_bank`: new table `question_review_entries` (`id`, `question_id`, `kind` string, `reason` nullable text, the `TenantEntity` audit columns; index `ix_question_review_entries_question_id_created_at`; column comments), and the comment of `questions.status` updated to list `InReview`. No stored status becomes invalid; no data is changed.

### Screens
Mockup: `docs/features/mockups/F-105-review-before-publishing.html` (the four screens below, every state, light and dark, pt-BR / pt-PT / en). Designed 2026-10-17 by `/agile:screen`; only kit patterns of the gallery, no new colour, no new token. Dates and times are shown with the culture's short format (`"g"`), as the History card does.

#### Kit additions (no new component)
- `AppIcons` (Material Outlined, one family): `Review` (`FactCheck`: the menu item and the row action), `Withdraw` (`Undo`), `Approve` (`TaskAlt`), `Reject` (`Cancel`). `Send` already exists and is **Send for review**. The exact constants are confirmed against the MudBlazor icon set at build.
- `QuestionStatusReasonDialog` (F-101) gains `Required` (bool, default `true`), `Label` and `Hint` (default the F-101 texts): the Approve dialog is the same dialog with `Required="false"`, so a reason or a note is asked one way everywhere. An empty optional note sends `Note: null`.
- `QuestionText.StatusName` and `StatusTone` gain `InReview`; `QuestionText` gains `ReviewKindName` and `ReviewKindTone` for the entries (below).
- `AppTextField` gains an optional `Counter` parameter (owner, OQ1 → option B, 2026-10-17): off by default; when on, the field shows "{n}/{max}" (`Common.Counter`, `n` = the current length, `max` = `MaxLength`) under the field, right-aligned on the same line as the hint or the error text, inside the field's description (`aria-describedby`); the count is text, not colour, and never replaces the error text. The change is announced politely through a visually hidden live region that says `Common.Counter.Spoken` ("12 of 500 characters"), so a screen reader is never interrupted while typing. Shown in the gallery ("Field kinds": a text field with a counter, empty, typing, at the limit, over the limit with the error). Used by the shared reason dialog `QuestionStatusReasonDialog` (`Counter="true"`), so it also applies to F-101's annul and reinstate reasons and to the Approve note here.

#### Tones
- Status chip `In review`: `AppStatusTone.Warning` (the kit's "waiting or expiring: pending" tone; the chip text is always beside the dot, so the state never depends on the colour).
- Review history chips: `Submitted` → `Info`, `Withdrawn` → `Neutral`, `Approved` → `Success`, `Rejected` → `Error` (the F-101 History card tones: an annulment is `Error`, a reinstatement `Neutral`).

#### 1. Review queue (`/admin/questions/review`) [new]
- `[Authorize(Policy = PermissionPolicy.Prefix + QuestionBankPermissions.Review)]`, `MainLayout`. Without the permission the app answers its not-found page, as every other admin page. Menu item `Nav.ReviewQueue` in the Content section, right after Questions (`NavigationItems`, `RequiredPermission: QuestionBankPermissions.Review`, icon `AppIcons.Review`): a role holding only `questions.review` sees this item and nothing else of the question bank.
- `AppPageHeader`: breadcrumbs Content / Review queue; title `ReviewQueue.Title`; no primary action (nothing is created here).
- `AppDataTable<ReviewQueueItemResponse>`, `Searchable="false"`, no filters, no sortable column (the order is fixed: oldest latest-send first, BR8), server paging through `GET /review-queue?page=&pageSize=` with the kit's page sizes. Columns, in order:
  1. `Questions.Column.Statement`: `AppTruncatedText` (26rem, `Lang` = the question's `ContentLanguage`, `Href` = `/admin/questions/review/{id}`): the name is a link because the review screen is a read-only detail page (rule `ui`). The whole excerpt is the tooltip.
  2. `Questions.Column.Topic`: topic (`AppTruncatedText` 14rem) with the subject under it in `app-cell-secondary`, as the question list.
  3. `Questions.Column.Type`: `AppChip` with `QuestionText.TypeName`.
  4. `Questions.Column.Language`: `QuestionText.LanguageName`, marked `lang`.
  5. `ReviewQueue.Column.SentAt`: `SubmittedAt` in `"g"`, `app-cell-nowrap`.
- Row actions (`AppRowActions`, `ItemName` = `Questions.Row.Name` with the excerpt cut at 60 characters): one `MoreActions` entry **Review** (`ReviewQueue.Action.Review`, `AppIcons.Review`, `Key: "review"`) that navigates to the review screen. No Edit, no Delete: the reviewer does not edit here, and a holder of `questions.review` alone has no `questions.manage`.
- States: loading (`AppLoadingState` inside the table), empty (`AppEmptyState` with `ReviewQueue.Empty`, no action: nothing is created from the queue), ready, load error (`AppErrorState` with Try again), long text (excerpt and topic truncated with tooltip), narrow width (the table scrolls inside `app-table-wrap`; the page header wraps).
- Accessibility: the table has a caption from the title (kit); each row's Review button is announced as "Review: question "<excerpt>""; the excerpt link is the only tab stop of its cell; the Sent column is text, not a chip. Tab order: drawer, breadcrumbs, rows (excerpt link, then Review) top to bottom, pager.

#### 2. Review screen (`/admin/questions/review/{id}`) [new]
- Same policy as the queue. Loads `GET /review-queue/{id}`; the reviewer sees what the student sees plus the key, the explanation and the classification (BR9). Nothing here edits content and no link leads to the editor.
- `AppPageHeader`: breadcrumbs Content / Review queue (link) / Review question; title `Review.Title`. The breadcrumb is the way back in the ready state; the explicit link `ReviewQueue.Back` (`AppLink`) appears in the not-found, no-longer-in-review and load-error cards, as the question form's not-found card does with `Questions.BackToList`.
- Layout `app-form-layout`: the question card on the left, `AppFormAside` on the right (title `Questions.Form.Aside.Title`, rows: Status chip, Subject, Topic, Content language, Difficulty, Type, Source (the F-96 source line with the paper number, or `Questions.Source.Authored`), `Review.Aside.SentAt`); no checklist. Below 1280 px the aside stacks under the card.
- Question card (`MudPaper.app-form-card`), the `QuestionPreviewDialog` content rendered inline with the key always shown (the reviewer must see it; there is no Show/Hide answer): source line (`QuestionText.SourceLine` + `Questions.Preview.Number`), the base text in `app-question-base` when there is one, the statement, then the options (`ol.app-question-options`, each correct one with `is-correct`, the icon and the text `Questions.Preview.CorrectAnswer`: never colour alone), or the two answers of a true/false question (`Questions.Preview.Answers`) with the correct one marked, and for multiple answer the line `Questions.Preview.SelectCount`. Then `section.app-preview-answer` with the headings `Questions.Preview.Answer` (the letters of the key, or True/False) and `Questions.Preview.Explanation` (the HTML, or `Questions.Preview.NoExplanation`). Everything the Api sanitised, marked `lang` with the content language.
- Actions bar (`div.app-form-actions`, under the card): **Reject** (`MudButton` `Variant.Outlined` `Color.Error`, `AppIcons.Reject`) then **Approve** (`Variant.Filled` `Color.Primary`, `AppIcons.Approve`): the page's one filled button. Both are disabled while a decision is in flight.
- Approve dialog: `QuestionStatusReasonDialog` with `Required="false"`, title `Review.Approve.Dialog.Title`, message `Review.Approve.Dialog.Message`, label `Review.Note.Label`, hint `Review.Note.Hint`, `MaxLength` 500, confirm `Review.Action.Approve` (`Color.Primary`), cancel `Questions.StatusReason.Cancel`; the field shows the counter ("0/500", counting as the reviewer types). The only field check is "too long" (`question.review_reason_too_long`, under the field, beside the counter).
- Reject dialog: the same dialog with `Required="true"`, `Destructive="true"`, title `Review.Reject.Dialog.Title`, message `Review.Reject.Dialog.Message`, label `Questions.StatusReason.Label`, hint `Questions.StatusReason.Hint`, confirm `Review.Action.Reject` (`Color.Error`); the field shows the counter ("0/500", counting as the reviewer types). An empty or blank reason refuses with `question.review_reason_required` under the field and calls nothing; 501 characters with `question.review_reason_too_long`; the counter stays visible beside the error text in both cases.
- In progress: the dialog's confirm button shows the progress circle and both dialog buttons are disabled (the dialog's `_busy`); Esc still closes it only when idle.
- Success: the dialog closes, the page navigates to the queue and a success snackbar says `Review.Approved` or `Review.Rejected`. The queue reloads from its first page.
- Conflict: the Api's 409 (`question.version_conflict`, `question.not_in_review`) is shown as the error alert inside the dialog (the F-101 rule: a refusal is read where the reviewer is typing). When the dialog closes without a success, the page re-reads `GET /review-queue/{id}`: a 409 `question.not_in_review` turns the page into the **no longer in review** card (`AppAlert` `Severity.Warning`, text `Review.NoLongerInReview`, then the link `ReviewQueue.Back`); a 200 shows the question again.
- Other states: loading (`AppLoadingState`), load error (`AppErrorState` with Try again, plus the back link), not found (`question.not_found` alert, back link), no longer in review on first load (the same 409 from the Api), long text (a long option, a long URL and a long explanation wrap inside the card: `app-question-content` has `overflow-wrap: anywhere`), narrow width (one column, buttons wrap, aside under the card).
- Accessibility: the page title is the `h1`; the aside is an `aside` labelled by its title; in both dialogs the counter is part of the field's description (read with the label on focus) and its change is announced politely, never assertively, and the error text is read before it; the options list is labelled (`Questions.Preview.Options` / `Questions.Preview.Answers`); the key is icon + text + border; the dialog is modal, labelled by its title and described by its message (MudBlazor's dialog); after the dialog closes the focus returns to the button that opened it (the F-101 pattern, `_pendingFocus`); the snackbar is the kit's; the frozen states are `role="status"` alerts. Tab order: drawer, breadcrumbs, card content (links inside the content only), Reject, Approve, aside rows are text.

#### 3. Question form (`/admin/questions/{id}`), Publication card [changed]
- `Draft`: the actions row is **Preview**, **Send for review** (`Questions.Action.SendForReview`, `Variant.Outlined` `Color.Primary`, `AppIcons.Send`), **Publish**, all outlined (Save stays the page's one filled button). Send is disabled for the same reasons as Publish, in the same order (unsaved changes first, then the incomplete checklist); the one visible reason line `#question-publication-reason` describes both buttons (`aria-describedby`). A refused send (400 with a publish code) shows in the card's error alert, as a refused publish does. Success: the card switches to the `InReview` state below and the snackbar says `Questions.SentForReview`; the page stays.
- `InReview`: above the form the alert `Questions.InReview.Alert` (`Severity.Warning`, the F-101 frozen alert); every field, option, the Add option button and the row actions are disabled or read as values exactly as `IsAnnulled` does (the one `IsFrozen` condition covers both); `AppFormActions` is not rendered. The card shows the chip `In review` (Warning), the sentence `Questions.Publication.InReview` with the time of the latest `Submitted` entry, and the actions **Preview** and **Withdraw from review** (`Questions.Action.Withdraw`, outlined, `AppIcons.Withdraw`). Withdraw asks no reason and no confirmation (BR4; it is reversible, as Unpublish is); while it runs the button shows progress; success puts the card back to `Draft`, the form becomes editable and the snackbar says `Questions.Withdrawn`. A 409 (`question.not_in_review`: decided meanwhile) reloads the question and shows the Api's text in the card's alert.
- `Draft` after a rejection: inside the card, above the status row, `AppAlert` `Severity.Info` with `Questions.Publication.Rejected` (date in `"g"`, the reason, wrapping). It is shown while the latest entry is `Rejected`, so it disappears with the next send.
- `Published` and `Annulled`: unchanged.
- The entries come from `GET /questions/{id}/reviews`, read with the question and after every send, withdrawal and reload. The Api answers oldest first; the page keeps the list and shows it newest first (next card).

#### 3b. Review history card [new]
- A `MudPaper.app-form-card` with `AppSectionCard` (`Questions.Section.ReviewHistory`, icon `AppIcons.Review`, subtitle `Questions.Section.ReviewHistory.Subtitle`) between the Publication card and the History card, rendered only when the question has at least one entry, or when the entries failed to load (then it shows `AppErrorState` with Try again, so the failure is not silent). No empty state: without entries there is no card.
- `AppDataTable<QuestionReviewEntryResponse>`, `Searchable="false"`, paged in memory as the History card is, newest first. Columns: `Questions.ReviewHistory.Column.Date` (`At` in `"g"`, `app-cell-nowrap`), `Questions.ReviewHistory.Column.Event` (`AppStatusChip` with `ReviewKindName` / `ReviewKindTone`), `Questions.ReviewHistory.Column.Reason` (`AppTruncatedText` 24rem, or the dash with the hidden `Questions.History.NoReason`). No row action, no person (Decisions).

#### 4. Question list (`/admin/questions`) [changed]
- The status chip says `Questions.Status.InReview` (Warning) through `QuestionText`; the status filter lists it (it enumerates `QuestionStatus`); `PublicationActions` returns no action for `InReview` (no Publish, no Unpublish, so the row has Edit and Delete only); Delete is disabled with `Questions.Delete.DisabledInReview` as its tooltip and accessible reason. Edit stays: it opens the frozen form, where Withdraw lives.
- `EditionQuestionsSection`: the chip through the shared status text; nothing else.

#### Claims to check at the build's screen check (`unverified (gallery DOM)`)
Not confirmable from source on 2026-10-17; the build reads them in the gallery's rendered DOM and corrects this section to what the markup says.
- `MudDialog` (the approve and reject dialogs): `role="dialog"`, `aria-modal`, labelled by its title and described by its message.
- `AppDataTable` (queue, review history): a `<caption>` or an accessible name from the page title.
- `AppRowActions` / `MudIconButton`: the row action's `aria-label` ("Review: question ...") and the disabled Delete's reason reach the accessibility tree.
- `MudSnackbar`: a `role="status"` live region.
- `MudButton` with `MudProgressCircular` (busy Send, Withdraw, Approve, Reject): what is announced while in progress.
- `AppRichTextField` disabled while in review: `contenteditable="false"` or `aria-readonly`.
- `AppTextField` with `Counter`: that the visually hidden live region (`aria-live="polite"`) announces the count once per pause in typing and not on every keystroke, and that MudBlazor's own `MudTextField` `Counter` (if the build uses it instead of the kit's span) renders text a screen reader reaches.
- The icon constants `FactCheck`, `TaskAlt`, `Cancel` and `Undo` exist in the installed MudBlazor icon set.

#### Resource keys (SharedResources; en / pt-BR / pt-PT)
| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Common.Counter` | {0}/{1} | {0}/{1} | {0}/{1} |
| `Common.Counter.Spoken` | {0} of {1} characters | {0} de {1} caracteres | {0} de {1} carateres |
| `Nav.ReviewQueue` | Review queue | Fila de revisão | Fila de revisão |
| `Questions.Status.InReview` | In review | Em revisão | Em revisão |
| `Questions.Action.SendForReview` | Send for review | Enviar para revisão | Enviar para revisão |
| `Questions.Action.Withdraw` | Withdraw from review | Retirar da revisão | Retirar da revisão |
| `Questions.Publication.InReview` | In review. Waiting for a reviewer since {0}. | Em revisão. Aguardando um revisor desde {0}. | Em revisão. À espera de um revisor desde {0}. |
| `Questions.InReview.Alert` | This question is in review and cannot be edited. Withdraw it from review to change it. | Esta questão está em revisão e não pode ser editada. Retire-a da revisão para alterá-la. | Esta questão está em revisão e não pode ser editada. Retire-a da revisão para a alterar. |
| `Questions.Publication.Rejected` | Rejected on {0}: {1} | Rejeitada em {0}: {1} | Rejeitada em {0}: {1} |
| `Questions.SentForReview` | Question sent for review. | Questão enviada para revisão. | Questão enviada para revisão. |
| `Questions.Withdrawn` | Question withdrawn from review. It is a draft again. | Questão retirada da revisão. Voltou a ser rascunho. | Questão retirada da revisão. Voltou a ser rascunho. |
| `Questions.Delete.DisabledInReview` | This question is in review. Withdraw it before deleting. | Esta questão está em revisão. Retire-a da revisão antes de excluir. | Esta questão está em revisão. Retire-a da revisão antes de eliminar. |
| `Questions.Section.ReviewHistory` | Review history | Histórico de revisão | Histórico de revisão |
| `Questions.Section.ReviewHistory.Subtitle` | Every time this question was sent for review, withdrawn, approved or rejected, newest first. | Cada vez que esta questão foi enviada para revisão, retirada, aprovada ou rejeitada, da mais recente para a mais antiga. | Cada vez que esta questão foi enviada para revisão, retirada, aprovada ou rejeitada, da mais recente para a mais antiga. |
| `Questions.ReviewHistory.Column.Date` | Date | Data | Data |
| `Questions.ReviewHistory.Column.Event` | Event | Evento | Evento |
| `Questions.ReviewHistory.Column.Reason` | Reason or note | Motivo ou observação | Motivo ou nota |
| `Questions.ReviewHistory.Kind.Submitted` | Sent | Enviada | Enviada |
| `Questions.ReviewHistory.Kind.Withdrawn` | Withdrawn | Retirada | Retirada |
| `Questions.ReviewHistory.Kind.Approved` | Approved | Aprovada | Aprovada |
| `Questions.ReviewHistory.Kind.Rejected` | Rejected | Rejeitada | Rejeitada |
| `ReviewQueue.Title` | Review queue | Fila de revisão | Fila de revisão |
| `ReviewQueue.Column.SentAt` | Sent on | Enviada em | Enviada em |
| `ReviewQueue.Empty` | Nothing is waiting for review. | Nada aguardando revisão. | Nada à espera de revisão. |
| `ReviewQueue.Action.Review` | Review | Revisar | Rever |
| `ReviewQueue.Back` | Back to the queue | Voltar para a fila | Voltar à fila |
| `Review.Title` | Review question | Revisar questão | Rever questão |
| `Review.Aside.SentAt` | Sent on | Enviada em | Enviada em |
| `Review.Action.Approve` | Approve | Aprovar | Aprovar |
| `Review.Action.Reject` | Reject | Rejeitar | Rejeitar |
| `Review.Approve.Dialog.Title` | Approve this question? | Aprovar esta questão? | Aprovar esta questão? |
| `Review.Approve.Dialog.Message` | It is published at once and practice can use it. A note is optional and stays in the review history. | Ela é publicada na hora e a prática pode usá-la. A observação é opcional e fica no histórico de revisão. | É publicada de imediato e a prática pode usá-la. A nota é opcional e fica no histórico de revisão. |
| `Review.Note.Label` | Note | Observação | Nota |
| `Review.Note.Hint` | Optional. Plain text, up to 500 characters. | Opcional. Texto simples, até 500 caracteres. | Opcional. Texto simples, até 500 carateres. |
| `Review.Reject.Dialog.Title` | Reject this question? | Rejeitar esta questão? | Rejeitar esta questão? |
| `Review.Reject.Dialog.Message` | It goes back to draft with your reason, which the curator sees on the question's page. | Ela volta a rascunho com o seu motivo, que o curador vê na página da questão. | Volta a rascunho com o seu motivo, que o curador vê na página da questão. |
| `Review.Approved` | Question approved and published. | Questão aprovada e publicada. | Questão aprovada e publicada. |
| `Review.Rejected` | Question rejected. It is a draft again. | Questão rejeitada. Voltou a ser rascunho. | Questão rejeitada. Voltou a ser rascunho. |
| `Review.NoLongerInReview` | This question is no longer in review: another reviewer decided it, or it was withdrawn. | Esta questão não está mais em revisão: outro revisor decidiu, ou ela foi retirada. | Esta questão já não está em revisão: outro revisor decidiu, ou foi retirada. |
| `question.in_review` | This question is in review and cannot be changed. Withdraw it from review first. | Esta questão está em revisão e não pode ser alterada. Retire-a da revisão primeiro. | Esta questão está em revisão e não pode ser alterada. Retire-a primeiro da revisão. |
| `question.already_in_review` | This question is already in review. | Esta questão já está em revisão. | Esta questão já está em revisão. |
| `question.not_in_review` | This question is not in review. | Esta questão não está em revisão. | Esta questão não está em revisão. |
| `question.review_reason_required` | Write the reason for the rejection. | Escreva o motivo da rejeição. | Escreva o motivo da rejeição. |
| `question.review_reason_too_long` | The reason or note has more than 500 characters. | O motivo ou a observação passa de 500 caracteres. | O motivo ou a nota excede 500 carateres. |

Reused as they are: `Questions.Column.*`, `Questions.Row.Name`, `Questions.Form.Aside.*`, `Questions.Field.*`, `Questions.Source.*`, `Questions.Preview.*`, `Questions.StatusReason.*`, `Questions.History.NoReason`, `Questions.Publication.Publish.Disabled*`, `Common.*`, `question.not_found`, `question.version_conflict`, the completeness codes.

### Wireframe: Question form, Publication card
```text
+--------------------------------------------------------+
| Publication                                            |
| [Draft]  Draft. Not available outside the back office. |
| [ Preview ] [ Send for review ] [new]  [ Publish ]     |
| Save your changes first.          (one reason, both)   |
+--------------------------------------------------------+

+--------------------------------------------------------+
| (!) This question is in review and cannot be edited.   |
|     Withdraw it from review to change it.        [new] |
+--------------------------------------------------------+
| Publication                                            |
| [In review] [new]  In review. Waiting for a reviewer   |
|                    since 16/10/2026 14:02.             |
| [ Preview ] [ Withdraw from review ] [new]             |
+--------------------------------------------------------+

+--------------------------------------------------------+
| Publication                                            |
| (i) Rejected on 17/10/2026 09:15: The key of option C  |
|     contradicts the explanation.                 [new] |
| [Draft]  Draft. Not available outside the back office. |
| [ Preview ] [ Send for review ]  [ Publish ]           |
+--------------------------------------------------------+

+--------------------------------------------------------+
| Review history                                   [new] |
| Date             | Event      | Reason or note         |
| 17/10/2026 09:15 | [Rejected] | The key of option C... |
| 16/10/2026 14:02 | [Sent]     | —                      |
+--------------------------------------------------------+
```

### Wireframe: Review queue
```text
+------------------------------------------------------------------------+
| Content / Review queue                                                 |
| Review queue                                                     [new] |
+------------------------------------------------------------------------+
| Statement             | Topic          | Type   | Language | Sent on   | |
| Acerca dos direitos…  | Direitos fund. | Single | pt-BR    | 16/10 14:02 | [Review]
|  (link)               |  Direito Const.|        |          |           | |
| Read the text and…    | Reading        | Multi  | English  | 16/10 15:40 | [Review]
+------------------------------------------------------------------------+
| (inbox)  Nothing is waiting for review.            (empty state) [new] |
+------------------------------------------------------------------------+
```

### Wireframe: Review screen
```text
+--------------------------------------------------------------+--------------------+
| Content / Review queue / Review question                      | Summary            |
| Review question                                         [new] | Status [In review] |
+--------------------------------------------------------------+ Subject  Direito…  |
| CEBRASPE · PRF 2024 · Policial Rodoviário   Question 12       | Topic    Direitos… |
| (base text)                                                   | Language pt-BR     |
| Statement ...                                                 | Difficulty 3       |
| A) ...   B) ...   [C) ... (v) Correct answer]   D) ...  E) ...| Type     Single    |
| ------------------------------------------------------------- | Source   CEBRASPE… |
| ANSWER  C                                                     | Sent on  16/10 …   |
| EXPLANATION ...                                               |                    |
+--------------------------------------------------------------+--------------------+
|                                   [ (x) Reject ]  [ (v) Approve ]                  |
+------------------------------------------------------------------------------------+

no longer in review:
+------------------------------------------------------------+
| (!) This question is no longer in review: another reviewer |
|     decided it, or it was withdrawn.                       |
| Back to the queue                                          |
+------------------------------------------------------------+
```

### Wireframe: Reject and Approve dialogs
```text
+----------------------------------------------+   +----------------------------------------------+
| Reject this question?                  [new] |   | Approve this question?                 [new] |
| It goes back to draft with your reason,      |   | It is published at once and practice can use |
| which the curator sees on the question's page|   | it. A note is optional and stays in the      |
| Reason *                                     |   | review history.                              |
| [__________________________________________] |   | Note                                         |
| Required. Plain text, up to 500 chars. 0/500 |   | [__________________________________________] |
|                    [ Cancel ]  [ Reject ]    |   | Optional. Plain text, up to 500 chars. 0/500 |
+----------------------------------------------+   |                    [ Cancel ]  [ Approve ]   |
                                                   +----------------------------------------------+

typing, at the limit, over the limit (the Api's 400):
| Required. Plain text, up to 500 characters.                            137/500 |
| Required. Plain text, up to 500 characters.                            500/500 |
| The reason or note has more than 500 characters.                       512/500 |
```

## Docs
- flow: new docs/flows/review-before-publishing.md — states, send, withdraw, approve, reject, the history, the permission and the screens.

## Acceptance criteria
- AC1 Given a complete `Draft` and a user with `questions.manage`, when it is sent for review, then its status is `InReview`, one `Submitted` entry exists, and no version is made. (UC1, BR1, BR2, BR7)
- AC2 Given an incomplete draft, when it is sent, then 400 with the code publishing would answer and nothing changes; given an `InReview`, `Published` or `Annulled` question, then 409 `question.already_in_review`, `question.already_published` or `question.annulled`. (BR2)
- AC3 Given an `InReview` question, when it is saved, published, unpublished or deleted, then 409 `question.in_review` and the stored question, options and entries are unchanged; get, list, versions, preview and annul (`question.not_published`) answer as before. (BR3)
- AC4 Given an `InReview` question, when it is withdrawn, then it is a `Draft` again with the same content and a `Withdrawn` entry; given any other status, then 409 `question.not_in_review`. (UC2, BR4)
- AC5 Given an `InReview` question, when a reviewer approves it with or without a note, then it is `Published`, version 1 holds its content (or the version F-98 makes), and an `Approved` entry holds the trimmed note; a note of 501 characters answers 400 `question.review_reason_too_long` and changes nothing; 500 is accepted. (UC4, BR5)
- AC6 Given an `InReview` question, when a reviewer rejects it with a reason, then it is a `Draft` and a `Rejected` entry holds the trimmed reason; a missing, blank or 501-character reason answers 400 `question.review_reason_required` or `question.review_reason_too_long` and it stays `InReview`; 500 is accepted. (UC5, BR6)
- AC7 Given a question that is not `InReview`, when approve or reject is called, then 409 `question.not_in_review`; given two decisions sent together, then one succeeds and the other answers 409 `question.version_conflict`. (BR11)
- AC8 Given a reviewer who is also the user who sent the question, when they approve it, then it is published and the entries `Submitted` and `Approved` both carry that user's id. (BR5, BR7)
- AC9 Given the permissions, then: with `questions.manage` only, send, withdraw and the history answer and the queue, review detail, approve and reject answer 403; with `questions.review` only, the queue, review detail, approve and reject answer and send, withdraw, save and the history answer 403; with neither, every route answers 403; a new `Curator` and `Admin` hold both; the menu item and the pages follow the permission. (BR10)
- AC10 Given questions in several statuses, a deleted one and two sends of which one was rejected and sent again, when the queue is listed, then only the `InReview` ones appear, oldest latest-send first, paginated, and an empty queue shows the empty state; the review detail of a question that is not `InReview` answers 409 `question.not_in_review`. (UC3, BR8)
- AC11 Given a question that went through send, rejection and send, when its history is read, then three entries come oldest first with their kinds and reasons; entries cannot be changed or deleted by any route. (UC6, BR7)
- AC12 Given a topic used only by an `InReview` question, when it is deleted, then 409 `topic.in_use`. (BR12)
- AC13 Given a question in review, then the list shows the chip and the filter finds it, the row menu has no Publish or Unpublish and Delete is disabled with its reason. (BR1, BR3)
- AC14 Given the question form, then a draft shows **Send for review** with Publish's disabled reasons, an `InReview` question is read-only with only **Withdraw from review**, and a rejected draft shows the rejection alert with the reason. (UC1, UC2, UC6)
- AC15 Given the review screen, then **Approve** publishes and returns to the queue, **Reject** needs a reason in its dialog, and a question decided meanwhile shows the "no longer in review" alert. (UC4, UC5)
- AC16 Given a complete draft, when it is published directly, and given a published question, when it is edited, then both work as in F-95 and F-98 with no review. (UC7, BR13, BR14)
- AC17 Given the migration, then existing rows stay valid, `question_review_entries` exists with its comments and index, and the enum column comments test passes. (migration)
- AC18 Given the reason dialogs of the review (approve, reject) and of F-101 (annul, reinstate), when the user types, then the field shows "{n}/500" updated as they type, stays visible beside an error text, and the count is part of the field's description; `AppTextField` without `Counter` renders as before. (owner decision of 2026-10-17)
- AC19 All new texts and error code texts appear in every locale of `UI locales:`.

## Criterion → test
<!-- Written by /agile:build at its start, one row per criterion above, in the same order. -->

## Decisions
- 2026-10-16 — The rejection reason is required, 1 to 500 characters, the F-101 rule — one rule for curators to learn — confirmed: F-101.
- 2026-10-16 — Direct publishing by `questions.manage` stays; review is a second way to publish — nothing of F-95 and F-98 breaks, the pilot with testers goes on, and E-9 will create its questions already `InReview` so the AI rule holds there — asked.
- 2026-10-16 — Review permission: who holds `questions.review` by default — owner chose "Admin and Curator" over the recommended "Admin only" (average 8.0 vs 7.5); kept. The brief and the glossary make the curator the person who reviews (`product/brief.md:10`, `docs/glossary.md:74`). "Curator of the tenant" is the `Curator` role: `TenantId` is dormant in v1 — asked.
- 2026-10-16 — The person who sent a question may approve it, with both ids recorded — the AI import has the curator review their own draft and a sole owner needs no second account — asked.
- 2026-10-16 — An `InReview` question is frozen and can be withdrawn — the reviewer judges what was sent, which cannot change under them — asked.
- 2026-10-16 — A rejection returns the question to `Draft` with the reason in the history; there is no `Rejected` status — one state less in every reader of `QuestionStatus` — asked.
- 2026-10-16 — Editing a published question needs no new review — as F-95 and F-98; the idea is F-123 — asked.
- 2026-10-16 — F-105 stays in epic E-4 — E-9 has no epic file yet; this item delivers the state, queue and permission E-9 will consume — asked.
- 2026-10-16 — The reviewer decides on a queue and a read-only review screen of their own, designed with `/agile:screen` — the form is an editor and a reviewer must not see fields they cannot change — asked.
- 2026-10-16 — Future work captured as ideas: F-121 (email the author of a decision), F-122 (assign a reviewer), F-123 (review edits of a published question) — owner chose to capture them over the recommended "only list them" (average 6.6 vs 7.0); kept — asked.
- 2026-10-16 — A send runs the publish completeness check — a reviewer should never receive a question that could not be published — Claude.
- 2026-10-16 — Approve reuses the Publish logic (status and version) and adds the entry in the same transaction — one rule for what "published" means — Claude.
- 2026-10-16 — The review routes live under `/review-queue` and need only `questions.review` — a role holding only the review permission must be able to review, and the least-privileged role is the one to validate with (B-28) — Claude.
- 2026-10-16 — The history is an append-only table, `question_review_entries`, one row per event, with the audit `CreatedBy` as "who" — the interceptor already writes it; no `Rejected` or `Withdrawn` flags on `questions` — Claude.
- 2026-10-16 — No names of people are shown: the module cannot resolve a user id to a name and the version history has none either; a name lookup contract is a separate piece (F-121 needs the author's email and would add it) — Claude, from the code.
- 2026-10-16 — Sending is only in the Publication card; the list shows the chip and the filter — one way to do it, the F-101 rule — Claude.
- 2026-10-16 — No new package (production or tests), setting, job or ADR; the glossary gets the rows `ReviewQueue`, `QuestionReviewEntry`, `Submit for review`, `Approve` and `Reject` before their first use in code — Claude.
- 2026-10-16 — No data is migrated: stored statuses stay valid and the new table starts empty — Claude, from the code.
- 2026-10-16 — The Simulae review source is unreachable on this machine and its status path is unused; nothing is imported — Claude, from the code.
- 2026-10-17 — The reason dialogs show a character counter ("0/500") — owner chose "with counter" over the recommended "no counter" (average 6.6 vs 8.0); kept. `AppTextField` gains an optional `Counter`, and because the reason dialog is shared, F-101's annul and reinstate dialogs show it too — asked.
- 2026-10-17 — Stitch offered — reference (project `projects/7595070180449228896`, design system from `docs/design/DESIGN.md`); failed (the one `generate_screen_from_text` call timed out and no screen appeared in the project after about 9 minutes of polling; not retried). The HTML mockup is the design — owner chose "reference beside the mockup" over the recommended "no" (average 6.8 vs 8.4); kept — asked.
- 2026-10-17 — Screen design (`ux-designer`, from the kit only): "In review" chip in the `Warning` tone; the history chips Sent `Info`, Withdrawn `Neutral`, Approved `Success`, Rejected `Error`; four new icon constants and no new colour, token or component; Approve and Reject reuse `QuestionStatusReasonDialog` (`Required`, `Label`, `Hint`, `Counter`); the queue has no search, filter or sort; the review screen always shows the key — Claude.

## Out of scope
- Email to the author when a decision is made — F-121.
- Assigning a specific reviewer to a question — F-122.
- Review of edits of a published question — F-123.
- Search, filters and sorting of the review queue; showing people's names in the history (a name lookup contract).
- Creating questions already `InReview` from the AI import — epic E-9.
- A dedicated `Reviewer` role in the seed: the owner can give `questions.review` to any role on the roles screen.
- Review of other content (base texts, exam editions).

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build, in two subsections with the same steps (English, pt-BR). -->

## Events
<!-- Written only by events.js. -->
- 2026-10-16 question — asked 9, from context 1
- 2026-10-16 override — who holds questions.review by default — chose "Admin and Curator" over "Admin only"
- 2026-10-16 override — capture future ideas — chose "capture the three as ideas" over "only list them"
- 2026-10-17 override — reason counter on the reject dialog — chose "with counter" over "no counter"
- 2026-10-17 override — Stitch reference — chose "reference beside the mockup" over "no"
- 2026-10-17 question — asked 2, from context 0

## Delivery
<!-- Filled by /agile:ship. -->
