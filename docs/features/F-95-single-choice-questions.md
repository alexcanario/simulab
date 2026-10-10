---
feature: F-95
epic: Question bank
status: building
board: 141
version: 1
---
# Single-choice questions in the back office

Technical terms: [glossary](../glossary.md)

## Summary
A curator writes a single-choice question in the back office: statement, an inline base text, options, answer key, explanation, difficulty, topic and content language; saves it as `Draft`, publishes it, lists it and previews it as a student will see it. This item creates the new `QuestionBank` module (schema `question_bank`, five-project shape, references `Topic` by id through `Catalog.Contracts`), the safe content format for statements and options (sanitized HTML edited in a visual editor), the question permission and the guard that blocks deleting a topic in use (F-79 BR9). Source: epic Question bank (E-4), decisions of 2026-10-09; Simulae `Question.cs` and `ConfigurationBase.cs` (T1) as the import source.

## Start
- Depends on: F-79 (done), F-75 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: none; every other E-4 feature needs it. F-77 also edits Catalog (aliases): expect a small conflict in `Catalog.Contracts` and `DeleteTopicHandler`.

## Goal
Give the question bank its first content: a curator can author, publish, find and preview a single-choice question, so every later E-4 feature adds to a working module instead of creating it.

## What exists (verified 2026-10-10)
- No question code in `src/`; the `Catalog` module has `Topic` and `Subject` (F-79) and a topic delete guard for one reason only: a live notice subject maps it, `topic.in_use` (F-75, `DeleteTopicHandler`, `ITopicStore.IsMappedAsync`).
- `docs/agile/profile.md` line 11 says `Catalog` also holds the question bank; the epic decision of 2026-10-09 makes `QuestionBank` its own module. This item corrects the profile and records it in ADR-0005.
- The content-language list is one list for the whole app: `SupportedLanguages` in `Identity.Contracts`; `Exam.ContentLanguage` already uses it.
- Markdig 1.3.2 is in `Directory.Packages.props` and used by `LegalDocumentProvider` with `DisableHtml()`. Measured on 2026-10-10: that pipeline still emits `<a href="javascript:...">` and `<img src="data:...">`. Hence sanitized HTML, not Markdown (comparison: `docs/features/mockups/F-95-content-format-comparison.md`).
- UI kit has `AppDataTable`, `AppChecklistItem`, `AppLookupField`, `AppConfirmDialog`, `AppRadioCards`; it has no rich-text field.
- Permissions: a module declares a `PermissionCatalog` with `InitialGrants` (F-33, F-36); the Web lists the same names in `WebPermissions`.
- Simulae defects not to import: raw HTML typed in a text field with no sanitizer; `EmRevisao` and `Rascunho` never assigned; the wrong-answer penalty on each question (E-6 owns it).

## Users and use cases
- UC1 A curator creates a question as `Draft`: topic, content language, statement, optionally base text, options with the correct one marked, explanation, difficulty.
- UC2 A curator edits a question, `Draft` or `Published`, in place.
- UC3 A curator publishes a complete `Draft`.
- UC4 A curator unpublishes a `Published` question back to `Draft`.
- UC5 A curator lists questions, filters by status and searches the statement text.
- UC6 A curator previews a question as a student will see it, then reveals the answer key and explanation.
- UC7 A curator deletes a `Draft`.
- UC8 An admin deletes a topic; the topic is refused while a live question points at it.

