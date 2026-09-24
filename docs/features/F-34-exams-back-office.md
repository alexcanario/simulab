---
feature: F-34
epic: Assessment catalog
status: validating
board: 753
version: 2
---
# Exams back office

## Summary
Admin screens to create, edit and remove an exam under the body that runs it: the issuing authority
(a required link to an `IssuingAuthority`, v2), the name, the assessment type (public service exam, certification,
university entrance exam, ENEM), the scope with its state or municipality, and the language of its
content (ADR-0001 #27). The exam board that applies a paper is not here — it belongs to the edition
(F-35), because the same exam changes board between editions. Brings `AppLookupField`, the parent
picker the kit does not have, and the issuing-authority back office the exam hangs on (v2).
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
- UC5 An Admin tries to delete an issuing authority that has at least one exam and is refused. (v2)
- UC6 An Admin registers an issuing authority (a city hall, a state government, a ministry, a university, a company) at `/admin/issuing-authorities` and uses it as the parent of an exam. (v2)
- UC7 A Student or Curator never sees the Exams menu item and gets the ordinary Not Found page on `/admin/exams`; the Api answers 403 `identity.forbidden`.

## Business rules
- BR1 `Exam : TenantEntity` lives in `Simulab.Catalog.Domain/Entities/`, mapped to `catalog.exams`. It is global data: `TenantId` is null and every unique index that includes it is `NULLS NOT DISTINCT` (F-33 BR5, ADR-0001 #8). Deleting is a soft delete.
- BR2 An exam belongs to its **issuing authority**: the body that publishes the notice and defines the positions, the syllabus and the rules of the exam (a city hall, a state government, a ministry, a university, a company). `Exam.IssuingAuthorityId` is a required foreign key to `catalog.issuing_authorities` (BR18). The **exam board** that elaborates, applies and marks one paper is not here: it is the `Organizer` of F-33, and it belongs to the edition (F-35), because the same exam changes board between editions. (v2)
- BR3 *(withdrawn in v2: the two roles are two tables now, so there is no kind to check against a role.)*
- BR4 *(withdrawn in v2: `OrganizerKind` keeps F-33 BR7's three values. `PublicBody` existed only to let a city hall be an organizer, which is what BR18 replaced.)*
- BR5 An exam has `Name` (required), `IssuingAuthorityId` (required), `AssessmentType` (required), `Scope` (required), `ScopeDetail` (conditional, BR8) and `ContentLanguage` (required). Every length is a constant in `Simulab.Catalog.Contracts.CatalogLimits`: name 200, scope detail 120.
- BR6 `AssessmentType` is one of `PublicServiceExam`, `Certification`, `UniversityEntranceExam`, `Enem` (glossary). It travels as a string; an unknown value is refused with `exam.assessment_type_invalid` (400), the way F-33 handles `OrganizerKind` after its review.
- BR7 `ExamScope` is one of `National`, `State`, `Municipal`. It travels as a string; an unknown value is `exam.scope_invalid` (400).
- BR8 `ScopeDetail` is required when the scope is `State` or `Municipal` — the state or the municipality — and is refused with `exam.scope_detail_required`. When the scope is `National` it is stored as null whatever the request sent. Longer than 120 characters is `exam.scope_detail_too_long`.
- BR9 `ContentLanguage` is one of `SupportedLanguages.All` (`en`, `pt-BR`, `pt-PT`), canonicalized by `SupportedLanguages.Canonical` so `PT-br` is stored as `pt-BR`; anything else is `exam.content_language_invalid`. It is the language of the exam's content and is never translated (ADR-0001 #27). The form offers `pt-BR` selected.
- BR10 The name is trimmed and 2 to 200 characters (`exam.name_required`, `exam.name_too_long`). It is unique **within its issuing authority**, ignoring case and accents, soft-deleted rows included: a unique index over a stored normalized column (`CatalogText.Normalize`, the F-33 technique) covering `TenantId`, `IssuingAuthorityId` and `NormalizedName`, with `NULLS NOT DISTINCT`. A duplicate is refused with `exam.name_taken` (409). Moving an exam to another issuing authority whose exams already hold that name is the same conflict.
- BR11 An `IssuingAuthorityId` that matches no issuing authority — or a soft-deleted one — is refused with 404 `issuing_authority.not_found`. (v2)
- BR12 Deleting an issuing authority that has at least one exam, deleted exams excluded, is refused with 409 `issuing_authority.has_exams`; the message says the exams must be deleted first, on the Exams page. It carries no count: `ErrorText.For` maps a code to a text and takes no argument, and giving the shared helper one for a single message is not worth it (the Admin sees them by filtering `/admin/exams` by that body). Deleting one with no exam works normally. `DeleteOrganizerHandler` goes back to what F-33 built: nothing points at an organizer until F-35. (v2)
- BR13 The exam has no publication state of its own: it is published through its editions (F-35), and a student sees an exam that has at least one published edition (F-36). Nothing blocks deleting an exam in F-34, because nothing points at it yet; F-35 adds that block when editions exist.
- BR14 The list is paged (`page`, `pageSize`, cap 100) and returns `{ items, total }`. Search matches the normalized name. The filters are issuing authority (by id), assessment type and scope, combined with AND, offered as three fields in the table's `ToolBarContent`; the toolbar wraps and grows when they no longer fit beside the search box. Sorting is by name, issuing authority, assessment type or scope; the default is name ascending. Sorting by a translated label follows the caller's order, as `OrganizerKindOrder` does for the kind (B-15).
- BR15 Domain rules return `Result`, never exceptions: `Exam.Create` and `Exam.Update` validate BR8 to BR10 and give back an `Error` with its code.
- BR16 `AppLookupField` is a new UI kit component: a searchable field that asks the server for a page of candidates as the user types, shows one line per candidate, keeps the chosen one's id, and has `Id`, `Label` and the search callback as `[EditorRequired]` (rule `ui`). It is shown on `/dev/ui` with its loading, no-result and error states, and it is the only way any screen picks a parent record from now on. For the issuing authority it calls the issuing-authority list with `search`, showing `Name (ACRONYM)`. (v2)
- BR17 Every UI text of the three screens, the new kit component and every new error code exists in pt-BR, pt-PT and en, in `SharedResources` in the Web host (F-33 v2).
- BR18 `IssuingAuthority : TenantEntity` is a new entity of the Catalog module, mapped to `catalog.issuing_authorities`: the body that publishes a notice. It has `Name` (required, 2 to 150), `Acronym` (required, 2 to 20, uppercased), `Description` and `Website` (both optional), plus the stored normalized columns the unique indexes and the search read — the same shape as `Organizer` in F-33, without a kind (nothing filters or groups by one yet). Name and acronym are each unique, ignoring case and accents, deleted rows included, with `NULLS NOT DISTINCT`. Its rules answer with a `Result` and never throw. Its back office is `/admin/issuing-authorities`, the list-plus-dialog pattern of `/admin/organizers`, behind `catalog.manage`. (v2)
- BR19 `Organizer` is the **exam board** and nothing else: F-33's three kinds, unchanged. Its pt-BR texts change from "Organizadora" to "Banca", which is what Brazil calls it; pt-PT and en keep their wording. Nothing else about F-33's screen moves. (v2)

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

### `/admin/issuing-authorities` — the new back office (BR18, v2)
- The list-plus-dialog pattern of `/admin/organizers`, behind the same `catalog.manage`: `AppPageHeader` with **Add**, a searchable `AppDataTable` with the columns name and acronym, `AppRowActions` with edit and delete, and `IssuingAuthorityDialog` with name, acronym, description and website. No kind column: the entity has none.
- Menu: `NavigationSection.Content`, key `Nav.IssuingAuthorities`, `AppIcons.IssuingAuthorities`, between Organizers and Exams.
- Deleting one that has exams is refused: the confirmation is accepted, the Api answers 409, and the `_error` alert at the top of the list shows `issuing_authority.has_exams`. It stays in the list.

### `/admin/organizers` — what changes (BR19, v2)
- Only its pt-BR texts: "Organizadora" becomes "Banca", in the menu, the title, the breadcrumb, the search placeholder, the empty states, the dialog titles and the messages. pt-PT and en are unchanged.
- `OrganizerKind` keeps F-33's three values; `PublicBody`, its texts and its place in the kind order are removed again.
- `DeleteOrganizerHandler` goes back to F-33's: nothing points at an organizer until F-35's editions.

### API
- `GET /api/v1/catalog/exams?page=&pageSize=&search=&issuingAuthorityId=&assessmentType=&scope=&sortBy=&descending=&assessmentTypeOrder=&scopeOrder=` — one page: `{ items, total }`. Each item is `ExamResponse(Id, Name, IssuingAuthorityId, IssuingAuthorityName, IssuingAuthorityAcronym, AssessmentType, Scope, ScopeDetail, ContentLanguage)`.
- `GET /api/v1/catalog/exams/{id:guid}` — one `ExamResponse`, for the form page; 404 `exam.not_found`.
- `POST /api/v1/catalog/exams` — `SaveExamRequest(IssuingAuthorityId, Name, AssessmentType, Scope, ScopeDetail, ContentLanguage)`; 201 with the `ExamResponse`. `AssessmentType`, `Scope` and `ContentLanguage` are `string?` in the contract and parsed by the handler, so an unknown value returns its own code instead of failing inside deserialization (the lesson F-33's review left).
- `PUT /api/v1/catalog/exams/{id:guid}` — the same request; 200.
- `DELETE /api/v1/catalog/exams/{id:guid}` — 204.
- `GET /api/v1/catalog/issuing-authorities?page=&pageSize=&search=&sortBy=&descending=` — one page of `IssuingAuthorityResponse(Id, Name, Acronym, Description, Website)`; `POST`, `PUT /{id:guid}` and `DELETE /{id:guid}` complete the CRUD, all behind `catalog.manage` (BR18, v2).
- The organizer routes do not change at all (v2).
- Error codes: as listed in BR5 to BR12, plus `issuing_authority.not_found` (404), `issuing_authority.name_required`, `issuing_authority.name_too_long`, `issuing_authority.acronym_required`, `issuing_authority.acronym_too_long`, `issuing_authority.website_invalid`, `issuing_authority.description_too_long` (400), `issuing_authority.name_taken`, `issuing_authority.acronym_taken`, `issuing_authority.has_exams` (409). Every one of them has a text in the three languages (BR17).

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
- AC9 Given a request whose `issuingAuthorityId` matches no issuing authority, or matches a deleted one, when it is sent, then the Api answers 404 `issuing_authority.not_found` and nothing is written. (BR11, v2)
- AC10 Given an assessment type, a scope or a content language that is not one of the allowed names, when the request is sent, then the Api answers 400 with `exam.assessment_type_invalid`, `exam.scope_invalid` or `exam.content_language_invalid`, and the OpenAPI document names the allowed values. (BR6, BR7, BR9)
- AC11 Given `pt-br` as the content language, when the exam is saved, then it is stored as `pt-BR`. (BR9)
- AC12 Given an existing exam, when the Admin opens `/admin/exams/{id}`, changes the name and the scope and saves, then the list shows the change and the row keeps its id. (UC3)
- AC12b Given the add form filled, when the Admin saves, then the page stays open with the title `Exams.Form.EditTitle`, the route `/admin/exams/{id}` of the new exam and the snackbar `Exams.Saved`; going back to the list shows it. (UC2)
- AC13 Given an exam, when the Admin confirms the deletion, then it disappears from the list and from `GET /api/v1/catalog/exams`, the row is still in the table with `IsDeleted` true, and creating a new exam with that name under the same issuing authority is refused with `exam.name_taken`. (UC4, BR1, BR10)
- AC14 Given an issuing authority with two exams, when the Admin confirms its deletion, then the Api answers 409 `issuing_authority.has_exams`, it is still listed, and the screen shows the translated message. (UC5, BR12, v2)
- AC15 Given an issuing authority whose only exam was deleted, when the Admin deletes it, then it is deleted; and deleting an organizer is unaffected, because nothing points at one yet. (BR12, v2)
- AC16 Given exams whose names differ only by accents, when the Admin searches without accents, then all of them are listed; and given the three filters set together, then only the exams matching all three come back, with `total` counting them. (BR14)
- AC17 Given a signed-in Admin, when they open `/admin/issuing-authorities`, then they see the list with name and acronym sorted by name, can add one in the dialog with the acronym uppercased, are refused a name or acronym another one already holds (ignoring case and accents), and see it offered by the exam form's picker. A Student gets Not Found there and 403 `identity.forbidden` from the Api. (UC6, BR18, v2)
- AC18 Given `AppLookupField` on `/dev/ui` and on the form page, when the Admin types two letters, then the server is asked with that term and the matching organizers are offered as `Name (ACRONYM)`; with no match the no-result state shows; when the call fails the error state offers "try again"; and the whole field is reachable and choosable by keyboard alone. (BR16)
- AC18b Given the form with the scope `Municipal` and a municipality typed, when the Admin changes the scope to `National`, then the field disappears, what was typed is dropped, and saving stores `ScopeDetail` null. (BR8)
- AC18c Given `/admin/exams/{id}` for an id that is not an exam, or one that was deleted, then the page shows `exam.not_found` with a link back to the list and no fields. (BR5)
- AC19 Given `Exam.Create` with a blank name, when it runs, then it returns a failed `Result` with `exam.name_required` and throws nothing. (BR15)
- AC20 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR17)
- AC21 Given the organizer screen, when it is read in pt-BR, then it says "Banca" everywhere it said "Organizadora"; pt-PT and en are unchanged, and `OrganizerKind` still has exactly F-33's three values. (BR19, v2)

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

### From the build (2026-09-24)
- The resource keys for the two enums are `Exams.AssessmentType.<value>` and `Exams.Scope.<value>`, not the bare `AssessmentType.<value>` the text table above listed — `Organizers.Kind.<value>` is the convention already in the file, and one prefix per screen keeps the 375 keys readable.
- The form's content language defaults to `pt-BR` through `ExamForm.DefaultContentLanguage`, not `SupportedLanguages.Default`: that constant is the app's fallback **UI** language (`en`), the wrong guess for a catalog whose first market is Brazil. Caught by the test that read what the form sends.
- `AppLookupField` renders the combobox pattern itself: MudBlazor 9.9 gives its autocomplete a plain text input, with no `role="combobox"` and no `aria-expanded`. Found by opening `/dev/ui` through the app host and reading the DOM, not by a test. The field now sets `role`, `aria-expanded` (through `OpenChanged`), `aria-autocomplete` and `aria-haspopup`; `aria-controls` is **not** set, because the popover's id belongs to the library — the accessibility section above claimed it and was wrong.
- `Lookup.Results` announces "Results: 1", not "1 results": the count is read aloud, and none of the three languages has a single plural form.
- `OrganizerQueries.KindSort` and `OrganizerKindOrder` were written for exactly three kinds (`Count: 3`, a nested conditional with two branches). The fourth made the whole caller order fall back silently. Both are now written over the enum, and their tests read `Enum.GetValues` instead of counting to three; `EnumOrder.Parse<T>` is the shared parser the exam list's two ordered columns use as well.
- The gallery gained a test that fails when any id appears twice: the section id `gallery-lookup` repeated the field id below it, which breaks every `label for` and `aria-describedby` pointing at it. Found on screen.
- `Simulab.Catalog.Domain` gained a project reference to `Simulab.Identity.Contracts` for `SupportedLanguages` (BR9). `ModuleBoundaryTests` allows it (a module reaches another through its contracts) and forbids the same reference on `Catalog.Contracts`, which is why it sits on the domain project.
- Found and **not** fixed here: the hint under every kit field reads 1.90:1 in dark and 2.64:1 in light, against the 4.5:1 of ADR-0001 #29. It is F-4's `.app-field-hint`, shared by every form in the app, so it is B-16 and not this item.

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

### v2 — 2026-09-24
- **What:** the exam board and the issuing authority become two records. `Organizer` goes back to being only
  the board (F-33's three kinds, `PublicBody` removed) and its pt-BR texts become "Banca"; a new
  `IssuingAuthority` entity, with its own table and back office at `/admin/issuing-authorities`, is what an
  exam hangs on. The English identifier `Organizer` stays: only the translation changes, which is what was
  asked, and renaming the entity would drag F-33's table, migrations and tests along for no gain.
- **Why:** found on validation. The exam `Guarda Municipal de Manaus` was linked to `Ibam Concursos2 (IBAM)`,
  a board, in a field that asks for the body that publishes the notice — the picker offered it and the Api
  accepted it, because one table held both roles. Calling that table "Banca" in pt-BR, as asked, would have
  made the contradiction plain instead of fixing it (owner, 2026-09-24).
- **Affected:** BR2, BR11, BR12, BR14 (the toolbar wrap), BR16; BR3 and BR4 withdrawn; BR18 and BR19 added.
  AC9, AC14, AC15 reworded; AC17 replaced; AC21 added. UC5 and UC6 reworded. The screens and API sections,
  the glossary and the mockup follow. Every other criterion is unchanged and stays green.
- **Not affected:** AC1 (except the foreign key's target), AC2, AC3, AC5, AC6, AC8, AC10, AC11, AC12, AC12b,
  AC13, AC16, AC18, AC18b, AC18c, AC19, AC20 — the exam itself, `AppLookupField`, the filters, the form page
  and the content language are all as built.
- **Data:** the exam rows created while validating point at organizers; the migration retargets the foreign
  key, so they do not survive. Two rows, and F-37 brings the real data (owner, 2026-09-24).
- **Also decided:** the issuing authority carries no kind for now — nothing filters or groups by one, and it
  arrives when F-36 shows the student needs it (owner, 2026-09-24).
- **Re-approved:** 2026-09-24 (owner).
- Status goes back to `building`: the file should say where the work really is, and the item returns to
  `validating` when the new screen is ready for the owner.

## Coverage

| Criterion | Test |
|---|---|
| AC1 the table, its foreign key and the unique index over the normalized name | `ExamEndpointTests.Start_CreatesTheExamsTableInTheCatalogSchema`, `ExamUniqueIndexTests.TheIndexIsUniqueAndTreatsNullTenantsAsEqual`, `.TheForeignKeyToTheIssuingAuthority_IsRestricted` (v2: it also pins that the key points at `issuing_authorities`), `IssuingAuthorityEndpointTests.Start_CreatesTheIssuingAuthoritiesTableInTheCatalogSchema` |
| AC2 the list, four columns, sorted by name | `ExamEndpointTests.List_WithoutASort_ComesBackByNameAscending`, `ExamsPageTests.Load_ShowsTheNameTheAuthorityAndTheTranslatedColumns`, `.Load_AsksTheServerForTheFirstPage` |
| AC3 a Student is refused everywhere | `ExamEndpointTests.EveryRoute_WithoutTheManagePermission_IsForbidden`, `.List_Anonymous_IsUnauthorized`, `AdminPagesAuthorizationTests.EveryAdminPage_RequiresItsModulesPermission`, `NavigationItemsTests.All_ContentItems_AreTheCatalogScreensBehindCatalogManage`, `.Visible_WithoutCatalogManage_HidesTheContentSection` (the Not Found page itself is step 8 of the script) |
| AC4 a national exam is created without a detail | `ExamEndpointTests.Create_NationalExam_StoresItWithoutADetailAndCarriesTheAuthorityName`, `ExamTests.Create_NationalWithADetail_DropsIt`, `ExamFormTests.Add_NationalExam_SendsItWithoutADetail` |
| AC5 the scope detail is required for State and Municipal | `ExamEndpointTests.Create_ScopeThatNeedsADetailWithoutOne_IsRefusedAndNothingIsWritten`, `ExamTests.Create_StateOrMunicipalWithoutItsDetail_FailsWithScopeDetailRequired`, `ExamFormTests.Save_MunicipalWithoutItsDetail_ShowsTheMessageAndCallsNothing` |
| AC6 the name is taken inside the authority, ignoring case and accents | `ExamEndpointTests.Create_NameTakenInTheSameAuthorityIgnoringCaseAndAccents_IsRefused`, `ExamFormTests.Save_NameTaken_ShowsTheMessageOnTheNameField` |
| AC7 the same name under two authorities | `ExamEndpointTests.Create_SameNameUnderTwoAuthorities_CreatesBoth`, `ExamUniqueIndexTests.TheSameNameUnderAnotherAuthority_IsAccepted` |
| AC8 PostgreSQL refuses two global rows with the same key | `ExamUniqueIndexTests.TwoGlobalRowsWithTheSameAuthorityAndName_AreRejectedByPostgres` |
| AC9 an issuing authority that is not in the catalog | `ExamEndpointTests.Create_IssuingAuthorityThatDoesNotExist_IsRefusedWithIssuingAuthorityNotFound`, `.Create_IssuingAuthorityThatWasDeleted_IsRefusedWithIssuingAuthorityNotFound`, `.Create_UnderAnOrganizerId_IsRefused` (v2: a board is not a parent) |
| AC10 an unknown assessment type, scope or language is that field's own 400 | `ExamEndpointTests.Create_ValueThatIsNotOneOfTheNames_IsRefusedWithThatFieldsCode`, `ExamTests.Create_AssessmentTypeThatIsNotOneOfTheFour_FailsWithAssessmentTypeInvalid`, `.Create_ScopeThatIsNotOneOfTheThree_FailsWithScopeInvalid`, `.Create_ALanguageTheAppDoesNotShipIn_FailsWithContentLanguageInvalid` |
| AC11 the language is stored canonically | `ExamEndpointTests.Create_LanguageInAnyCase_IsStoredCanonically`, `ExamTests.Create_ALanguageOfTheApp_StoresItCanonically` |
| AC12 editing keeps the id and shows the change | `ExamEndpointTests.Update_NewNameAndScope_KeepsTheIdAndShowsTheChange`, `.Update_ItsOwnName_IsNotATakenConflict`, `ExamFormTests.Edit_OpensFilledAndSendsAPutOnThatExam` |
| AC12b saving keeps the Admin on the page, now editing | `ExamFormTests.Add_Saved_StaysOnThePageAsTheEditForm` |
| AC13 the soft delete keeps the row and the name | `ExamEndpointTests.Delete_ExistingExam_HidesItKeepsTheRowAndKeepsItsNameTaken`, `ExamsPageTests.Delete_Confirmed_SendsTheDeleteAndShowsTheSnackbar`, `.Delete_Cancelled_SendsNothing` |
| AC14 an issuing authority with exams does not leave the catalog | `ExamEndpointTests.DeleteIssuingAuthority_ThatHasExams_IsRefusedAndItStays`, `IssuingAuthoritiesPageTests.Delete_RefusedBecauseItHasExams_ShowsTheMessageAndKeepsTheRow` |
| AC15 once its exams are gone, the issuing authority goes too | `ExamEndpointTests.DeleteIssuingAuthority_WhoseOnlyExamWasDeleted_Succeeds`, `IssuingAuthoritiesPageTests.Delete_Confirmed_SendsTheDeleteAndShowsTheSnackbar` |
| AC16 the search ignores accents; the three filters combine | `ExamEndpointTests.List_SearchWithoutAccents_FindsAccentedNames`, `.List_TheThreeFiltersTogether_ReturnsOnlyWhatMatchesAllOfThem`, `.List_FilterValueThatIsNotOneOfTheNames_IsIgnored`, `ExamsPageTests.Search_SendsTheTermToTheServer`, `.Filter_ByAssessmentType_ReloadsTheListWithThatFilter` |
| AC17 the issuing-authority back office | `IssuingAuthorityEndpointTests` (15: the table beside the boards', the permission on every route, create with the acronym uppercased, name and acronym taken ignoring case and accents, the blank name, the website, update, the soft delete that keeps the name taken, the accent-insensitive search, paging, the cap and the sort), `IssuingAuthoritiesPageTests` (9: the two columns and no kind column, the first page, the search, the error and empty states, add, the name taken on its field, the website refused without a call, edit, and the two deletes), `ExamFormTests.Authority_Typing_AsksTheIssuingAuthorityListWithTheTerm` |
| AC21 pt-BR says Banca; en and pt-PT unchanged; three kinds | `OrganizerWordingTests` (9: the six screen texts, the two error codes, the sweep that no organizer text still says "organizadora", the two other languages, the three enum values, and the issuing authority's own pt-BR name) |
| AC18 `AppLookupField` and its states | `AppLookupFieldTests` (seven: the closed combobox, the minimum-characters hint, the error, nothing found, the failed search, try again, the announced count), `ExamFormTests.Authority_Typing_AsksTheIssuingAuthorityListWithTheTerm`, `ExamsPageTests.AuthorityFilter_Typing_AsksTheIssuingAuthorityListWithTheTerm`, `UiGalleryTests.Render_Development_ShowsHeaderAndEverySection`, `.Render_Development_EveryIdOnThePageIsUnique` (keyboard only is step 7 of the script) |
| AC18b the conditional field follows the scope and drops what it held | `ExamFormTests.Scope_Municipal_ShowsTheMunicipalityFieldAndStateShowsTheStateOne`, `.Scope_BackToNational_DropsTheDetailAndSendsItNull`, `ExamTests.Create_NationalWithADetail_DropsIt` |
| AC18c an id that is not an exam | `ExamEndpointTests.Find_AnIdThatIsNotAnExam_IsNotFound`, `ExamFormTests.Edit_AnIdThatIsNotAnExam_ShowsNotFoundAndALinkBack` |
| AC19 the domain answers with a `Result` and never throws | `ExamTests` (the 30 cases: every code, `Update_Refused_ChangesNothing` included), `IssuingAuthorityTests` (13, the same shape for the new entity) |
| AC20 every text in the three languages | `ResourceParityTests.Every_key_exists_in_every_language`, `RoleResourcesTests.EveryPermissionSystemRoleAndErrorCode_HasAText` (over `CatalogErrorCodes`, which now holds the ten exam codes and `organizer.has_exams`) |
| BR2, BR11 the picker names the parent, and the exam carries its authority's name | `ExamEndpointTests.Find_AnExistingExam_ComesBackFilledForTheForm`, `ExamFormTests.Save_IssuingAuthorityGone_ClearsThePickerAndAsksForAnother` |
| BR9 the content language is offered in its own name, pt-BR first | `ExamFormTests.ContentLanguage_IsOfferedInItsOwnName`, `.Add_NationalExam_SendsItWithoutADetail` |
| BR14 sorting a translated column sends the reader's order | `ExamEndpointTests.List_SortedByScopeWithTheCallersOrder_FollowsIt`, `.List_SortedByIssuingAuthority_OrdersByTheAuthorityName`, `ExamsPageTests.SortByScope_SendsTheReadersOrderForThatColumn` |

## Validation script

Start the app host **from this worktree** and sign in as an Admin. Claude opened `/dev/ui` and both catalog
routes through the app host but could not sign in (its rules forbid entering credentials), so steps 2 to 8
are yours. Close any IDE or app host running from another checkout first: two hosts fight for port 17162.

The exams you created in the first round are gone: v2 moved the exam's parent to a new table, and the
migration drops the rows it cannot remap (change note v2).

Git Bash and PowerShell 7, from `D:/dev/_icontrol/wt/simulab/f-34` (same command in both):

```bash
dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

Expected: `Application host directory is: D:\dev\_icontrol\wt\simulab\f-34\src\Hosts\Simulab.AppHost` and
`Now listening on: https://localhost:17162`. The Web is at `https://localhost:7125`. Ctrl+C stops it.

1. Open `https://localhost:7125/admin/exams` signed out → the sign-in page. Sign in as an Admin: the
   **Conteúdo** section now lists **Bancas**, **Órgãos contratantes** and **Exames**, in that order. Open
   **Bancas** → the page that used to say "Organizadoras" says **Bancas** everywhere, and the **Tipo** column
   still offers exactly three types.
2. On **Órgãos contratantes**, **Adicionar**: name `Prefeitura Municipal de Manaus`, acronym `pmm`, site
   `manaus.am.gov.br` → the site field refuses it; change it to `https://www.manaus.am.gov.br` and save →
   the row appears with the acronym as `PMM`. Add a second: `Polícia Federal`, sigla `pf`. Try a third named
   `POLÍCIA FEDERAL` → refused on the name field.
3. On **Bancas**, add `Ibam Concursos`, sigla `ibam`, tipo **Banca examinadora**. This is the row that was
   wrong before: it is a banca, and it must **not** be offered as contratante in the next step.
4. **Exames** → **Adicionar**. In **Órgão contratante** type `ibam` → nothing is found; type `manaus` →
   `Prefeitura Municipal de Manaus (PMM)` appears; pick it. Name `Guarda Municipal de Manaus`, tipo
   **Concurso público**, abrangência **Municipal**, município `Manaus`, idioma **Português (Brasil)** →
   **Salvar**. The page stays open as **Editar exame** and o endereço passa a terminar no id do exame.
5. Back on the list: the row shows `Prefeitura Municipal de Manaus / PMM` under **Órgão contratante** — the
   banca não aparece em lado nenhum. The three filters and the search box sit on one line beside each other,
   inside the card, and never over the table header. Narrow the browser window until they wrap: they move to
   their own line and the table header stays below them.
6. Filter by **Órgão contratante** = `Prefeitura Municipal de Manaus`, **Tipo de avaliação** = **Concurso
   público**, **Abrangência** = **Municipal** → only that exam. Clear the three. Click the **Abrangência**
   and **Tipo de avaliação** headers → the order follows the words on screen, not the English names.
7. Try to delete `Prefeitura Municipal de Manaus` on **Órgãos contratantes** → refused, "Este órgão
   contratante tem exames no catálogo. Exclua esses exames primeiro, na página Exames". Delete the exam,
   then the órgão → it goes. Deleting the banca `Ibam Concursos` works straight away: nothing points at it.
8. Keyboard only, with Tab, the arrows and Enter: from the exam list reach **Adicionar**, then on the form
   reach the órgão contratante, type two letters, pick an option with the arrows and Enter, and reach
   **Salvar**. Then switch the language to pt-PT and to English → **Entidades contratantes** / **Issuing
   authorities**, **Entidades organizadoras** / **Organizers**, and every column, filter and message follow;
   the names of the bodies and of the exams do not. Sign in as a Student and open `/admin/issuing-authorities`
   → Not Found, and no **Conteúdo** section in the menu.

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-34
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
