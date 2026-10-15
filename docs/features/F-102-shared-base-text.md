---
feature: F-102
epic: Question bank
status: building
board: 148
version: 1
---
# Shared base text

Technical terms: [glossary](../glossary.md)

## Summary
One `BaseText` (a reading passage, a table, a case) serves several questions, as in reading comprehension blocks: the curator writes it once and links the questions to it; the simulators show it beside each of them. F-95 keeps only an inline base text per question. Source: epic Question bank (E-4), glossary `BaseText`.

## Start
- Depends on: F-95 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: F-104.

## Goal
A curator writes a passage once and links every question that refers to it, so a reading block of ten questions is one text that is fixed in one place, not ten copies that drift apart.

## What exists (verified in the code, 2026-10-15)
- `Question.BaseTextHtml` is an inline, optional field: column `question_bank.questions.base_text_html`, 20 000 characters of sanitized HTML (`QuestionLimits.BaseTextMaxLength`); error `question.base_text_too_long`. F-95.
- `QuestionVersion.BaseTextHtml` copies it into the immutable snapshot (F-98 BR1); `Question.NextVersion` compares it with the latest version; an annulled question is frozen (F-101 BR5).
- `SaveQuestionRequest.BaseTextHtml`, `QuestionResponse.BaseTextHtml`, `QuestionVersionResponse.BaseTextHtml`; the form section "Base text" (`AppRichTextField Id="question-base-text"`), the preview (`QuestionPreviewDialog`) and the transcription screen (F-99 BR3 clears it after each save) read that field only.
- There is no `BaseText` entity, table, route or screen. The glossary already defines `BaseText` ("Text or image shared by one or more questions").
- Permission `questions.manage` gates every question route and screen (F-95 BR14). A topic in use is protected by an inverted usage check (ADR-0005 #3).

## Users and use cases
- UC1 A curator creates a base text: a title, a content language and the text, written in the visual editor.
- UC2 A curator edits a base text; every published question linked to it gets a new version.
- UC3 A curator links a question to a base text in the question form, changes the link or removes it.
- UC4 A curator opens a base text and sees the questions linked to it.
- UC5 A curator finds base texts by title or language and deletes one that no question uses.
- UC6 A curator transcribes a paper: the base text chosen for one question stays chosen for the next, until it is changed.

## Business rules
- BR1 A base text has a title (1 to 200 characters after trimming), a content language (one of the app's languages, canonical, never translated) and a text of sanitized HTML, required, up to 20 000 characters of sanitized HTML. A refused value is never truncated. Global data: `TenantId` is null in v1.
- BR2 A question has at most one base text, held by `BaseTextId`; none is the normal case. The inline field is gone: a question never carries base text of its own.
- BR3 A question and its base text are in the same content language. Linking a question of another language, or changing the language of a question that is linked, is refused (`question.base_text_language_mismatch`); the language of a base text that has linked questions cannot change (`base_text.language_in_use`).
- BR4 Editing the text of a base text records, in the same transaction, a new version (F-98) of every linked question that is `Published` and whose latest version holds a different base text HTML: same content, the new base text HTML, not an answer key change, no reason. A `Draft` follows the live text and makes no version; an `Annulled` question is frozen and makes none (F-101 BR5). Editing only the title makes no version (it is not versioned content).
- BR5 What a student was shown stays what the version says: a version still copies the base text HTML (F-98 BR1 is unchanged), and a version already recorded is never touched.
- BR6 A base text that any live question links, whatever its status, cannot be deleted (`base_text.in_use`, 409); the curator removes the links first. A base text with no question is deleted softly, as the other bank data.
- BR7 A link to a base text that is unknown or deleted answers 404 `question.base_text_not_found` before the content is judged, as the topic does (F-95 BR16).
- BR8 The text is sanitized by the Api on every write against the one allow-list of the bank (ADR-0005 #5); the Web shows what was stored.
- BR9 Where a question shows its base text (preview, student preview), the linked text is shown before the statement, as the inline text was (F-95 BR5).
- BR10 Existing data: every question with a non-empty inline base text (live or soft-deleted) gets one `BaseText` of its own, linked to it: title = the first 80 characters of the plain text, language = the question's, text = the stored HTML; then the inline column is dropped; a base text made from a soft-deleted question is soft-deleted too, with that question's deletion date. No version is made (the HTML is the same) and no two texts are merged by the migration.
- BR11 The list of base texts shows title, language and the number of linked questions, sorted by title; it filters by language and searches by title. The detail shows the questions linked (excerpt, status, paper number, a link to the question), most recently changed first.
- BR12 Every route and screen of base texts needs `questions.manage`, checked by the endpoint and by the screen (menu, page, action), by permission (F-95 BR14).
- BR13 The transcription screen (F-99) keeps the chosen base text across saves, as it keeps the edition, notice subject and content language; the type, statement, difficulty and key are still cleared (this changes F-99 BR3 for the base text only). If the question's language is changed to one the text does not have, the choice is cleared with a message; the question form does the same (screen design, 2026-10-15).

## Screens and API
- `/admin/base-texts` — [new] the list: search by title, language filter, columns of BR11, create, open, delete (refused with the reason when in use).
- `/admin/base-texts/new` and `/admin/base-texts/{id}` — [new] the form: title, language, text (`AppRichTextField`), the linked questions (detail only), delete. One way to edit.
- `/admin/questions/new`, `/admin/questions/{id}` — [changed] the "Base text" section: a picker of the base texts in the question's language, "open" and "remove link", and a link to create a new one; the inline editor is removed. Details of the pick-and-create flow are settled by `/agile:screen`.
- `/admin/questions/{id}` preview, transcription screen (F-99) — [changed] as BR9 and BR13.
- Routes under `/api/v1/question-bank`, gated by `questions.manage`:
  - `GET /base-texts` (`search`, `language`, `page`, `pageSize`) → page of `BaseTextListItemResponse(Id, Title, ContentLanguage, QuestionCount, UpdatedAt)`.
  - `GET /base-texts/{id}` → `BaseTextResponse(Id, Title, ContentLanguage, TextHtml, QuestionCount, UpdatedAt)`.
  - `POST /base-texts`, `PUT /base-texts/{id}` with `SaveBaseTextRequest(Title, ContentLanguage, TextHtml)`; `DELETE /base-texts/{id}`.
  - `GET /base-texts/{id}/questions` → page of the question list items linked to it.
  - `SaveQuestionRequest.BaseTextHtml` is replaced by `BaseTextId` (nullable); `QuestionResponse.BaseTextHtml` by `BaseText` (`Id`, `Title`, `TextHtml`; null when none). `QuestionVersionResponse.BaseTextHtml` is unchanged.
- Migration `question_bank`: table `base_texts` (id, tenant, title, content language, text html, audit and soft-delete columns, column comments), `questions.base_text_id` with a foreign key inside the module that restricts delete and an index, the backfill of BR10, `base_text_html` dropped from `questions`.
- Error codes: `base_text.not_found`, `base_text.title_required`, `base_text.title_too_long`, `base_text.text_required`, `base_text.text_too_long`, `base_text.content_language_invalid`, `base_text.in_use`, `base_text.language_in_use`, `question.base_text_not_found`, `question.base_text_language_mismatch`; `question.base_text_too_long` leaves with the inline field.

Mockup: [F-102-shared-base-text.html](mockups/F-102-shared-base-text.html). Details of every screen and state: `## Screen design`.

### Wireframe: Base texts
```text
+----------------------------------------------------------------------+ [new]
| Content / Base texts                                                 |
| Base texts                                                 [ + Add ] |
| [Q Search by title_____________________]  LANGUAGE [All languages v] |
| TITLE                                   LANGUAGE        QUESTIONS    |
| Caso clinico - UBS do bairro Esperanca  Portugues (Br.)         3  [edit][delete: off, "in use"]
| Grafico - Taxa de desemprego por regiao Portugues (Br.)         1  [edit][delete: off]
| Tabela de tarifas de energia 2025       Portugues (Br.)         0  [edit][delete]
| Text 1 - The Great Pacific Garbage Patch English                5  [edit][delete: off]
| Rows per page: 25                                      1-6 of 6      |
+----------------------------------------------------------------------+
```

### Wireframe: Base text
```text
+----------------------------------------------------------------------+ [new]
| Content / Base texts / Edit base text                                |
| Edit base text                                                       |
| +- Base text ------------------------------------------------------+ |
| | TITLE *                            CONTENT LANGUAGE *            | |
| | [Texto I - A barragem de Tres Marias] [Portugues (Brasil) v] off | |
| | Up to 200 characters...            Locked while questions use    | |
| |                                    this text (8)...              | |
| | (i) Saving a change to the text records a new version of each    | |
| |     published question that uses it; drafts and annulled ...     | |
| | TEXT *                                                           | |
| | [B I U S | x2 x2 | lists quote link | clear]                     | |
| | [ visual editor ...................................... ]        | |
| |                                          Cancel   [ Save ]       | |
| +------------------------------------------------------------------+ |
| +- Questions that use this text (8) -------------------------------+ |
| | STATEMENT                         SOURCE              STATUS     | |
| | De acordo com o texto, a barr...  FGV - CEMIG 2024    Published [edit]
| |                                   Question 12                    | |
| | O termo "cota", no terceiro ...   FGV - CEMIG 2024    Draft     [edit]
| +------------------------------------------------------------------+ |
| +- Delete this base text ------------------------------------------+ |
| | [ Delete ] (off)  Questions use this text (8). Remove their      | |
| |                   links before deleting it.                      | |
| +------------------------------------------------------------------+ |
+----------------------------------------------------------------------+
```

### Wireframe: Question form, base text section
```text
+----------------------------------------------------------------------+ [changed]
| Base text                                                            |
| A text shared by several questions, shown before the statement.      |
| BASE TEXT                                                            |
| [Q Texto I - A barragem de Tres Marias                         (x)]  |
|   picker open: Texto I - A barragem de Tres Marias                   |
|                Questions: 8 - changed on 10/14/2026                  |
|                Caso clinico - UBS do bairro Esperanca                |
|                Questions: 3 - changed on 10/02/2026                  |
| Only texts in Portugues (Brasil), the language of this question.     |
| Open the base text [->]     v Show the text                          |
| Not in the list? Create a base text [->]                             |
| It opens in a new tab, so this question stays as it is. Save the     |
| text there, come back and pick it here.                              |
| (i) after a language change: The base text "Texto I - ..." was       |
|     removed: it is in Portugues (Brasil), and this question is now   |
|     in English.                                                      |
+----------------------------------------------------------------------+
```

### Wireframe: Transcription, the base text kept
```text
+----------------------------------------------------------------------+ [changed]
| (ok) Question saved. Keep going.                                     |
| FGV - CEMIG 2024 - Analista          Question 4 of this session      |
| ...                                                                  |
| BASE TEXT                                                            |
| [Q Texto I - A barragem de Tres Marias                         (x)]  |
| Stays chosen for the next question. Change or remove it when the     |
| block of questions ends.                                             |
| STATEMENT * [ blank ]   TYPE, DIFFICULTY, KEY: blank                 |
|                                        Cancel  [ Save and continue ] |
+----------------------------------------------------------------------+
```

### Wireframe: Delete confirmation
```text
+--------------------------------------------+ [new]
| Delete item?                               |
| "Tabela de tarifas de energia 2025" leaves |
| the list and the question picker. No       |
| question uses it.                          |
|            Cancel  [ Delete this base text ] (error colour)
+--------------------------------------------+
```

## Screen design
Mockup: [F-102-shared-base-text.html](mockups/F-102-shared-base-text.html) (state, language and theme switches). Kit components of `Simulab.Web/Components/Ui` and `AppIcons` only: no new kit component, no new icon, no new colour (every colour is a token of `docs/design/identity.tokens.json`, light and dark).

### Navigation and permissions
- Menu: a new entry `Nav.BaseTexts` in the Content section, right after Questions, route `/admin/base-texts`, icon `AppIcons.BaseText` (the icon the question form's Base text section already uses), `RequiredPermission: QuestionBankPermissions.Manage`.
- Both new pages carry `[Authorize(Policy = PermissionPolicy.Prefix + QuestionBankPermissions.Manage)]`, as the question pages do. Without the permission the entry is not rendered and the route shows the not-found page (F-2 BR7). No button on these pages has a permission of its own: `questions.manage` covers list, create, edit and delete (BR12).

### Screen 1: `/admin/base-texts` (`BaseTexts.razor`)
Top to bottom:
1. `AppPageHeader`: title `BaseTexts.Title`; breadcrumb `Nav.Section.Content` / `BaseTexts.Title`; primary action `Common.Add` with `AppIcons.Add`, which opens `/admin/base-texts/new`. The shared word is "Add" (rule ui: one term per action), not the wireframe's "New base text".
2. `AppAlert` Error, only after a refused delete (`base_text.in_use`, `base_text.not_found`, unexpected), through `ErrorText`.
3. `AppDataTable<BaseTextListItemResponse>`: server paging with the kit page sizes, `Searchable`, `SearchPlaceholder` `BaseTexts.Search.Placeholder` (search by title, BR11).
   - `ToolBarContent`: `AppSelectField<string?>` `base-texts-filter-language`, label `BaseTexts.Filter.Language`. The first option is `BaseTexts.Filter.Language.All` (null), then each language of `SupportedCultures.All` in its own native name, as the question form's language select shows them. It is a select because the list is fixed and short (rule ui-project, F-114). A change reloads from the first page.
   - Columns, none sortable, with rows sorted by title (BR11):
     - Title: `AppTruncatedText` 32rem with `Lang` = the content language. The whole title is the tooltip.
     - Language: the native name in a `span lang`.
     - Questions: `AppNumberColumn`, right-aligned, in the user's culture.
   - `RowActions`: `AppRowActions` with `ItemName` `BaseTexts.Row.Name` (the title cut at `QuestionText.RowNameLength`) and `IdPrefix` `base-text-actions-{id}`.
     - Edit opens `/admin/base-texts/{id}`.
     - Delete opens Screen 4. While `QuestionCount > 0`, `DeleteDisabledReason` is `BaseTexts.Delete.DisabledInUse` with the count (BR6).
   - The title is plain text, not a link. There is no read-only detail page, and the form is reached by Edit (rule ui: the name links only to a read-only detail).

States:
- Loading: the table's own.
- Ready.
- Empty: `BaseTexts.Empty` with `Common.Add`.
- Empty by filter: `BaseTexts.Empty.Filters`.
- Empty by search: `BaseTexts.Empty.Search`.
- Error: `Common.LoadFailed` with `Common.TryAgain`.
- Delete confirmation.
- Deleted: snackbar `BaseTexts.Deleted`, and the table reloads.
- Delete refused: a question was linked meanwhile, so the Api answers 409. The alert shows `base_text.in_use`, and the table reloads so the row shows its count and a disabled Delete.
- Long title: one line, truncated, with the whole title as tooltip.
- Permission denied: the not-found page, with no menu entry.

### Screen 2: `/admin/base-texts/new` and `/admin/base-texts/{id}` (`BaseTextForm.razor`)
One column and no aside, because three fields leave nothing to summarise. Top to bottom:
1. `AppPageHeader`:
   - Title: `BaseTexts.Form.AddTitle` or `BaseTexts.Form.EditTitle`.
   - Breadcrumb: Content / Base texts (a link to the list) / the title.
   - No header action: Save is the page's only filled button, as on the question form.
2. Form card (`MudPaper` `app-form-card`):
   - `AppErrorSummary` `base-text-error-summary`: shown and focused after a refused save, one line per field.
   - `AppAlert` Error: a refusal that no field owns (`base_text.not_found` on save, unexpected).
   - `AppSectionCard` `base-text-section-text`: title `BaseTexts.Section.Text`, icon `AppIcons.BaseText`, subtitle `BaseTexts.Section.Text.Subtitle`. It holds:
     - `AppFormGrid Columns="2"` with two fields:
       - `AppTextField<string>` `base-text-title`, label `BaseTexts.Field.Title`, required, `MaxLength` 200, placeholder `BaseTexts.Field.Title.Placeholder`, hint `BaseTexts.Field.Title.Hint`. It is checked on leaving the field and on save: blank after trim gives `base_text.title_required`, over 200 gives `base_text.title_too_long`.
       - `AppSelectField<string>` `base-text-content-language`, label `BaseTexts.Field.ContentLanguage`, required, `LeadingIcon` `AppIcons.Language`, the three languages by native name.
         - New: `?language=<tag>` preselects a valid language (the question form's Create link passes it). Otherwise the default content language applies (`QuestionText.DefaultContentLanguage()`).
         - Edit with `QuestionCount > 0`: `Disabled`, hint `BaseTexts.Field.ContentLanguage.Locked` with the count (BR3). Otherwise hint `BaseTexts.Field.ContentLanguage.Hint`.
         - `base_text.content_language_invalid` and `base_text.language_in_use` show on this field.
     - `AppAlert` Info `BaseTexts.Versions.Notice`, above the editor, only when editing a text with `QuestionCount > 0` (BR4).
     - `AppRichTextField` `base-text-text`, label `BaseTexts.Field.Text`, required, placeholder `BaseTexts.Field.Text.Placeholder`, hint `BaseTexts.Field.Text.Hint`, `Lang` = the chosen content language. A text that is blank once tags are removed gives `base_text.text_required`, and over 20 000 gives `base_text.text_too_long`. The value is never cut (BR1).
   - `AppFormActions`: `Saving`, `HasChanges` (title, language and normalized HTML against what is stored), `OnSave`, `OnCancel` (back to the list).
3. Edit only, card `base-text-section-questions`: `AppSectionCard` titled `BaseTexts.Section.Questions` (the count as `{0}`), icon `AppIcons.Questions`, subtitle `BaseTexts.Section.Questions.Subtitle`. Inside, an `AppDataTable<QuestionListItemResponse>` with `Searchable="false"`, server paging, most recently changed first (BR11).
   - Columns:
     - Statement: `AppTruncatedText` 26rem, `Lang`.
     - Source: the cell of the Questions list, the source line with `Questions.Source.Number` (the paper number) under it. An authored question shows a dash and the hidden text `Questions.Source.Authored`.
     - Status: `AppStatusChip` with `QuestionText.StatusTone`.
   - Row action Edit opens `/admin/questions/{id}` in the same tab. The form's `NavigationLock` asks first when there are unsaved changes.
   - Empty: `BaseTexts.Questions.Empty`, with no button (screen open point 4).
   - Error: `Common.LoadFailed` with `Common.TryAgain`.
4. Edit only, card `base-text-section-delete`: `AppSectionCard` titled `BaseTexts.Section.Delete`, icon `AppIcons.Delete`, subtitle `BaseTexts.Section.Delete.Subtitle`.
   - It holds a `MudButton` Outlined with `Color.Error`: `Common.Delete` with `AppIcons.Delete`, id `base-text-delete`. This is the question form's Annul button pattern.
   - While `QuestionCount > 0` the button is `Disabled`. The reason `BaseTexts.Delete.DisabledInUse` is written in a paragraph under it and tied by `aria-describedby`, because a disabled button cannot take focus.

What Save does:
- New (201): snackbar `BaseTexts.Saved`, a render, then `NavigateTo("/admin/base-texts/{id}", replace: true)`. This is the question form's F-35 order, so the lock does not ask about what was just saved.
- Edit (200): snackbar. The form shows what was stored, including the sanitized HTML (BR8), and the linked list reloads.

What Delete does:
- Screen 4's confirmation comes first.
- 204: snackbar `BaseTexts.Deleted`, then the list, without the discard question (the confirmation already said what goes).
- 409 `base_text.in_use`: the error alert at the top, and the page reloads the text so the count, the lock and the disabled Delete are current.

States:
- New: empty fields, language preset.
- Edit, in use: language locked, versions notice shown, Delete disabled with its reason.
- Edit, not in use: language free, Delete enabled.
- Loading: `AppLoadingState`.
- Load error: `AppErrorState` with retry.
- Not found: `AppAlert` `base_text.not_found` and `AppLink` `BaseTexts.BackToList`.
- Validation error: the summary and the field errors.
- Server refusal at a field: `base_text.language_in_use`, when a question was linked while the language was being changed.
- Saving: Save disabled, with progress.
- Unsaved changes: leaving asks `Common.Discard.*` (kit `NavigationLock`, inside the app and on closing the tab).
- Saved: snackbar.
- Linked questions: loading, empty or error.
- Delete confirmation, and delete refused.
- Long text: a 200-character title wraps in the page title and in the breadcrumb, and a long unbroken word in the text breaks (`overflow-wrap: anywhere`, kit).
- Permission denied.

### Screen 3: the question form's "Base text" section (changed)
The section stays where it is today, after Statement: `AppSectionCard` `question-section-base-text`, title `Questions.Section.BaseText`, icon `AppIcons.BaseText`, subtitle `Questions.Section.BaseText.Subtitle` (new value). The inline `AppRichTextField` is removed.

The picker:
- `AppLookupField` `question-base-text`, label `Questions.Field.BaseText`, placeholder `Questions.Field.BaseText.Placeholder` (new value). It has `MinChars="0"` and `OpenOnFocus="true"`, as the edition field does. The kit clear button removes the link. The field is disabled while the question is annulled.
- It is a lookup and not a select: the bank may hold hundreds of texts (BR10 makes one per existing inline text), so the server searches them (rule ui-project, F-114).
- `SearchAsync` calls `GET /base-texts?search=<term>&language=<the form's content language>&pageSize=20`, so only texts in the question's language are offered (BR3, AC13).
- Each option shows the title as text, with `Questions.BaseText.Option.Secondary` (linked-question count and last change date) as its secondary line. Two texts with the same title, which BR10 makes likely, can still be told apart.
- Hint `Questions.Field.BaseText.Hint`, with the native name of the question's language, says why a text in another language is not listed.
- No results show the kit's `Lookup.NoResults`. A failed search shows the kit's `Lookup.LoadFailed` with `Common.TryAgain`.

When a text is chosen, a row under the field holds:
- `AppLink` `Questions.BaseText.Open` to `/admin/base-texts/{id}`, with `target="_blank"`, `rel="noopener"` and `AppIcons.OpenInNew` (`aria-hidden`). Its accessible name is `Common.LinkOpensNewTab` with the visible text. This is the pattern `CatalogExam.razor` uses for the notice link.
- `AppDisclosure` `question-base-text-preview`, with `Questions.BaseText.Show` and `Questions.BaseText.Hide`. It shows the text read only, in its `lang`, followed by `Questions.BaseText.ReadOnlyNote`.
  - For the stored link it uses `QuestionResponse.BaseText.TextHtml`.
  - After a new pick it reads `GET /base-texts/{id}` the first time it is opened.

Under that, unless the question is annulled:
- `Questions.BaseText.Create.Lead`, then `AppLink` `Questions.BaseText.Create` to `/admin/base-texts/new?language=<content language>`. It also opens in a new tab, with `AppIcons.OpenInNew` and the `Common.LinkOpensNewTab` name.
- The help line `Questions.BaseText.Create.Help`.

The pick-and-create flow:
1. Create opens the base text form in a new tab, preset to the question's language. The unsaved question stays in its own tab as it was.
2. The curator saves the text there (Screen 2).
3. Back in the question tab, the curator types in the picker or opens it. The lookup asks the server on every search and caches nothing, so the new text is listed.

There is no create dialog: a second form would be a second way to edit a base text (rule ui-project; decision of 2026-10-15, "the question form only picks and unpicks").

Changing the content language while a text in another language is chosen (BR13 for transcription, the same here, screen open point 1):
- The choice is cleared.
- An `AppAlert` Info `Questions.BaseText.Cleared` (title, old language, new language) appears inside the section with `role="status"`.
- The alert goes away at the next pick or save.

Refusals on save:
- `question.base_text_not_found` (404): the choice is cleared and the error goes on the field.
- `question.base_text_language_mismatch` (409): the choice is kept, so the curator sees which text was refused, and the error goes on the field.
- Both refusals join `AppErrorSummary`, with a link to `question-base-text`.

Unsaved changes: `HasChanges` compares the chosen id with `_saved.BaseText?.Id` (AC13), so a changed choice has three effects:
- Publish shows `Questions.Publication.Publish.DisabledUnsaved`.
- The preview says it shows the saved version.
- Leaving the page asks `Common.Discard.*`.

Other parts of the form:
- Aside: a new row `Questions.Field.BaseText`, with the chosen title or `Common.Summary.NotFilled`.
- Annulled question (F-101 BR5): the picker is disabled. Open and Show the text stay, because reading is allowed, and the Create line is hidden.
- Published question: changing or removing the link and saving records a version (F-98: the HTML differs). It is not a key change, so no reason is asked and nothing new appears on screen.
- Preview (`QuestionPreviewDialog`) and the version view look as today: the base text comes before the statement (BR9), from `QuestionResponse.BaseText.TextHtml` and `QuestionVersionResponse.BaseTextHtml`.

### Screen 4: delete confirmation and refusal
- The kit call is `IConfirmService.ConfirmDeleteAsync(L, L["BaseTexts.Delete.Object"], L["BaseTexts.Delete.Message", title])`:
  - The dialog title is `Common.Delete.Title`.
  - The message says the text leaves the list and the picker, and that no question uses it. It is a soft delete (rule ui-project).
  - The confirm button is filled in the error colour: `Common.Delete.Confirm` with `BaseTexts.Delete.Object` ("Delete this base text"), because a 200-character title does not fit a button.
  - Cancel is a text button, Esc closes the dialog, and focus returns to the button that opened it.
- The dialog opens only when `QuestionCount == 0`.
- A text in use: the list row's Delete is disabled with its tooltip, and the form's Delete is disabled with its visible reason.
- Race: 409 `base_text.in_use` puts the error alert at the top of the page, and the list or the form reloads.

### Screen 5: transcription (F-99), the kept choice
- It is Screen 3's section, inside `/admin/questions/transcribe`.
- After "Save and continue" the base text stays chosen, while type, statement, difficulty and key are blank (BR13). While a text is chosen, the field's hint is `Questions.BaseText.Kept`.
- `HasChanges` ignores the kept choice in transcription, as it ignores the edition and the language. Finish does not ask to discard a blank question.
- Changing the language to another one clears the choice with `Questions.BaseText.Cleared` (BR13, AC12).
- The `Questions.Transcribe.Saved` alert and the batch counter are unchanged.

### Accessibility
Tab order:
- List: Add, search, language filter, then Edit and Delete in each row, then the pager.
- Form: title, language, the editor toolbar and editor, Cancel, Save, the linked questions' Edit buttons, Delete.
- Question section: the picker, its clear button, Open the base text, Show the text, Create a base text.

Labels, roles and announcements:
- Every field has a visible label above it. Required fields show `*` and carry `aria-required`. Hints and errors are tied by `aria-describedby` (kit).
- Elements are what they do:
  - Add, Save, Cancel, Delete, Edit and the disclosure are buttons. The disclosure carries `aria-expanded` and `aria-controls`.
  - Open and Create are links, because they navigate. Their accessible name says they open a new tab.
- A screen reader hears:
  - The picker's result count (`Lookup.Results`, kit live region).
  - The cleared-choice alert, as a polite status.
  - The error summary, which takes focus after a refused save.
  - Row actions as "Edit: base text "..."" (`Common.ActionOnItem`).
  - The versions notice, as a status alert.
- Content language:
  - Titles and texts carry the `lang` of their content language: list cells, the linked questions' excerpts, the read-only text and the editor's `Lang`.
  - Picker options carry none, because `AppLookupOption` has no language (screen open point 6).

Kit-wide behaviour:
- Click targets are 24 px or more.
- The focus ring and the row hover come from the kit.
- Every screen works by keyboard alone.

Claims about library components, not verified in the rendered DOM:
- `unverified (gallery DOM)`: the clear button of `MudAutocomplete` (`AppLookupField` with `Clearable`) has an accessible name and is reached by Tab.
- `unverified (gallery DOM)`: `MudSelect` (`AppSelectField` with `Disabled`) exposes the disabled state (`aria-disabled` or `disabled`) to a screen reader.
- `unverified (gallery DOM)`: `MudAlert` (`AppAlert`) puts the `role` the kit passes on its root element.
- `unverified (gallery DOM)`: `MudIconButton` with `Disabled` inside `MudTooltip` (`AppRowActions` Delete) is skipped by Tab, so its tooltip reason is not reachable by keyboard. The Questions column shows the count that explains it.

### Resource keys
New keys, in `SharedResources` (`.resx`, `.pt-BR.resx`, `.pt-PT.resx`):

| Key | en | pt-BR | pt-PT |
| --- | --- | --- | --- |
| `Nav.BaseTexts` | Base texts | Textos-base | Textos de apoio |
| `BaseTexts.Title` | Base texts | Textos-base | Textos de apoio |
| `BaseTexts.Search.Placeholder` | Search by title | Pesquisar pelo título | Pesquisar pelo título |
| `BaseTexts.Filter.Language` | Language | Idioma | Idioma |
| `BaseTexts.Filter.Language.All` | All languages | Todos os idiomas | Todos os idiomas |
| `BaseTexts.Column.Title` | Title | Título | Título |
| `BaseTexts.Column.Language` | Language | Idioma | Idioma |
| `BaseTexts.Column.Questions` | Questions | Questões | Questões |
| `BaseTexts.Empty` | No base text yet. | Nenhum texto-base ainda. | Ainda não há textos de apoio. |
| `BaseTexts.Empty.Filters` | No base text in this language. | Nenhum texto-base neste idioma. | Nenhum texto de apoio neste idioma. |
| `BaseTexts.Empty.Search` | No base text matches "{0}". | Nenhum texto-base corresponde a "{0}". | Nenhum texto de apoio corresponde a "{0}". |
| `BaseTexts.Row.Name` | base text "{0}" | texto-base "{0}" | texto de apoio "{0}" |
| `BaseTexts.Delete.DisabledInUse` | Questions use this text ({0}). Remove their links before deleting it. | Há questões que usam este texto ({0}). Remova os vínculos delas antes de excluir. | Há questões que usam este texto ({0}). Retire as ligações delas antes de o eliminar. |
| `BaseTexts.Delete.Object` | this base text | este texto-base | este texto de apoio |
| `BaseTexts.Delete.Message` | "{0}" leaves the list and the question picker. No question uses it. | "{0}" sai da lista e do seletor das questões. Nenhuma questão o usa. | "{0}" sai da lista e do seletor das questões. Nenhuma questão o usa. |
| `BaseTexts.Deleted` | Base text deleted. | Texto-base excluído. | Texto de apoio eliminado. |
| `BaseTexts.Saved` | Base text saved. | Texto-base salvo. | Texto de apoio guardado. |
| `BaseTexts.Form.AddTitle` | Add base text | Adicionar texto-base | Adicionar texto de apoio |
| `BaseTexts.Form.EditTitle` | Edit base text | Editar texto-base | Editar texto de apoio |
| `BaseTexts.BackToList` | Back to the base texts | Voltar para os textos-base | Voltar aos textos de apoio |
| `BaseTexts.Section.Text` | Base text | Texto-base | Texto de apoio |
| `BaseTexts.Section.Text.Subtitle` | A passage, table or case that several questions refer to. Written once, shown before each of their statements. | Um trecho, tabela ou caso a que várias questões se referem. Escrito uma vez, aparece antes do enunciado de cada uma. | Um excerto, tabela ou caso a que várias questões se referem. Escrito uma vez, aparece antes do enunciado de cada uma. |
| `BaseTexts.Field.Title` | Title | Título | Título |
| `BaseTexts.Field.Title.Placeholder` | A name that is easy to find | Um nome fácil de encontrar | Um nome fácil de encontrar |
| `BaseTexts.Field.Title.Hint` | Up to 200 characters. It names the text in lists and in the question form. | Até 200 caracteres. É o nome do texto nas listas e no formulário da questão. | Até 200 caracteres. É o nome do texto nas listas e no formulário da questão. |
| `BaseTexts.Field.ContentLanguage` | Content language | Idioma do conteúdo | Idioma do conteúdo |
| `BaseTexts.Field.ContentLanguage.Hint` | The language the text is written in. Only questions in this language can use it. | O idioma em que o texto está escrito. Só questões neste idioma podem usá-lo. | O idioma em que o texto está escrito. Só as questões neste idioma o podem usar. |
| `BaseTexts.Field.ContentLanguage.Locked` | Locked while questions use this text ({0}). A question and its base text are always in the same language. | Bloqueado enquanto houver questões usando este texto ({0}). Uma questão e seu texto-base estão sempre no mesmo idioma. | Bloqueado enquanto houver questões a usar este texto ({0}). Uma questão e o seu texto de apoio estão sempre no mesmo idioma. |
| `BaseTexts.Field.Text` | Text | Texto | Texto |
| `BaseTexts.Field.Text.Placeholder` | Paste or write the text | Cole ou escreva o texto | Cole ou escreva o texto |
| `BaseTexts.Field.Text.Hint` | Up to 20,000 characters. | Até 20.000 caracteres. | Até 20 000 caracteres. |
| `BaseTexts.Versions.Notice` | Saving a change to the text records a new version of each published question that uses it; drafts and annulled questions get none. Changing only the title records nothing. | Salvar uma mudança no texto registra uma nova versão de cada questão publicada que o usa; rascunhos e questões anuladas não ganham versão. Mudar só o título não registra nada. | Guardar uma alteração ao texto regista uma nova versão de cada questão publicada que o usa; os rascunhos e as questões anuladas não recebem versão. Alterar só o título não regista nada. |
| `BaseTexts.Section.Questions` | Questions that use this text ({0}) | Questões que usam este texto ({0}) | Questões que usam este texto ({0}) |
| `BaseTexts.Section.Questions.Subtitle` | Most recently changed first. Open a question to change or remove its link. | As alteradas mais recentemente primeiro. Abra uma questão para trocar ou remover o vínculo. | As alteradas mais recentemente primeiro. Abra uma questão para mudar ou retirar a ligação. |
| `BaseTexts.Questions.Empty` | No question uses this text yet. Pick it in the Base text section of a question. | Nenhuma questão usa este texto ainda. Escolha-o na seção Texto-base de uma questão. | Ainda nenhuma questão usa este texto. Escolha-o na secção Texto de apoio de uma questão. |
| `BaseTexts.Section.Delete` | Delete this base text | Excluir este texto-base | Eliminar este texto de apoio |
| `BaseTexts.Section.Delete.Subtitle` | Only a text that no question uses can be deleted. | Só um texto que nenhuma questão usa pode ser excluído. | Só um texto que nenhuma questão usa pode ser eliminado. |
| `Questions.BaseText.Option.Secondary` | Questions: {0} · changed on {1} | Questões: {0} · alterado em {1} | Questões: {0} · alterado em {1} |
| `Questions.BaseText.Open` | Open the base text | Abrir o texto-base | Abrir o texto de apoio |
| `Questions.BaseText.Show` | Show the text | Mostrar o texto | Mostrar o texto |
| `Questions.BaseText.Hide` | Hide the text | Ocultar o texto | Ocultar o texto |
| `Questions.BaseText.ReadOnlyNote` | Edited on its own page, for every question that uses it. | É editado na página dele, para todas as questões que o usam. | É editado na sua própria página, para todas as questões que o usam. |
| `Questions.BaseText.Create.Lead` | Not in the list? | Não está na lista? | Não está na lista? |
| `Questions.BaseText.Create` | Create a base text | Criar um texto-base | Criar um texto de apoio |
| `Questions.BaseText.Create.Help` | It opens in a new tab, so this question stays as it is. Save the text there, come back and pick it here. | Abre em uma nova aba, e esta questão fica como está. Salve o texto lá, volte e escolha-o aqui. | Abre num novo separador, e esta questão fica como está. Guarde o texto lá, volte e escolha-o aqui. |
| `Questions.BaseText.Cleared` | The base text "{0}" was removed: it is in {1}, and this question is now in {2}. | O texto-base "{0}" foi removido: ele está em {1}, e esta questão agora está em {2}. | O texto de apoio "{0}" foi retirado: está em {1}, e esta questão está agora em {2}. |
| `Questions.BaseText.Kept` | Stays chosen for the next question. Change or remove it when the block of questions ends. | Continua escolhido para a próxima questão. Troque ou remova quando o bloco de questões acabar. | Continua escolhido para a próxima questão. Mude-o ou retire-o quando o bloco de questões terminar. |
| `Common.LinkOpensNewTab` | {0} (opens in a new tab) | {0} (abre em uma nova aba) | {0} (abre num novo separador) |
| `base_text.not_found` | This base text no longer exists. | Este texto-base não existe mais. | Este texto de apoio já não existe. |
| `base_text.title_required` | Write the title. | Escreva o título. | Escreva o título. |
| `base_text.title_too_long` | The title has more than 200 characters. | O título passa de 200 caracteres. | O título excede 200 caracteres. |
| `base_text.text_required` | Write the text. | Escreva o texto. | Escreva o texto. |
| `base_text.text_too_long` | The text has more than 20,000 characters. | O texto passa de 20.000 caracteres. | O texto excede 20 000 caracteres. |
| `base_text.content_language_invalid` | Choose one of the listed languages. | Escolha um dos idiomas da lista. | Escolha um dos idiomas da lista. |
| `base_text.in_use` | Questions still use this base text. Remove their links before deleting it. | Ainda há questões usando este texto-base. Remova os vínculos delas antes de excluir. | Ainda há questões a usar este texto de apoio. Retire as ligações delas antes de o eliminar. |
| `base_text.language_in_use` | Questions use this base text, so its language cannot change. | Há questões usando este texto-base, então o idioma dele não pode mudar. | Há questões a usar este texto de apoio, por isso o idioma dele não pode mudar. |
| `question.base_text_not_found` | This base text no longer exists. Pick another or leave the field empty. | Este texto-base não existe mais. Escolha outro ou deixe o campo vazio. | Este texto de apoio já não existe. Escolha outro ou deixe o campo vazio. |
| `question.base_text_language_mismatch` | The base text and the question must be in the same language. | O texto-base e a questão precisam estar no mesmo idioma. | O texto de apoio e a questão têm de estar no mesmo idioma. |

Changed values:

| Key | en | pt-BR | pt-PT |
| --- | --- | --- | --- |
| `Questions.Section.BaseText.Subtitle` | A text shared by several questions, shown before the statement. Optional. | Um texto compartilhado por várias questões, mostrado antes do enunciado. Opcional. | Um texto partilhado por várias questões, mostrado antes do enunciado. Opcional. |
| `Questions.Field.BaseText.Placeholder` | Title of the base text | Título do texto-base | Título do texto de apoio |
| `Questions.Field.BaseText.Hint` | Only texts in {0}, the language of this question, are listed. | Só aparecem textos em {0}, o idioma desta questão. | Só aparecem textos em {0}, o idioma desta questão. |

Removed: `question.base_text_too_long`, which leaves with the inline field.

Reused unchanged: `Common.Add`, `Common.Edit`, `Common.Delete`, `Common.Save`, `Common.Cancel`, `Common.Saving`, `Common.Search`, `Common.Actions`, `Common.ActionOnItem`, `Common.TryAgain`, `Common.Loading`, `Common.LoadFailed`, `Common.ErrorSummary.Title`, `Common.Summary.NotFilled`, `Common.Delete.Title`, `Common.Delete.Confirm`, `Common.Discard.*`, `Lookup.*`, `Nav.Section.Content`, `Questions.Section.BaseText`, `Questions.Field.BaseText`, `Questions.Column.Statement`, `Questions.Column.Source`, `Questions.Column.Status`, `Questions.Source.Number`, `Questions.Source.Authored`, `Questions.Status.*`, `Questions.Field.ContentLanguage`, `Questions.Publication.Publish.DisabledUnsaved`, `Questions.Transcribe.*`, `NotFound.*`.

## Docs
- flow: new docs/flows/shared-base-text.md — manage a base text, link and unlink, the versions an edit makes, the delete guard, the transcription.
- flow: update docs/flows/transcribe-paper.md#after-each-save — the base text stays chosen.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they create a base text with a title, a language and a text, then it is stored with sanitized HTML and answered with 201; a blank title, a title over 200, a text that is empty once its tags are removed, a text over 20 000 characters or an unknown language answer 400 with `base_text.title_required`, `base_text.title_too_long`, `base_text.text_required`, `base_text.text_too_long`, `base_text.content_language_invalid`, and nothing is stored. (UC1, BR1, BR8)
- AC2 Given a base text, when a question in the same language is saved with its `BaseTextId`, then the question response carries the text; when saved with another language, then 409 `question.base_text_language_mismatch`; when the id is unknown or deleted, then 404 `question.base_text_not_found`; when `BaseTextId` is null, the link is removed. (UC3, BR2, BR3, BR7)
- AC3 Given a base text linked by a published question at version N and by a draft and an annulled question, when its text changes, then the published question has version N+1 with the new base text HTML, `AnswerKeyChanged` false and no reason, the draft and the annulled question get none, and versions 1 to N are unchanged; when only the title changes, no version is made. (UC2, BR4, BR5)
- AC4 Given a base text linked by two published questions, when its text changes, then both versions are recorded in one save and a failure of either leaves the text and both questions as they were. (BR4)
- AC5 Given a base text that has linked questions, when its language is changed, then 409 `base_text.language_in_use`; when it has none, the language changes. (BR3)
- AC6 Given a base text linked by a live question of any status, when it is deleted, then 409 `base_text.in_use` and it stays; given a base text with no question, then it is deleted softly and disappears from the list and from the picker. (UC5, BR6)
- AC7 Given the migration on a bank with questions that have inline base text, then each of those gets one linked base text with the stored HTML, the question's language and the 80-character title, the questions without base text have no link, the column `base_text_html` is gone and the number of versions is unchanged. (BR10)
- AC8 Given base texts in two languages, when the list is opened, then it shows title, language and the number of linked questions sorted by title; the search narrows by title and the language filter by language. (UC5, BR11)
- AC9 Given a base text, when its detail is opened, then it lists the linked questions with excerpt, status and paper number, and each opens the question. (UC4, BR11)
- AC10 Given a user without `questions.manage`, when any base text route is called, then 403, and the menu entry, the page and the actions are not offered. (BR12)
- AC11 Given a question with a linked base text, when it is previewed, then the base text is shown before the statement, and a question without one shows none. (BR9)
- AC12 Given the transcription screen, when a question is saved with a base text chosen, then the next question still has it chosen while type, statement, difficulty and key are blank; when its language is changed to another, the choice is cleared with a message. (UC6, BR13)
- AC13 Given the question form, when the curator picks, changes and removes the base text, then only texts of the question's language are offered, the choice is saved with the question and "unsaved changes" is shown while it differs from the stored one. (UC3, BR3)
- AC14 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en), the missing-key test is green, and every new error code has its text in the three languages.

## Criterion → test

Tests locked: c5e0b8a483ea0832f48ab684c0108c16080a9ebb (5 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `BaseTextTests.Create_ValidContent_TrimsTheTitle_CanonicalizesTheLanguage_AndDerivesTheSearchForm`; `BaseTextTests.Create_BlankTitle_IsRefusedWithTitleRequired`; `BaseTextTests.Create_TitleOfExactlyTheLimitAfterTrimming_IsAccepted_AndOneMoreIsRefused`; `BaseTextTests.Create_TextWithNoBody_IsRefusedWithTextRequired`; `BaseTextTests.Create_TextOfExactlyTheLimit_IsAccepted_AndOneMoreIsRefusedWithTextTooLong`; `BaseTextTests.Create_ALanguageTheAppDoesNotKnow_IsRefusedWithContentLanguageInvalid`; `BaseTextEndpointTests.Create_AValidBaseText_Answers201WithTheSanitizedHtml_AndAFreshCount`; `BaseTextEndpointTests.Create_ABadBaseText_IsRefusedWithTheFieldsOwnCode_AndStoresNothing` |
| AC2 | `BaseTextEndpointTests.Save_AQuestionWithABaseTextId_ReturnsTheLinkedText_AndNullRemovesIt`; `BaseTextEndpointTests.Save_ABaseTextOfAnotherLanguage_Answers409LanguageMismatch_AndStoresNothing`; `BaseTextEndpointTests.Save_ChangingTheLanguageOfALinkedQuestion_Answers409LanguageMismatch_AndTheLinkStays`; `BaseTextEndpointTests.Save_AnUnknownOrDeletedBaseText_Answers404_AndAnInvalidLanguageStillAnswers400` |
| AC3 | `QuestionBaseTextVersionTests.NextVersion_FirstPublication_SnapshotsTheLinkedBaseTextHtml`; `QuestionBaseTextVersionTests.NextVersion_TheLinkedTextChanged_MakesTheNextVersion_NotAKeyChange_WithNoReason`; `QuestionBaseTextVersionTests.NextVersion_TheLinkedTextIsTheSame_MakesNothing`; `QuestionBaseTextVersionTests.NextVersion_TheLinkWasRemoved_MakesTheNextVersionWithNoBaseText`; `BaseTextEndpointTests.Edit_TheText_RecordsANewVersionOnEachPublishedLinkedQuestion_AndOnNoDraftNorAnnulled`; `BaseTextEndpointTests.Edit_OnlyTheTitle_RecordsNoVersion` |
| AC4 | `BaseTextEndpointTests.Edit_TheText_OfTwoPublishedQuestions_RecordsBothVersionsInOneSave`; `BaseTextRaceTests.Edit_StartedWhileAPublishHoldsTheBaseText_WaitsForIt_AndThenVersionsTheNewlyPublishedQuestion` |
| AC5 | `BaseTextTests.Update_ChangingTheLanguage_WhileQuestionsLinkIt_IsRefusedWithLanguageInUse_AndNothingChanges`; `BaseTextTests.Update_KeepingTheLanguage_WhileQuestionsLinkIt_ReplacesTitleAndText`; `BaseTextTests.Update_ChangingTheLanguage_WhenNoQuestionLinksIt_Changes`; `BaseTextEndpointTests.Edit_TheLanguage_Answers409WhileAQuestionLinksIt_AndChangesWhenNoneDoes` |
| AC6 | `BaseTextEndpointTests.Delete_WhileAQuestionLinksIt_Answers409InUse_AndWithNoneItDisappears`; `BaseTextRaceTests.Delete_StartedWhileAQuestionSaveHoldsTheBaseText_WaitsForIt_AndThenAnswersInUse` |
| AC7 | `AddBaseTextsMigrationTests.Up_GivesEachQuestionWithInlineTextABaseTextOfItsOwn_AndLinksIt`; `AddBaseTextsMigrationTests.Up_CutsTheTitleAtEightyCharacters`; `AddBaseTextsMigrationTests.Up_SoftDeletesTheTextOfASoftDeletedQuestion_AndWritesNoVersion`; `AddBaseTextsMigrationTests.Up_Then_Down_CopiesTheTextBackIntoTheQuestions_AndDropsTheTable`; `AddBaseTextsMigrationTests.TheDatabase_RefusesABlankTitleAndABlankText_AndAHardDeleteOfALinkedText` |
| AC8 | `BaseTextEndpointTests.List_SortsByTitle_SearchesWithoutCaseOrAccents_AndFiltersByLanguage` |
| AC9 | `BaseTextEndpointTests.Questions_OfABaseText_ListsTheLiveLinkedQuestions_AndAnUnknownTextAnswers404` |
| AC10 | `BaseTextEndpointTests.EveryBaseTextEndpoint_WithoutThePermission_AnswersForbidden_AndACuratorHasIt` |
| AC11 | pending |
| AC12 | pending |
| AC13 | pending |
| AC14 | pending |

## Decisions
- 2026-10-15 — One concept: every question links to a `BaseText` or has none; the inline field is dropped and the migration turns each existing text into a base text linked to its question — one way to edit, the data keeps nothing in two places — asked.
- 2026-10-15 — Editing a base text records a new version of each linked published question in the same transaction; the version keeps copying the HTML — attempts keep what the student saw and the F-98 model is untouched — asked.
- 2026-10-15 — A base text carries title and content language; the language is locked while questions use it — a mixed-language block is a data error, and the question picker can filter by language — asked.
- 2026-10-15 — A base text in use is not deleted: `base_text.in_use`, like a topic in use (ADR-0005 #3) — asked.
- 2026-10-15 — Own list and form pages under `/admin/base-texts`; the question form only picks and unpicks — one way to edit an item (rule `ui-project`); the screen design settles the pick-and-create flow — asked.
- 2026-10-15 — Permission `questions.manage`; no new permission to seed — asked.
- 2026-10-15 — The transcription keeps the chosen base text across saves — a reading block is transcribed in sequence — asked.
- 2026-10-15 — Size judged: one module, one migration, three screens; doubt keeps it whole, as the epic sizes it M — technical, no owner card.
- 2026-10-15 — Draft questions follow the live text and an annulled question makes no version, so the back office of an annulled question may show the edited text while its latest version is what was graded — F-101 BR5 freezes content, and a version is what attempts use — technical.
- 2026-10-15 — The foreign key from `questions.base_text_id` to `base_texts` is inside the module and restricts delete; the topic and edition stay ids with no foreign key — ADR-0005 #2 — confirmed: docs/decisions/ADR-0005-question-bank-module.md.
- 2026-10-15 — The text is sanitized HTML written in the visual editor and sanitized by the Api on every write — confirmed: docs/decisions/ADR-0005-question-bank-module.md (#5).
- 2026-10-15 — No new package; the migration backfill and the editor are the existing ones — technical.
- 2026-10-15 — Screen design: on the question form, changing the language clears a base text of another language with an info message, as the transcription does — the curator sees it before saving instead of a 409 — asked.
- 2026-10-15 — Screen design: "Create a base text" opens `/admin/base-texts/new?language=` in a new tab, so the unsaved question stays and there is one way to edit; the first internal link to open a new tab — asked.
- 2026-10-15 — Screen design: Delete is on the list row and in its own card on the base text form, both with confirmation and disabled while in use — asked.
- 2026-10-15 — Screen design: the linked-questions list shows the source line with the paper number under it, from the question list items the endpoint already returns — asked.
- 2026-10-15 — Screen design: an empty linked-questions list has guidance text and no button; saving a text change that versions several questions asks no confirmation (no answer key change, the notice says it) — technical.

- 2026-10-15 — Build design (system-design and architect passes, opus, verified against the code): vertical slice inside QuestionBank, `BaseText` entity with `base_texts` table, `questions.base_text_id` foreign key inside the module (restrict), the text read by its own query and never a navigation property, `NextVersion` takes the linked text HTML from its caller, one `IQuestionStore.TrySaveChangesAsync` for the fan-out, hand-ordered migration with a `Down` that copies the text back — technical.
- 2026-10-15 — Breaking change of the v1 contract: `SaveQuestionRequest.BaseTextHtml` becomes `BaseTextId` and `QuestionResponse.BaseTextHtml` becomes `BaseText`; an exception to `api-contracts.md` ("inside a version only add optional fields"), accepted because only `Simulab.Web` consumes it and ships with the Api; the ship bumps MAJOR (0.37.0 to 1.0.0, profile rule) — owner chose it over the additive option (asked, design pass).
- 2026-10-15 — Migration soft-deletes the base texts it makes from soft-deleted questions, with the question's deletion date (adds to BR10) — never visible, so the list and the picker get no item with 0 questions — owner chose it over keeping them live (asked, design pass).
- 2026-10-15 — A base text save that loses a race answers 409 `question.version_conflict` (existing code and texts); Screen 2's error alert shows it — owner chose it over a new code (asked, design pass).
- 2026-10-15 — A `normalized_title` column on `base_texts` (the recipe of `ComparableText`, as `NormalizedStatement`), so the title search ignores case and accents (i18n rule) — technical; it is data of the item's own table, not a product choice.
- 2026-10-15 — Races (architect finding 2): the base text save and delete take `SELECT ... FOR UPDATE` on the base text row, and a question save or publish that links a text takes `FOR SHARE` on it, in one explicit transaction through the store; each of the two races gets a test — technical.
- 2026-10-15 — The language check compares the canonical language with the text's only when the language is valid, so an invalid language still answers 400 first; the backfill takes `created_at`, `created_by` and `tenant_id` from the question and decodes entities and block spacing like `PlainTextOf`; `questions.base_text_id` has a column comment; `question.base_text_too_long` leaves the three resource files and the gallery sample gets keys of its own; one port (`IBaseTextStore`) reads a base text — technical.
- 2026-10-15 — Design coverage: layout and references, naming and vocabulary, API contract rule, i18n and error-code text check, migration rules and tests, simpler version — read against ADR-0005, `tests/Simulab.ArchitectureTests` and the module's migration tests. Not checked: F-104's status.
- 2026-10-15 — Traps: T1 `Question.BaseText` navigation with `Include` (the soft-delete filter gives null and a version with no text); T2 trusting the status token for the fan-out (it only inserts versions); T3 the scaffolded migration drops the column before the backfill and its `Down` loses every text — all avoided by the design.
- 2026-10-15 — A reinstated question that was annulled while its base text changed gets the old HTML back (F-101 BR6 copies the latest version); the next save makes a version with the live text — kept as F-101 decided, said in the flow page — technical.

- 2026-10-15 — Tests unlocked (the blank title and hard delete migration test inserted a question with the `base_text_html` column the migration drops; the test was wrong, not the code) — owner chose to unlock and rewrite, asked in the build; rewritten with a helper that inserts without the column, the foreign key code corrected to restrict's 23001, and one parity test added for the SQL title and search form against `ComparableText`.
- 2026-10-15 — Build: `Question.NextVersion` takes the linked text HTML as an optional third parameter so the existing callers and tests stay as they are; the base text row locks and the transaction live in `QuestionStore`/`BaseTextStore` (`RowLockTransactions`), committed by `IQuestionStore` saves; `QuestionListQuery` gains an optional `BaseTextId` and `GET /base-texts/{id}/questions` goes through the question list; SQL `upper` writes capital sharp s for ß where C# keeps ß, so the migration folds it back — technical.

## Out of scope
- Images in a base text — F-103 (`IFileStorage`).
- Merging duplicate base texts the migration created, or finding near-duplicates — a later item if it hurts.
- A filter of the question list by base text — the base text's detail lists its questions; an idea if the list needs it.
- How a simulator lays the base text beside the question — epics E-5 and E-6.
- Sharing a statement stem or an option set between questions — not asked.
- A language tag on each option of the kit's lookup (`AppLookupField`), so a screen reader reads a title in its own language — a kit change, an idea.
- An "add a question with this text preselected" button on an empty linked-questions list — an idea.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of the build. -->

## Events

- 2026-10-15 question — asked 7, from context 0
- 2026-10-15 question — asked 3, from context 0
- 2026-10-15 blocked — test wrong: the blank title and hard delete test inserted a question with the base_text_html column, which the migration under test drops

## Delivery
