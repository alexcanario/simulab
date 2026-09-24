---
feature: F-34
epic: Assessment catalog
status: approved
board: 753
version: 1
---
# Exams back office

## Summary
Admin screens to create, edit and remove an exam under the body that runs it: the issuing authority
(a required link to an organizer), the name, the assessment type (public service exam, certification,
university entrance exam, ENEM), the scope with its state or municipality, and the language of its
content (ADR-0001 #27). The exam board that applies a paper is not here — it belongs to the edition
(F-35), because the same exam changes board between editions. Brings `AppLookupField`, the parent
picker the kit does not have, and the `PublicBody` organizer kind a city hall or a ministry needs.
Source in Simulae: `Exam`, `ContestListPage`, `RegisterExamPage`, `ExamBoardSelect` — all rewritten,
none imported. Needs /agile:screen.

## Start
- Depends on: F-33 (`done`, merged as 756e505) — the `Catalog` module, the `organizers` table and
  `catalog.manage` are all in `main`. Nothing blocks the build.
- Waits on: nothing. `/agile:screen` ran on 2026-09-24 and the owner approved the mockup together
  with the feature, so the build starts from an approved screen.
- Suggested path: the glossary rows and `OrganizerKind.PublicBody` with its three texts; then `Exam`
  in the domain with its `Result` rules and its migration; then the endpoints; then `AppLookupField`
  in the kit with its gallery entry; then the two pages; last the organizer delete block (BR12).
- Can run beside it: nothing — one item in `building` per checkout, and F-35 depends on this one.

## What exists
- The `Catalog` module is complete and merged (F-33, commit 756e505): five projects under `src/Modules/Catalog/`, `CatalogModuleDbContext` on schema `catalog`, the `InitialCatalog` migration, `catalog.manage` seeded into `identity.permissions` and granted to `Admin`, tests in `tests/Modules/Catalog/Simulab.Catalog.Tests`.
- `Organizer` (`Simulab.Catalog.Domain/Entities/Organizer.cs`) is the parent this feature hangs an exam on: `Name`, `Acronym`, `Kind`, `Description`, `Website`, plus the stored `NormalizedName` and `NormalizedAcronym` that `CatalogText.Normalize` fills. `Create`/`Update` answer with `Result`, never throw. It is a `TenantEntity` with soft delete and a null `TenantId`.
- `Simulab.Catalog.Contracts` already holds `CatalogLimits`, `CatalogErrorCodes`, `CatalogPermissions`, `OrganizerKind`, `OrganizerSort`, `OrganizerListQuery` (paging, search, sort, cap 100) and `OrganizerPageResponse`.
- `OrganizerEndpoints` maps `GET`, `POST`, `PUT /{id}`, `DELETE /{id}` under `/api/v1/catalog/organizers`, the whole group behind `PermissionPolicy.NameFor(CatalogPermissions.Manage)`. There is **no** `GET /{id}` and **no** lookup route; the list with `search` and `pageSize` is the only read path.
- `DeleteOrganizerHandler` soft-deletes without checking anything downstream — nothing referenced an organizer until now.
- `/admin/organizers` (`Organizers.razor`, `OrganizerDialog.razor`, `OrganizerText.cs`) is the list-plus-dialog precedent, with `CatalogApiClient` as the module's typed client and `NavigationItems.All` carrying the single `Content` section item.
- UI kit (`Simulab.Web/Components/Ui/`): `AppDataTable` (server-side paging, search), `AppPageHeader`, `AppRowActions`, `AppConfirmDialog`, `AppTextField`, `AppSelectField` (a full in-memory option list, no search), `AppFilterChip`, `AppFormActions`, `AppTruncatedText`, `AppIcons`, the empty/loading/error states. **There is no autocomplete or lookup component** — the picker this feature needs does not exist in any form.
- `SupportedLanguages.All` (`Simulab.Identity.Contracts`) is `["en", "pt-BR", "pt-PT"]` with `Canonical()`; `User.PreferredLanguage` is a plain `string`. No language type exists in `Catalog`.
- `FieldLengthTests` forbids a numeric `MaxLength=` in a page: every limit is a constant in `CatalogLimits`. `RoleResourcesTests` requires a text for every error code in `CatalogErrorCodes`. `ResourceParityTests` covers `SharedResources` in the three languages, which is where the catalog's keys live (F-33 v2 change note).
- Packages: `Directory.Packages.props` already has everything — MudBlazor (so `MudAutocomplete` is available), EF Core with Npgsql, `xunit`, `AwesomeAssertions`, `Testcontainers.PostgreSql`, `bunit`, `Microsoft.AspNetCore.Mvc.Testing`.

Premises of the summary and the epic that the code corrects or qualifies:
- **Simulae's `Exam` has no exam board.** `Exam` is `Name`, `IssuingAuthority` (free text, required), `Scope` (`Federal`/`Estadual`/`Municipal`) and `ScopeDetail` (required unless Federal). The board is attached **per notice**, on purpose: `RegisterExamPage.razor` says so on screen — "A Banca é vinculada por Edital, não aqui — um mesmo Exame pode ter mais de um Edital ao longo do tempo". The epic's "exams under an organizer" is therefore a change of model, not a rename, and it is question 1.
- **Simulae has no assessment type and no content language.** Both are new work; the glossary names `AssessmentType` and its four values, and ADR-0001 #27 fixes the content-language rule.
- **`ExamBoardSelect` is not convertible as it stands.** It is a plain `MudSelect` that loads every board once per instance through `IExamBoardLookupService` and keeps them in memory, with hardcoded pt-BR text and `Icons.Material.Filled.AccountBalance`. Nothing of its code survives the kit's rules (one icon family, `AppSelectField` shape, no hardcoded text, server-side paging); what carries over is the idea of a parent picker showing "Name (ACRONYM)".
- **The exam is a complex entity by rule.** `ui-project` names "question, exam, exam edition, import draft" as the entities edited on their own page, not in a dialog — so this screen is a list route plus a form route, unlike `/admin/organizers`.
- **Nothing today protects an organizer that has exams.** F-33's out-of-scope hands that to this feature, and `DeleteOrganizerHandler` is where it lands (question 6).

## Goal
Give the catalog its middle level: the exam a student aims at. An edition (F-35) is one paper of an exam,
a question belongs to an edition, and a target exam is what the coach plans towards — none of it exists
until an exam can be created under the body that runs it. It also brings the parent picker every later
screen needs, which the kit does not have in any form.

## Users and use cases
- UC1 An Admin opens `/admin/exams` and sees every exam: name, issuing authority, assessment type and scope, sorted by name, paged, with a search box and filters by issuing authority, assessment type and scope.
- UC2 An Admin adds an exam on its own page: it picks the issuing authority by typing part of its name or acronym, then fills the name, the assessment type, the scope (and the state or municipality when the scope asks for it) and the language of the content.
- UC3 An Admin opens an existing exam on the same page, changes it and saves; the list shows the change.
- UC4 An Admin deletes an exam after confirming; it disappears from the catalog.
- UC5 An Admin tries to delete an organizer that is the issuing authority of at least one exam and is refused, with the number of exams in the message.
- UC6 An Admin registers an organizer of the new kind "public body" (a city hall, a state government, a ministry, a public foundation) and uses it as the issuing authority of an exam.
- UC7 A Student or Curator never sees the Exams menu item and gets the ordinary Not Found page on `/admin/exams`; the Api answers 403 `identity.forbidden`.

## Business rules
- BR1 `Exam : TenantEntity` lives in `Simulab.Catalog.Domain/Entities/`, mapped to `catalog.exams`. It is global data: `TenantId` is null and every unique index that includes it is `NULLS NOT DISTINCT` (F-33 BR5, ADR-0001 #8). Deleting is a soft delete.
- BR2 An exam belongs to its **issuing authority**: the body that publishes the notice and defines the positions, the syllabus and the rules of the exam (a city hall, a state government, a ministry, a university, a certifying body). `Exam.IssuingAuthorityId` is a required foreign key to `catalog.organizers`. The **exam board** that elaborates, applies and marks one paper is not here: it belongs to the edition (F-35), because the same exam changes board between editions.
- BR3 The organizer's kind is not checked against the role it plays: any organizer may be an exam's issuing authority. The same institution really does hold both roles (FGV is a board for a public service exam and the issuing authority of its own certifications; USP is the issuing authority of the exam FUVEST runs).
- BR4 `OrganizerKind` gains a fourth value, `PublicBody`, for a city hall, a state or federal government body, a ministry, an agency or a public foundation. It has its name in the three languages, it appears in `/admin/organizers` (list, filter and dialog) and in `OrganizerKindOrder` (B-15), and it supersedes F-33 BR7's list of three.
- BR5 An exam has `Name` (required), `IssuingAuthorityId` (required), `AssessmentType` (required), `Scope` (required), `ScopeDetail` (conditional, BR8) and `ContentLanguage` (required). Every length is a constant in `Simulab.Catalog.Contracts.CatalogLimits`: name 200, scope detail 120.
- BR6 `AssessmentType` is one of `PublicServiceExam`, `Certification`, `UniversityEntranceExam`, `Enem` (glossary). It travels as a string; an unknown value is refused with `exam.assessment_type_invalid` (400), the way F-33 handles `OrganizerKind` after its review.
- BR7 `ExamScope` is one of `National`, `State`, `Municipal`. It travels as a string; an unknown value is `exam.scope_invalid` (400).
- BR8 `ScopeDetail` is required when the scope is `State` or `Municipal` — the state or the municipality — and is refused with `exam.scope_detail_required`. When the scope is `National` it is stored as null whatever the request sent. Longer than 120 characters is `exam.scope_detail_too_long`.
- BR9 `ContentLanguage` is one of `SupportedLanguages.All` (`en`, `pt-BR`, `pt-PT`), canonicalized by `SupportedLanguages.Canonical` so `PT-br` is stored as `pt-BR`; anything else is `exam.content_language_invalid`. It is the language of the exam's content and is never translated (ADR-0001 #27). The form offers `pt-BR` selected.
- BR10 The name is trimmed and 2 to 200 characters (`exam.name_required`, `exam.name_too_long`). It is unique **within its issuing authority**, ignoring case and accents, soft-deleted rows included: a unique index over a stored normalized column (`CatalogText.Normalize`, the F-33 technique) covering `TenantId`, `IssuingAuthorityId` and `NormalizedName`, with `NULLS NOT DISTINCT`. A duplicate is refused with `exam.name_taken` (409). Moving an exam to another issuing authority whose exams already hold that name is the same conflict.
- BR11 An `IssuingAuthorityId` that matches no organizer — or a soft-deleted one — is refused with 404 `organizer.not_found`, the code F-33 already defines and translates.
- BR12 Deleting an organizer that is the issuing authority of at least one exam, deleted exams excluded, is refused with 409 `organizer.has_exams`; the message says the exams must be deleted first, on the Exams page. It carries no count: `ErrorText.For` maps a code to a text and takes no argument, and giving the shared helper one for a single message is not worth it (the Admin sees them by filtering `/admin/exams` by that body). Deleting an organizer with no exam still works as it did.
- BR13 The exam has no publication state of its own: it is published through its editions (F-35), and a student sees an exam that has at least one published edition (F-36). Nothing blocks deleting an exam in F-34, because nothing points at it yet; F-35 adds that block when editions exist.
- BR14 The list is paged (`page`, `pageSize`, cap 100) and returns `{ items, total }`. Search matches the normalized name. The filters are issuing authority (by id), assessment type and scope, combined with AND, offered as three fields in the table's `ToolBarContent`. Sorting is by name, issuing authority, assessment type or scope; the default is name ascending. Sorting by a translated label follows the caller's order, as `OrganizerKindOrder` does for the kind (B-15).
- BR15 Domain rules return `Result`, never exceptions: `Exam.Create` and `Exam.Update` validate BR8 to BR10 and give back an `Error` with its code.
- BR16 `AppLookupField` is a new UI kit component: a searchable field that asks the server for a page of candidates as the user types, shows one line per candidate, keeps the chosen one's id, and has `Id`, `Label` and the search callback as `[EditorRequired]` (rule `ui`). It is shown on `/dev/ui` with its loading, no-result and error states, and it is the only way any screen picks a parent record from now on. For the issuing authority it calls the organizer list with `search`, showing `Name (ACRONYM)`.
- BR17 Every UI text of the two screens, the new kit component and every new error code exists in pt-BR, pt-PT and en, in `SharedResources` in the Web host (F-33 v2).

## Screens and API

Mockup: [`docs/features/mockups/F-34-exams-back-office.html`](mockups/F-34-exams-back-office.html) — every state below,
with the theme switch and the three languages.

### Route map
| Route | Purpose | Gate |
|---|---|---|
| `/admin/exams` | the list | `[Authorize(Policy = PermissionPolicy.Prefix + CatalogPermissions.Manage)]`, `MainLayout` |
| `/admin/exams/new` | the form, adding | the same |
| `/admin/exams/{id:guid}` | the form, editing | the same |
| `/dev/ui` | `AppLookupField` added to the gallery | development only |

Menu: `NavigationSection.Content`, key `Nav.Exams`, `AppIcons.Exams` (new semantic name, Material Outlined
`Description`), gated by `catalog.manage`, placed after `Nav.Organizers`.

### `/admin/exams` — the list
- `AppPageHeader`: breadcrumb `Content › Exams` (both disabled, the pattern of `/admin/organizers`), title `Exams.Title`, primary action `Common.Add` with `AppIcons.Add`, which navigates to `/admin/exams/new` — it does not open a dialog.
- `AppDataTable` with `Searchable="true"` over the name, and the three filters in `ToolBarContent`, in this order: issuing authority (an `AppLookupField` with a clear button), assessment type (`AppSelectField`, first option `Common.Filter.All`), scope (`AppSelectField`, first option `Common.Filter.All`). No new kit parameter is needed: `ToolBarContent` already exists.
- Columns, in order:
  | Column | Content | Sortable |
  |---|---|---|
  | `Exams.Column.Name` | `AppTruncatedText`, `MaxWidth="26rem"` — a name is up to 200 characters | yes, default ascending |
  | `Exams.Column.IssuingAuthority` | `Name (ACRONYM)`, the name truncated at `14rem` | yes |
  | `Exams.Column.AssessmentType` | the translated label | yes, in the caller's order (B-15) |
  | `Exams.Column.Scope` | the translated label, with `ScopeDetail` under it as a caption when there is one | yes, in the caller's order |
- `AppRowActions` with edit (navigates to `/admin/exams/{id}`) and delete (`AppConfirmDialog` through `IConfirmService`).
- Paging, page sizes and the footer come from `AppDataTable` unchanged.

### `/admin/exams/new` and `/admin/exams/{id:guid}` — the form page
The exam is a complex entity (rule `ui-project`), so the form is a page, not a dialog. It is also where F-35
adds the editions section, below the card.

- `AppPageHeader`: breadcrumb `Content › Exams › <Exams.Form.AddTitle | the exam's name>`, with `Exams.Title` linking back to the list; title `Exams.Form.AddTitle` or `Exams.Form.EditTitle`. **No primary action in the header** — the only primary button on the page is Save, at the bottom right of the form (rule `ui`).
- One card, one column, at most `44rem` wide, fields in this order:
  | Field | Component | Required | Limits and behaviour |
  |---|---|---|---|
  | `Exams.Field.IssuingAuthority` | `AppLookupField` | yes | searches organizers by name or acronym from 2 characters; the option shows `Name (ACRONYM)` with the kind as a second line; the chosen one stays in the field with a clear button. Hint `Exams.Field.IssuingAuthority.Hint` |
  | `Exams.Field.Name` | `AppTextField` | yes | 2 to 200 (`CatalogLimits.ExamNameMaxLength`), counter, hint `Exams.Field.Name.Hint` |
  | `Exams.Field.AssessmentType` | `AppSelectField` | yes | the four labels, nothing preselected |
  | `Exams.Field.Scope` | `AppSelectField` | yes | the three labels, nothing preselected |
  | `Exams.Field.State` / `Exams.Field.Municipality` | `AppTextField` | conditional | shown **only** when the scope is `State` or `Municipal`, and then required; the label follows the scope; at most 120 (`CatalogLimits.ExamScopeDetailMaxLength`). Changing the scope to `National` hides it and drops what was typed |
  | `Exams.Field.ContentLanguage` | `AppSelectField` | yes | the three cultures by `SupportedCultures.NativeName` ("Português (Brasil)", "Português (Portugal)", "English"), so each is shown in its own language and needs no resource key; `pt-BR` preselected. Hint `Exams.Field.ContentLanguage.Hint` |
- `AppFormActions`: `Common.Save` (filled, right) then `Common.Cancel` (text) which returns to the list. Saving keeps the Admin on the page: after a create the route is replaced by `/admin/exams/{id}` and the form becomes the edit form, so the editions F-35 adds below are one scroll away. While saving the primary button is disabled and shows progress (rule `ui`).
- Leaving with unsaved changes asks for confirmation (`Common.Discard.Title` / `Common.Discard.Message`, the kit's existing confirmation).
- Validation on leaving the field and on submit; a server business error is an alert at the top of the card and, when it belongs to one field (`exam.name_taken`), also next to that field.

### `AppLookupField` — new kit component (BR16)
- Wraps `MudAutocomplete`, the same field anatomy as `AppTextField` and `AppSelectField`: label above with the required marker, the control, then the hint or the error.
- Parameters: `Id`, `Label`, `SearchAsync` (`Func<string, CancellationToken, Task<IReadOnlyList<AppLookupOption>>>`) and `Placeholder` are `[EditorRequired]` — what to search and how to name it depends on what the page means (rule `ui`). Optional: `Value`, `ValueChanged`, `Hint`, `Error`, `Required`, `Disabled`, `MinChars` (default 2), `Clearable` (default true).
- `AppLookupOption(Guid Id, string Text, string? Secondary)`: the line and the smaller line under it.
- Its own states, all in the gallery: idle (nothing chosen), below the minimum characters (hint `Lookup.Hint.MinChars`), searching (the field's progress), results open, no result (`Lookup.NoResults`), search failed (`Lookup.LoadFailed` with `Common.TryAgain`), chosen (the text with a clear button, `Lookup.Clear`).
- It debounces by 300 ms and asks the server for one page of at most 20 candidates; it never caches a list in memory (which is exactly what `ExamBoardSelect` did wrong).

### `/admin/organizers` — what changes (BR4, BR12)
- The kind `AppSelectField` in `OrganizerDialog`, the kind column and the kind order (B-15) gain `OrganizerKind.PublicBody`; nothing else on that screen moves.
- Deleting an organizer that is an issuing authority is refused: the confirmation is accepted, the Api answers 409, and the existing `_error` alert at the top of the list shows `organizer.has_exams`. The organizer stays in the list.
- The delete confirmation text itself does not change: it still says the organizer leaves the catalog. It does not promise anything about exams, because the refusal is what tells the Admin.

### API
- `GET /api/v1/catalog/exams?page=&pageSize=&search=&issuingAuthorityId=&assessmentType=&scope=&sortBy=&descending=&assessmentTypeOrder=&scopeOrder=` — one page: `{ items, total }`. Each item is `ExamResponse(Id, Name, IssuingAuthorityId, IssuingAuthorityName, IssuingAuthorityAcronym, AssessmentType, Scope, ScopeDetail, ContentLanguage)`.
- `GET /api/v1/catalog/exams/{id:guid}` — one `ExamResponse`, for the form page; 404 `exam.not_found`.
- `POST /api/v1/catalog/exams` — `SaveExamRequest(IssuingAuthorityId, Name, AssessmentType, Scope, ScopeDetail, ContentLanguage)`; 201 with the `ExamResponse`. `AssessmentType`, `Scope` and `ContentLanguage` are `string?` in the contract and parsed by the handler, so an unknown value returns its own code instead of failing inside deserialization (the lesson F-33's review left).
- `PUT /api/v1/catalog/exams/{id:guid}` — the same request; 200.
- `DELETE /api/v1/catalog/exams/{id:guid}` — 204.
- The organizer routes do not change their shape; `DELETE /api/v1/catalog/organizers/{id}` gains the 409.
- Error codes: as listed in BR5 to BR12. Every one of them has a text in the three languages (BR17).

### States
| State | What the screen shows |
|---|---|
| List — loading | `AppDataTable`'s loading state; the toolbar and the filters are already visible and disabled |
| List — ready | rows, sorted by name ascending, the footer with the total |
| List — empty (no exam at all) | `Exams.Empty` with the primary action `Common.Add` |
| List — search with no match | `Exams.Empty.Search` with the term |
| List — filters with no match | `Exams.Empty.Filters` |
| List — server error | the kit error state with `Common.TryAgain` |
| List — delete confirmation | `AppConfirmDialog`, the confirm button in the error colour naming the exam (`Common.Delete.Confirm`), message `Exams.Delete.Message` |
| List — deleted | snackbar `Exams.Deleted` |
| List — delete refused (another Admin already deleted it) | the alert at the top with `exam.not_found`, and the list reloads |
| Form — loading (editing) | the card with a centred progress, the actions disabled |
| Form — add, empty | every field empty, nothing preselected except the content language |
| Form — edit, filled | the fields filled; the state or municipality visible because the scope asks for it |
| Form — lookup below the minimum | the hint `Lookup.Hint.MinChars` |
| Form — lookup searching | the field's own progress and `Lookup.Searching` announced |
| Form — lookup results | the list of `Name (ACRONYM)` with the kind under each |
| Form — lookup no result | `Lookup.NoResults` with the term |
| Form — lookup failed | `Lookup.LoadFailed` with `Common.TryAgain` inside the field's description |
| Form — validation error | the message under the field; the first field in error takes focus on submit |
| Form — server error, name taken | the alert at the top of the card plus the message on the name field |
| Form — server error, issuing authority gone | the alert at the top with `organizer.not_found`; the lookup is cleared |
| Form — saving | Save disabled with progress, Cancel disabled |
| Form — saved | the page stays open, now in edit mode: the title becomes `Exams.Form.EditTitle`, the URL becomes `/admin/exams/{id}`, the breadcrumb shows the exam's name, snackbar `Exams.Saved` |
| Form — exam not found (edit) | the alert `exam.not_found` and a link back to the list; no fields |
| Form — unsaved changes | the kit confirmation before leaving |
| Organizers — delete refused | the alert at the top with `organizer.has_exams` |
| Permission denied | the ordinary Not Found page on both routes, and no Exams item in the menu |

### UI texts
All in `SharedResources` (F-33 v2). `Common.Add`, `Common.Edit`, `Common.Delete`, `Common.Save`, `Common.Saving`,
`Common.Cancel`, `Common.Search`, `Common.TryAgain`, `Common.Actions`, `Common.Delete.Title`,
`Common.Delete.Confirm`, `Common.RemoveFilter` and the table's paging texts already exist and are reused.

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Nav.Exams` | Exams | Exames | Exames |
| `Exams.Title` | Exams | Exames | Exames |
| `Exams.Search.Placeholder` | Search by name | Pesquisar pelo nome | Pesquisar pelo nome |
| `Exams.Empty` | No exam in the catalog yet. | Nenhum exame no catálogo ainda. | Ainda não há exames no catálogo. |
| `Exams.Empty.Search` | No exam matches "{0}". | Nenhum exame corresponde a "{0}". | Nenhum exame corresponde a "{0}". |
| `Exams.Empty.Filters` | No exam matches the chosen filters. | Nenhum exame corresponde aos filtros escolhidos. | Nenhum exame corresponde aos filtros escolhidos. |
| `Exams.Column.Name` | Name | Nome | Nome |
| `Exams.Column.IssuingAuthority` | Issuing authority | Órgão contratante | Entidade contratante |
| `Exams.Column.AssessmentType` | Assessment type | Tipo de avaliação | Tipo de avaliação |
| `Exams.Column.Scope` | Scope | Abrangência | Abrangência |
| `Exams.Filter.IssuingAuthority` | Issuing authority | Órgão contratante | Entidade contratante |
| `Exams.Filter.AssessmentType` | Assessment type | Tipo de avaliação | Tipo de avaliação |
| `Exams.Filter.Scope` | Scope | Abrangência | Abrangência |
| `Common.Filter.All` | All | Todos | Todos |
| `Exams.Form.AddTitle` | Add exam | Adicionar exame | Adicionar exame |
| `Exams.Form.EditTitle` | Edit exam | Editar exame | Editar exame |
| `Exams.Field.IssuingAuthority` | Issuing authority | Órgão contratante | Entidade contratante |
| `Exams.Field.IssuingAuthority.Hint` | The body that publishes the notice and sets the rules. The board that applies each paper belongs to the edition. | O órgão que publica o edital e define as regras. A banca que aplica cada prova pertence à edição. | A entidade que publica o aviso e define as regras. A entidade que aplica cada prova pertence à edição. |
| `Exams.Field.IssuingAuthority.Placeholder` | Name or acronym | Nome ou sigla | Nome ou sigla |
| `Exams.Field.Name` | Name | Nome | Nome |
| `Exams.Field.Name.Hint` | 2 to 200 characters | De 2 a 200 caracteres | Entre 2 e 200 caracteres |
| `Exams.Field.AssessmentType` | Assessment type | Tipo de avaliação | Tipo de avaliação |
| `Exams.Field.Scope` | Scope | Abrangência | Abrangência |
| `Exams.Field.State` | State | Estado | Estado |
| `Exams.Field.Municipality` | Municipality | Município | Município |
| `Exams.Field.ScopeDetail.Hint` | Where this exam applies. | Onde este exame se aplica. | Onde este exame se aplica. |
| `Exams.Field.ContentLanguage` | Content language | Idioma do conteúdo | Idioma do conteúdo |
| `Exams.Field.ContentLanguage.Hint` | The language the exam and its questions are written in. Content is never translated. | O idioma em que o exame e suas questões estão escritos. O conteúdo nunca é traduzido. | O idioma em que o exame e as suas questões estão escritos. O conteúdo nunca é traduzido. |
| `Exams.Saved` | Exam saved. | Exame salvo. | Exame guardado. |
| `Exams.Deleted` | Exam deleted. | Exame excluído. | Exame eliminado. |
| `Exams.Delete.Message` | The exam {0} leaves the catalog. Its name stays reserved inside {1}. | O exame {0} sai do catálogo. O nome continua reservado dentro de {1}. | O exame {0} sai do catálogo. O nome continua reservado dentro de {1}. |
| `Exams.BackToList` | Back to the exams | Voltar para os exames | Voltar para os exames |
| `AssessmentType.PublicServiceExam` | Public service exam | Concurso público | Concurso público |
| `AssessmentType.Certification` | Certification | Certificação | Certificação |
| `AssessmentType.UniversityEntranceExam` | University entrance exam | Vestibular | Exame de acesso |
| `AssessmentType.Enem` | ENEM | ENEM | ENEM |
| `ExamScope.National` | National | Nacional | Nacional |
| `ExamScope.State` | State | Estadual | Estadual |
| `ExamScope.Municipal` | Municipal | Municipal | Municipal |
| `OrganizerKind.PublicBody` | Public body | Órgão público | Organismo público |
| `Lookup.Hint.MinChars` | Type at least {0} characters | Digite ao menos {0} caracteres | Escreva pelo menos {0} caracteres |
| `Lookup.Searching` | Searching... | Pesquisando... | A pesquisar... |
| `Lookup.NoResults` | Nothing found for "{0}". | Nada encontrado para "{0}". | Nada encontrado para "{0}". |
| `Lookup.LoadFailed` | The search failed. | A pesquisa falhou. | A pesquisa falhou. |
| `Lookup.Clear` | Clear the selection | Limpar a seleção | Limpar a seleção |
| `exam.not_found` | This exam no longer exists. | Este exame não existe mais. | Este exame já não existe. |
| `exam.name_required` | Enter a name with at least 2 characters. | Informe um nome com ao menos 2 caracteres. | Indique um nome com pelo menos 2 caracteres. |
| `exam.name_too_long` | The name may have at most 200 characters. | O nome pode ter no máximo 200 caracteres. | O nome pode ter no máximo 200 caracteres. |
| `exam.name_taken` | This issuing authority already has an exam with this name. | Este órgão já tem um exame com este nome. | Esta entidade já tem um exame com este nome. |
| `exam.issuing_authority_required` | Choose the issuing authority. | Escolha o órgão contratante. | Escolha a entidade contratante. |
| `exam.assessment_type_invalid` | Choose an assessment type. | Escolha um tipo de avaliação. | Escolha um tipo de avaliação. |
| `exam.scope_invalid` | Choose a scope. | Escolha a abrangência. | Escolha a abrangência. |
| `exam.scope_detail_required` | Say where this exam applies. | Informe onde este exame se aplica. | Indique onde este exame se aplica. |
| `exam.scope_detail_too_long` | This field may have at most 120 characters. | Este campo pode ter no máximo 120 caracteres. | Este campo pode ter no máximo 120 caracteres. |
| `exam.content_language_invalid` | Choose the language of the content. | Escolha o idioma do conteúdo. | Escolha o idioma do conteúdo. |
| `organizer.has_exams` | This organizer is the issuing authority of exams in the catalog. Delete those exams first, on the Exams page. | Esta organização é o órgão contratante de exames no catálogo. Exclua esses exames primeiro, na página Exames. | Esta organização é a entidade contratante de exames no catálogo. Elimine esses exames primeiro, na página Exames. |

The pt-PT column keeps the Brazilian wording for `ExamScope.State` and `Exams.Field.State`; Portugal has districts,
not states, and epic 704 revisits it when Portuguese exams arrive.

### Accessibility
- Every field has a `<label>` pointing at its input, the required marker is `aria-hidden` and the requirement travels as `aria-required`, and the hint or error is the input's `aria-describedby` — the anatomy `AppTextField` and `AppSelectField` already have.
- `AppLookupField` is a combobox: `role="combobox"`, `aria-expanded`, `aria-controls` and `aria-activedescendant` from `MudAutocomplete`; the result count and `Lookup.Searching` are announced through an `aria-live="polite"` region, and Escape closes the list without clearing the choice.
- Tab order on the form: breadcrumb link, issuing authority, its clear button, name, assessment type, scope, state or municipality when visible, content language, Save, Cancel. Showing the conditional field does not move the focus; hiding it while it has the focus moves the focus to the scope select.
- On submit with a validation error the focus goes to the first field in error and the alert at the top of the card is `role="alert"`.
- The delete confirmation is an `alertdialog` whose confirm button names the exam, never "Yes".
- Both pages were checked in light and dark, in the three languages, and by keyboard alone, in the mockup and again on screen during validation.
- No new colour: every surface, text and border in these screens comes from the existing `SimulabTheme` tokens, which `ThemeContrastTests` and `ThemePaletteTests` already cover. The scope detail caption under the scope label uses `TextSecondary`, which reads 6.92:1 on the surface in light and 4.89:1 in dark (F-17 and B-10 measured both).

## Acceptance criteria
- AC1 Given a database with the previous migration, when the new migration runs, then `catalog.exams` exists with a required foreign key to `catalog.organizers` and a unique index over `TenantId`, `IssuingAuthorityId` and the normalized name, `NULLS NOT DISTINCT`. (BR1, BR10)
- AC2 Given a signed-in Admin, when they open `/admin/exams`, then they see the list with name, issuing authority, assessment type and scope, sorted by name ascending. (UC1, BR14)
- AC3 Given a signed-in Student, when they open `/admin/exams` or `/admin/exams/new`, then they get Not Found, the Exams item is not in their menu, and every `/api/v1/catalog/exams` route answers 403 `identity.forbidden`. (UC7)
- AC4 Given the form page with an issuing authority, a name, an assessment type, the scope `National` and a content language, when the Admin saves, then the exam is created with `ScopeDetail` null and appears in the list. (UC2, BR5, BR8)
- AC5 Given the scope `Municipal` and no state or municipality, when the Admin saves, then the Api answers 400 `exam.scope_detail_required`, no row is written, and the message sits next to that field. (BR8)
- AC6 Given an exam of an issuing authority, when the Admin saves another one for the same authority whose name differs only in case or accents, then the Api answers 409 `exam.name_taken` and the message shows on the name field. (BR10)
- AC7 Given two issuing authorities, when each gets an exam with the same name, then both are created. (BR10)
- AC8 Given two rows inserted directly with `TenantId` null, the same issuing authority and the same normalized name, when the second is saved, then PostgreSQL rejects it. (BR1, BR10)
- AC9 Given a request whose `issuingAuthorityId` matches no organizer, or matches a deleted one, when it is sent, then the Api answers 404 `organizer.not_found` and nothing is written. (BR11)
- AC10 Given an assessment type, a scope or a content language that is not one of the allowed names, when the request is sent, then the Api answers 400 with `exam.assessment_type_invalid`, `exam.scope_invalid` or `exam.content_language_invalid`, and the OpenAPI document names the allowed values. (BR6, BR7, BR9)
- AC11 Given `pt-br` as the content language, when the exam is saved, then it is stored as `pt-BR`. (BR9)
- AC12 Given an existing exam, when the Admin opens `/admin/exams/{id}`, changes the name and the scope and saves, then the list shows the change and the row keeps its id. (UC3)
- AC12b Given the add form filled, when the Admin saves, then the page stays open with the title `Exams.Form.EditTitle`, the route `/admin/exams/{id}` of the new exam and the snackbar `Exams.Saved`; going back to the list shows it. (UC2)
- AC13 Given an exam, when the Admin confirms the deletion, then it disappears from the list and from `GET /api/v1/catalog/exams`, the row is still in the table with `IsDeleted` true, and creating a new exam with that name under the same issuing authority is refused with `exam.name_taken`. (UC4, BR1, BR10)
- AC14 Given an organizer that is the issuing authority of two exams, when the Admin confirms its deletion, then the Api answers 409 `organizer.has_exams`, the organizer is still listed, and the screen shows the translated message. (UC5, BR12)
- AC15 Given an organizer whose only exam was deleted, when the Admin deletes the organizer, then it is deleted. (BR12)
- AC16 Given exams whose names differ only by accents, when the Admin searches without accents, then all of them are listed; and given the three filters set together, then only the exams matching all three come back, with `total` counting them. (BR14)
- AC17 Given the new kind, when the Admin opens `/admin/organizers`, then "public body" is offered in the dialog, shown in the list, available in the kind filter and placed in the caller's kind order; and an exam can be created under an organizer of that kind. (UC6, BR4)
- AC18 Given `AppLookupField` on `/dev/ui` and on the form page, when the Admin types two letters, then the server is asked with that term and the matching organizers are offered as `Name (ACRONYM)`; with no match the no-result state shows; when the call fails the error state offers "try again"; and the whole field is reachable and choosable by keyboard alone. (BR16)
- AC18b Given the form with the scope `Municipal` and a municipality typed, when the Admin changes the scope to `National`, then the field disappears, what was typed is dropped, and saving stores `ScopeDetail` null. (BR8)
- AC18c Given `/admin/exams/{id}` for an id that is not an exam, or one that was deleted, then the page shows `exam.not_found` with a link back to the list and no fields. (BR5)
- AC19 Given `Exam.Create` with a blank name, when it runs, then it returns a failed `Result` with `exam.name_required` and throws nothing. (BR15)
- AC20 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR17)

## Decisions
- 2026-09-24 — An exam belongs to its **issuing authority** (the body that publishes the notice), and the **exam board** belongs to the edition (BR2) — the owner separated the three actors of a public service exam: the contracting body defines the positions, the syllabus and the rules; the board elaborates, applies and marks with its own criteria, and changes between editions of the same exam; the author of practice content is a third actor. Tying the exam to the board would duplicate the same exam at every change of board. The epic's wording "exams under an organizer" is kept, with the organizer being the issuing authority (owner, question 1, revised after the owner's correction).
- 2026-09-24 — One `organizers` table for both roles, with no check of kind against role (BR3) — the same institution holds both roles for real (FGV, USP, AWS), so the role belongs to the relationship, not to the row; a rule per kind would refuse correct data (owner, follow-up question 3).
- 2026-09-24 — `OrganizerKind` gains `PublicBody` (BR4) — a city hall, a ministry or a public foundation is none of the three kinds F-33 named, and it is the commonest issuing authority in Brazil. `PrivateCompany` was offered and left out: no use for it yet (owner, follow-up question 2).
- 2026-09-24 — The issuing authority is a required link, not free text — "Prefeitura de Guarulhos" and "PREF. GUARULHOS" would live side by side in a text column, and F-36's filter by body needs the id (owner, follow-up question 1).
- 2026-09-24 — `ExamScope` is `National` / `State` / `Municipal`, always required, with the detail required for the last two (BR7, BR8) — one rule instead of a validation conditional on the assessment type; ENEM and certifications are `National` (owner, question 3).
- 2026-09-24 — The content language reuses `SupportedLanguages.All` from `Simulab.Identity.Contracts` (BR9) — one list, no drift; F-33 BR1 allows a module to reference another module's `Contracts`. If a content language ever needs to exist without a UI in that language, a `CatalogLanguages` list of its own is the change to make then (owner, question 4).
- 2026-09-24 — The exam name is unique within its issuing authority, not across the catalog (BR10) — "Agente" may exist in two different bodies, and a catalog-wide rule would force the body into the name it is already linked to (owner, question 5).
- 2026-09-24 — Deleting an organizer that has exams is refused with 409 `organizer.has_exams` (BR12) — cascade would let one click remove a whole catalog, and leaving the exams orphaned contradicts the required link (owner, question 6).
- 2026-09-24 — The organizer never gets an active/inactive status — a simulation reproduces something that already happened, so a catalog record is historical and is never "retired"; the refusal to delete is the only protection needed. This closes what F-33 deferred here, with no idea captured (owner, question 7).
- 2026-09-24 — The exam has no publication state; publication lives on the edition (BR13) — two states can disagree (a draft exam holding a published edition), and F-36 only needs one place to decide visibility (owner, question 8).
- 2026-09-24 — List plus a form on its own page (`/admin/exams`, `/admin/exams/new`, `/admin/exams/{id}`) — rule `ui-project` names the exam among the complex entities, and it is where F-35 adds the editions section without redoing the screen (owner, question 9).
- 2026-09-24 — `AppLookupField` is a new kit component, a server-backed searchable field (BR16) — the kit has no search field at all, `AppSelectField` loads every option in memory, and F-37 plus the imports of epic 698 will bring hundreds of organizers. It serves every later parent picker (edition, subject, question) (owner, question 10).
- 2026-09-24 — The list has search plus three filters, including scope (BR14) — municipal exams are the commonest case and the scope filter is what separates them (owner, question 11).
- 2026-09-24 — `/agile:screen` runs before the build — first form-on-its-own-page screen of Simulab and first search component of the kit, with no precedent in the repository to copy (owner, question 12).
- 2026-09-24 — No new package (rule `build-config`): `MudAutocomplete`, which `AppLookupField` wraps, ships with the MudBlazor already in `Directory.Packages.props`, and every test library this feature needs is there (owner, question 13).
- 2026-09-24 — Simulae's `Exam` is not imported, it is rewritten — it throws `ContentCatalogDomainException` instead of answering with a `Result`, its messages are pt-BR strings inside the domain, it has no assessment type and no content language, and its `IssuingAuthority` is free text. What carries over is the field set and the idea of a scope with a detail.
- 2026-09-24 — `ExamBoardSelect` is not converted either: it is a `MudSelect` that loads every board into memory, with hardcoded pt-BR text and a Filled icon. `AppLookupField` is written fresh against the kit's rules; what carries over is the `Name (ACRONYM)` display.
- 2026-09-24 — No seed rows: F-37 brings the real exams, remapped and tested (the same decision F-33 took).
- 2026-09-24 — The glossary gains the rows for `IssuingAuthority`, `OrganizerKind.PublicBody`, `ExamScope` and `ContentLanguage` before the first use in code (rule `naming`).
- 2026-09-24 — The generated technical docs are regenerated with the new table: `tools/Simulab.DocGen` already references `Simulab.Catalog.Infrastructure`, so no project reference changes.

### From the screen design (2026-09-24)
- The form page's header carries **no** primary action: the page's single primary button is Save, at the bottom right of the form (rule `ui`). `/admin/organizers` keeps its header action because it opens a dialog.
- The conditional field's label follows the scope (`State` / `Municipality`) instead of one neutral "Area or region" — Simulae used a neutral label and had to explain it in a hint on every screen.
- The content language options are `SupportedCultures.NativeName` ("Português (Brasil)", "Português (Portugal)", "English"), not resource keys — a content language shown in its own language does not depend on the UI language, and it saves three keys per language.
- The three filters go in `AppDataTable`'s existing `ToolBarContent`; the table needs no new parameter, and `AppFilterChip` is not used (it shows an active filter that came from a link, which is not this case).
- `AppLookupField` debounces 300 ms, asks for at most 20 candidates and caches nothing — the opposite of `ExamBoardSelect`, which loaded every board into memory per instance.
- Saving keeps the Admin on the form page, which becomes the edit form of the new exam, instead of returning to the list — adding the first edition is the usual next step, and in F-35 that section is on this same page (owner, screen question 1).
- The state or municipality stays free text: the 27 states are data the catalog does not hold and the 5570 municipalities are a feature of their own. A state picker is captured as F-42 (owner, screen question 2).
- The search box searches the exam's name only; the issuing authority has its own filter beside it, and a search over both would answer questions the Admin did not ask (owner, screen question 3).
- F-35's editions section is reserved below the form card, on the same page; it is not drawn in the mockup, because inventing its shape now is out of this item's scope.
- No new colour in either screen: every surface, text and border is an existing `SimulabTheme` token already covered by `ThemeContrastTests` and `ThemePaletteTests`, so no contrast number had to be computed for this feature.
- `organizer.has_exams` carries no count (BR12) — `ErrorText.For` takes a code and no argument.

## Out of scope
- Editions, and the exam board that applied a paper: F-35. Blocking the deletion of an exam that has editions arrives with them.
- Students browsing and filtering the catalog: F-36.
- Seed rows for exams and organizers: F-37.
- The author of practice content — a course, a teacher, Simulab itself: it is an attribute of the question and of the practice session, not of the exam (epics 693 and 696).
- The notice document and the syllabus of an edition: F-35 (link only) and epic 692.
- Scoring rules, wrong-answer penalty and cut-off: epic 695.
- An audit trail of catalog changes, like F-14's role history.
- An active/inactive status on the organizer or on the exam: decided against, not deferred.
- A `Country` field on the organizer, and anything multi-tenant: epics 704 and 703.
- Splitting `catalog.manage` into finer permissions.

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-34
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
