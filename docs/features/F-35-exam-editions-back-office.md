---
feature: F-35
epic: Assessment catalog
status: done
board: 754
version: 1
---
# Exam editions back office

Technical terms: [glossary](../glossary.md)

## Summary
Admin screens to manage the editions of an exam. An edition is one paper actually applied: the year, the job it selects for (named in the edition itself), the application date, the official link to its notice, and whether it is a draft or published. An edital that opens several jobs with different papers becomes one edition per paper (owner, 2026-09-20). The closest Simulae source is `ExamNotice` and `ExamNoticeFormDialog`, but the model is new work. Needs /agile:screen.

The edition is also **where the exam board lives** — a required link to `Organizer`, not a field on the exam (owner, 2026-09-26). Two reasons, both checked:

- The Manaus municipal guard edital (`Edital nº 01, de 23 de março de 2026`, read 2026-09-26) names the contracting body on its cover ("A PREFEITURA DE MANAUS, por meio da SEMAD, torna pública…") and the board in item 1.1 ("sendo sua execução de responsabilidade do Instituto Consulplan"). The board is declared inside the edital, and the edital is the edition.
- The simulator needs it there. The owner's use case: *"select 20 Mathematics questions from board Consulplan, from the editais of 2021 to 2026"* — a student practising a board's style (epic Exam Simulator, 695). That query walks question → paper → edition → board, and only works if each edition carries its own board: the same exam changes board between years, so a board on the exam would return the wrong questions for every year it changed.

So the edition carries the board and the notice year, and both have to be filterable. Recorded here because the data has to exist before the engine can ask for it.

## Start
- Depends on: F-34 (`done`, merged as 3386b40) and F-43 (`done`) — `Exam`, `/admin/exams/{id}`, `AppLookupField`, `AppItemRows` and `AppStatusChip` are all in `main`.
- Waits on (to start): nothing. The `/agile:screen` mockup was approved together with the feature on 2026-09-28.
- Needed to validate: the app host started from this worktree and an Admin account signed in by the owner (Claude does not enter credentials).
- Suggested path: glossary rows; `ExamEdition` in the domain with its `Result` rules and the migration; the endpoints and the two delete guards; `AppDateField` in the kit with its gallery entry; the editions section on the exam page; the edition page.
- Can run beside it: nothing in the catalog — F-36 and F-37 both depend on it. An item outside `Catalog` and the exam page could, with the owner's `--worktree`.

## What exists
- `Exam` (`Simulab.Catalog.Domain/Entities/Exam.cs`): `IssuingAuthorityId`, `Name`, `AssessmentType`, `Scope`, `ScopeDetail`, `ContentLanguage`, `NormalizedName`; `TenantEntity`, soft delete, null `TenantId`, rules answer with `Result`. Its unique index is `ux_exams_tenant_authority_normalized_name` (`IsUniquePerTenant()`, `NULLS NOT DISTINCT`).
- `Organizer` is the exam board and nothing else since F-34 v2 (three kinds, pt-BR "Banca"). Nothing points at it: `DeleteOrganizerHandler.cs:9` says the guard arrives "when F-35's editions start pointing here".
- `DeleteExamHandler.cs:9` soft-deletes with no check and says the guard for an exam with editions "arrives with F-35 (BR13)".
- `/admin/exams/{id}` (`ExamForm.razor`, 503 lines) is the exam's own page, recomposed by F-43 into section cards plus a read-only aside. Line 18 and line 455 reserve the editions section below the form; saving a new exam already turns the page into the edit form so the editions are "one scroll away".
- Kit (`Simulab.Web/Components/Ui/`): `AppItemRows` (child items with per-row actions, built by F-43 for exactly this section and used only in the gallery so far — `AppItemRowsTests.cs:7`), `AppStatusChip` (tones `Neutral`/`Success`/`Warning`/`Error`/`Info`), `AppLookupField` (server-backed parent picker), `AppRadioCards`, `AppSectionCard`, `AppFormGrid`, `AppFormAside`, `AppErrorSummary`, `AppFormActions`, `AppDateColumn`. **There is no date input** in the kit, and no page uses `MudDatePicker`.
- `CatalogApiClient` is the module's typed client; `ExamEndpoints` groups `/api/v1/catalog/exams` behind `PermissionPolicy.NameFor(CatalogPermissions.Manage)`.
- Glossary: `ExamEdition` (edição), `Notice` (edital) and the content states `Draft`/`InReview`/`Published` are already named. The job an edition selects for has no identifier yet, and `Cargo` is on the forbidden list.
- Packages: MudBlazor 9.9.0 (`MudDatePicker` included), EF Core with Npgsql, `xunit`, `AwesomeAssertions`, `Testcontainers.PostgreSql`, `bunit`, `Microsoft.AspNetCore.Mvc.Testing` — all in `Directory.Packages.props`.

Simulae (`repo/src/Modules/ContentCatalog/Modules.ContentCatalog.Domain/Entities/ExamNotice.cs`, read-only):
- `ExamNotice` is `ContestId`, `ExamBoardId`, `NoticeYear` (1900 to 2100), `PublicationDate` (optional, shown as "Data da Prova"), `Status` `Open`/`Closed` with `Close()`/`Reopen()`. No job, no notice link, no notice number, no uniqueness rule. It throws `ContentCatalogDomainException` with pt-BR messages.
- `ExamNoticeFormDialog.razor` is a dialog on `RegisterExamPage` with `ExamBoardSelect`, a `MudNumericField` for the year, a `MudDatePicker` and an Open/Closed button group; all texts hardcoded pt-BR, `Icons.Material.Filled`.
- What carries over: the board per edition, the notice year and an optional date. The rest is rewritten: states, job, notice fields, uniqueness, `Result` rules, the page instead of the dialog.

## Goal
Give the catalog its lowest level: the paper actually applied, with its board and its year. Questions, the syllabus and every simulation attach to an edition, and F-36 shows a student only what has a published one.

## Users and use cases
- UC1 An Admin opens an existing exam at `/admin/exams/{id}` and sees, below the form, its editions: notice year, position, board acronym, publication state and the application date when there is one, newest year first.
- UC2 An Admin adds an edition from that section on its own page: picks the board by typing part of its name or acronym, types the notice year, and optionally the position, the notice reference, the notice link and the application date; it is saved as a draft unless the Admin chooses Published.
- UC3 An Admin opens an edition on the same page, changes it — including publishing or unpublishing it — and saves.
- UC4 An Admin deletes a draft edition after confirming; it disappears from the exam's section.
- UC5 An Admin tries to delete a published edition and is told to unpublish it first.
- UC6 An Admin tries to delete an exam that has editions, or a board that some edition names, and is refused.
- UC7 A Student or Curator gets the ordinary Not Found page on the edition routes; the Api answers 403 `identity.forbidden`.