## Business rules
- BR1 `Question : TenantEntity` maps to `question_bank.questions`, its options to `question_bank.question_options`. Global data: `TenantId` null; delete is a soft delete.
- BR2 A question has a type (`QuestionType.SingleChoice` is the only value this item defines; each later type adds its own), a status (`Draft` or `Published`), a statement, an optional base text, an optional explanation, an optional difficulty, a topic and a content language.
- BR3 `Draft` needs a non-empty statement (empty after tags are removed counts as empty), a topic and a content language; it may have from 0 to 5 options and no correct one yet.
- BR4 `Published` needs, besides BR3: from 2 to 5 options, each with non-empty text, and exactly one marked correct. Explanation and difficulty stay optional. Editing a published question runs the same check and refuses a change that breaks it.
- BR5 Options keep a fixed position; the letters A to E come from the position, never stored. Reordering is by moving an option up or down in the form.
- BR6 Difficulty is a whole number from 1 to 5 or empty.
- BR7 The content language is one of `SupportedLanguages`, canonicalized, and is never translated (ADR-0001 #27). The form preselects the curator's UI language.
- BR8 Every text field (statement, base text, option, explanation) is HTML written by a person, so the Api sanitizes it on every write (create, update) against an allow-list and stores only the sanitized result. The response carries what is stored. Allow-list: `p br strong b em i u s sub sup ul ol li blockquote table thead tbody tr th td` and `a` with `http`/`https` hrefs only (`rel="noopener noreferrer"`, `target` dropped). No `img` (F-103), no `style`, `class` or `data-*`, no script, no event attributes. List markup produced by the editor is normalized by the sanitizer step to plain `ul`/`ol` (BR8b, build spike).
- BR8b The allow-list is one list in one place; a test pins it, so widening it is a visible change.
- BR9 Limits (characters of sanitized HTML): statement 10 000, base text 20 000, explanation 10 000, option 2 000. Over the limit is refused, never truncated.
- BR10 The question keeps a plain-text copy of the statement (tags removed, normalized like `CatalogText.Normalize`) for the list search; it is derived on every write and never edited.
- BR11 A `Published` question cannot be deleted; unpublish first. A `Draft` delete is a soft delete.
- BR12 Publishing and unpublishing change the status only; they do not touch the content.
- BR13 Deleting a topic is refused with 409 `topic.in_use` while at least one live question (any status, deleted ones excluded) points at it. Moving a topic to another subject stays allowed.
- BR14 Pages and endpoints check the permission `questions.manage`, never a role name. Identity grants it once to `Curator` when the row is created (`InitialGrants`); `Admin` has every permission.
- BR15 The list is paginated, newest first, never shows deleted questions, and offers only the status filter and the statement search; the other filters are F-100.
- BR16 Unknown or deleted ids answer 404 `question.not_found`; an unknown or deleted topic answers 404 `question.topic_not_found`.

## Screens and API
Mockup: `docs/features/mockups/F-95-single-choice-questions.html` (both screens, the kit component, every state, three languages, light and dark). Screen design of 2026-10-10 (`ux-designer`); the mockup draws only identity tokens (`docs/design/identity.tokens.json`) and gallery patterns. No new colour.

- `/admin/questions` — "Questions" in the Content menu section (item shown with `questions.manage`). `AppDataTable`: statement excerpt, topic (with its subject), difficulty, language, status chip; search box, status filter; row actions edit, publish or unpublish, delete (disabled with the reason when `Published`). Empty and empty-search states.
- `/admin/questions/new` and `/admin/questions/{id}` — the question form (mockup above): classification (subject then topic, language, difficulty), statement, base text, options (2 to 5, mark the correct one, move, add, remove), explanation, a publish checklist (BR4 shown before the click), and the student preview with a "show answer" reveal.
- New kit component `AppRichTextField` in `Components/Ui` and the gallery: wraps the editor, label and error text, keyboard operable; the only place that knows the editor package.
- Typed client: `QuestionBankApiClient` in the Web, next to `CatalogApiClient`.

### Screen 1 — `/admin/questions` (list)
Pattern: `Exams.razor` and `Subjects.razor` (list on its own page; the question is a complex entity, so add and edit are a page, never a dialog — rule `ui-project`).

- Navigation: `NavigationItems` gets `new(NavigationSection.Content, "/admin/questions", AppIcons.Questions, "Nav.Questions", RequiredPermission: QuestionBankPermissions.Manage)` after Subjects. New `AppIcons.Questions = Icons.Material.Outlined.Quiz`.
- `AppPageHeader`: `Title = Questions.Title`, breadcrumbs `Nav.Section.Content / Questions.Title` (both disabled), primary action `Common.Add` with `AppIcons.Add` → `/admin/questions/new`.
- `AppAlert` (error) above the table for a refused row action (`question.delete_requires_draft`, `question.already_published`, `question.not_published`, `question.not_found`, unexpected): text through `ErrorText`.
- `AppDataTable<QuestionListItemResponse>`, `Searchable`, `SearchPlaceholder = Questions.Search.Placeholder`, `EmptyMessage = Questions.Empty` (no filter) or `Questions.Empty.Filters` (status filter set), `EmptySearchMessage = Questions.Empty.Search` with the term, empty action `Common.Add`. `ToolBarContent`: one `AppSelectField<QuestionStatus?>` `Id="questions-filter-status"`, `Label = Questions.Filter.Status`, options `Common.Filter.All` (null), `Questions.Status.Draft`, `Questions.Status.Published`; a change reloads from the first page. Nothing else: the other filters are F-100 (BR15).
- Columns (none sortable: the order is newest first, BR15; sorting is F-100):
  1. `Questions.Column.Statement` — `AppTruncatedText Text=StatementExcerpt MaxWidth="26rem" Lang=ContentLanguage`. `StatementExcerpt` is the BR10 plain-text copy cut by the Api at 200 characters; the tooltip shows the whole excerpt. Not a link: there is no read-only detail page (rule `ui`).
  2. `Questions.Column.Topic` — topic name, then the subject name on a second line in `app-cell-secondary` (the pattern of the Scope column of `Exams.razor`). Both truncated at 14rem with tooltip.
  3. `Questions.Column.Difficulty` — `AppNumberColumn<…, int?>`; an empty difficulty renders an em dash with `aria-hidden="true"` and the visually hidden text `Questions.Difficulty.NotSet.Spoken` (the dash pattern of `NoticeSubjectsSection`).
  4. `Questions.Column.Language` — the language in its own name through `SupportedCultures.NativeName`, with `lang` set to the culture (as `ExamForm` does); no resource key.
  5. `Questions.Column.Status` — `AppStatusChip`: `Draft` → `Tone=Neutral`, `Text=Questions.Status.Draft`; `Published` → `Tone=Success`, `Text=Questions.Status.Published`.
- Row actions (`AppRowActions`, `ItemName = Questions.Row.Name` with the excerpt cut at 60 characters so the accessible name stays short): `OnEdit` → `/admin/questions/{id}`; `OnDelete` with `DeleteDisabledReason = Questions.Delete.DisabledPublished` while `Published` (BR11, AC8); `MoreActions` with one entry: `Questions.Action.Publish` (`AppIcons.Publish`) for a draft, `Questions.Action.Unpublish` (`AppIcons.Unpublish`) for a published one. Three visible buttons, no overflow menu. Publish and unpublish from the row have no confirmation (both are reversible, BR12); success is the snackbar `Questions.Published` / `Questions.Unpublished` and the table reloads. A publish refused with 400 (BR4 incomplete) shows the code's text in the top alert; the curator completes the question on its page.
- Delete: `Confirm.ConfirmDeleteAsync(L, Questions.Delete.Object, Questions.Delete.Message)` → the kit dialog titled `Common.Delete.Title`, destructive button `Common.Delete.Confirm` with `Questions.Delete.Object` ("this question"); the statement is not quoted in the button (it can be 10 000 characters). Success: snackbar `Questions.Deleted`, reload. Refusal: alert above the table, reload.
- Paging: the kit's sizes (10, 25, 50), page info and result announcement (`Common.Table.*`) as every list.
- Permissions: page `[Authorize(Policy = PermissionPolicy.Prefix + QuestionBankPermissions.Manage)]`; without it the menu item is absent and the route renders the not-found page (F-2 BR7). `Curator` and `Admin` see everything the same; there is no read-only variant in this item.
- States: loading (`AppLoadingState` inside the grid), empty, empty with filter, empty search, ready, load error (`AppErrorState` with `Common.TryAgain`), row-action error (alert), permission denied (not-found page), long text (excerpt truncated with tooltip; topic and subject truncated; the status chip never wraps).

### Screen 2 — `/admin/questions/new` and `/admin/questions/{id}` (form)
Pattern: `ExamForm.razor` (sectioned cards in a form card, read-only `AppFormAside` beside it, one primary button — Save — at the bottom right) plus `ExamEditionsSection`/`NoticeSubjectsSection` (a card below the layout whose actions are not the form's Save).

- `AppPageHeader`: `Title = Questions.Form.AddTitle` / `Questions.Form.EditTitle`; breadcrumbs `Nav.Section.Content` (disabled) / `Questions.Title` (link `/admin/questions`) / the title again (disabled; the statement is too long for a crumb). No header action (Save is the page's one primary button).
- Page states before the form: `AppLoadingState` while the question loads; not found → `app-form-card` with `AppAlert` `question.not_found` and `AppLink` `Questions.BackToList`; load error → `AppErrorState` (retry); denied → not-found page.
- `app-form-layout`: the form card (`MudPaper.app-form-card`) and the aside.
- Top of the form card: `AppErrorSummary Id="question-error-summary"` (`Common.ErrorSummary.Title`; one link per refused field, in form order, shown after a refused Save and focused by `simulabShell.focusElement`) and `AppAlert` for a refusal that belongs to no field (unexpected, `question.not_found` on save, `question.topic_not_found` also sets the Topic field).

Sections, in order (`AppSectionCard`, ids `question-section-*`):
1. **Classification** — `Questions.Section.Classification` / `.Subtitle`, icon `AppIcons.Topics`. `AppFormGrid Columns="2"`:
   - Subject — `AppLookupField Id="question-subject"`, `Label = Questions.Field.Subject`, `Placeholder = Questions.Field.Subject.Placeholder`, `LeadingIcon = AppIcons.Subjects`, `Hint = Questions.Field.Subject.Hint`, `SearchAsync` = `CatalogApiClient.ListSubjectsAsync` (name search, `MaxCandidates`), not required by the Api (the question stores only the topic) but marked required on screen because the topic cannot be picked without it. Changing or clearing the subject clears the topic.
   - Topic — `AppLookupField Id="question-topic"`, `Label = Questions.Field.Topic`, `Placeholder = Questions.Field.Topic.Placeholder`, `LeadingIcon = AppIcons.Topics`, `Required`, `Disabled` while no subject is chosen with `Hint = Questions.Field.Topic.DisabledHint`, else `Hint = Questions.Field.Topic.Hint`; `SearchAsync` lists the topics of the chosen subject (F-79 endpoint) filtered by the term; `MinChars="0"`, `OpenOnFocus` (a subject has a short topic list). `Error`: `question.topic_not_found` (the topic was deleted meanwhile: the value is cleared and the field asks again, as `ExamForm` does for the authority) or the client-side required message `Questions.Field.Topic.Required`.
   - Content language — `AppSelectField<string> Id="question-content-language"`, `Label = Questions.Field.ContentLanguage`, `Hint = Questions.Field.ContentLanguage.Hint`, `LeadingIcon = AppIcons.Language`, `Required`, options from `SupportedCultures.All` in their own names with `lang`, preselected with the curator's UI culture (BR7, AC14). `Error`: `question.content_language_invalid`.
   - Difficulty — `AppSelectField<int?> Id="question-difficulty"`, `Label = Questions.Field.Difficulty`, `Hint = Questions.Field.Difficulty.Hint`, options `Questions.Difficulty.NotSet` (null) then `1` to `5` (numbers; no key). `Error`: `question.difficulty_invalid`.
2. **Statement** — `Questions.Section.Statement` / `.Subtitle`, icon `AppIcons.Questions`. `AppRichTextField Id="question-statement"`, `Label = Questions.Field.Statement`, `Placeholder = Questions.Field.Statement.Placeholder`, `Required`, `Hint = Questions.Field.Statement.Hint`, `Lang = ContentLanguage`. Errors: `question.statement_required` (client: empty after tags are removed, on blur and on submit, AC2), `question.statement_too_long` (client: HTML longer than 10 000 characters; the Api decides on the sanitized length, so the client check is comfort only).
3. **Base text** — `Questions.Section.BaseText` / `.Subtitle`, icon `AppIcons.BaseText`. `AppRichTextField Id="question-base-text"`, optional, `Hint = Questions.Field.BaseText.Hint`. Error `question.base_text_too_long`.
4. **Options** — `Questions.Section.Options` / `.Subtitle`, icon `AppIcons.Options`. The list is an `AppItemRows<OptionDraft>` (`EmptyMessage = Questions.Options.Empty`, `AriaLabelledBy` the section title) inside a `MudRadioGroup<int?>` whose value is the correct option's position (`Questions.Options.Correct.Label` as the group's accessible name). Each row (`RowTemplate`) is one line on desktop, compact:
   - the letter badge `A`–`E` from the position (BR5; `app-option-letter`: `textPrimary` on `primaryLighten`, a pair the theme tests already measure), rendered as text so a screen reader reads the letter;
   - `MudRadio<int?>` with `aria-label = Questions.Options.MarkCorrect` ("Mark {letter} as correct"), `Dense`; the group lets exactly one be checked (BR4), and a draft may have none (BR3);
   - `AppRichTextField Compact="true"` `Id="question-option-{position}"`, `Label = Questions.Options.Field` ("Text of option {letter}", visually hidden: the badge is the visible label), no hint, `Lang = ContentLanguage`; `Error`: `question.option_text_required` (client: empty when the question is `Published` or when Publish is pressed), `question.option_too_long` (client: over 2 000). Compact = inline toolbar only (bold, italic, underline, strikethrough, superscript, subscript, link, clear formatting) and a one-line minimum height that grows with the text: an option is a phrase, and block formats (lists, quote) would break the lettered list; the sanitizer still keeps them if pasted (the Api is the authority).
   - `Actions`: `AppRowActions ItemName = Questions.Options.Letter IdPrefix="question-option-actions-{position}"` with `OnMoveUp` (`MoveUpDisabledReason = Questions.Options.MoveUp.DisabledFirst` on the first), `OnMoveDown` (`…MoveDown.DisabledLast` on the last) and `OnDelete` (remove, no confirmation: nothing is saved until Save, and the row is restored by Cancel/discard). A move is client-side (the order is sent with the Save); after it, focus stays on the pressed button or its pair (the `NoticeSubjectsSection` rule) and a `role="status"` region says `Questions.Options.Moved`; a removal says `Questions.Options.Removed` and focuses the next row's remove button, else the Add button.
   - Below the list, left: `MudButton Variant="Outlined" Color="Primary" StartIcon="AppIcons.Add" id="question-option-add"` `Questions.Options.Add`, disabled at five with the tooltip `Questions.Options.Add.DisabledMax`; right: the count `Questions.Options.Count` ("{0} of 5 options") always visible, so the limit is never told by a tooltip alone (a disabled button is not focusable). Adding focuses the new row's editor.
   - Section-level errors under the list (`app-field-error-text`, `id="question-options-description"`, referenced by the group's `aria-describedby`): `question.options_count`, `question.key_invalid`, and the server fallback for `question.option_text_required` / `question.option_too_long` when the problem does not say which option (decision of 2026-10-10).
5. **Explanation** — `Questions.Section.Explanation` / `.Subtitle`, icon `AppIcons.Explanation`. `AppRichTextField Id="question-explanation"`, optional, `Hint = Questions.Field.Explanation.Hint`. Error `question.explanation_too_long`.
6. `AppFormActions Saving HasChanges OnSave OnCancel`: `Common.Save` (filled, progress while saving) and `Common.Cancel` (text) → `/admin/questions`; leaving with changes asks `Common.Discard.*` (kit).

Save: client checks (comfort) — topic, language, statement (BR3); when the question is `Published`, also BR4 (2 to 5 options with text, exactly one correct), because the Api refuses an edit that breaks a published question (AC7). Any failure: `AppErrorSummary` + field errors, focus to the summary, nothing sent. Then `POST /questions` (new) or `PUT /questions/{id}`; success: `_saved` takes the response (the sanitized HTML replaces what the editors hold, so the curator sees what was stored), snackbar `Questions.Saved`, a new question navigates to `/admin/questions/{id}` with `replace: true` (the `ExamForm` pattern). Refusal by code: `question.statement_required|statement_too_long` → Statement; `base_text_too_long` → Base text; `explanation_too_long` → Explanation; `option_text_required|option_too_long|options_count|key_invalid` → Options section; `difficulty_invalid` → Difficulty; `content_language_invalid` → Language; `topic_not_found` → Topic (cleared) + alert; others → alert.

**Aside** — `AppFormAside Title = Questions.Form.Aside.Title`, `EmptyText = Common.Summary.NotFilled`, rows: `Questions.Form.Aside.Status` (`Questions.Status.Draft` / `.Published`; "—" for a new question is shown as `Common.Summary.NotFilled`), `Questions.Field.Subject`, `Questions.Field.Topic`, `Questions.Field.ContentLanguage`, `Questions.Field.Difficulty`, `Questions.Form.Aside.Options` (the count). `ChecklistTitle = Questions.Form.Aside.Checklist` ("Before publishing"), `DoneText/PendingText = Common.Checklist.*`, items in this order (BR3 then BR4): `Questions.Checklist.Topic`, `.Language`, `.Statement`, `.OptionsCount` (2 to 5), `.OptionsText` (every option has text), `.Key` (exactly one correct). The first three are also what a draft needs; the required markers on the fields say so. The checklist is computed from the form's current values, live but not a live region (the aside never speaks).

**Publication card** — a second `MudPaper.app-form-card mt-4` below `app-form-layout`, holding `AppSectionCard Id="question-section-publication"` `Questions.Section.Publication` / `.Subtitle`, icon `AppIcons.Publish`. Its buttons are outside the form: Save never publishes (BR12) and Publish never saves.
- New (unsaved) question: `MudText body2` `Questions.Publication.SaveFirst`; no button (the `NoticeSubjects.Section.SaveEditionFirst` pattern).
- Saved question: a row with `AppStatusChip` (as in the list) and the sentence `Questions.Publication.Draft` / `Questions.Publication.Published`; then two outlined buttons: `Questions.Action.Preview` (`AppIcons.View`) and, for a draft, `Questions.Action.Publish` (`AppIcons.Publish`) or, for a published one, `Questions.Action.Unpublish` (`AppIcons.Unpublish`). Publish is disabled with the reason as tooltip and as visible helper text under the buttons (`Questions.Publication.Publish.DisabledUnsaved` while `HasChanges`, because publishing acts on what is stored; else `Questions.Publication.Publish.DisabledIncomplete` while any checklist item is pending). Unpublish is never disabled. Both call `POST /questions/{id}/publish|unpublish`, show progress on the pressed button, then snackbar `Questions.Published` / `Questions.Unpublished` and refresh the chip, the aside and the breadcrumb; a 400/409 goes to an `AppAlert` inside this card (`question.options_count`, `question.key_invalid`, `question.option_text_required`, `question.already_published`, `question.not_published`, `question.not_found`).
- Preview is always enabled for a saved question; with unsaved changes the dialog starts with the `AppAlert Severity="Info"` `Questions.Preview.UnsavedNote`, because the preview renders the stored, sanitized HTML (the Web never renders editor HTML that the Api has not sanitized — decision "the Api sanitizes, the Web shows what is stored").

**Preview dialog** — `QuestionPreviewDialog` (page-level component under `Pages/QuestionBank`, `MudDialog`, `MaxWidth.Medium`, `FullWidth`, `CloseOnEscapeKey = true`, title `Questions.Preview.Title`). Content, in the order a student reads a paper: base text (when present, `app-question-content`), statement (`app-question-content`), then the options as `<ol class="app-question-options">` with one `<li>` per option: the letter badge as text and the option HTML. Everything content is `MarkupString` of the stored HTML inside a container with `lang = ContentLanguage`; the container's CSS gives `p`, lists, `blockquote`, `table` (bordered with `divider`, header row in `textSecondary`), `sup`/`sub` and links (underlined, `primary`) their look; nothing in the HTML carries style (BR8). Below: `MudButton Variant="Outlined"` `Questions.Preview.ShowAnswer` toggling to `Questions.Preview.HideAnswer` (`aria-expanded`, controls the answer region). The answer region (`role="region"`, `aria-labelledby` its heading, `aria-live="polite"` on first reveal) marks the correct option with a 2px `success` border, a `AppIcons.Verified` icon and the text `Questions.Preview.CorrectAnswer` (never colour alone), and shows the explanation under the heading `Questions.Preview.Explanation`, or `Questions.Preview.NoExplanation`. A question with no key (draft) shows `Questions.Preview.NoKey` instead. Dialog actions: `Common.Close` (text). Focus returns to the Preview button on close.

### Kit — `AppRichTextField` (new, `Components/Ui`, gallery section `Gallery.Section.RichText`)
The only component that references `Blazored.TextEditor` (Quill 2); pages never see the editor. Same anatomy as `AppTextField`: label above, required marker, hint or error below.
- Parameters: `Id` (`[EditorRequired]`), `Label` (`[EditorRequired]`), `Value` (HTML string) / `ValueChanged` (raised on blur and after a 300 ms pause while typing, so the aside and checklist follow), `Placeholder` (`[EditorRequired]`: it names the thing), `Hint`, `Error`, `Required`, `Disabled`, `Compact` (inline toolbar, one-line minimum height), `Lang` (BCP 47 of the content: set on the editable region so a screen reader switches voice), `LabelVisuallyHidden` (the option rows: the badge is the visible label).
- Anatomy and DOM: `<div class="app-field app-rich-text [app-field-error] [app-rich-text-compact]">` → `<label id="{Id}-label">` (not `for`: the editable is a `div`) → toolbar `<div role="toolbar" aria-label="RichText.Toolbar" aria-controls="{Id}">` with `type="button"` buttons, each with `aria-label` (`RichText.Bold`, `.Italic`, `.Underline`, `.Strikethrough`, `.Superscript`, `.Subscript`, `.BulletList`, `.NumberedList`, `.Quote`, `.Link`, `.ClearFormatting`), `aria-pressed` for toggles, a tooltip with the same text and the shortcut; icons through `AppIcons` (new constants `FormatBold`, `FormatItalic`, `FormatUnderline`, `FormatStrikethrough`, `Superscript`, `Subscript`, `BulletList`, `NumberedList`, `Quote`, `ClearFormatting`; `Link` exists) → the editable region `id="{Id}"` with `role="textbox" aria-multiline="true" aria-labelledby="{Id}-label" aria-describedby="{Id}-description" aria-required aria-invalid lang`, `data-placeholder` → `<span id="{Id}-description">` with the hint or the error (`aria-live="polite"` on the error span, so an error that appears on blur is spoken — AC17).
- Formats whitelisted in the editor (`formats` option) match the allow-list of BR8: bold, italic, underline, strike, script (sup/sub), list (bullet, ordered), blockquote, link. No header, colour, font, size, align, image, video, code block. No table button in F-95: a pasted or imported table is kept by the sanitizer and rendered by the preview (decision of 2026-10-10).
- Keyboard: the toolbar is one tab stop (roving `tabindex`, Left/Right between buttons, Home/End); Tab from the toolbar enters the editor; inside the editor Tab and Shift+Tab leave it (the kit removes Quill's `tab`/`indent` bindings: WCAG 2.1.2, no keyboard trap); Ctrl/Cmd+B, I, U toggle; Esc inside the editor does nothing (there is no popup); the link button opens the kit's own small `MudDialog` for the URL (label `RichText.Link.Url`, `http`/`https` only — a `javascript:` URL is refused client-side with `RichText.Link.Invalid`; the Api drops it anyway) instead of Quill's inline tooltip, which is not labelled.
- Focus ring: the editable's wrapper takes the `primary` 2px ring on `:focus-within`, like every outlined field; the error state swaps the `linesInputs` border for `error`; disabled lowers opacity and sets `aria-disabled` and `contenteditable="false"` (disabled styling is MudBlazor's default: no token).
- Long text: the editable grows with its content (no inner scroll), wraps long words (`overflow-wrap: anywhere`), and tables scroll horizontally inside the field; the full toolbar wraps to two lines on a phone.
- Hosting: Quill's script and CSS are served from `wwwroot` (decision of 2026-10-10); the kit registers Material Outlined SVGs over Quill's `ui/icons` so the toolbar follows the one-family rule (decision of 2026-10-10); `.ql-snow` CSS is overridden to the theme's variables (`--mud-palette-*`), radius 10px, no literal colour.
- Gallery: empty, focused, filled, error, disabled, compact (one option row) and long text, each with a caption; the gallery also shows the option-row composition (badge, radio, compact editor, move/remove) once.
- bUnit: the editor is swapped for a fake (decision of 2026-10-10); the test asserts the label, `aria-*`, the error text, the toolbar's accessible names and that the long-text host has no fixed width.

### Accessibility and keyboard (WCAG 2.2 AA)
- Tab order, list: skip link → app bar → drawer → page title → Add → search → status filter → rows (truncated excerpt, then the three row buttons) → pager. Form: title → error summary (only when shown, focused by the page) → Subject → Topic → Language → Difficulty → Statement toolbar → Statement → Base text toolbar → Base text → each option: radio (one tab stop for the group, arrows move), toolbar, editor, Move up, Move down, Remove → Add option → Explanation toolbar → Explanation → Cancel → Save → Preview → Publish/Unpublish.
- Announcements: table result count (`Common.Table.Results.*`, kit); lookup results (kit); option moved / removed (`role="status"`); field errors (`aria-describedby`, `aria-live` on the rich text error); failed save (`AppErrorSummary`, `role="alert"`, focused); success (snackbar, `role="status"` by MudBlazor — `unverified (gallery DOM)`); publish disabled reason as visible text, not tooltip alone.
- Names: every icon button has `aria-label` and tooltip from resources (`Common.ActionOnItem` with the letter or the excerpt); the status chip is text with a decorative dot; letters A–E are text; the correct answer in the preview is icon + text + border.
- Content language: every rendered or edited content carries `lang` (list excerpt, editors, preview), as F-36 did for exam names.
- Targets: every button is at least 24 px; the radio's hit area is the 40 px MudBlazor target.
- Contrast (tokens only, both themes): badge `textPrimary` on `primaryLighten` (already measured by the theme tests); pressed toolbar button `primary` on `primaryLighten`: 4.79:1 light, 5.70:1 dark; correct-answer border `success` on `surface`: 3.33:1 light, 4.88:1 dark (UI part, 3:1 needed); everything else is an existing pair.
- Claims the design makes about the rendered DOM that could not be confirmed from source are listed in the screen report as `unverified (gallery DOM)`: Quill 2's editable region attributes, `MudRadio`'s `role`/`aria-checked`, `MudDialog`'s focus trap and `aria-modal`, the snackbar's role.

### Texts (resource keys; en / pt-BR / pt-PT)
| Key | en | pt-BR | pt-PT |
| --- | --- | --- | --- |
| `Nav.Questions` | Questions | Questões | Questões |
| `Questions.Title` | Questions | Questões | Questões |
| `Questions.Search.Placeholder` | Search the statement | Pesquisar no enunciado | Pesquisar no enunciado |
| `Questions.Filter.Status` | Status | Situação | Estado |
| `Questions.Status.Draft` | Draft | Rascunho | Rascunho |
| `Questions.Status.Published` | Published | Publicada | Publicada |
| `Questions.Column.Statement` | Statement | Enunciado | Enunciado |
| `Questions.Column.Topic` | Topic | Tópico | Tópico |
| `Questions.Column.Difficulty` | Difficulty | Dificuldade | Dificuldade |
| `Questions.Column.Language` | Language | Idioma | Idioma |
| `Questions.Column.Status` | Status | Situação | Estado |
| `Questions.Empty` | No question in the bank yet. | Nenhuma questão no banco ainda. | Ainda não há questões no banco. |
| `Questions.Empty.Filters` | No question matches the chosen filter. | Nenhuma questão corresponde ao filtro escolhido. | Nenhuma questão corresponde ao filtro escolhido. |
| `Questions.Empty.Search` | No question matches "{0}". | Nenhuma questão corresponde a "{0}". | Nenhuma questão corresponde a "{0}". |
| `Questions.Difficulty.NotSet` | Not set | Não definida | Não definida |
| `Questions.Difficulty.NotSet.Spoken` | No difficulty set | Dificuldade não definida | Dificuldade não definida |
| `Questions.Row.Name` | question "{0}" | questão "{0}" | questão "{0}" |
| `Questions.Action.Publish` | Publish | Publicar | Publicar |
| `Questions.Action.Unpublish` | Unpublish | Despublicar | Retirar publicação |
| `Questions.Action.Preview` | Preview as a student | Visualizar como aluno | Pré-visualizar como estudante |
| `Questions.Delete.DisabledPublished` | This question is published. Unpublish it before deleting. | Esta questão está publicada. Despublique-a antes de excluir. | Esta questão está publicada. Retire a publicação antes de eliminar. |
| `Questions.Delete.Object` | this question | esta questão | esta questão |
| `Questions.Delete.Message` | The draft leaves the bank and stops appearing anywhere in the app. Its topic stays. | O rascunho sai do banco e deixa de aparecer no app. O tópico permanece. | O rascunho sai do banco e deixa de aparecer na aplicação. O tópico mantém-se. |
| `Questions.Deleted` | Question deleted. | Questão excluída. | Questão eliminada. |
| `Questions.Saved` | Question saved. | Questão salva. | Questão guardada. |
| `Questions.Published` | Question published. | Questão publicada. | Questão publicada. |
| `Questions.Unpublished` | Question unpublished. It is a draft again. | Questão despublicada. Voltou a ser rascunho. | Publicação retirada. Voltou a ser rascunho. |
| `Questions.BackToList` | Back to the questions | Voltar para as questões | Voltar às questões |
| `Questions.Form.AddTitle` | Add question | Adicionar questão | Adicionar questão |
| `Questions.Form.EditTitle` | Edit question | Editar questão | Editar questão |
| `Questions.Section.Classification` | Classification | Classificação | Classificação |
| `Questions.Section.Classification.Subtitle` | Where the question sits in the taxonomy, its language and how hard it is. | Onde a questão fica na taxonomia, seu idioma e sua dificuldade. | Onde a questão fica na taxonomia, o seu idioma e a sua dificuldade. |
| `Questions.Field.Subject` | Subject | Disciplina | Disciplina |
| `Questions.Field.Subject.Placeholder` | Subject name | Nome da disciplina | Nome da disciplina |
| `Questions.Field.Subject.Hint` | Pick the subject to list its topics. | Escolha a disciplina para listar seus tópicos. | Escolha a disciplina para listar os seus tópicos. |
| `Questions.Field.Topic` | Topic | Tópico | Tópico |
| `Questions.Field.Topic.Placeholder` | Topic name | Nome do tópico | Nome do tópico |
| `Questions.Field.Topic.Hint` | Only topics of the chosen subject. | Só tópicos da disciplina escolhida. | Só tópicos da disciplina escolhida. |
| `Questions.Field.Topic.DisabledHint` | Choose a subject first. | Escolha uma disciplina primeiro. | Escolha primeiro uma disciplina. |
| `Questions.Field.Topic.Required` | Choose the topic. | Escolha o tópico. | Escolha o tópico. |
| `Questions.Field.ContentLanguage` | Content language | Idioma do conteúdo | Idioma do conteúdo |
| `Questions.Field.ContentLanguage.Hint` | The language the question is written in. Content is never translated. | O idioma em que a questão está escrita. O conteúdo nunca é traduzido. | O idioma em que a questão está escrita. O conteúdo nunca é traduzido. |
| `Questions.Field.Difficulty` | Difficulty | Dificuldade | Dificuldade |
| `Questions.Field.Difficulty.Hint` | From 1 (easiest) to 5 (hardest). Optional. | De 1 (mais fácil) a 5 (mais difícil). Opcional. | De 1 (mais fácil) a 5 (mais difícil). Opcional. |
| `Questions.Section.Statement` | Statement | Enunciado | Enunciado |
| `Questions.Section.Statement.Subtitle` | What the student is asked. Formatting, lists and links are kept; images come later. | O que se pede ao aluno. Formatação, listas e links são mantidos; imagens vêm depois. | O que se pede ao estudante. Formatação, listas e ligações são mantidas; as imagens vêm mais tarde. |
| `Questions.Field.Statement` | Statement | Enunciado | Enunciado |
| `Questions.Field.Statement.Placeholder` | Write the statement | Escreva o enunciado | Escreva o enunciado |
| `Questions.Field.Statement.Hint` | Up to 10,000 characters. | Até 10.000 caracteres. | Até 10 000 caracteres. |
| `Questions.Section.BaseText` | Base text | Texto-base | Texto de apoio |
| `Questions.Section.BaseText.Subtitle` | A text the question refers to, shown before the statement. Optional. | Um texto a que a questão se refere, mostrado antes do enunciado. Opcional. | Um texto a que a questão se refere, mostrado antes do enunciado. Opcional. |
| `Questions.Field.BaseText` | Base text | Texto-base | Texto de apoio |
| `Questions.Field.BaseText.Placeholder` | Paste or write the base text | Cole ou escreva o texto-base | Cole ou escreva o texto de apoio |
| `Questions.Field.BaseText.Hint` | Up to 20,000 characters. | Até 20.000 caracteres. | Até 20 000 caracteres. |
| `Questions.Section.Options` | Options | Alternativas | Opções |
| `Questions.Section.Options.Subtitle` | 2 to 5 options to publish. The letters follow the order; mark the correct one. | De 2 a 5 alternativas para publicar. As letras seguem a ordem; marque a correta. | De 2 a 5 opções para publicar. As letras seguem a ordem; assinale a correta. |
| `Questions.Options.Correct.Label` | Correct option | Alternativa correta | Opção correta |
| `Questions.Options.MarkCorrect` | Mark {0} as correct | Marcar {0} como correta | Assinalar {0} como correta |
| `Questions.Options.Letter` | option {0} | alternativa {0} | opção {0} |
| `Questions.Options.Field` | Text of option {0} | Texto da alternativa {0} | Texto da opção {0} |
| `Questions.Options.Field.Placeholder` | Option text | Texto da alternativa | Texto da opção |
| `Questions.Options.Add` | Add option | Adicionar alternativa | Adicionar opção |
| `Questions.Options.Add.DisabledMax` | A question has at most 5 options. | Uma questão tem no máximo 5 alternativas. | Uma questão tem no máximo 5 opções. |
| `Questions.Options.Count` | {0} of 5 options | {0} de 5 alternativas | {0} de 5 opções |
| `Questions.Options.Empty` | No option yet. A draft can be saved without options; publishing needs 2 to 5. | Nenhuma alternativa ainda. Um rascunho pode ser salvo sem alternativas; publicar exige de 2 a 5. | Ainda sem opções. Um rascunho pode ser guardado sem opções; publicar exige de 2 a 5. |
| `Questions.Options.MoveUp.DisabledFirst` | Already the first option. | Já é a primeira alternativa. | Já é a primeira opção. |
| `Questions.Options.MoveDown.DisabledLast` | Already the last option. | Já é a última alternativa. | Já é a última opção. |
| `Questions.Options.Moved` | Option moved to position {0} of {1}. | Alternativa movida para a posição {0} de {1}. | Opção movida para a posição {0} de {1}. |
| `Questions.Options.Removed` | Option removed. {0} left. | Alternativa removida. Restam {0}. | Opção removida. Restam {0}. |
| `Questions.Section.Explanation` | Explanation | Explicação | Explicação |
| `Questions.Section.Explanation.Subtitle` | Why the key is right. Optional; shown with the answer key. | Por que o gabarito está certo. Opcional; aparece junto do gabarito. | Porque é que a chave está certa. Opcional; aparece junto da chave de resposta. |
| `Questions.Field.Explanation` | Explanation | Explicação | Explicação |
| `Questions.Field.Explanation.Placeholder` | Explain the answer | Explique a resposta | Explique a resposta |
| `Questions.Field.Explanation.Hint` | Up to 10,000 characters. | Até 10.000 caracteres. | Até 10 000 caracteres. |
| `Questions.Form.Aside.Title` | Summary | Resumo | Resumo |
| `Questions.Form.Aside.Status` | Status | Situação | Estado |
| `Questions.Form.Aside.Options` | Options | Alternativas | Opções |
| `Questions.Form.Aside.Checklist` | Before publishing | Antes de publicar | Antes de publicar |
| `Questions.Checklist.Topic` | Topic | Tópico | Tópico |
| `Questions.Checklist.Language` | Content language | Idioma do conteúdo | Idioma do conteúdo |
| `Questions.Checklist.Statement` | Statement | Enunciado | Enunciado |
| `Questions.Checklist.OptionsCount` | 2 to 5 options | De 2 a 5 alternativas | De 2 a 5 opções |
| `Questions.Checklist.OptionsText` | Every option has text | Toda alternativa tem texto | Todas as opções têm texto |
| `Questions.Checklist.Key` | Exactly one correct option | Exatamente uma alternativa correta | Exatamente uma opção correta |
| `Questions.Section.Publication` | Publication | Publicação | Publicação |
| `Questions.Section.Publication.Subtitle` | A draft is seen only here. A published question is ready for the bank and must stay complete. | Um rascunho só aparece aqui. Uma questão publicada está pronta para o banco e precisa continuar completa. | Um rascunho só aparece aqui. Uma questão publicada está pronta para o banco e tem de continuar completa. |
| `Questions.Publication.SaveFirst` | Save the question first to preview or publish it. | Salve a questão primeiro para visualizar ou publicar. | Guarde primeiro a questão para a pré-visualizar ou publicar. |
| `Questions.Publication.Draft` | Draft. Not available outside the back office. | Rascunho. Não aparece fora do back office. | Rascunho. Não aparece fora do back office. |
| `Questions.Publication.Published` | Published. Edits apply at once and must keep it complete. | Publicada. Edições valem na hora e precisam mantê-la completa. | Publicada. As edições aplicam-se de imediato e têm de a manter completa. |
| `Questions.Publication.Publish.DisabledIncomplete` | Complete the checklist before publishing. | Complete a lista "Antes de publicar" primeiro. | Complete primeiro a lista "Antes de publicar". |
| `Questions.Publication.Publish.DisabledUnsaved` | Save your changes first. | Salve suas alterações primeiro. | Guarde primeiro as suas alterações. |
| `Questions.Preview.Title` | Preview | Visualização | Pré-visualização |
| `Questions.Preview.UnsavedNote` | Showing the saved version. Unsaved changes are not included. | Mostrando a versão salva. Alterações não salvas não aparecem. | A mostrar a versão guardada. As alterações por guardar não aparecem. |
| `Questions.Preview.Options` | Options | Alternativas | Opções |
| `Questions.Preview.ShowAnswer` | Show answer | Mostrar resposta | Mostrar resposta |
| `Questions.Preview.HideAnswer` | Hide answer | Ocultar resposta | Ocultar resposta |
| `Questions.Preview.Answer` | Answer | Resposta | Resposta |
| `Questions.Preview.CorrectAnswer` | Correct answer | Resposta correta | Resposta correta |
| `Questions.Preview.NoKey` | This draft has no correct option marked yet. | Este rascunho ainda não tem alternativa correta marcada. | Este rascunho ainda não tem opção correta assinalada. |
| `Questions.Preview.Explanation` | Explanation | Explicação | Explicação |
| `Questions.Preview.NoExplanation` | No explanation. | Sem explicação. | Sem explicação. |
| `RichText.Toolbar` | Formatting | Formatação | Formatação |
| `RichText.Bold` | Bold (Ctrl+B) | Negrito (Ctrl+B) | Negrito (Ctrl+B) |
| `RichText.Italic` | Italic (Ctrl+I) | Itálico (Ctrl+I) | Itálico (Ctrl+I) |
| `RichText.Underline` | Underline (Ctrl+U) | Sublinhado (Ctrl+U) | Sublinhado (Ctrl+U) |
| `RichText.Strikethrough` | Strikethrough | Tachado | Rasurado |
| `RichText.Superscript` | Superscript | Sobrescrito | Superior à linha |
| `RichText.Subscript` | Subscript | Subscrito | Inferior à linha |
| `RichText.BulletList` | Bulleted list | Lista com marcadores | Lista com marcas |
| `RichText.NumberedList` | Numbered list | Lista numerada | Lista numerada |
| `RichText.Quote` | Quote | Citação | Citação |
| `RichText.Link` | Link | Link | Ligação |
| `RichText.Link.Url` | Address (https://…) | Endereço (https://…) | Endereço (https://…) |
| `RichText.Link.Invalid` | Only http and https addresses. | Só endereços http e https. | Só endereços http e https. |
| `RichText.Link.Insert` | Insert link | Inserir link | Inserir ligação |
| `RichText.ClearFormatting` | Clear formatting | Limpar formatação | Limpar formatação |
| `RichText.Hint.Keyboard` | Tab leaves the editor. | Tab sai do editor. | Tab sai do editor. |
| `Gallery.Section.RichText` | Rich text field | Campo de texto formatado | Campo de texto formatado |
| `question.not_found` | This question no longer exists. | Esta questão não existe mais. | Esta questão já não existe. |
| `question.topic_not_found` | The topic no longer exists. Pick another. | O tópico não existe mais. Escolha outro. | O tópico já não existe. Escolha outro. |
| `question.statement_required` | Write the statement. | Escreva o enunciado. | Escreva o enunciado. |
| `question.statement_too_long` | The statement has more than 10,000 characters. | O enunciado passa de 10.000 caracteres. | O enunciado excede 10 000 caracteres. |
| `question.base_text_too_long` | The base text has more than 20,000 characters. | O texto-base passa de 20.000 caracteres. | O texto de apoio excede 20 000 caracteres. |
| `question.explanation_too_long` | The explanation has more than 10,000 characters. | A explicação passa de 10.000 caracteres. | A explicação excede 10 000 caracteres. |
| `question.option_too_long` | An option has more than 2,000 characters. | Uma alternativa passa de 2.000 caracteres. | Uma opção excede 2 000 caracteres. |
| `question.options_count` | A question is published with 2 to 5 options. | Uma questão é publicada com 2 a 5 alternativas. | Uma questão é publicada com 2 a 5 opções. |
| `question.option_text_required` | Every option needs text. | Toda alternativa precisa de texto. | Todas as opções precisam de texto. |
| `question.key_invalid` | Mark exactly one option as correct. | Marque exatamente uma alternativa como correta. | Assinale exatamente uma opção como correta. |
| `question.difficulty_invalid` | Difficulty is a number from 1 to 5, or empty. | A dificuldade é um número de 1 a 5, ou vazia. | A dificuldade é um número de 1 a 5, ou vazia. |
| `question.content_language_invalid` | Choose one of the supported languages. | Escolha um dos idiomas suportados. | Escolha um dos idiomas suportados. |
| `question.delete_requires_draft` | Only a draft can be deleted. Unpublish the question first. | Só um rascunho pode ser excluído. Despublique a questão primeiro. | Só um rascunho pode ser eliminado. Retire primeiro a publicação. |
| `question.already_published` | This question is already published. | Esta questão já está publicada. | Esta questão já está publicada. |
| `question.not_published` | This question is not published. | Esta questão não está publicada. | Esta questão não está publicada. |

Existing keys reused as they are: `Common.Add`, `Common.Edit`, `Common.Delete`, `Common.Save`, `Common.Saving`, `Common.Cancel`, `Common.Close`, `Common.Search`, `Common.Actions`, `Common.More`, `Common.MoveUp`, `Common.MoveDown`, `Common.TryAgain`, `Common.Loading`, `Common.LoadFailed`, `Common.Filter.All`, `Common.Table.*`, `Common.ActionOnItem`, `Common.Delete.Title`, `Common.Delete.Confirm`, `Common.Discard.*`, `Common.ErrorSummary.Title`, `Common.Summary.NotFilled`, `Common.Checklist.Done`, `Common.Checklist.Pending`, `Nav.Section.Content`, `NotFound.*`, `common.unexpected_error`, `Lookup.*`. Texts that need a wider meaning because of BR13 (decision of 2026-10-10): `Topics.Delete.DisabledInUse` and `topic.in_use` today name only notice subjects.

### Decisions of the screen design
Settled on 2026-10-10 and recorded in `## Decisions`: difficulty direction (1 easiest, 5 hardest); Quill's icons replaced by `AppIcons` SVGs; the Api problem of `option_text_required` and `option_too_long` carries the option `position`; no table button in F-95; `topic.in_use` and the topic delete texts widened to "notice subjects or questions" with one `TopicResponse.InUse` flag; the preview shows the stored version only; `MudRadio` used directly in the option rows.

### Api and contracts
- Api, group `/api/v1/question-bank`, policy `questions.manage`:
  - `GET /questions?status=&q=&page=&pageSize=` — list.
  - `GET /questions/{id}` — one question with options and key.
  - `POST /questions` — create as `Draft`.
  - `PUT /questions/{id}` — update (replaces the content).
  - `POST /questions/{id}/publish`, `POST /questions/{id}/unpublish`.
  - `DELETE /questions/{id}` — soft delete of a draft.
- Catalog contracts gain two interfaces, so the modules never read each other's tables: `ITopicDirectory` (topic name and subject by id, for validation and the list) and `ITopicUsageCheck` (does anything use this topic?), implemented by `QuestionBank`; `DeleteTopicHandler` asks every registered check besides `IsMappedAsync`.
- Error codes: `question.not_found`, `question.topic_not_found`, `question.statement_required`, `question.statement_too_long`, `question.base_text_too_long`, `question.explanation_too_long`, `question.option_too_long`, `question.options_count`, `question.option_text_required`, `question.key_invalid`, `question.difficulty_invalid`, `question.content_language_invalid`, `question.delete_requires_draft`, `question.already_published`, `question.not_published`; reused `topic.in_use`.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they save a question with a topic, language and statement, then it is stored as `Draft`, appears in the list with its topic and subject, and the response carries the sanitized statement. (UC1, BR1, BR2, BR3)
- AC2 Given a draft being saved, when its statement is empty or only empty tags (`<p><br></p>`), then 400 `question.statement_required`; with no options and no key the draft is accepted. (BR3)
- AC3 Given text with `<script>`, an `onclick`, a `javascript:` link and a `data:` image in any of the four text fields, when it is saved, then the stored and returned HTML has none of them; `strong`, `sup`, `sub`, lists, a table and an `https` link survive. (BR8, BR8b)
- AC4 Given a question, when a sixth option is sent, then 400 `question.options_count`; when it is published with one option, then 400 `question.options_count`; an empty option text on publish is 400 `question.option_text_required`. (BR3, BR4)
- AC5 Given a draft with options, when it is published with no correct option or with two, then 400 `question.key_invalid`; with exactly one, it becomes `Published`. (BR4, UC3)
- AC6 Given a published question, when it is unpublished, then it is `Draft` again with the same content; publishing a published one answers 409 `question.already_published`, unpublishing a draft 409 `question.not_published`. (UC4, BR12)
- AC7 Given a published question, when the curator edits the statement, then it stays `Published` with the new sanitized text; an edit that leaves one option is refused and the stored question is unchanged. (UC2, BR4)
- AC8 Given a draft, when it is deleted, then it leaves the list and its rows stay with `IsDeleted` true; given a published question, then the delete action is disabled with the reason and a `DELETE` answers 409 `question.delete_requires_draft`. (UC7, BR11)
- AC9 Given a topic with one live question of any status, when it is deleted, then 409 `topic.in_use` and its delete action is disabled with the reason; after the question is deleted the topic can be deleted; moving the topic to another subject is allowed. (UC8, BR13)
- AC10 Given a user without `questions.manage`, then every endpoint answers 403, the menu item is absent and the pages deny; a new `Curator` has the permission and `Admin` too. (BR14)
- AC11 Given questions in several statuses, when the curator filters by status and types a word of a statement ignoring case and accents, then only the matches are listed, paginated, newest first; with none, the empty-search state shows. (UC5, BR10, BR15)
- AC12 Given a question, when the curator opens the preview, then it shows the statement, the base text and the options lettered A to E from the stored HTML, with the key and explanation hidden until "show answer". (UC6, BR5)
- AC13 Given a difficulty of 0 or 6, then 400 `question.difficulty_invalid`; empty and 1 to 5 are accepted. (BR6)
- AC14 Given a language outside `SupportedLanguages`, then 400 `question.content_language_invalid`; `pt-br` is stored as `pt-BR`; the form preselects the UI language. (BR7)
- AC15 Given a statement over its limit, then 400 `question.statement_too_long` and nothing is truncated; the same for the other three fields with their own codes. (BR9)
- AC16 Given the architecture rules, then `QuestionBank` references other modules only through their `Contracts`, its `Domain` does not depend on EF Core or ASP.NET, the vocabulary test passes on its assemblies, and the architecture overview lists the module. (profile)
- AC17 The visual editor is operable by keyboard, has an accessible name and announces its validation error (WCAG 2.2 AA); checked on screen. (UC1)
- AC18 All new texts appear in pt-BR, pt-PT and en, including every error code text.

## Criterion → test

Tests locked: 6592781d7c702e6a5789f76423d53cccbf2b2789 (8 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `QuestionEndpointTests.Create_TopicLanguageAndStatement_IsStoredAsDraft_AndListedWithItsTopicAndSubject`, `QuestionTests.Create_ValidContent_IsADraftOfTypeSingleChoice` |
| AC2 | `QuestionEndpointTests.Create_EmptyStatement_IsRefused`, `QuestionEndpointTests.Create_DraftWithNoOptionsAndNoKey_IsAccepted`, `QuestionTests.Create_StatementEmptyAfterTags_IsRefused`, `QuestionHtmlSanitizerTests.Sanitize_NothingButEmptyTags_HasNoPlainText` |
| AC3 | `QuestionHtmlSanitizerTests` (script, event attribute, javascript link, data image, allowed markup, allow-list pin), `QuestionEndpointTests.Create_HostileHtmlInEveryField_IsStrippedAndTheAllowedMarkupSurvives` |
| AC4 | `QuestionEndpointTests.Create_SixOptions_IsRefusedWithOptionsCount`, `QuestionPublicationEndpointTests.Publish_OneOption_IsRefusedWithOptionsCount`, `QuestionPublicationEndpointTests.Publish_EmptyOptionText_IsRefusedWithOptionTextRequired` |
| AC5 | `QuestionPublicationEndpointTests.Publish_NoKeyOrTwoKeys_IsRefusedWithKeyInvalid`, `QuestionPublicationEndpointTests.Publish_CompleteDraft_BecomesPublished` |
| AC6 | `QuestionPublicationEndpointTests.Unpublish_ReturnsToDraft_WithTheSameContent`, `QuestionPublicationEndpointTests.Publish_AlreadyPublished_Is409AlreadyPublished`, `QuestionPublicationEndpointTests.Unpublish_Draft_Is409NotPublished` |
| AC7 | `QuestionEndpointTests.Update_Published_StatementChange_StaysPublishedWithTheNewSanitizedText`, `QuestionEndpointTests.Update_Published_LeavingOneOption_IsRefused_AndTheStoredQuestionIsUnchanged`, `QuestionTests.Update_Published_LeavingOneOption_IsRefused_AndNothingChanges` |
| AC8 | `QuestionEndpointTests.Delete_Draft_LeavesTheList_AndSoftDeletesTheQuestionAndItsOptions`, `QuestionEndpointTests.Delete_Published_IsRefusedWithDeleteRequiresDraft`; screen part (disabled action with its reason): pending (screen test) |
| AC9 | `TopicDeleteGuardTests` (draft, published, list flag, after delete, move); screen part (disabled action with its reason): pending (screen test) |
| AC10 | `QuestionEndpointTests.EveryEndpoint_WithoutThePermission_AnswersForbidden`, `QuestionEndpointTests.ACurator_HasThePermission_WithoutBeingAnAdmin`; menu and page: pending (screen test) |
| AC11 | `QuestionEndpointTests.List_FiltersByStatusAndSearchesTheStatementIgnoringCaseAndAccents`, `QuestionEndpointTests.List_IsPagedNewestFirst`, `QuestionEndpointTests.List_UnknownStatus_IsIgnored` |
| AC12 | pending (screen test) |
| AC13 | `QuestionEndpointTests.Create_DifficultyOutsideTheRange_IsRefused`, `QuestionEndpointTests.Create_DifficultyInsideTheRangeOrEmpty_IsAccepted` |
| AC14 | `QuestionEndpointTests.Create_UnsupportedLanguage_IsRefused_AndALowercaseOneIsStoredCanonical`; preselected UI language: pending (screen test) |
| AC15 | `QuestionEndpointTests.Create_OverTheLimit_IsRefusedWithEachFieldsOwnCode`, `QuestionEndpointTests.Create_TextAtTheLimit_IsStoredWhole`, `QuestionTests.Create_StatementOverTheLimit_IsRefused` |
| AC16 | pending (`ModuleBoundaryTests`, `SolutionLayoutTests`, `VocabularyTests` and `ArchitectureOverviewTests` once the module is registered in them) |
| AC17 | validation step |
| AC18 | pending (`ErrorCodeTextTests`, the missing-key test and `RoleResourcesTests` over the new keys) |


## Decisions
- 2026-10-09 — New module `QuestionBank`, five projects, references `Topic` by id through `Catalog.Contracts`; only `Draft` and `Published`; first release cut F-95 to F-99 (owner, epic decisions).
- 2026-10-10 — Question text is sanitized HTML, not Markdown — owner chose the visual editor after reading the comparison; a visual editor produces HTML, and measured Markdig `DisableHtml()` still emits `javascript:` links and `data:` images (owner, refinement; `docs/features/mockups/F-95-content-format-comparison.md`).
- 2026-10-10 — Editor: visual (WYSIWYG) — owner chose "Visual editor" over the recommended "Markdown text box with preview" (average 5.3 vs 6.5, gap 1.3: nothing asked); kept.
- 2026-10-10 — New packages, for the owner's yes at approval: `HtmlSanitizer` 9.2.1039 (MIT, 2026-08-28) and `Blazored.TextEditor` 1.1.3 (MIT, wraps Quill 2.0.3, 2025-04-11), both checked against the NuGet registry on 2026-10-10; no test package is needed (the sanitizer is a plain unit test, the editor sits behind `AppRichTextField` and bUnit renders the component with a fake) (Claude).
- 2026-10-10 — From 2 to 5 options, letters from position — same as Simulae T1; ENEM and most exams use 4 or 5 (owner, refinement).
- 2026-10-10 — Difficulty 1 to 5, optional — same as Simulae `DificuldadeEstimada`, so the import loses nothing (owner, refinement).
- 2026-10-10 — Publish needs statement, 2+ options with one correct, topic and language; explanation and difficulty optional — past-exam questions rarely have an explanation (owner, refinement).
- 2026-10-10 — `Draft` ↔ `Published`, a published question is edited in place, only drafts are deleted — nothing consumes questions yet and F-98 adds versions before any attempt exists (owner, refinement).
- 2026-10-10 — Permission `questions.manage`, granted to `Curator` at creation, `Admin` through the union; review gets its own in F-105 (owner, refinement).
- 2026-10-10 — The list offers status and statement search only; the rest is F-100 (owner, refinement).
- 2026-10-10 — Profile line 11 is corrected and ADR-0005 records `QuestionBank` as its own module — the profile still says Catalog holds the question bank, which the 2026-10-09 decision replaced (Claude, premise verified in `docs/agile/profile.md`).
- 2026-10-10 — The topic guard is an inverted dependency: `Catalog.Contracts` declares `ITopicUsageCheck`, `QuestionBank` implements it, `DeleteTopicHandler` asks every check — Catalog never references `QuestionBank`, so no cycle and each later item with a topic reference adds its own check without editing Catalog (Claude).
- 2026-10-10 — A draft needs a topic and a language — the list shows both and the delete guard (BR13) needs the topic; a topic is always known for a question a curator has started (Claude).
- 2026-10-10 — A topic with only draft questions is also protected — otherwise a draft would point at a deleted topic and silently lose its classification (Claude).
- 2026-10-10 — The Api sanitizes, the Web shows what is stored — one place decides what is safe; the Web never calls the sanitizer (Claude).
- 2026-10-10 — The editor's list markup is a build spike: Quill 2 writes bullet lists as `<ol><li data-list="bullet">`, and a sanitizer that drops `data-*` would turn them into numbered lists. First build step measures what `Blazored.TextEditor` returns and sets the normalization (BR8) before anything else is built (Claude).
- 2026-10-10 — Quill's script and style are hosted by the app, not loaded from a CDN — the Web sets security headers and works offline in development; the build confirms the package allows it, else raises it as a stop (Claude).
- 2026-10-10 — Route group `/api/v1/question-bank/questions`, like `/api/v1/catalog/...` (Claude).
- 2026-10-10 — Difficulty hint reads "1 (easiest) to 5 (hardest)" — same direction as Simulae `DificuldadeEstimada`, so the import inverts nothing (owner, screen design).
- 2026-10-10 — No table button in the editor in F-95; pasted or imported tables are kept by the sanitizer and shown in the preview — Quill 2's table module is unverified and F-96 imports tables as HTML; the button is captured as an idea (owner, screen design).
- 2026-10-10 — The preview renders the stored, sanitized version only, with a note when the form has unsaved changes — one place (the Api) decides what is safe (owner, screen design).
- 2026-10-10 — Preview and Publish/Unpublish sit in a "Publication" card below the form, Publish disabled with the reason as visible text while changes are unsaved or the checklist is incomplete — the `ExamEditionsSection` pattern; Save never publishes (owner, screen design).
- 2026-10-10 — Quill's toolbar icons are replaced by `AppIcons` Material Outlined SVGs registered by the kit at init — one icon family rule (`ui-project`); checked at build (Claude).
- 2026-10-10 — The Api problem for `question.option_text_required` and `question.option_too_long` carries the option `position`; the client pre-checks both, so the section-level message is only a fallback (Claude).
- 2026-10-10 — `topic.in_use` and `Topics.Delete.DisabledInUse` are reworded to "notice subjects or questions use this topic" in the three languages; `TopicResponse.InUse` stays one flag (Claude).
- 2026-10-10 — `MudRadio` is used directly in the option rows; a second use in another page promotes it to an `AppRadio` kit component (Claude).
- 2026-10-10 — F-95 approved ("aprovo F-95"), with the screen (`docs/features/mockups/F-95-single-choice-questions.html`) and the two new packages `HtmlSanitizer` and `Blazored.TextEditor` (owner).
- 2026-10-10 — Vocabulary: pt-BR "Alternativa" / pt-PT "Opção"; pt-BR "Despublicar" / pt-PT "Retirar publicação"; the glossary rows are added at build (Claude).
- 2026-10-10 — Design passes (`system-design`, then `architect`), verified against the files (`ModuleBoundaryTests.cs:27-37,99-109`, `RouteMapDoc.cs:90-91`, `RoleResourcesTests.cs:38-42`, `DeleteTopicHandler.cs:22`): accepted. Application references only its own Contracts and Domain, so `Catalog.Contracts` is used from Infrastructure; Catalog implements `ITopicDirectory`, `QuestionBank` implements `ITopicUsageCheck` (the item's "implemented by QuestionBank" reads for the check only: only Catalog can read `catalog.topics`) (Claude).
- 2026-10-10 — No unique index on `(question_id, position)`: the options are written only through `Question`, and a move swaps two live positions, which a non-deferrable unique index refuses depending on statement order; a plain index on `question_id` stays (architect) (Claude).
- 2026-10-10 — The sanitizer step returns the sanitized HTML and its plain text together, and the Domain only judges length and emptiness; one parser decides "empty" (architect) (Claude).
- 2026-10-10 — The option `position` on the problem goes through an Application result (`QuestionSaveFailure`) mapped by the endpoint as a problem extension; `SharedKernel.Error` is not changed (system-design, architect) (Claude).
- 2026-10-10 — Two columns for the statement text: `statement_text` (plain, for the list excerpt) and `normalized_statement` (for the search), the `Name`/`NormalizedName` pattern of `Topic`; BR10 is read that way (system-design open point 1) (Claude).
- 2026-10-10 — List query parameter is `search` (as Catalog), an unknown `status` value is ignored, a null `Options` array is an empty one, an empty option text is allowed on a draft (system-design open points 2, 3, 7, 8) (Claude).
- 2026-10-10 — The sanitizer is a concrete class in Application (no interface with one implementation); wrappers it does not allow (`span`, `div`, `h1`) keep their text, `script`, `style`, `iframe`, `object`, `embed`, `svg` and `math` lose it (Claude).
- 2026-10-10 — DocGen's route area name joins hyphenated segments in PascalCase (`question-bank` becomes `QuestionBank`, beside its model folder); no existing area has a hyphen, so no current folder moves; one unit test in `RouteMapDocTests` (architect) (Claude).
- 2026-10-10 — Registration sweep also carries the resource keys `Permission.questions.manage.Name`, `.Description` and `PermissionGroup.questions` in the three languages, `ThreadPoolMinimumTests` in the new test project, and table and column comments (architect) (Claude).

## Out of scope
- Images in statement, base text and options — F-103 (`IFileStorage`).
- One base text shared by several questions — F-102; here the base text is inline in the question.
- Question versions and answer-key change history — F-98. Until then an edit of a published question overwrites it.
- Origin: edition, number in the paper, notice subject, source line — F-96.
- Filters by subject, topic, organizer, edition, type, difficulty — F-100.
- Annulment — F-101. Review queue and `InReview` — F-105.
- Other question types — F-97, F-104, F-106 to F-109.
- A per-question "may shuffle options" flag — E-5, because options such as "all of the above" depend on order.
- Anything a student sees: the student side belongs to E-5 and E-6.
- Importing Simulae questions and seeded sample questions — F-96 and F-110.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Events

- 2026-10-10 test-fail — 1 new warning(s): CA1822 in src/Modules/QuestionBank/Simulab.QuestionBank.Application/Content/QuestionHtmlSanitizer.cs, 1 red

## Delivery
<!-- Filled by /agile:ship. -->
