---
feature: F-36
epic: Assessment catalog
status: validating
board: 755
version: 1
---
# Catalog browsing for students

## Summary
The student-facing half of the catalog: a signed-in student searches published exams and filters by assessment type, exam board, year and scope, then opens an exam and sees its published editions. It is how a student finds the exam they are preparing for, and the read side every simulator will call. Needs /agile:screen.

## Start
- Depends on: F-35 (`done`, merged as 0d547e2) — `ExamEdition` with `Draft`/`Published`, the `(OrganizerId, NoticeYear)` index and the exam page kit are in `main`.
- Waits on (to start): nothing. The `/agile:screen` mockup was approved together with the feature on 2026-09-29. Recommended: merge B-20 first (same migration snapshot).
- Needed to validate: the app host started from this worktree, at least one exam with a published edition and one with only drafts (created by the owner in the back office, or by F-37 if it lands first), and two accounts signed in by the owner — a Student and an Admin (Claude does not enter credentials).
- Suggested path: glossary rows; `catalog.browse` in `CatalogPermissions` and its one-time grant in Identity's seed; `NormalizedScopeDetail` and its migration; `IPublishedExamQueries` and the three endpoints; the search page; the exam page; the menu item.
- Can run beside it: F-37 (seed data only, no screen or contract in common) with the owner's `--worktree`; B-20 touches the `exams.scope` column comment and may conflict in the migration snapshot — merge it first or rebase after.

## What exists
- `Exam` (`Simulab.Catalog.Domain/Entities/Exam.cs`): `IssuingAuthorityId`, `Name`, `AssessmentType`, `Scope`, `ScopeDetail`, `ContentLanguage`, `NormalizedName`. No normalized form of `ScopeDetail`.
- `IssuingAuthority` and `Organizer` both carry `NormalizedName` and `NormalizedAcronym`; `CatalogText.Normalize` is the one normalizer (trim, upper invariant, no accents).
- `ExamEdition` (F-35): `ExamId`, `OrganizerId` (the board), `NoticeYear`, `Position`, `NoticeReference`, `NoticeUrl`, `AppliedOn`, `Status` (`Draft`/`Published`). Index over `(OrganizerId, NoticeYear)` (F-35 BR13).
- Api: every `/api/v1/catalog/*` route requires `catalog.manage`; `ExamQueries.ListAsync` searches `NormalizedName` with one `Contains` of the whole text and filters by authority, type and scope. There is no read side for students.
- Permissions: `CatalogPermissions.All` is `[catalog.manage]`. `IdentityModule.EnsureRolesAndPermissionsAsync` creates every declared permission and grants it to Admin on every start; the other seed roles get theirs only from the back office (F-6 BR3). A new account is a Student (F-30 BR4) and the Student role holds no permission today.
- Web: `NavigationSection.Study` exists and has no item; `NavigationItems.All` hides an item behind its `RequiredPermission`. `/` is a placeholder home. Kit: `AppDataTable` (server paging), `AppSelectField`, `AppItemRows`, `AppStatusChip`, `AppPageHeader`, the empty/loading/error states.
- Items touching this code since F-35: none merged. B-20 (`approved`, worktree `b-20-exam-scope-column`) changes the `exams.scope` column comment. F-42 (state picker) and F-37 (seed) are `idea`.
- Packages: nothing new. The production code and the tests use what `Directory.Packages.props` already has (EF Core with Npgsql, MudBlazor, xunit, AwesomeAssertions, Testcontainers.PostgreSql, bunit, Mvc.Testing).

## Goal
Let a student find the exam they are preparing for among what the catalog has published, and give every later module (simulators, target exam, coach) one read side that shows only published content.

## Users and use cases
- UC1 A Student opens "Catalog" in the Study section of the menu and sees the published exams, one row per exam, ordered by name, paged.
- UC2 The Student types part of an exam's name, its issuing authority's name or acronym, or its state or municipality, and the list narrows to the matching exams.
- UC3 The Student filters by assessment type, scope, exam board and notice year, alone or together with the text, and clears the filters.
- UC4 The Student opens an exam and sees what it is (name, issuing authority, assessment type, scope, content language) and its published editions, newest year first, each with its board, position, notice reference, application date and a link to the official notice.
- UC5 A Student, Curator or Admin never sees a draft edition, nor an exam whose editions are all drafts, in the catalog, the exam page or the filter options.
- UC6 A user whose roles lack `catalog.browse` sees no menu item and gets the ordinary Not Found page on the catalog routes; the Api answers 403 `identity.forbidden`.
- UC7 An Admin removes `catalog.browse` from a role in the roles back office and it stays removed after the app restarts.