## Business rules
- BR1 `ExamEdition : TenantEntity` lives in `Simulab.Catalog.Domain/Entities/`, mapped to `catalog.exam_editions`. Global data: `TenantId` null, every unique index that includes it `NULLS NOT DISTINCT` (F-33 BR5). Deleting is a soft delete.
- BR2 An edition belongs to one exam: `ExamId` is a required foreign key to `catalog.exams`, fixed at creation — an edition never moves to another exam. The content language is the exam's; the edition has none of its own (ADR-0001 #27).
- BR3 An edition names its exam board: `OrganizerId` is a required foreign key to `catalog.organizers` (`exam_edition.organizer_required` when missing). An id that matches no organizer, or a deleted one, is 404 `organizer.not_found`; an exam id that matches no exam, or a deleted one, is 404 `exam.not_found`.
- BR4 `NoticeYear` is required, an integer from 1990 to the current year plus one, where the current year comes from `TimeProvider`; anything else is `exam_edition.notice_year_invalid`.
- BR5 `Position` — the job the paper selects for — is optional, trimmed, at most 200 characters (`exam_edition.position_too_long`); blank is stored as null. It is text, not an entity (brief, owner 2026-09-20).
- BR6 `NoticeReference` — how the notice names itself, such as "Edital nº 01/2026" — is optional, trimmed, at most 100 characters (`exam_edition.notice_reference_too_long`). Editions cut from the same edital share it.
- BR7 `NoticeUrl` is optional; when given it is an absolute `http` or `https` URL of at most 300 characters, else `exam_edition.notice_url_invalid` (the organizer website rule, F-33 BR10).
- BR8 `AppliedOn` — the date the paper was applied — is optional, a date without time (`DateOnly`); an edition may be published without it. When given it is not before 1 January of the notice year, else `exam_edition.applied_on_before_notice_year`.
- BR9 `Status` is `Draft` or `Published`, required, travelling as a string; an unknown value is `exam_edition.status_invalid`. A new edition is `Draft` unless the request says `Published`. The change goes both ways with no condition beyond BR4 to BR8: nothing points at an edition yet. `InReview` is not offered (it arrives with epic 698).
- BR10 An edition is unique within its exam by notice year, normalized position and board: a unique index over `TenantId`, `ExamId`, `NoticeYear`, `NormalizedPosition` and `OrganizerId`, `NULLS NOT DISTINCT`, deleted rows included (the F-33 and F-34 precedent). `NormalizedPosition` is `CatalogText.Normalize` of the position, or empty when there is none, so "no position" collides with "no position". A duplicate is 409 `exam_edition.duplicate`.
- BR11 Deleting a `Published` edition is refused with 409 `exam_edition.published`; the message says to unpublish it first. A draft is soft-deleted.
- BR12 Deleting an exam that has at least one edition, deleted editions excluded, is refused with 409 `exam.has_editions`. Deleting an organizer that at least one edition names, deleted editions excluded, is refused with 409 `organizer.has_editions`. Both carry no count (F-34 BR12: `ErrorText.For` takes no argument). Both foreign keys are `Restrict` in the database as well.
- BR13 An index over `(OrganizerId, NoticeYear)` exists so the simulator's "board X, years A to B" query (epic 695) has one to use. No query endpoint over it is built here.
- BR14 An exam's editions come back in one call, not paged, ordered by notice year descending, then position, then the board's name. An exam has a handful of editions; a paged table would be too much furniture for the section (F-43 `AppItemRows`).
- BR15 Domain rules return `Result`, never exceptions: `ExamEdition.Create` and `Update` validate BR3 to BR9, and `Delete` guards BR11.
- BR16 `AppDateField` is a new kit component wrapping `MudDatePicker`, with the anatomy of `AppTextField` (label above with the required marker, the control, then the hint or the error), shown on `/dev/ui` with its empty, filled, error and disabled states. It is the only date input from now on (rule `ui`). It shows and parses dates in the reader's culture and binds a `DateOnly?`.
- BR17 Every UI text of the new section, the edition page, `AppDateField` and every new error code exists in pt-BR, pt-PT and en, in `SharedResources` (F-33 v2).
- BR18 One permission covers it all: `catalog.manage` gates the endpoints, the section and the edition page (F-33 BR4).

## Screens and API

Designed by `/agile:screen` on 2026-09-28. Mockup: `docs/features/mockups/F-35-exam-editions-back-office.html` (three screens, every state, light and dark, pt-BR / pt-PT / en). Only kit patterns from `/dev/ui` are used; the one new pattern is `AppDateField` (BR16). Every text below is a `SharedResources` key; error codes are keys of their own and reach the screen only through `ErrorText.For`.

### Routes
| Route | Purpose | Gate |
|---|---|---|
| `/admin/exams/{id:guid}` | the exam page gains the editions section below the form card | `catalog.manage` |
| `/admin/exams/{examId:guid}/editions/new` | the edition page, adding | the same |
| `/admin/exams/{examId:guid}/editions/{id:guid}` | the edition page, editing | the same |
| `/dev/ui` | `AppDateField` added to the gallery | development only |

No menu item: an edition is reached through its exam. No global editions list (owner, question 1).

### Screen 1 — the editions section on the exam page (`ExamForm.razor`)

**Place.** A second `MudPaper Class="app-form-card"` directly below the `app-form-layout` grid, full width (it spans the form column and the aside column). It is outside the exam form: the exam's `AppFormActions` (the page's only primary button, Save) stays at the bottom of the exam card and never saves an edition. On a narrow screen (< 1280 px) it comes after the aside, like everything else on the page.