## Business rules
- BR1 An exam is published when at least one of its editions is `Published` (deleted editions excluded). Only published exams exist for the student side: the list, the exam page, the filter options and `IPublishedExamQueries` never return an unpublished exam or a draft edition, whoever asks (owner, question 10).
- BR2 The list has one row per published exam: name, issuing authority name and acronym, assessment type, scope and scope detail, content language, the number of published editions and the latest published notice year. Ordered by exam name A to Z, then id; paged with the shared rule (default 25, cap 100).
- BR3 The search text is normalized with `CatalogText.Normalize` and split into words on whitespace; an exam matches when every word appears in at least one of: the exam's normalized name, its issuing authority's normalized name or acronym, its normalized scope detail. "guarda sp" finds "Guarda Municipal" of "Prefeitura de São Paulo (SP)". Blank text is no filter.
- BR4 `Exam` gains `NormalizedScopeDetail` (`CatalogText.Normalize` of `ScopeDetail`, empty when there is none), kept by `Exam.Create`/`Update`. The migration adds the column and fills it for the existing rows.
- BR5 Filters: assessment type (one of the four), scope (`National`, `State`, `Municipal`; no state or municipality picker — F-42), exam board (one organizer id) and notice year (one year). They combine with AND, and with the text. The board and the year match on the same published edition: an exam with a 2024 FGV edition and a 2025 CEBRASPE edition does not match "FGV, 2025".
- BR6 A filter value the Api cannot read (an unknown type, scope, id or year) is no filter, as in the back office list (F-34 BR14): a stale bookmark shows results, not an error.
- BR7 The filter options come from the published catalog: the boards that name at least one published edition (acronym and name, ordered by acronym, shown as "ACRONYM — Name") and the notice years of published editions (newest first). A board or a year with nothing published is not offered.
- BR8 The exam page shows the exam (BR2 fields) and every published edition of it — all of them, whatever filters led there — ordered by notice year descending, then position, then board name (F-35 BR14 order). Each edition shows notice year, position, board name and acronym, notice reference, application date and the notice link. An exam that does not exist, is deleted or is not published is 404 `exam.not_found`.
- BR9 The notice link opens the official page in a new tab (`target="_blank"`, `rel="noopener noreferrer"`) and says so to screen readers. Content is shown as stored, never translated (ADR-0001 #27); the exam's content language is shown.
- BR10 A new permission `catalog.browse` gates the three endpoints, the menu item and both pages. Identity grants it to Student, Curator and Admin only in the start where the permission row is created; afterwards only the roles back office changes who holds it, and a later start never grants it again (Admin keeps receiving every permission on each start, F-6 BR3). Having `catalog.manage` alone does not open the student catalog (owner, question 9).
- BR11 `IPublishedExamQueries` in `Simulab.Catalog.Contracts` is the read side other modules call (list, find with editions, filter options); the endpoints use the same interface.
- BR12 Every new UI text and error code exists in pt-BR, pt-PT and en in `SharedResources`; the enum labels reuse the back office keys.
- BR13 `/catalog` keeps its search, filters, page and page size in the query string; opening that address restores them, and the exam page's breadcrumb and back link return to it. An unreadable value is no filter (BR6). Three kit components gain optional parameters for it and for the exam page: `AppDataTable` (`InitialSearch`, `InitialPage`, `InitialPageSize`), `AppTruncatedText` (`Href`, `Lang`) and `AppPageHeader` (`TitleLang`) (owner, screen questions 1, 3 and 4).

## Screens and API

Designed by `/agile:screen` on 2026-09-29. Mockup: `docs/features/mockups/F-36-catalog-browsing-for-students.html` (two screens and the menu, every state, light and dark, pt-BR / pt-PT / en, three permission sets). Only kit patterns from `/dev/ui` are used; no new component and no new colour. Three kit components gain parameters (`AppDataTable`, `AppTruncatedText`, `AppPageHeader` — see "Kit changes") and three `AppIcons` constants are new. Every text below is a `SharedResources` key; error codes reach the screen only through `ErrorText.For`. Both screens are read-only: nothing is written, so there is no saving, success or validation state (BR6: a filter value the Api cannot read is no filter, never an error).

### Routes
| Route | Purpose | Gate |
|---|---|---|
| `/catalog` | search and filters, one row per published exam | `catalog.browse` |
| `/catalog/exams/{id:guid}` | the exam and its published editions | `catalog.browse` |

Menu: item "Catalog" (`Nav.Catalog`) in the Study section, `RequiredPermission: CatalogPermissions.Browse`.

### Menu item (`NavigationItems.All`)
`new(NavigationSection.Study, "/catalog", AppIcons.Catalog, "Nav.Catalog", RequiredPermission: CatalogPermissions.Browse)`, listed before the Content items. The default `NavLinkMatch.Prefix` keeps the item current on `/catalog/exams/{id}` too. The Study section shows only when the item is visible (`NavigationItems.Sections` drops empty sections), so a user without `catalog.browse` sees no Study heading (AC12). `AppIcons.Catalog` is new: `Icons.Material.Outlined.ManageSearch`.

### Screen 1 — the catalog search page (`CatalogSearch.razor`, `/catalog`)

**Page header.** `AppPageHeader Title="Catalog.Title"`, no primary action (a student adds nothing here). Breadcrumb: `Nav.Section.Study` (disabled) › `Catalog.Title` (disabled). `<PageTitle>` comes from the header.

**Composition.** One `AppDataTable TItem="PublishedExamResponse"`, `Searchable="true"`, no `RowActions`, with `InitialSearch`, `InitialPage` and `InitialPageSize` read from the URL (see "The URL keeps the search"):
1. Search box (the kit's, first in the toolbar): `SearchPlaceholder` = `Catalog.Search.Placeholder`; debounced 300 ms by the kit, sends `search` as typed; the Api normalizes and splits it (BR3).
2. `ToolBarContent` → `div.app-table-filters` with four `AppSelectField`s, then the clear button (table below).
3. Columns (below). No column is sortable (`Sortable="false"` on each): the Api has one order, exam name A to Z then id (BR2), and a sort arrow that does nothing would lie.
4. The kit pager: page sizes `AppDataTable.PageSizes` (10, 25, 50), default 25 (BR2 cap 100 is the Api's).
5. An `AppAlert Severity="Warning"` above the table, only when the filter options failed to load, with `ActionText="Common.TryAgain"` (reloads the options only).

**Filters.** Each change calls `ReloadFromFirstPageAsync()` (the F-9 path: a new filter starts from page 1).

| Filter | Id | Component | Options | Sends |
|---|---|---|---|---|
| Assessment type | `catalog-filter-type` | `AppSelectField TValue="AssessmentType?"`, label `Exams.Filter.AssessmentType` | `Common.Filter.All`, then the four types in `ExamText.AssessmentTypeOrder(L)` with `Exams.AssessmentType.*` (BR12) | `assessmentType` |
| Scope | `catalog-filter-scope` | `AppSelectField TValue="ExamScope?"`, label `Exams.Filter.Scope` | `Common.Filter.All`, `National`, `State`, `Municipal` with `Exams.Scope.*`; no state or municipality picker (F-42) | `scope` |
| Exam board | `catalog-filter-organizer` | `AppSelectField TValue="Guid?"`, label `Catalog.Filter.Organizer` | `Common.Filter.All`, then `PublishedExamFiltersResponse.Organizers` in the Api's order (by acronym, BR7), text `ACRONYM — Name` | `organizerId` |
| Notice year | `catalog-filter-year` | `AppSelectField TValue="int?"`, label `Catalog.Filter.NoticeYear` | `Common.Filter.All`, then `NoticeYears` in the Api's order (newest first, BR7), written as plain digits (`2026`, never `2.026`) | `noticeYear` |

- The board and year options come from `GET /published-exam-filters`, requested once when the page opens, beside the first list call. While it runs the two selects are `Disabled`. If it fails, the two selects stay disabled with only `Common.Filter.All`, the warning alert `Catalog.Filter.OptionsFailed` shows with Try again, and the list, the search and the other two filters keep working.
- Board and year are two independent selects; the rule that they match on the same edition (BR5) is the Api's. The screen never narrows one select by the other.
- **Clear filters**: a text `MudButton` (`Variant.Text`, `StartIcon="AppIcons.ClearFilters"`, new: `Icons.Material.Outlined.FilterAltOff`), text `Catalog.Filter.Clear`, placed last in `app-table-filters`, shown only while at least one of the four selects is not "All". It sets the four back to "All" and reloads from page 1. It does not clear the search text: the search box keeps its own clear button (the kit's `Clearable`).

**The URL keeps the search.** `/catalog` reads and writes its state in the query string, so a bookmark, a shared link, the browser's Back button and the links back from the exam page all bring the same list.

| Query key | Holds | Read from the URL | Unreadable value |
|---|---|---|---|
| `search` | the search text as typed | trimmed; blank is none | — |
| `assessmentType` | an `AssessmentType` name | case-insensitive enum name | no filter (BR6) |
| `scope` | an `ExamScope` name | case-insensitive enum name | no filter (BR6) |
| `organizerId` | a board id | a `Guid` | not a `Guid` → no filter; a `Guid` missing from the loaded options → dropped from the URL and the list reloads (BR6) |
| `noticeYear` | a year | an integer | not an integer, or missing from the loaded options → no filter, as above (BR6) |
| `page` | the page, 1-based | integer ≥ 1 | page 1; a page past the last one reloads page 1 |
| `pageSize` | the rows per page | one of `AppDataTable.PageSizes` (10, 25, 50) | 25 |

- **Writing.** After every load the page rewrites the URL from the `AppTableQuery` it received (page, page size, search) plus its four filters, with `NavigationManager.NavigateTo(uri, replace: true)`: typing, filtering and paging never add history entries, so Back from the list leaves the catalog. Keys at their default (blank search, "All", page 1, size 25) are left out: the plain list is just `/catalog`.
- **Reading.** On open, the page parses the keys (table above), sets its four filters and passes `InitialSearch`, `InitialPage` (0-based for the kit) and `InitialPageSize` to `AppDataTable`. While the filter options load or after they failed, a readable `organizerId` / `noticeYear` from the URL is still sent (the Api decides, BR6) and "Clear filters" shows so the reader can drop it.
- **To the exam and back.** The exam-name link is `/catalog/exams/{id}` followed by the catalog's current query string (only the seven keys above). The exam page ignores those keys for itself and builds its breadcrumb "Catalog" link and its "Back to the catalog" link as `/catalog` + the same query string, so both restore the list as it was. Opened without them (a bookmark, a new tab), both links go to plain `/catalog`.

**Columns.**

| # | Column | Title key | Kit | Cell |
|---|---|---|---|---|
| 1 | Name | `Exams.Column.Name` | `TemplateColumn` | `AppTruncatedText Text="@Name" MaxWidth="26rem" Href="/catalog/exams/{id}?{catalog query}" Lang="@ContentLanguage"` (kit change): a truncated link with the full name as tooltip, one tab stop (rule `ui-project`) |
| 2 | Issuing authority | `Exams.Column.IssuingAuthority` | `TemplateColumn` | `AppTruncatedText` name (14rem) and the acronym below in `app-cell-secondary` (the back office cell) |
| 3 | Assessment type | `Exams.Column.AssessmentType` | `TemplateColumn` | `ExamText.AssessmentTypeName` |
| 4 | Scope | `Exams.Column.Scope` | `TemplateColumn` | `ExamText.ScopeName`, and the scope detail below in `app-cell-secondary` when there is one |
| 5 | Content language | `Catalog.Column.ContentLanguage` | `TemplateColumn` | `SupportedCultures.NativeName` of the exam's language, with `lang` set to it (the `ExamEditionForm` aside precedent); the raw tag when it is not a UI culture |
| 6 | Editions | `Catalog.Column.Editions` | `AppNumberColumn`, `Format="N0"` | number of published editions, right-aligned |
| 7 | Latest year | `Catalog.Column.LatestYear` | `AppNumberColumn`, `Format="0"` | latest published notice year, right-aligned, no group separator |

The name is the row's only link and the only way to the exam page (rule `ui`: the name links to a read-only detail page). No row action column, no row click.

**Empty messages** (`AppDataTable` picks them): with search text → `EmptySearchMessage` = `Catalog.Empty.Search` with `{0}` the text; else with a filter → `EmptyMessage` = `Catalog.Empty.Filters` and `EmptyActionText` = `Catalog.Filter.Clear`, `EmptyActionIcon` = `AppIcons.ClearFilters`, `OnEmptyAction` = clear filters; else → `Catalog.Empty`, no action (the student has no primary action on this page).

### Screen 2 — the exam page (`CatalogExam.razor`, `/catalog/exams/{id:guid}`)

**Page header.** `AppPageHeader Title` = the exam name, `TitleLang` = the exam's content language (kit change); while loading, not found or failed the title is `Catalog.Exam.Title` with no `TitleLang`. No primary action. Breadcrumb: `Nav.Section.Study` (disabled) › `Catalog.Title` (link `/catalog` + the catalog query it came with) › the exam name (disabled; absent while loading, not found or failed).

**Layout.** The F-43 composition, read-only: `div.app-form-layout` → `MudPaper.app-form-card` (the editions section) + `AppFormAside` (the exam). On a screen narrower than 1280 px the aside comes below the editions, as on every page of that layout.

**The editions** — `AppSectionCard Id="catalog-exam-editions"`, `Icon="AppIcons.Editions"`, title `ExamEditions.Section.Title`, subtitle `Catalog.Editions.Subtitle`; body `AppItemRows TItem="PublishedExamEditionResponse"` with no `Actions` (a read-only list), `EmptyMessage="Catalog.Editions.Empty"` (reached only if the last edition is unpublished between the two reads). Order is the Api's (BR8); the page does not sort. Every published edition shows, whatever filters led there (BR8).

Row template, in one line that wraps on a phone:
1. **Label** `NoticeYear · Position · OrganizerName (OrganizerAcronym)` in `app-item-row-label`; the position part is left out when there is none; the ` · ` separators are `aria-hidden`. The position carries `lang` = the exam's content language (content, never translated, BR9).
2. `NoticeReference` in `text-secondary`, when there is one, with the same `lang`.
3. `ExamEditions.Row.AppliedOn` with the date as a short date in the reader's culture (`"d"`), when there is one.
4. When `NoticeUrl` has a value: an `AppLink Href="@NoticeUrl" target="_blank" rel="noopener noreferrer"` with the visible text `Catalog.Editions.NoticeLink` followed by the `AppIcons.OpenInNew` icon (new: `Icons.Material.Outlined.OpenInNew`, `aria-hidden`, 16 px), and `aria-label` = `Catalog.Editions.NoticeLink.Name` with `{0}` the label parts joined by `, ` (the accessible name starts with the visible text and says it opens in a new tab, BR9, AC10). With no URL, nothing is shown in its place.

**The exam** — `AppFormAside` reused as a read-only detail summary (the build widens its doc comment, which today says "beside a long form"), `Title="Catalog.Exam.About"`, no checklist, `EmptyText="Common.Summary.NotFilled"` (never reached: rows without a value are left out). Rows, in order:

| Label key | Value |
|---|---|
| `Exams.Field.IssuingAuthority` | `Name (ACRONYM)` |
| `Exams.Field.AssessmentType` | `ExamText.AssessmentTypeName` |
| `Exams.Field.Scope` | `ExamText.ScopeName` |
| `Exams.Field.State` or `Exams.Field.Municipality` | the scope detail, only for `State` / `Municipal` with a detail |
| `Exams.Field.ContentLanguage` | `SupportedCultures.NativeName` of the exam's language |
| `Catalog.Exam.PublishedEditions` | the number of published editions (`N0`) |
| `Catalog.Exam.LatestNoticeYear` | the latest published notice year (plain digits) |

**Not found.** A `404 exam.not_found` (unknown id, deleted exam, exam with only drafts — BR8, AC9) shows, inside the page layout, one `app-form-card` with `AppAlert Severity="Error" Text="exam.not_found"` and below it `AppLink Href="/catalog"` + the catalog query it came with, text `Catalog.BackToCatalog`. The alert does not say why (drafts are never revealed, BR1).

### Kit changes
Each is a new optional parameter; every existing page keeps its behaviour. Each gets a `/dev/ui` example and a bunit test.

| Component | Parameter | Behaviour |
|---|---|---|
| `AppDataTable` | `InitialSearch` (`string?`) | the search box starts with this text and the first load sends it; blank is none |
| `AppDataTable` | `InitialPage` (`int`, 0-based, default 0) | the first load asks for this page; if the result is empty while `TotalItems` > 0 (past the last page), the table reloads page 0 |
| `AppDataTable` | `InitialPageSize` (`int?`) | the rows per page of the first load when it is one of `PageSizes`, else `DefaultPageSize` |
| `AppTruncatedText` | `Href` (`string?`) | with a value, the text renders as an `AppLink` (underlined, the kit's link colour and focus ring) carrying the truncation, inside the same `MudTooltip`; the link is the only tab stop (the span's `tabindex="0"` is dropped). Without it, unchanged |
| `AppTruncatedText` | `Lang` (`string?`) | the `lang` attribute of the text or link, for content in the exam's language |
| `AppPageHeader` | `TitleLang` (`string?`) | the `lang` attribute of the `h1`, for a title that is content (the exam name) |

### States

**Screen 1 — catalog search**

| State | When | What shows | Announced |
|---|---|---|---|
| Loading | first load, and every search, filter or page change while the call runs | the kit's `AppLoadingState` in the table body; toolbar stays usable | `role="status"` "Loading..." |
| Options loading | the filter options call has not answered | board and year selects disabled with only "All" | — |
| Ready | one or more exams | rows by name, pager `1-25 of 132` | — |
| Ready, filtered | search text and/or a filter | the matching rows; "Clear filters" shown when a select is not "All"; the URL carries the query | — |
| Restored from the URL | opened with query keys (bookmark, Back, the exam page's links) | search box, selects and page as the URL says; unreadable keys ignored (BR6) | — |
| Empty catalog | no published exam and no search or filter (AC1 with nothing published) | `AppEmptyState` `Catalog.Empty`, no action | — |
| No match, search | search text, no row | `Catalog.Empty.Search` with the text | — |
| No match, filters | a filter, no search text, no row | `Catalog.Empty.Filters` + outlined "Clear filters" | — |
| Options failed | the filter options call failed | warning alert `Catalog.Filter.OptionsFailed` + Try again; board and year disabled; list unaffected | `role="status"` (the kit's non-error alert) |
| Server error | the list call failed | the kit's `AppErrorState` (`Common.LoadFailed`, Try again) in the table body | `role="alert"` |
| Permission denied | no `catalog.browse` (AC12) | no menu item; the route is the ordinary Not Found page | page title |

**Screen 2 — exam page**

| State | When | What shows | Announced |
|---|---|---|---|
| Loading | the exam is requested | header `Catalog.Exam.Title`, breadcrumb Study › Catalog, `AppLoadingState` in an `app-form-card` | `role="status"` |
| Ready | 200 | header with the exam name, the editions, the aside | page title |
| Ready, sparse | an edition with no position, no reference, no date or no link | only the parts that exist; no placeholder text | — |
| Not found | 404 `exam.not_found` (AC9) | alert `exam.not_found` + link `Catalog.BackToCatalog` | `role="alert"` |
| Server error | any other failure | `AppErrorState Message="Catalog.Exam.LoadFailed"` with Try again | `role="alert"` |
| Permission denied | no `catalog.browse` (AC12) | the ordinary Not Found page | page title |

### Permissions
One permission, `catalog.browse` (BR10): `[Authorize(Policy = PermissionPolicy.Prefix + CatalogPermissions.Browse)]` on both pages (as `Exams.razor` does with `Manage`), the menu item's `RequiredPermission`, and the same policy on the three endpoints. The pages never check `catalog.manage` nor a role name. No per-action check: both pages only read. An Admin sees exactly what a Student sees (BR1, AC13).

| Who | Menu | `/catalog`, `/catalog/exams/{id}` |
|---|---|---|
| Student, Curator, Admin holding `catalog.browse` (seed) | Study › Catalog | open |
| Any user whose roles lack `catalog.browse` (even with `catalog.manage`) | no Study section | ordinary Not Found page |

### Accessibility
- **Elements.** The exam name in a row is a link (it navigates); "Clear filters" and Try again are buttons (they act); the breadcrumb "Catalog" and "Back to the catalog" are links; the notice link is a link to another site, opening in a new tab. Each filter is the kit's labelled `AppSelectField` (label above, `aria-labelledby`, `aria-describedby`).
- **Tab order, search page:** skip link → app bar → menu → the warning alert's Try again (only in "Options failed"; the alert sits above the table) → search box → its clear button (when filled) → Assessment type → Scope → Exam board → Notice year → Clear filters (when shown) → per row: the exam name (a truncated link, one tab stop), then the issuing authority's `AppTruncatedText` (focusable in the kit so its tooltip can be read) → pager (rows per page, then page buttons).
- **Tab order, exam page:** breadcrumb "Catalog" → each edition's notice link, newest first → nothing in the aside (read-only). Not found: the back link.
- **Screen reader.** Headings: `h1` page title; on the exam page `h2` "Editions" (section card) and `h2` "About this exam" (aside). The editions are a `<ul>`; the aside is a `<dl>`. Each notice link reads "Official notice, 2026, Guarda Municipal de 3ª Classe, Instituto Consulplan (CONSULPLAN), opens in a new tab". Content text (exam name, position, notice reference) carries `lang` of the exam, so a reader in `en` hears Portuguese content pronounced as Portuguese. Loading, empty and error states are the kit's (`role="status"` / `role="alert"`).
- **Targets.** Links in rows and the pager buttons are at least 24 px high (kit line height and button sizes); "Clear filters" is a kit button (36 px).
- **Contrast.** No new colour. Secondary cell text, row meta and the notice reference use `text-secondary`: on `surface` 6.92:1 light, 4.89:1 dark (F-43 figures, recomputed); on `background` (row hover) 6.33:1 light, 5.36:1 dark. Links are `AppLink` (`text-primary`, underlined, B-6). The warning alert and the icons are existing tokens.

### UI texts
New keys, in `SharedResources.resx` / `.pt-BR.resx` / `.pt-PT.resx`. Reused keys are listed after the table.

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `Nav.Catalog` | Catalog | Catálogo | Catálogo |
| `Catalog.Title` | Catalog | Catálogo | Catálogo |
| `Catalog.Search.Placeholder` | Exam, authority or place | Exame, órgão ou local | Exame, entidade ou local |
| `Catalog.Filter.Organizer` | Board | Banca | Entidade organizadora |
| `Catalog.Filter.NoticeYear` | Notice year | Ano do edital | Ano do aviso |
| `Catalog.Filter.Clear` | Clear filters | Limpar filtros | Limpar filtros |
| `Catalog.Filter.OptionsFailed` | We could not load the board and year options. The rest of the search works. | Não foi possível carregar as opções de banca e ano. O resto da busca funciona. | Não foi possível carregar as opções de entidade organizadora e ano. O resto da pesquisa funciona. |
| `Catalog.Column.ContentLanguage` | Language | Idioma | Idioma |
| `Catalog.Column.Editions` | Editions | Edições | Edições |
| `Catalog.Column.LatestYear` | Latest year | Último ano | Último ano |
| `Catalog.Empty` | No exam is published in the catalog yet. | Ainda não há exames publicados no catálogo. | Ainda não há exames publicados no catálogo. |
| `Catalog.Empty.Search` | No published exam matches "{0}". | Nenhum exame publicado corresponde a "{0}". | Nenhum exame publicado corresponde a "{0}". |
| `Catalog.Empty.Filters` | No published exam matches the chosen filters. | Nenhum exame publicado corresponde aos filtros escolhidos. | Nenhum exame publicado corresponde aos filtros escolhidos. |
| `Catalog.Exam.Title` | Exam | Exame | Exame |
| `Catalog.Exam.About` | About this exam | Sobre este exame | Sobre este exame |
| `Catalog.Exam.PublishedEditions` | Published editions | Edições publicadas | Edições publicadas |
| `Catalog.Exam.LatestNoticeYear` | Latest notice year | Último ano de edital | Último ano de aviso |
| `Catalog.Exam.LoadFailed` | We could not load this exam. | Não foi possível carregar este exame. | Não foi possível carregar este exame. |
| `Catalog.BackToCatalog` | Back to the catalog | Voltar para o catálogo | Voltar ao catálogo |
| `Catalog.Editions.Subtitle` | Each paper applied, newest first. The official notice has the full rules. | Cada prova aplicada, da mais recente para a mais antiga. O edital oficial traz as regras completas. | Cada prova aplicada, da mais recente para a mais antiga. O aviso oficial contém as regras completas. |
| `Catalog.Editions.Empty` | This exam has no published edition. | Este exame não tem edições publicadas. | Este exame não tem edições publicadas. |
| `Catalog.Editions.NoticeLink` | Official notice | Edital oficial | Aviso oficial |
| `Catalog.Editions.NoticeLink.Name` | Official notice, {0}, opens in a new tab | Edital oficial, {0}, abre em uma nova aba | Aviso oficial, {0}, abre num novo separador |

Reused, unchanged: `Nav.Section.Study`, `Exams.Filter.AssessmentType`, `Exams.Filter.Scope`, `Exams.Column.Name`, `Exams.Column.IssuingAuthority`, `Exams.Column.AssessmentType`, `Exams.Column.Scope`, `Exams.Field.IssuingAuthority`, `Exams.Field.AssessmentType`, `Exams.Field.Scope`, `Exams.Field.State`, `Exams.Field.Municipality`, `Exams.Field.ContentLanguage`, `Exams.AssessmentType.*`, `Exams.Scope.National` / `.State` / `.Municipal`, `ExamEditions.Section.Title`, `ExamEditions.Row.AppliedOn`, `Common.Filter.All`, `Common.Search`, `Common.Loading`, `Common.LoadFailed`, `Common.TryAgain`, `Common.Table.RowsPerPage`, `Common.Table.PageInfo`, `Common.Summary.NotFilled`, `NotFound.*`, `exam.not_found`.

New `AppIcons` constants (Material Outlined): `Catalog` (`ManageSearch`), `ClearFilters` (`FilterAltOff`), `OpenInNew` (`OpenInNew`). The gallery's icon list shows them by reflection.

### What the screens read
- List row (BR2): id, name, issuing authority name and acronym, assessment type, scope, scope detail, content language, number of published editions, latest published notice year.
- Exam page (BR8): the same exam fields, and per edition: notice year, position, board name and acronym, notice reference, notice URL, application date (`yyyy-MM-dd`).
- Filter options (BR7): organizers (id, name, acronym) and notice years.

### API
- `GET /api/v1/catalog/published-exams?page&pageSize&search&assessmentType&scope&organizerId&noticeYear` — `PublishedExamPageResponse(Items, Total)` of `PublishedExamResponse`.
- `GET /api/v1/catalog/published-exams/{id}` — `PublishedExamDetailResponse` (the exam plus `Editions`: `PublishedExamEditionResponse` list); 404 `exam.not_found`.
- `GET /api/v1/catalog/published-exam-filters` — `PublishedExamFiltersResponse(Organizers, NoticeYears)`.
- All three: `catalog.browse`, else 403 `identity.forbidden`. No new error code (`exam.not_found` exists).

## Acceptance criteria
- AC1 Given exam A with one published and one draft edition and exam B with only drafts, when a Student lists published exams, then A appears with 1 edition and its latest published year, and B does not. (BR1, BR2, UC1, UC5)
- AC2 Given three published exams, when the list is requested with no text, then they come ordered by name, paged with the given size, and `Total` counts all three. (BR2)
- AC3 Given exam "Guarda Municipal" of "Prefeitura de São Paulo" (acronym "PMSP") with scope detail "São Paulo", when the search is "guarda sao", "PMSP" or "GUARDA  paulo", then it is found; when it is "guarda rio", then it is not. (BR3, UC2)
- AC4 Given an existing exam saved before the migration with scope detail "Goiânia", when the migration runs, then its `NormalizedScopeDetail` is "GOIANIA"; and saving an exam with a new scope detail updates it. (BR4)
- AC5 Given a published exam with a 2024 FGV edition and a 2025 CEBRASPE edition, when filtered by FGV and 2025, then it is not listed; by FGV and 2024, it is; by assessment type or scope that differs, it is not. (BR5, UC3)
- AC6 Given the filters `assessmentType=Foo`, `scope=Bar`, an unknown `organizerId` and `noticeYear=abc`, when the list is requested, then 200 with the unfiltered result. (BR6)
- AC7 Given a board that names only draft editions and a year with only drafts, when the filter options are requested, then neither is offered; boards come ordered by acronym and years newest first. (BR7)
- AC8 Given a published exam with editions 2023 (published), 2025 (published) and 2024 (draft), when a Student opens its page, then the two published editions show with board, position, notice reference, application date and notice link, 2025 first, and the 2024 draft does not. (BR8, UC4)
- AC9 Given an exam with only drafts, a deleted exam and a random id, when its page is opened, then the page shows the `exam.not_found` alert with a link back to the catalog; and `GET /published-exams/{id}` answers 404 `exam.not_found`. (BR8)
- AC10 Given an edition with a notice URL, when its row renders, then the link has `target="_blank"`, `rel="noopener noreferrer"` and an accessible name that says it opens in a new tab. (BR9)
- AC11 Given a fresh database, when the app starts, then Student, Curator and Admin hold `catalog.browse`; given the Admin removed it from Student and the app restarts, then Student still lacks it. (BR10, UC7)
- AC12 Given a user whose roles hold `catalog.manage` but not `catalog.browse`, then the menu shows no Catalog item, `/catalog` and `/catalog/exams/{id}` show Not Found, and the three endpoints answer 403 `identity.forbidden`; with `catalog.browse`, the item shows in the Study section. (BR10, UC6)
- AC13 Given a signed-in Admin with both permissions, when the catalog is listed, then draft-only exams are not shown. (BR1, UC5)
- AC14 Given `IPublishedExamQueries` resolved from the container, then it returns the same results as the endpoints for AC1, AC5 and AC8. (BR11)
- AC15 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR12)
- AC16 Given `/catalog?search=guarda&scope=State&page=2`, when it opens, then the search box shows "guarda", the scope filter shows State and page 2 loads; after opening an exam and following its breadcrumb or back link, the same search, filters and page are shown; `scope=Bar` in the address is ignored. (BR13, BR6)
- AC17 Given the three kit components on `/dev/ui`, then `AppDataTable` starts with `InitialSearch`/`InitialPage`, `AppTruncatedText` with `Href` renders one link with its tooltip and `lang`, and `AppPageHeader` with `TitleLang` sets the `lang` of its `h1`; without the new parameters each renders as before. (BR13)

## Decisions
- 2026-09-29 — One row per exam; its editions show on the exam page — the student looks for "the exam", and a row per edition repeats the exam name (owner, question 1).
- 2026-09-29 — Two screens (search, exam page); no edition page yet — an edition has six fields today; its own page arrives with the syllabus (epic 692) (owner, question 2).
- 2026-09-29 — Menu item "Catalog" in the Study section at `/catalog`; Home untouched — the section exists and is empty, and the Home is another item (owner, question 3).
- 2026-09-29 — Default order by exam name — predictable and the same as the back office (owner, question 4).
- 2026-09-29 — The "organizer" filter is the edition's exam board, not the issuing authority — it is what the brief calls organizer and what the Question Bank Simulator filters by; the authority is reachable through the text search (owner, question 5).
- 2026-09-29 — One notice year, not a range — one field on screen (owner, question 6).
- 2026-09-29 — Scope filter without state or municipality; the detail enters the text search — F-42 is still an idea and free text would split spellings (owner, question 7).
- 2026-09-29 — Search over exam name, issuing authority name and acronym, and scope detail, word by word — a student types "guarda sp", not the exact name (owner, question 8).
- 2026-09-29 — New permission `catalog.browse`, granted once to the three seed roles when it is created — permissions are checked by name, not by role, and the Admin can restrict it later (plans, institutions) without a start undoing it (owner, question 9).
- 2026-09-29 — Nobody sees drafts in the student catalog, Admin included — one rule; the back office is where drafts live (owner, question 10).
- 2026-09-29 — Board and year must match on the same edition — otherwise "FGV 2025" would list an exam FGV never applied in 2025 (Claude, technical).
- 2026-09-29 — Filter options are served by the Api from the published catalog, not by the back office lookup — `ListOrganizers` requires `catalog.manage`, and offering a board with nothing published leads to an empty list (Claude, technical).
- 2026-09-29 — `NormalizedScopeDetail` is a stored column, backfilled by the migration — the search stays in the database without a PostgreSQL extension, the F-33 BR9 precedent (Claude, technical). The backfill's accent stripping is checked by AC4.
- 2026-09-29 — Routes `published-exams` and `published-exam-filters` instead of a `browse` verb — the naming rule wants plural nouns (Claude, technical).
- 2026-09-29 — A Student with no published exam to show sees the empty state, not an error; the query cost is measured with `EXPLAIN` on the test container during build, not assumed (Claude, technical).
- 2026-09-29 — `/catalog` keeps search, filters, page and page size in the query string; the exam page's breadcrumb and back link carry it back, and an unreadable value is no filter (BR6); `AppDataTable` gains `InitialSearch`, `InitialPage` and `InitialPageSize` (owner, screen question 1).
- 2026-09-29 — Board options read "ACRONYM — Name", ordered by acronym — students know boards by acronym and long names were cut in the closed select; BR7 and AC7 changed (owner, screen question 2).
- 2026-09-29 — The exam name in the list stays truncated with a tooltip and is a link; `AppTruncatedText` gains `Href` (renders an `AppLink`, one tab stop) and `Lang` (owner, screen question 3).
- 2026-09-29 — `AppPageHeader` gains `TitleLang`; the exam page passes the exam's content language. The search results-count announcement is not in this item (a separate idea) (owner, screen question 4).
- 2026-09-29 — AC9 reworded: the exam page shows the `exam.not_found` alert with a link back to the catalog, and the Api answers 404 `exam.not_found`; the ordinary Not Found page stays for a missing permission (Claude, technical).
- 2026-09-29 — The exam facts reuse `AppFormAside` as a read-only summary; the build widens its doc comment instead of adding a detail-list component (Claude, technical).
- 2026-09-29 — Build passes (`system-design`, then `architect`), checked against the files. Accepted: everything stays in the Catalog module (no new project or package, nothing in Application); the one-time grant is `PermissionCatalog.InitialGrants` (optional last parameter, permission to role names, never Admin), declared by `CatalogModule` with `IdentityRoles.Student`/`Curator` and applied only in the seed branch that creates the permission row; `organizerId` and `noticeYear` are bound as `string?` and parsed (AC6); the backfill translates both cases of every accented Latin letter before `upper`, checked by AC4 with ç and ã rows (Claude, technical).
- 2026-09-29 — Dropped or changed after the review: page and page-size sanitizing lives in `PublishedExamListQuery.Sanitized()` and is applied by `PublishedExamQueries` (other modules calling `IPublishedExamQueries` get the cap too), not in the endpoint; the column length reads `CatalogLimits.ExamScopeDetailMaxLength`; the existing test `GetSession_SignedInAsStudent_HasNoPermissions` is rewritten to expect exactly `catalog.browse` (it is AC11 evidence); generated docs (`docs/architecture/`, `docs/api/`) are regenerated with the migration; one clause is added to `docs/agile/profile.md` line 20 about one-time grants (Claude, technical).
- 2026-09-29 — Screens built by the `frontend` agent, then checked by the main session (files read, build and Web tests run: 794 passed). Changes to the spec found on the way: the latest-year column uses `Format="0"` because MudBlazor formats through a floating-point value and `"D"` throws; `Permission.catalog.browse.Name` / `.Description` were added in three languages (the roles back office needs them and the item table did not list them); the row label and secondary text use the kit classes `font-weight-medium` and `app-muted`, since `app-item-row-label` and `text-secondary` do not exist in `app.css`; two dev-only gallery keys were added (Claude, technical).
- 2026-09-29 — Independent review (`/agile:review`), one round. Fixed: search words split on any whitespace, not only the space (BR3); the Api reads the enum filters by name only, as the Web does (`scope=1` is no filter on either side); the past-the-last-page fallback in `AppDataTable` logs a failure instead of leaving it unobserved (awaiting it would wait on the data callback itself). Accepted with a reason: the backfill's `btrim` and fixed `translate` list can differ from `CatalogText.Normalize` for a character outside Portuguese place names — the entity rewrites the column on the next save (comment in the migration). Not a defect of the build: the manual pages in three languages are written by `/agile:ship` (step of the definition of done), listed under `## Delivery` (Claude, technical).
- 2026-09-29 — Query cost measured on a PostgreSQL test container with 5,000 exams and 25,000 published editions: plain list page 84 ms, three-word search 18 ms, board and year filter 9 ms, filter options 37 ms. No new index (Claude, technical).

## Out of scope
- Choosing and saving a target exam with a date: that belongs to the AI coach's study plan (epic 701).
- An edition's own page, its syllabus and its questions — epics 692 and 693.
- A state or municipality picker in the filters — F-42.
- Visitors who are not signed in: the catalog is for signed-in users.
- Changing the Home page.
- Announcing the search result count to screen readers in every table — F-50.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: the app host started from this worktree (provided by the owner, in place when step 1 runs), one Student account and one Admin account (signed in by the owner; Claude enters no credentials), and two exams to create in the back office. Not checked by Claude: the app host was not started (it starts PostgreSQL and Redis containers, which the project rules keep to the test containers) and both screens sit behind sign-in, so every screen step below is unverified on screen; the same rules are covered by the bUnit tests and the Api tests listed in the coverage table.

1. Start the app: in `D:\dev\_icontrol\wt\simulab\f-36-catalog-browsing-for` run `dotnet run --project src/Hosts/Simulab.AppHost` (Git Bash and PowerShell 7 are the same); open the Web address the Aspire dashboard shows. Expected: the app starts and applies the migration.
2. Sign in as the Admin, open Exams, and create (or use) two exams: one with a Published edition (year 2025, a board, a notice link) and one with only a Draft edition. Expected: both exist in the back office.
3. Sign in as a Student (a new account is a Student). Expected: the menu has a Study section with "Catalog"; opening it lists the exam with the published edition and not the draft-only one; the row shows authority, type, scope, language, edition count and latest year.
4. Type part of the exam's name, then of its authority's acronym, then its state or city without accents: the list narrows each time. Pick a board and year filter: only exams with a published edition of that board in that year stay; "Clear filters" brings the list back. Expected: the address changes without adding history entries (Back leaves the catalog).
5. Open the exam by its name link. Expected: the header is the exam name; the editions list has only the published edition with board, position, notice reference, date and the notice link; the draft edition is not shown; the link opens in a new tab.
6. Use the breadcrumb "Catalog" link, then reload `/catalog?scope=Bar&page=2`. Expected: the search and filters you had are back after the breadcrumb; `scope=Bar` is ignored and the address is cleaned.
7. Switch the language in My account to pt-PT and then en, and return to the catalog. Expected: menu, headings, filters, empty states and the notice link name are translated; exam names stay as stored. Open `/catalog/exams/00000000-0000-0000-0000-000000000000`. Expected: the "not found" alert with a link back to the catalog.
8. Keyboard only, on `/catalog`: Tab to the search box, the four filters, then the exam name link, press Enter. Expected: every stop has a visible focus and the exam name is one tab stop. Then, as the Admin, open Roles and remove "Browse the catalog" from Student, restart the app and sign in as the Student. Expected: no Catalog item and `/catalog` shows Not Found.

## Delivery
- Branch: feature/F-36
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