**Composition.**
1. `AppSectionCard Id="exam-section-editions"`, `Icon="AppIcons.Editions"` (new constant, `Icons.Material.Outlined.EventNote`), title `ExamEditions.Section.Title`, subtitle `ExamEditions.Section.Subtitle`.
2. `AppAlert Severity="Error"` at the top of the card body, only after a refused or failed delete (the section's own business/server alert).
3. The body by state (table below): `AppLoadingState`, `AppErrorState` with `OnRetry`, or `AppItemRows TItem="ExamEditionResponse"` with `EmptyMessage="ExamEditions.Empty"`.
4. Row template, in one line that wraps on a phone: **label** `NoticeYear · Position · OrganizerAcronym` (the position part is left out when there is none; the ` · ` separators are `aria-hidden`), then `AppStatusChip` (`Draft` → `AppStatusTone.Neutral`, text `ExamEditions.Status.Draft`; `Published` → `AppStatusTone.Success`, text `ExamEditions.Status.Published`), then, when `AppliedOn` has a value, `ExamEditions.Row.AppliedOn` with the date as a short date in the reader's culture (format `"d"`, the `AppDateColumn` precedent) in `text-secondary`. The row is not a link (rule `ui`: the name links only to a read-only detail page, and there is none).
5. Row actions: `AppRowActions` with `OnEdit` (navigates to `/admin/exams/{examId}/editions/{id}`) and `OnDelete`, `ItemName` = the label joined with `, ` instead of ` · ` (read as "Edit: 2026, Guarda Municipal de 3ª Classe, CONSULPLAN").
6. Under the rows (also under the empty message): an outlined `MudButton` with `Href="/admin/exams/{examId}/editions/new"` (so it renders as a link, because it navigates), `StartIcon="AppIcons.Add"`, text `ExamEditions.Form.AddTitle`. It is outlined, not filled: Save of the exam stays the page's only primary button.

**Delete.** `Confirm.ConfirmDeleteAsync(L, objectName, consequence)`: title `Common.Delete.Title`, message `ExamEditions.Delete.Message` with `{0}` the row label and `{1}` the exam name, confirm button `Common.Delete.Confirm` with `{0}` = `ExamEditions.Delete.Object` (error colour, names the action and the edition). Confirmed → `DELETE`; 204 → the row leaves the list, snackbar `ExamEditions.Deleted`; 409 `exam_edition.published` → the section alert with that text, the row stays; 404 `exam_edition.not_found` → the section alert and the list reloads; anything else → the section alert with `common.unexpected_error`. On a published row the delete action is disabled, with `DeleteDisabledReason` = `ExamEditions.Delete.DisabledPublished` as its tooltip; the 409 stays as the Api's guard, reached only when another Admin published the edition meanwhile (owner, screen question 1).

**Add mode** (`/admin/exams/new`): the same card and heading, with only the sentence `ExamEditions.Section.SaveExamFirst` and no add link (AC3). When the first Save turns the page into the edit form, the section loads (it is empty) and the add link appears.

**Order** is the Api's (BR14); the page does not sort.

**Elsewhere.** Deleting an exam on `/admin/exams` and a board on `/admin/organizers` keeps its current refusal path; only the new texts `exam.has_editions` and `organizer.has_editions` arrive (AC13, AC14). No layout change on those pages.

### Screen 2 — the edition page (`ExamEditionForm.razor`, `/admin/exams/{examId}/editions/new` and `/{id}`)

**Page header.** `AppPageHeader`, no primary action at the top (Save is at the bottom, the F-43 rule). Title `ExamEditions.Form.AddTitle` / `ExamEditions.Form.EditTitle`. Breadcrumb: `Nav.Section.Content` (disabled) › `Exams.Title` (link `/admin/exams`) › exam name (link `/admin/exams/{examId}`) › current (disabled): `ExamEditions.Form.AddTitle` while adding, `NoticeYear · Position` (or `NoticeYear` alone) while editing — the saved values, not the ones being typed.

**Layout.** The F-43 composition: `div.app-form-layout` → `MudPaper.app-form-card` (error summary, alert, three section cards, form actions) + `AppFormAside`.

| # | Section card | Id | Icon | Grid | Fields |
|---|---|---|---|---|---|
| 1 | `ExamEditions.Section.Paper` + subtitle | `edition-section-paper` | `AppIcons.Exams` | `AppFormGrid Columns="2"`, then `Columns="1"` | Board, Notice year; Position (full width) |
| 2 | `ExamEditions.Section.Notice` + subtitle | `edition-section-notice` | `AppIcons.Notice` (new, `Article`) | `AppFormGrid Columns="2"`, twice | Notice reference, Notice link; Application date, (empty cell) |
| 3 | `ExamEditions.Section.Publication` + subtitle | `edition-section-publication` | `AppIcons.View` | — | Status (`AppRadioCards Columns="2"`) |

**Fields.** Client checks are comfort only; the Api runs the same rules (BR3–BR9). Each is checked on leaving the field and on Save.

| Field | Id | Component | Required | Type and limits | Leading icon | Placeholder / hint keys | Client check → text |
|---|---|---|---|---|---|---|---|
| Board | `edition-organizer` | `AppLookupField` over `ListOrganizersAsync(term)`, option text `Name (ACRONYM)`, `MinChars` 2 (kit default) | yes | an organizer id | `AppIcons.Organizers` | `ExamEditions.Field.Organizer.Placeholder` / `.Hint` | empty → `exam_edition.organizer_required` |
| Notice year | `edition-notice-year` | `AppTextField TValue="int?"`, `InputType.Number`, `MaxLength` 4, `Autocomplete="off"` | yes | integer 1990 to current year + 1 (the page reads `TimeProvider`) | none | `ExamEditions.Field.NoticeYear.Placeholder` / `.Hint` (`{0}` = current year + 1) | empty or out of range → `exam_edition.notice_year_invalid` |
| Position | `edition-position` | `AppTextField TValue="string"`, `MaxLength` 200, `Autocomplete="off"` | no | trimmed text; blank sends null | none | `ExamEditions.Field.Position.Placeholder` / `.Hint` | the max length stops typing; the Api code `exam_edition.position_too_long` is mapped to the field |
| Notice reference | `edition-notice-reference` | `AppTextField TValue="string"`, `MaxLength` 100 | no | trimmed text; blank sends null | none | `ExamEditions.Field.NoticeReference.Placeholder` / `.Hint` | Api `exam_edition.notice_reference_too_long` mapped to the field |
| Notice link | `edition-notice-url` | `AppTextField TValue="string"`, `InputType.Url`, `MaxLength` 300, `Autocomplete="off"` | no | absolute `http`/`https` URL | `AppIcons.Link` (new, `Link`) | `ExamEditions.Field.NoticeUrl.Placeholder` / `.Hint` | not absolute http(s) → `exam_edition.notice_url_invalid` |
| Application date | `edition-applied-on` | `AppDateField` (new) with `Min` = 1 January of the typed notice year when it is valid | no, also to publish | `DateOnly?`, sent as `yyyy-MM-dd` | the kit's calendar button | hint `ExamEditions.Field.AppliedOn.Hint` | not a date → the kit's `DateField.Invalid`; before 1 January of the notice year → `exam_edition.applied_on_before_notice_year` (re-checked when the year changes) |
| Status | `edition-status` | `AppRadioCards TValue="ExamEditionStatus?"`, two cards, no icon | yes | `Draft` (default on a new edition) or `Published` | none | card texts `ExamEditions.Status.Draft` / `.Published`, descriptions `….Description` | never empty on screen; Api `exam_edition.status_invalid` goes to the field |

**Aside** (`AppFormAside`, read-only, no button): title `Common.Summary.Title`; rows `ExamEditions.Field.Exam` (the exam name), `Exams.Field.ContentLanguage` (the exam's, BR2 — shown so the reader sees which language the paper's content is in), Board, Notice year, Position, Notice reference, Application date (short date), Status; empty text `Common.Summary.NotFilled`. Checklist `Common.Summary.Checklist`: Board, Notice year; `DoneText` / `PendingText` `Common.Checklist.Done` / `Common.Checklist.Pending`.

**Actions.** `AppFormActions` at the bottom right: Cancel (text) → `/admin/exams/{examId}`; Save (filled). `HasChanges` compares with the saved edition (or the empty new form with Status `Draft`); leaving with changes asks `Common.Discard.*`. No delete on this page: an edition is deleted from its row, one way (rule `ui`).

**Save.** Add → `POST`; 201 → snackbar `ExamEditions.Saved`, `NavigateTo("/admin/exams/{examId}/editions/{id}", replace: true)`, the page becomes its edit form (title, breadcrumb) (AC4). Edit → `PUT`; 200 → snackbar, stay. Refusals: a field code goes to its field (table above) and focus goes to the error summary; `organizer.not_found` clears the board, sets `exam_edition.organizer_required` on it and shows the alert; `exam_edition.duplicate` is the alert only (it concerns three fields); `exam.not_found` and `exam_edition.not_found` show the alert with the link back (`Exams.BackToList` or `ExamEditions.BackToExam`); anything else is the alert with `common.unexpected_error`.

### Screen 3 — `AppDateField` (kit) and its `/dev/ui` entry

**Anatomy** — the one of `AppTextField`: label above (`for` the input) with the required marker; the outlined `MudDatePicker` with `Editable="true"` (the reader types), `DateFormat` = the reader culture's short date pattern, `Culture` = `CultureInfo.CurrentCulture` (month and weekday names, first day of the week), `AutoClose="true"`, `Clearable` when not required, the adornment `AppIcons.Calendar` (new, `CalendarToday`) as the calendar button; below, the hint or the error, in `{Id}-description`.

**Parameters.** `Id`, `Label` (`[EditorRequired]`), `Value` (`DateOnly?`), `ValueChanged`, `Hint`, `Error`, `Required`, `Disabled`, `Min` and `Max` (`DateOnly?`: days outside are disabled in the calendar; they do not replace the page's rule text). The kit converts `DateOnly?` ↔ the picker's `DateTime?`, so a page never sees a time or a time zone (AC5: same calendar day).

**Behaviour.** Typing is the full keyboard path. On leaving the input, text that is not a date in the reader's format shows `DateField.Invalid` (with `DateField.Format`) and the value stays `null`; blank is `null`. With no `Hint`, the description shows `DateField.Hint.Format` — the way `AppLookupField` shows its minimum characters.

**Gallery** (`UiGallery.razor`, section `id="gallery-date-field"`, after the lookup field): heading `Gallery.Section.DateField`, hint `Gallery.DateField.Hint`, one `MudPaper Outlined` with four fields labelled `ExamEditions.Field.AppliedOn`: empty (interactive), filled (14 June 2026), error (`DateField.Invalid`), disabled (filled).

### States

**Screen 1 — editions section**

| State | When | What shows | Announced |
|---|---|---|---|
| Add mode | `/admin/exams/new` | heading, subtitle, `ExamEditions.Section.SaveExamFirst`; no add link | read with the section |
| Loading | exam loaded, list requested | `AppLoadingState` inside the card | `role="status"` "Loading..." |
| Empty | list is `[]` | `ExamEditions.Empty` + add link | — |
| Ready | one or more editions | rows, newest year first, + add link | — |
| Load error | list call failed | `AppErrorState` (`Common.LoadFailed`, Try again); no add link | `role="alert"` |
| Confirm delete | delete clicked | kit confirmation dialog | dialog title |
| Deleted | 204 | row gone, snackbar `ExamEditions.Deleted`; focus to the next row's edit button, else the add link | snackbar `role="status"` |
| Delete refused | 409 `exam_edition.published` (another Admin published it after the list loaded; a published row's delete is disabled) | section alert, row stays, the list reloads | `role="alert"` |
| Delete failed | 404 / other | section alert (`exam_edition.not_found` → list reloads; else `common.unexpected_error`) | `role="alert"` |
| Permission denied | no `catalog.manage` | the whole route is the ordinary Not Found page (UC7) | page title |

**Screen 2 — edition page**

| State | When | What shows |
|---|---|---|
| Loading | exam (and edition) requested | header with a generic title, `AppLoadingState` |
| New | `/editions/new`, exam found | empty form, Status `Draft`, aside "Not filled", checklist pending |
| Ready (edit) | edition found | filled form, breadcrumb `2026 · Guarda Municipal de 3ª Classe` |
| Board search | typing ≥ 2 characters | the kit's lookup states: results, no result (`Lookup.NoResults`), search failed (`Lookup.LoadFailed` + Try again) |
| Calendar open | calendar button | the picker popover, Esc closes it |
| Saving | Save clicked, request out | Save disabled with progress and `Common.Saving`; Cancel disabled |
| Success | 201 / 200 | snackbar `ExamEditions.Saved`; add → becomes the edit form (URL replaced) |
| Validation error | client checks fail on Save, or a field code from the Api | `AppErrorSummary` (`Common.ErrorSummary.Title`, one link per field) at the top, focused; each field shows its error |
| Business error | 409 `exam_edition.duplicate`, 404 `organizer.not_found` | `AppAlert` error at the top of the card; board cleared for the second |
| Server error | anything else | `AppAlert` `common.unexpected_error`; the form keeps what was typed |
| Exam not found | unknown or deleted `examId` | card with alert `exam.not_found` + link `Exams.BackToList` |
| Edition not found | unknown id, or an id under another exam | card with alert `exam_edition.not_found` + link `ExamEditions.BackToExam` |
| Unsaved changes | leaving with changes | `Common.Discard.*` dialog |
| Permission denied | no `catalog.manage` | Not Found page (UC7, AC16) |

**Screen 3 — `AppDateField`**: empty (format hint), filled, error (`aria-invalid`, error text), disabled (not focusable, no calendar button action), calendar open.

### Permissions
One permission, `catalog.manage` (BR18): the page attribute `[Authorize(Policy = PermissionPolicy.Prefix + CatalogPermissions.Manage)]` on the edition page (as on `ExamForm.razor`), which already guards the exam page and therefore the section; the endpoints use the same policy. No per-action check: whoever sees the page may add, edit and delete. No menu item.

### Accessibility
- **Section.** `AppSectionCard` is a `<section aria-labelledby>` with an `h2`; the rows are a `<ul>`; the chip's state is its text, the dot is decorative; the date reads "Applied on 14/06/2026". Row buttons are buttons (they act); the add action is a link (it navigates), with a visible focus ring from the kit.
- **Tab order, exam page:** the exam form, its Cancel and Save, then the section: each row's Edit and Delete, then the add link.
- **Tab order, edition page:** breadcrumb links → (error summary, when shown) → board → notice year → position → notice reference → notice link → application date input → its calendar button → its clear button (when filled) → status (one tab stop; arrows move) → Cancel → Save.
- **Screen reader.** Each field announces label, "required" (`aria-required`), and its hint or error through `aria-describedby`; `aria-invalid` on a field in error. The lookup is a `combobox` with `aria-expanded` and a polite result count (kit). The status is a `radiogroup` of two `radio`s with their descriptions. The error summary is `role="alert"`, gets focus, and each line moves focus to its field. Saving is announced by the button's `role="status"` text; success by the snackbar.
- **Date field.** The input keeps the label, `aria-describedby`, `aria-invalid`, `aria-required`, and `inputmode="numeric"`; the calendar button has the accessible name and tooltip `DateField.OpenCalendar`, the clear button `DateField.Clear`; the popover is MudBlazor's, Esc closes it and focus returns to the button. Typing the date is enough (AC17); the calendar is never the only way.
- **Targets.** Row action buttons and the calendar button are at least 24 px (kit sizes).
- **Contrast.** No new colour. Row date and hints use `text-secondary` on `surface` (6.92:1 light, 4.89:1 dark — the F-43 figures); the chip text is `text-primary`.

### UI texts
New keys, in `SharedResources.resx` / `.pt-BR.resx` / `.pt-PT.resx`. Reused keys are listed after the table.

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `ExamEditions.Section.Title` | Editions | Edições | Edições |
| `ExamEditions.Section.Subtitle` | Each paper actually applied, with its year, position and board. Newest first. | Cada prova efetivamente aplicada, com ano, cargo e banca. As mais recentes primeiro. | Cada prova efetivamente aplicada, com ano, posto de trabalho e entidade organizadora. As mais recentes primeiro. |
| `ExamEditions.Section.SaveExamFirst` | Save the exam first. Its editions are added here. | Salve o exame primeiro. As edições dele são adicionadas aqui. | Guarde primeiro o exame. As suas edições são adicionadas aqui. |
| `ExamEditions.Empty` | This exam has no edition yet. | Este exame ainda não tem edições. | Este exame ainda não tem edições. |
| `ExamEditions.Row.AppliedOn` | Applied on {0} | Aplicada em {0} | Aplicada em {0} |
| `ExamEditions.Status.Draft` | Draft | Rascunho | Rascunho |
| `ExamEditions.Status.Published` | Published | Publicada | Publicada |
| `ExamEditions.Status.Draft.Description` | Kept in the administration. Students do not see it. | Fica só na administração. Os alunos não a veem. | Fica apenas na administração. Os alunos não a veem. |
| `ExamEditions.Status.Published.Description` | Offered to students in the catalog. Set it back to Draft before deleting it. | Oferecida aos alunos no catálogo. Volte para Rascunho antes de excluí-la. | Disponível para os alunos no catálogo. Volte a Rascunho antes de a eliminar. |
| `ExamEditions.Delete.Object` | edition {0} | edição {0} | edição {0} |
| `ExamEditions.Delete.Message` | The edition {0} leaves the exam {1}. The same year, position and board cannot be added to this exam again. | A edição {0} sai do exame {1}. O mesmo ano, cargo e banca não poderão ser adicionados de novo a este exame. | A edição {0} sai do exame {1}. O mesmo ano, posto de trabalho e entidade organizadora não poderão voltar a ser adicionados a este exame. |
| `ExamEditions.Deleted` | Edition deleted. | Edição excluída. | Edição eliminada. |
| `ExamEditions.Delete.DisabledPublished` | Set it back to Draft before deleting it. | Volte para Rascunho antes de excluir. | Volte a colocar em Rascunho antes de eliminar. |
| `ExamEditions.Form.AddTitle` | Add edition | Adicionar edição | Adicionar edição |
| `ExamEditions.Form.EditTitle` | Edit edition | Editar edição | Editar edição |
| `ExamEditions.Section.Paper` | Paper | Prova | Prova |
| `ExamEditions.Section.Paper.Subtitle` | The board that applied it, the notice year and the job it selects for. | A banca que a aplicou, o ano do edital e o cargo a que se destina. | A entidade organizadora que a aplicou, o ano do aviso e o posto de trabalho a que se destina. |
| `ExamEditions.Section.Notice` | Notice and application | Edital e aplicação | Aviso e aplicação |
| `ExamEditions.Section.Notice.Subtitle` | How the notice names itself, where it is published and when the paper was applied. All optional. | Como o edital se identifica, onde está publicado e quando a prova foi aplicada. Tudo opcional. | Como o aviso se identifica, onde está publicado e quando a prova foi aplicada. Tudo opcional. |
| `ExamEditions.Section.Publication` | Publication | Publicação | Publicação |
| `ExamEditions.Section.Publication.Subtitle` | Who can see this edition. | Quem pode ver esta edição. | Quem pode ver esta edição. |
| `ExamEditions.Field.Exam` | Exam | Exame | Exame |
| `ExamEditions.Field.Organizer` | Board | Banca | Entidade organizadora |
| `ExamEditions.Field.Organizer.Placeholder` | Name or acronym | Nome ou sigla | Nome ou sigla |
| `ExamEditions.Field.Organizer.Hint` | The board that applied this paper, as the notice names it. | A banca que aplicou esta prova, como o edital a indica. | A entidade organizadora que aplicou esta prova, como o aviso a indica. |
| `ExamEditions.Field.NoticeYear` | Notice year | Ano do edital | Ano do aviso |
| `ExamEditions.Field.NoticeYear.Placeholder` | E.g.: 2026 | Ex.: 2026 | Ex.: 2026 |
| `ExamEditions.Field.NoticeYear.Hint` | From 1990 to {0}. | De 1990 a {0}. | De 1990 a {0}. |
| `ExamEditions.Field.Position` | Position | Cargo | Posto de trabalho |
| `ExamEditions.Field.Position.Placeholder` | E.g.: Municipal guard, 3rd class | Ex.: Guarda Municipal de 3ª Classe | Ex.: Agente de Polícia Municipal |
| `ExamEditions.Field.Position.Hint` | The job this paper selects for. Leave it empty for ENEM, entrance exams and certifications. At most 200 characters. | O cargo que esta prova seleciona. Deixe vazio para ENEM, vestibulares e certificações. Até 200 caracteres. | O posto de trabalho para que esta prova seleciona. Deixe vazio para ENEM, exames de acesso e certificações. No máximo 200 caracteres. |
| `ExamEditions.Field.NoticeReference` | Notice reference | Identificação do edital | Referência do aviso |
| `ExamEditions.Field.NoticeReference.Placeholder` | E.g.: Notice no. 01/2026 | Ex.: Edital nº 01/2026 | Ex.: Aviso n.º 1/2026 |
| `ExamEditions.Field.NoticeReference.Hint` | As the notice names itself. Editions cut from the same notice share it. | Como o edital se identifica. Edições do mesmo edital repetem este texto. | Como o aviso se identifica. Edições do mesmo aviso repetem este texto. |
| `ExamEditions.Field.NoticeUrl` | Notice link | Link do edital | Ligação do aviso |
| `ExamEditions.Field.NoticeUrl.Placeholder` | https:// | https:// | https:// |
| `ExamEditions.Field.NoticeUrl.Hint` | The official address of the notice. At most 300 characters. | O endereço oficial do edital. Até 300 caracteres. | O endereço oficial do aviso. No máximo 300 caracteres. |
| `ExamEditions.Field.AppliedOn` | Application date | Data de aplicação | Data de aplicação |
| `ExamEditions.Field.AppliedOn.Hint` | The day the paper was applied. Optional, also to publish. | O dia em que a prova foi aplicada. Opcional, também para publicar. | O dia em que a prova foi aplicada. Opcional, também para publicar. |
| `ExamEditions.Field.Status` | Status | Situação | Estado |
| `ExamEditions.Saved` | Edition saved. | Edição salva. | Edição guardada. |
| `ExamEditions.BackToExam` | Back to the exam | Voltar para o exame | Voltar ao exame |
| `DateField.OpenCalendar` | Open the calendar | Abrir o calendário | Abrir o calendário |
| `DateField.Clear` | Clear the date | Limpar a data | Limpar a data |
| `DateField.PreviousMonth` (screen question 4) | Previous month | Mês anterior | Mês anterior |
| `DateField.NextMonth` (screen question 4) | Next month | Próximo mês | Mês seguinte |
| `DateField.Format` | mm/dd/yyyy | dd/mm/aaaa | dd/mm/aaaa |
| `DateField.Hint.Format` | Format: {0} | Formato: {0} | Formato: {0} |
| `DateField.Invalid` | This is not a date. Type it as {0}. | Isto não é uma data. Digite no formato {0}. | Isto não é uma data. Escreva no formato {0}. |
| `Gallery.Section.DateField` | Date field | Campo de data | Campo de data |
| `Gallery.DateField.Hint` | Type the date in the reader's format or pick it from the calendar. Empty, filled, error and disabled. | Digite a data no formato de quem lê ou escolha no calendário. Vazio, preenchido, com erro e desativado. | Escreva a data no formato de quem lê ou escolha-a no calendário. Vazio, preenchido, com erro e desativado. |
| `exam_edition.not_found` | This edition no longer exists. | Esta edição não existe mais. | Esta edição já não existe. |
| `exam_edition.organizer_required` | Choose the board. | Escolha a banca. | Escolha a entidade organizadora. |
| `exam_edition.notice_year_invalid` | Type a year from 1990 to next year. | Informe um ano entre 1990 e o próximo ano. | Indique um ano entre 1990 e o próximo ano. |
| `exam_edition.position_too_long` | The position is too long: at most 200 characters. | O cargo é longo demais: no máximo 200 caracteres. | O posto de trabalho é demasiado longo: no máximo 200 caracteres. |
| `exam_edition.notice_reference_too_long` | The notice reference is too long: at most 100 characters. | A identificação do edital é longa demais: no máximo 100 caracteres. | A referência do aviso é demasiado longa: no máximo 100 caracteres. |
| `exam_edition.notice_url_invalid` | Type a full address of at most 300 characters, starting with http:// or https:// | Digite o endereço completo, com até 300 caracteres, começando com http:// ou https:// | Escreva o endereço completo, com no máximo 300 caracteres, a começar por http:// ou https:// |
| `exam_edition.applied_on_before_notice_year` | The application date cannot be before 1 January of the notice year. | A data de aplicação não pode ser anterior a 1º de janeiro do ano do edital. | A data de aplicação não pode ser anterior a 1 de janeiro do ano do aviso. |
| `exam_edition.status_invalid` | Choose Draft or Published. | Escolha Rascunho ou Publicada. | Escolha Rascunho ou Publicada. |
| `exam_edition.duplicate` | This exam already has an edition with this year, position and board (a deleted one counts too). | Este exame já tem uma edição com este ano, cargo e banca (uma excluída também conta). | Este exame já tem uma edição com este ano, posto de trabalho e entidade organizadora (uma eliminada também conta). |
| `exam_edition.published` | This edition is published. Set it back to Draft, save, then delete it. | Esta edição está publicada. Volte-a para Rascunho, salve e depois exclua. | Esta edição está publicada. Volte a colocá-la em Rascunho, guarde e depois elimine-a. |
| `exam.has_editions` | This exam has editions. Delete its editions first. | Este exame tem edições. Exclua as edições primeiro. | Este exame tem edições. Elimine primeiro as edições. |
| `organizer.has_editions` | Some editions name this board. Change or delete those editions first. | Há edições que indicam esta banca. Altere ou exclua essas edições primeiro. | Há edições que indicam esta entidade organizadora. Altere ou elimine primeiro essas edições. |

Reused, unchanged: `Nav.Section.Content`, `Exams.Title`, `Exams.BackToList`, `Exams.Field.ContentLanguage`, `Common.Delete.Title`, `Common.Delete.Confirm`, `Common.Discard.*`, `Common.Save`, `Common.Saving`, `Common.Cancel`, `Common.Edit`, `Common.Delete`, `Common.Actions`, `Common.ActionOnItem`, `Common.Loading`, `Common.LoadFailed`, `Common.TryAgain`, `Common.ErrorSummary.Title`, `Common.Summary.Title`, `Common.Summary.NotFilled`, `Common.Summary.Checklist`, `Common.Checklist.Done`, `Common.Checklist.Pending`, `Lookup.*`, `Gallery.Title`, `Gallery.Breadcrumb.Dev`, `NotFound.*`, `exam.not_found`, `organizer.not_found`, `common.unexpected_error`.

New `AppIcons` constants (Material Outlined): `Editions` (`EventNote`), `Notice` (`Article`), `Link` (`Link`), `Calendar` (`CalendarToday`).

### API
- `GET /api/v1/catalog/exams/{examId:guid}/editions` — the exam's editions, `ExamEditionResponse[]` (BR14); 404 `exam.not_found`.
- `GET /api/v1/catalog/exams/{examId:guid}/editions/{id:guid}` — one; 404 `exam_edition.not_found`.
- `POST /api/v1/catalog/exams/{examId:guid}/editions` — `SaveExamEditionRequest(OrganizerId, NoticeYear, Position, NoticeReference, NoticeUrl, AppliedOn, Status)`; 201.
- `PUT /api/v1/catalog/exams/{examId:guid}/editions/{id:guid}` — the same request; 200.
- `DELETE /api/v1/catalog/exams/{examId:guid}/editions/{id:guid}` — 204.
- `ExamEditionResponse(Id, ExamId, OrganizerId, OrganizerName, OrganizerAcronym, NoticeYear, Position, NoticeReference, NoticeUrl, AppliedOn, Status)`. `Status` is `string?` in the request, parsed by the handler (the F-33 review lesson); `AppliedOn` is an ISO date (`yyyy-MM-dd`) through `AppJson.Options`.
- An edition id under another exam's route is 404 `exam_edition.not_found`.
- Error codes: `exam_edition.not_found` (404); `exam_edition.organizer_required`, `exam_edition.notice_year_invalid`, `exam_edition.position_too_long`, `exam_edition.notice_reference_too_long`, `exam_edition.notice_url_invalid`, `exam_edition.applied_on_before_notice_year`, `exam_edition.status_invalid` (400); `exam_edition.duplicate`, `exam_edition.published`, `exam.has_editions`, `organizer.has_editions` (409). Reused: `exam.not_found`, `organizer.not_found` (404).

## Acceptance criteria
- AC1 Given a database with the previous migration, when the new migration runs, then `catalog.exam_editions` exists with restricted foreign keys to `catalog.exams` and `catalog.organizers`, the unique index of BR10 `NULLS NOT DISTINCT`, and the `(organizer_id, notice_year)` index. (BR1, BR10, BR12, BR13)
- AC2 Given an exam with editions of 2024 and 2026, when the Admin opens `/admin/exams/{id}`, then the section lists both, 2026 first, each with its year, position, board acronym, state chip and date when there is one. (UC1, BR14)
- AC3 Given `/admin/exams/new`, then the section says the exam must be saved first and offers no Add. (UC1)
- AC4 Given the edition page with a board and a notice year only, when the Admin saves, then the edition is created as `Draft`, the page becomes its edit form, and it appears in the exam's section. (UC2, BR9)
- AC5 Given an edition with a position, a notice reference, a notice URL and an application date, when it is saved and reopened, then every field comes back as saved and the date is the same calendar day. (UC2, UC3, BR5 to BR8)
- AC6 Given a notice year of 1989 or of the current year plus two, when the request is sent, then the Api answers 400 `exam_edition.notice_year_invalid` and nothing is written. (BR4)
- AC7 Given an application date before 1 January of the notice year, then 400 `exam_edition.applied_on_before_notice_year`; given a notice URL that is not an absolute http or https address, then 400 `exam_edition.notice_url_invalid`. (BR7, BR8)
- AC8 Given an edition of an exam, when the Admin saves another one for the same exam with the same year, the same board and a position that differs only in case or accents, then the Api answers 409 `exam_edition.duplicate`; with another board, or another year, it is created. (BR10)
- AC9 Given two rows inserted directly with `TenantId` null, the same exam, year, board and no position, when the second is saved, then PostgreSQL rejects it. (BR1, BR10)
- AC10 Given a request whose `organizerId` matches no organizer or a deleted one, then 404 `organizer.not_found`; whose exam is unknown or deleted, then 404 `exam.not_found`; and nothing is written. (BR3)
- AC11 Given a draft edition, when the Admin publishes it, then its chip reads Published; when they set it back to Draft, then it reads Draft. (UC3, BR9)
- AC12 Given a published edition, then its row's delete action is disabled with the reason `ExamEditions.Delete.DisabledPublished`, and a `DELETE` sent to the Api answers 409 `exam_edition.published` and it stays; given a draft, when the Admin confirms its deletion, then it disappears from the section and from the list call, and the row is still in the table with `IsDeleted` true. (UC4, UC5, BR11)
- AC13 Given an exam with one edition, when the Admin confirms deleting the exam, then 409 `exam.has_editions` and the translated message; once its only edition is deleted, the exam can be deleted. (UC6, BR12)
- AC14 Given a board that an edition names, when the Admin confirms deleting it on `/admin/organizers`, then 409 `organizer.has_editions` and the translated message; once that edition is deleted, the board can be deleted. (UC6, BR12)
- AC15 Given a status that is not `Draft` or `Published`, then 400 `exam_edition.status_invalid`, and the OpenAPI document names the two values. (BR9)
- AC16 Given a signed-in Student, when they open an edition route, then Not Found, and every `/api/v1/catalog/exams/{examId}/editions` route answers 403 `identity.forbidden`. (UC7, BR18)
- AC17 Given `AppDateField` on `/dev/ui` and on the edition page, then it shows its empty, filled, error and disabled states, reads and writes the date in pt-BR, pt-PT and en formats, and can be filled by keyboard alone. (BR16)
- AC18 Given `ExamEdition.Create` with a notice year of 1989, when it runs, then it returns a failed `Result` with `exam_edition.notice_year_invalid` and throws nothing. (BR15)
- AC19 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR17)

## Decisions
- 2026-09-28 — The editions are a section of the exam page, and each edition is edited on its own page; no global editions list — rule `ui-project` names the edition a complex entity, F-43 built `AppItemRows` for this section, and nobody asked for a cross-exam list yet (owner, question 1).
- 2026-09-28 — The position is optional free text — ENEM, entrance exams and certifications select for no job; no `Job` entity (brief, 2026-09-20) (owner, question 2).
- 2026-09-28 — The edition keeps an optional notice reference besides the link — it is what shows that several editions came from one edital (owner, question 3).
- 2026-09-28 — The application date is always optional, also to publish — the owner chose not to tie publication to it (owner, question 4; the recommendation was "required to publish").
- 2026-09-28 — An edition is unique by exam, notice year, position and board — a paper annulled and re-applied by another board in the same year is a real case (owner, question 5; the recommendation was year and position).
- 2026-09-28 — Deleting an exam or a board that has editions is refused with 409 — the F-34 pattern; cascade would remove a whole history in one click (owner, question 6).
- 2026-09-28 — Notice years from 1990 to the current year plus one, and a given application date not before the notice year — old papers are still practised, and an edital published in December is for next year's paper (owner, question 7).
- 2026-09-28 — Two states only, `Draft` and `Published`; `InReview` arrives with the AI import (epic 698) (owner, question 8).
- 2026-09-28 — Publishing goes both ways — nothing points at an edition yet; the lock arrives with questions and simulations (owner, question 9).
- 2026-09-28 — A published edition must be unpublished before it can be deleted — a student may be looking at it (owner, question 10).
- 2026-09-28 — The board and year are stored and indexed, with no query endpoint — the simulator (epic 695) owns the shape of that query (owner, question 11).
- 2026-09-28 — `/agile:screen` runs before approval — first child-entity page and first date field of the kit (owner, question 12).
- 2026-09-28 — `AppDateField` is a new kit component wrapping `MudDatePicker`; no new package, for production or for tests (rule `build-config`) (owner, question 13).
- 2026-09-28 — A section row reads year · position · board acronym, with the state chip and the date — two editions of the same year stay distinguishable (owner, question 14).
- 2026-09-28 — English identifiers: `ExamEdition`, `Position` (the job; `Cargo` is forbidden and `Job` would suggest an entity), `NoticeYear`, `NoticeReference`, `NoticeUrl`, `AppliedOn`, `ExamEditionStatus` with `Draft`/`Published` (the glossary's content states). The glossary gets the `Position`, `NoticeReference` and `AppliedOn` rows before their first use in code (rule `naming`).
- 2026-09-28 — The edition list under an exam is not paged (BR14) — a handful of rows; `AppItemRows` has no paging, and adding it would be structure without a use.
- 2026-09-28 — The status travels inside the save request, not through `publish`/`unpublish` routes — the form owns the choice, and a transition with no rule of its own does not need its own endpoint.
- 2026-09-28 — Simulae's `ExamNotice` is not imported: it throws with pt-BR messages, its `Open`/`Closed` means something else, and it has no job, notice link or uniqueness. What carries over is the board per edition, the notice year and the optional date.
- 2026-09-28 — No seed rows: F-37 brings the real editions (the F-33 and F-34 decision).

### From the screen design (2026-09-28)
- The editions card sits below the whole exam layout, full width, outside the exam form — the exam's Save stays the page's only primary button and never touches an edition; the aside keeps summarising only the exam.
- "Add edition" is an outlined link under the rows, not a header button — `AppSectionCard` has no action slot and `AppPageHeader`'s primary action would compete with the exam's Save; a link because it navigates.
- One key, `ExamEditions.Form.AddTitle`, for the add link, the page title and the breadcrumb — one term per action (rule `ui`).
- Rows are not links; edit and delete are the `AppRowActions` icons — there is no read-only detail page (rule `ui`).
- The row's accessible name joins its parts with commas, the visible ` · ` separators are hidden from screen readers.
- The edition page has three section cards (Paper; Notice and application; Publication) — the required fields first, the optional notice data together, the status alone with what each choice means.
- The status is `AppRadioCards` with two cards and a one-line consequence each — publishing is the choice that decides who sees the paper, so it gets the explanation, not a bare select.
- No delete on the edition page — an edition is deleted one way, from its row.
- `exam_edition.duplicate` is shown as the top alert only — it concerns three fields at once; the other field codes go to their own field.
- The delete confirmation says the same year, position and board cannot be added again — BR10 keeps deleted rows in the unique index, and rule `ui-project` asks the text to say what happens.
- The aside shows the exam and its content language read-only — the edition has no language of its own (BR2), and the reader sees which one applies.
- The application date gets `Min` = 1 January of the notice year in the calendar, and the rule's text on leaving the field — the calendar only greys days; the message stays the error code's.
- `AppDateField` wraps `MudDatePicker` with `Editable="true"` and the culture's short date pattern; typing is the full keyboard path, the calendar is an aid; with no hint it shows the format, the way `AppLookupField` shows its minimum characters.
- Dates in rows and in the aside use the short date of the reader's culture (`"d"`), the `AppDateColumn` precedent.
- Four new `AppIcons` constants (`Editions`, `Notice`, `Link`, `Calendar`) — semantic names in the one icon family; no new colour.
- 2026-09-28 — The delete action on a published row is disabled with the reason `ExamEditions.Delete.DisabledPublished`; the 409 `exam_edition.published` stays as the Api's guard. AC12 now checks both (owner, screen question 1).
- 2026-09-28 — pt-PT words: position "Posto de trabalho", notice "Aviso", board "Entidade organizadora"; the glossary gets the `Position` row with them and loses the "(?)" on those two rows (owner, screen question 2).
- 2026-09-28 — English dates stay month first (`6/14/2026`): the rule is the reader's culture, the app's `en` is neutral, and `AppDateColumn` already writes them so; the field shows the format under it (owner, screen question 3).
- 2026-09-28 — The Published card keeps "Offered to students in the catalog" although it only becomes true with F-36 — it states what publishing is for, and F-36 is next on this path (owner, approval round).
- 2026-09-28 — `AppDateField` registers a `MudLocalizer` backed by `SharedResources`, so the picker's own controls are read in the reader's language; the keys MudBlazor 9.9 asks for are listed by the build, beyond the two `DateField.*Month` rows (owner, screen question 4).

### From the build design passes (2026-09-28)
Verified against the files (`system-design` and `architect` reports; `ExamEndpoints.cs`, `DeleteIssuingAuthorityHandler.cs`, `ExamStore.cs` read).
- Accepted: the F-34 slice mirrored one-for-one; the current year enters the domain as an `int` ceiling (year + 1 from `TimeProvider` in the handler), so `Simulab.Catalog.Domain` stays clock-free and AC18 calls `Create` with literals.
- Accepted: the editions routes are mapped inside `MapExamEndpoints` on the `exams` group (the group is a local there), so the `catalog.manage` policy is inherited (AC16).
- Accepted: `ExamEditionResponse.Status` is the enum and the request carries `string?` with `ParseStatus()`, so the OpenAPI document names `Draft` and `Published` (AC15).
- Accepted (architect): a blank `Status` is `Draft` only on `POST`; on `PUT` it is `exam_edition.status_invalid`, because a client that omits the field must not silently unpublish a live edition (BR9: the status is required).
- Accepted: a third index, `ix_exam_editions_exam`, for the list, the delete guard and the duplicate check, none of which the tenant-led unique index can serve (the `ix_exams_issuing_authority` precedent). It adds to AC1 without changing it.
- Accepted: column comments on `exam_editions` from the start (`TableDescriptionTests`), and the generated docs (`docs/architecture/Catalog/*`, `docs/api/openapi.json`) regenerated with the migration.
- Noted, not fixed: a soft delete is an UPDATE, so the `Restrict` foreign keys only bite a hard delete; the handler guards are the real protection, with the same small race window F-34 accepted.
- Dropped: nothing.

### From the independent review (2026-09-28)
No blocker, no major. Six minors:
- Fixed: `SaveExamEditionHandler` checks the `Result` of `Update` instead of discarding it.
- Accepted: `MaxLength="4"` on the number input is ignored by browsers (the range check and the Api refuse a 5-digit year); the constant is kept for the item's screen table.
- Accepted: the OpenAPI document names `Draft` and `Published` in the request's description, as it does for the exam's enums; no response schemas exist anywhere in this app's document.
- Accepted: an edition whose board was soft-deleted through the accepted race would be hidden by the inner join while still blocking the exam's delete; the window is the one F-34 accepted.
- Accepted: `/editions/{id}` to `/editions/new` inside the same page instance is not reachable from any link today; to be handled by the first item that adds such a link.
- Accepted: the unique-violation translator runs twice in the `when` filter and the body, the same shape as `ExamStore`.

## Out of scope
- The notice document itself: only the official link is stored; the upload arrives with epic 698 (owner, 2026-09-23).
- Scoring rules, wrong-answer penalty and cut-off: epic 695 (owner, 2026-09-23).
- The notice's syllabus (`NoticeSubject`): epic 692.
- The `InReview` state and any review workflow: epic 698.
- A query endpoint by board and year range: epic 695.
- A list of every edition across exams: not asked.
- Students seeing published editions: F-36. Seed rows: F-37.
- Locking a published edition that questions or simulations point at: when those exist (epics 693 and 695).

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: the app host started from this worktree and an Admin account signed in by the owner. Both are yours: Claude did not start the app host (it starts the shared local PostgreSQL, Redis and Mailpit containers, which needs the owner's yes) and does not enter credentials. Close any app host running from another checkout first: two hosts fight for port 17162. Applying the migration happens at start (Development).

Git Bash and PowerShell 7, from `D:/dev/_icontrol/wt/simulab/f-35-exam-editions-back` (same command in both):

```bash
dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

Expected: the Aspire dashboard URL is printed; Web at https://localhost:7125. To repeat, stop it (Ctrl+C) and run it again.

1. Sign in as an Admin, open **Exams**, and open (or add) an exam. Below the exam form a card **Editions** says the exam has none; "Add edition" is an outlined link, and Save of the exam is still the only filled button. On `/admin/exams/new` the card only says to save the exam first.
2. Choose **Add edition**. Type `ces` in Board and pick one (add a board in **Organizers** first if none), type a Notice year of `2026`, leave the rest empty and Save. A snackbar says "Edition saved.", the title becomes "Edit edition" and the URL carries the edition id.
3. Back on the exam (breadcrumb), add a second edition: same board and year, position `Guarda Municipal de 3ª Classe`, notice reference, an `https://` link and an application date typed as `14/06/2026`; choose **Published** and Save. Then add a third for year `2024`. The section lists 2026 rows before 2024, each with the board acronym, a chip (Draft or Published) and "Applied on 14/06/2026" when there is a date.
4. Try to break the rules: a notice year of `1989` or `2028`, a link `edital.pdf`, an application date `01/01/2025` with year 2026, and a repeated year, board and position (spaces and accents changed). Each shows its message in the error summary or the top alert; nothing is saved.
5. In the section, the delete icon of the Published row is disabled with the reason as tooltip. Open that edition, set it back to **Draft** and Save; then delete it from its row and confirm. It leaves the list. Try to delete the exam (**Exams**) while an edition remains: it is refused with "This exam has editions". In **Organizers**, deleting the board an edition names is refused the same way.
6. Language and format: switch the language to English, then Português (Portugal). Labels, hints and errors follow, and the date field shows `mm/dd/yyyy` in English and `dd/mm/aaaa` in the Portuguese ones. Open `/dev/ui` and check the **Date field** section: empty, filled, error and disabled.
7. Keyboard only, on the edition page: Tab through Board, Notice year, Position, Notice reference, Notice link, Application date (type the date, no mouse), its calendar button, Status (arrows change the card) and Save; Enter on Save saves. Esc closes an open calendar.
8. Sign in as a Student (or use a private window with a Student account) and open `/admin/exams/{any id}/editions/new`: the ordinary Not Found page.

Known limits (agent report, not seen on screen): the date field's clear button keeps MudBlazor's English "Clear" in every language, and in English the picker's own controls use MudBlazor's built-in English. Say if either bothers you.

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-35
- Merge: see `git log --merges` for "Merge feature/F-35" (AB#754)
- Tests: full suite 1579 passed, 0 failed, 56 s test + 20 s build (2026-09-29); Catalog 261, Web 717, architecture 148
- Manual pages: `docs/manual/<pt-BR|pt-PT|en>/exam-editions.md` (new), `exams.md`, `organizers.md`, `index.md`
