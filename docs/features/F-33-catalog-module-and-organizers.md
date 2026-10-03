---
feature: F-33
epic: Assessment catalog
status: done
board: 752
version: 2
---
# Catalog module and organizers back office

Technical terms: [glossary](../glossary.md)

## Summary
Create the `Catalog` module — five projects, the `catalog` schema, its own `DbContext` and first migration, the `catalog.manage` permission and the mechanism a second module needs to register a permission at all — and, on it, the first screen: an Admin manages organizers (exam boards, certifying bodies and universities) at `/admin/organizers`, in the list-plus-dialog pattern of `/admin/roles`.

## Goal
Give the catalog a home and an owner record. Nothing in epics 691 to 698 can exist before there is a module to hold it and an organizer to hang an exam on.

## What exists
- Identity is the only module (F-4 to F-20): five projects under `src/Modules/Identity/`, tests in `tests/Modules/Identity/Simulab.Identity.Tests`.
- `Simulab.Persistence`: `ModuleDbContext` (schema per module, tenant and soft-delete query filters built from the context instance), the audit and soft-delete interceptors, the database health check.
- `SharedKernel`: `TenantEntity` (nullable `TenantId`, audit, soft delete), `Result`/`Error` (`Code`, `ErrorKind`, English `Detail` for logs only), `AppJson.Options`.
- UI kit: `AppDataTable` with server-side paging (`AppTableQuery` → `AppTablePage<T>`, page sizes 10/25/50), `AppPageHeader` with a primary action, `AppRowActions`, `AppConfirmDialog`/`IConfirmService`, `AppTextField`, `AppSelectField`, `AppFormActions`, `AppTruncatedText`, the empty/loading/error states, `AppIcons` (Material Outlined).
- `/admin/roles` (`Roles.razor`, `RoleDialog.razor`, `RoleRow.cs`) is the exact pattern this screen follows.
- `NavigationItems.All` is the only place that lists menu items; `NavigationSection` already has `Content`, with no item in it.
- The Web reaches the Api through one typed client per module (`IdentityApiClient`), base address `https+http://api` from service discovery, `ApiResult<T>` carrying the error code, never a message.
- `FieldLengthTests` forbids a numeric `MaxLength=` in a page: every limit is a constant in the module's contracts (`AccountLimits` is the precedent).
- `SolutionAssemblies.All` lists the production assemblies every architecture rule runs against; `IdentityModuleBoundaryTests` checks the five-project direction of dependency for Identity only.
- DocGen builds the model of every `DbContext` in the assemblies next to it; it reaches them through the project references in `tools/Simulab.DocGen/Simulab.DocGen.csproj` (today: Jobs and Identity.Infrastructure).
- Packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Microsoft.EntityFrameworkCore.Design`, `xunit`, `AwesomeAssertions`, `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing` and `bunit` are all already in `Directory.Packages.props`.

Premises of the epic that the code corrected:
- **The permission table belongs to Identity.** `identity.permissions` is written by `EnsureRolesAndPermissionsAsync` (`IdentityModule.cs:323`) from the fixed list `IdentityPermissions.All`, which the Web also reads to build its policies (`Simulab.Web/Program.cs:101`). A second module cannot write there, so F-33 has to open the catalog to contributions (BR2).
- **Simulae's `ExamBoard` form is a page, not a dialog**, and `ExamBoard` has no soft delete — it has `ExamBoardStatus` (`Ativa`/`Inativa`). Rule `ui-project` puts a simple entity in a dialog and makes deleting a soft delete, so the screen is written fresh; what carries over from Simulae is the field set, the lengths (`Nome` 150, `Sigla` 20 uppercased, `Descricao` 500, `SiteOficial` 300) and the unique index on `(TenantId, Sigla)` with `AreNullsDistinct(false)`.

## Users and use cases
- UC1 An Admin opens `/admin/organizers` and sees every organizer: name, acronym and kind, sorted by name, paged, with a search box.
- UC2 An Admin adds an organizer in a dialog: name, acronym, kind, and optionally a description and an official website.
- UC3 An Admin edits an organizer in the same dialog.
- UC4 An Admin deletes an organizer after confirming; it disappears from the catalog.
- UC5 A Student or Curator never sees the Content section item and gets the ordinary Not Found page on `/admin/organizers`; the Api answers 403 `identity.forbidden`.

## Business rules
- BR1 The `Catalog` module is five projects (`Domain`, `Application`, `Contracts`, `Infrastructure`, `Api`) under `src/Modules/Catalog/`, with `CatalogModuleDbContext : ModuleDbContext`, schema `catalog`, its own migrations, and tests in `tests/Modules/Catalog/Simulab.Catalog.Tests`. It reads no other module's tables and references other modules only through their `Contracts`.
- BR2 A module declares its own permissions and Identity seeds the union. `Simulab.Catalog.Contracts.CatalogPermissions` holds `Manage = "catalog.manage"` and `All`; each module registers its list through a permission-catalog contribution in `SharedKernel`, and `EnsureRolesAndPermissionsAsync` seeds every registered name into `identity.permissions`, as it seeds `identity.roles.manage` today. Identity keeps ownership of the table; Catalog never writes to it. The Web builds one policy per registered permission instead of one per `IdentityPermissions.All`.
- BR3 `catalog.manage` is granted to the `Admin` seed role only, by the same idempotent start-up seed. The Curator gets it when AI-assisted import arrives (epic 698).
- BR4 One permission covers the whole module: organizers, and later exams and editions. It is never checked by role name, and it gates the endpoint, the page and the menu item.
- BR5 `Organizer : TenantEntity` is global data: `TenantId` is null, and every unique index that includes it is `NULLS NOT DISTINCT` (`.AreNullsDistinct(false)`).
- BR6 An organizer has `Name` (required), `Acronym` (required), `Kind` (required), `Description` (optional) and `Website` (optional). The lengths live in `Simulab.Catalog.Contracts.CatalogLimits` and are read by the Api, the EF mapping and the page: name 150, acronym 20, description 500, website 300.
- BR7 `OrganizerKind` is one of `ExamBoard`, `CertifyingBody`, `University`. An unknown value is refused with `organizer.kind_invalid`. The enum travels as a string.
- BR8 The name is trimmed and 2 to 150 characters. The acronym is trimmed, uppercased with the invariant culture, and 2 to 20 characters.
- BR9 Name and acronym are each unique across all organizers, ignoring case and accents, deleted rows included. Uniqueness is enforced by a unique index over a stored normalized column (trimmed, uppercased invariantly, accents removed), one per field, each including `TenantId` and `NULLS NOT DISTINCT`. A duplicate is refused with `organizer.name_taken` or `organizer.acronym_taken` (409).
- BR10 The website, when given, is an absolute `http` or `https` URL of at most 300 characters; anything else is `organizer.website_invalid`.
- BR11 Deleting is a soft delete. A deleted organizer disappears from every list and lookup, and its name and acronym stay taken (BR9). The confirmation says the organizer leaves the catalog; it says nothing about exams, which do not exist until F-34.
- BR12 The list is paged (`page`, `pageSize`, cap 100) and returns `{ items, total }`. Search matches the normalized name or the normalized acronym, so it ignores case and accents. Sorting is by name, acronym or kind; the default is name ascending.
- BR13 Domain rules return `Result`, never exceptions: `Organizer.Create` and `Organizer.Update` validate BR8 and BR10 and give back an `Error` with its code.
- BR14 Every UI text of the screen and every error code of the module exists in pt-BR, pt-PT and en, in `SharedResources` in the Web host, where the rest of the back office keeps its keys (see the change note of v2; the original wording asked for a separate `CatalogResources` set).

## Screens and API
- `/admin/organizers` — the list. Gated by `[Authorize(Policy = PermissionPolicy.Prefix + CatalogPermissions.Manage)]`, `MainLayout`, `AppPageHeader` with the primary action "Add", a searchable `AppDataTable` with the columns name, acronym and kind, `AppRowActions` with edit and delete.
- `OrganizerDialog` — add and edit, the only way to edit an organizer (rule `ui-project`): name, acronym, kind (`AppSelectField`), description, website.
- Menu: `NavigationSection.Content`, key `Nav.Organizers`, gated by `catalog.manage`.
- `GET /api/v1/catalog/organizers?page=&pageSize=&search=&sortBy=&descending=` — one page of organizers.
- `POST /api/v1/catalog/organizers` — create.
- `PUT /api/v1/catalog/organizers/{id:guid}` — update.
- `DELETE /api/v1/catalog/organizers/{id:guid}` — soft delete.
- Error codes (`Simulab.Catalog.Contracts.CatalogErrorCodes`): `organizer.not_found` (404); `organizer.name_required`, `organizer.name_too_long`, `organizer.acronym_required`, `organizer.acronym_too_long`, `organizer.kind_invalid`, `organizer.description_too_long`, `organizer.website_invalid` (400); `organizer.name_taken`, `organizer.acronym_taken` (409). A caller without the permission gets Identity's 403 `identity.forbidden`.

## Acceptance criteria
- AC1 Given the solution, when it is built, then `src/Modules/Catalog/` holds the five projects, `CatalogModuleDbContext` maps the `catalog` schema, and the architecture tests check the five-project direction of dependency for Catalog as they do for Identity. (BR1)
- AC2 Given a fresh database, when the Api starts, then the `catalog` schema exists with the `organizers` table, `identity.permissions` holds `catalog.manage`, and the `Admin` role holds it; starting again changes nothing. (BR2, BR3)
- AC3 Given a signed-in Admin, when they open `/admin/organizers`, then they see the list with name, acronym and kind, sorted by name. (UC1, BR12)
- AC4 Given a signed-in Student, when they open `/admin/organizers`, then they get Not Found, the Content section is not in their menu, and `GET /api/v1/catalog/organizers` answers 403 `identity.forbidden`. (UC5, BR4)
- AC5 Given the add dialog with a name, an acronym and a kind, when the Admin saves, then the organizer is created with the acronym uppercased and appears in the list. (UC2, BR6, BR8)
- AC6 Given an existing organizer, when the Admin saves another one whose name differs only in case or accents, then the Api answers 409 `organizer.name_taken` and the dialog shows the translated message. (BR9)
- AC7 Given an existing organizer, when the Admin saves another one with the same acronym in any case, then the Api answers 409 `organizer.acronym_taken`. (BR9)
- AC8 Given two organizers inserted directly with `TenantId` null and the same normalized acronym, when the second is saved, then PostgreSQL rejects it. (BR5, BR9)
- AC9 Given the edit dialog, when the Admin changes the name and saves, then the list shows the new name and the row keeps its id. (UC3)
- AC10 Given an organizer, when the Admin confirms the deletion, then it disappears from the list and from `GET /api/v1/catalog/organizers`, the row is still in the table with `IsDeleted` true, and creating a new organizer with that name is refused with `organizer.name_taken`. (UC4, BR11, BR9)
- AC11 Given a website that is not an absolute http or https URL, when the Admin saves, then the Api answers 400 `organizer.website_invalid` and no row is written. (BR10)
- AC12 Given organizers whose names differ only by accents, when the Admin searches without accents, then all of them are listed. (BR12)
- AC13 Given more organizers than one page, when the Admin moves to the next page, then the server returns that page and `total` counts all of them. (BR12)
- AC14 Given `Organizer.Create` with a blank name, when it runs, then it returns a failed `Result` with `organizer.name_required` and throws nothing. (BR13)
- AC15 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR14)

## Decisions
- 2026-09-23 — A module declares its permissions and Identity seeds the union (BR2) — `identity.permissions` is Identity's table and a module never writes to another module's tables; the alternative, a fixed list inside Identity naming every future module, makes Identity depend on all of them.
- 2026-09-23 — One permission for the whole module, `catalog.manage` — structure on the second use: split it when Curator and Admin really diverge (owner, question 5).
- 2026-09-23 — `catalog.manage` goes to Admin only; the Curator gets it with epic 698 (owner, question 6).
- 2026-09-23 — The menu item goes in the `Content` section, until now empty; Administration stays about users and access (owner, question 7).
- 2026-09-23 — No `/agile:screen` for this screen — it is the `/admin/roles` pattern with four fields, and the precedent is in the repository (owner, question 8).
- 2026-09-23 — `OrganizerKind` is a required enum from the start — the brief names the three kinds and F-36's student filter needs it (owner, question 1).
- 2026-09-23 — Soft delete only; no active/inactive status — nothing references an organizer yet, so deleting is free. The status arrives in F-34, when past exams exist to protect (owner, question 2).
- 2026-09-23 — Name and acronym are both unique, ignoring case and accents — a duplicated board is the commonest import error (owner, question 3).
- 2026-09-23 — No country on the organizer; it arrives with epic 704 (owner, question 4).
- 2026-09-23 — F-33 seeds no organizer rows; F-37 brings the real ones, remapped and tested (owner, question 9).
- 2026-09-23 — The list is searchable by name and acronym from the start (owner, question 10).
- 2026-09-23 — Uniqueness and search both run over stored normalized columns rather than a PostgreSQL `unaccent` extension — the extension is one more thing to install in every environment and test container, and Identity already normalizes a name this way.
- 2026-09-23 — `IdentityModuleBoundaryTests` is generalized to run over both modules instead of copied — Catalog is the second module, which is when the shared shape is due.
- 2026-09-23 — `tools/Simulab.DocGen` gets a project reference to `Simulab.Catalog.Infrastructure` in this feature, and the generated docs are regenerated with it — DocGen only sees the contexts whose assemblies sit next to it.
- 2026-09-23 — No new package: everything the module and its tests need is already in `Directory.Packages.props` (rule `build-config`, one list, no additions).
- 2026-09-23 — The shared Api test host went to a new project, `tests/Simulab.Testing.ApiHost`, instead of into `tests/Simulab.Testing` — the owner chose to share rather than duplicate, and putting it inside `Simulab.Testing` dragged the Api host into every test project that references it: `Simulab.Web.Tests` got two top-level `Program` types (28 build errors) and `Jobs.Tests`/`Persistence.Tests` got an EF Core version-conflict warning (MSB3277), which the gate treats as new warnings. No BR or AC changes; recorded in `docs/agile/profile.md`, as the project rules require for a new shared test project.

### From the independent review (2026-09-23)
- `catalog.manage` has its name, description and group text in the three languages, and `RoleResourcesTests` now reads `WebPermissions.All` instead of `IdentityPermissions.All` — the roles screen lists every permission Identity seeds, so a module that adds one without its text was showing the raw identifier there.
- `SaveOrganizerRequest.Kind` is `string?`, parsed by `ParseKind()` — with the enum in the contract an unknown kind failed inside deserialization and returned a 400 with no `code`, while BR7 promises `organizer.kind_invalid`. The OpenAPI document now names the three values in the property's description.
- The name column truncates with a tooltip (rule `ui-project`); a name is up to 150 characters.
- New guard: every permission a page or a menu item asks for is in `WebPermissions.All`, so a module whose permission is forgotten there fails in the tests and not when the page is opened.
- Accepted, captured as ideas: two concurrent creates of the same name give a 500 instead of 409 (the unique index refuses it, the handler does not translate the violation back); sorting by kind orders by the stored English name, not by the translated label; `Problem`/`StatusFor` is duplicated between `CatalogEndpoints` and `IdentityEndpoints`.

## Out of scope
- Exams, editions and anything that hangs off an organizer: F-34 and F-35.
- Blocking the deletion of an organizer that has exams, and the active/inactive status: F-34, when there is something to protect.
- A student-facing list of organizers: F-36.
- Seed rows: F-37.
- An audit trail of catalog changes, like F-14's role history: not in this epic.
- Splitting `catalog.manage` into finer permissions.
- A `Country` field, and anything multi-tenant: epics 704 and 703.

## Open questions
- (none)

## Change notes

### v2 — 2026-09-23
- What: BR14 said the texts live in "a `CatalogResources` set in the Web host". They live in `SharedResources` instead, with the rest of the back office.
- Why: `ErrorText`, which turns an API error code into the message a screen shows, reads `SharedResources` and nothing else. A separate set would have needed `ErrorText` changed to consult several sets, for no gain: every admin screen (Roles, Users, Role history, Account events) already keeps its keys there, and `ResourceParityTests` covers that set in the three languages.
- Affected: BR14 only; AC15 is unchanged and green (the three languages and the missing-key test).
- Re-approved: 2026-09-24 (owner).

## Coverage

| Criterion | Test |
|---|---|
| AC1 five projects, `catalog` schema, boundary rules | `ModuleBoundaryTests` (every check, run over Identity and Catalog), `SolutionLayoutTests` |
| AC2 schema, table, permission, Admin grant, idempotent | `OrganizerEndpointTests.Start_CreatesTheCatalogSchemaAndSeedsTheManagePermissionForAdmin`, `.Seeding_RunAgainOnASeededDatabase_ChangesNothing`; also checked in the running app host |
| AC3 the list, sorted by name | `OrganizerEndpointTests.List_WithoutASort_ComesBackByNameAscending`, `OrganizersPageTests.Load_ShowsNameAcronymAndTheKindTranslated` |
| AC4 a Student is refused everywhere | `OrganizerEndpointTests.EveryRoute_WithoutTheManagePermission_IsForbidden`, `AdminPagesAuthorizationTests.EveryAdminPage_RequiresItsModulesPermission`, `NavigationItemsTests.All_ContentItems_AreOrganizersBehindCatalogManage`, `.Visible_WithoutCatalogManage_HidesTheContentSection` (the Not Found page itself is step 9 of the script) |
| AC5 create, acronym uppercased | `OrganizerEndpointTests.Create_ValidData_StoresTheAcronymUppercasedAndListsIt`, `OrganizerTests.Create_ValidData_TrimsAndUppercasesTheAcronym`, `OrganizersPageTests.Add_ValidData_SendsItAndShowsTheSnackbar` |
| AC6 the name is taken, ignoring case and accents | `OrganizerEndpointTests.Create_NameTakenIgnoringCaseAndAccents_IsRefusedWithNameTaken`, `OrganizersPageTests.Save_NameTaken_ShowsTheMessageOnTheNameField` |
| AC7 the acronym is taken, in any case | `OrganizerEndpointTests.Create_AcronymTakenInAnyCase_IsRefusedWithAcronymTaken`, `OrganizersPageTests.Save_AcronymTaken_ShowsTheMessageOnTheAcronymField` |
| AC8 PostgreSQL refuses two global rows with the same key | `OrganizerUniqueIndexTests.TwoGlobalRowsWithTheSameKey_AreRejectedByPostgres`, `.TheTwoIndexesAreUniqueAndTreatNullTenantsAsEqual` |
| AC9 edit keeps the id | `OrganizerEndpointTests.Update_NewName_ShowsOnTheListUnderTheSameId`, `.Update_ItsOwnNameAndAcronym_IsNotATakenConflict`, `OrganizersPageTests.Edit_OpensFilledAndSendsAPutOnThatOrganizer` |
| AC10 soft delete keeps the row and the name | `OrganizerEndpointTests.Delete_ExistingOrganizer_HidesItKeepsTheRowAndKeepsItsNameTaken` |
| AC11 the website must be an absolute web address | `OrganizerEndpointTests.Create_WebsiteThatIsNotAnAbsoluteWebAddress_IsRefusedAndNothingIsWritten`, `OrganizerTests.Create_WebsiteThatIsNotAnAbsoluteWebAddress_FailsWithWebsiteInvalid`, `OrganizersPageTests.Save_WebsiteThatIsNotAnAbsoluteAddress_IsRefusedWithoutCallingTheApi` |
| AC12 the search ignores case and accents | `OrganizerEndpointTests.List_SearchWithoutAccents_FindsAccentedNames`, `.List_SearchByAcronym_FindsTheOrganizer`, `OrganizersPageTests.Search_SendsTheTermToTheServer` |
| AC13 paging, with the full total | `OrganizerEndpointTests.List_MoreRowsThanOnePage_ReturnsThePageAndTheFullTotal`, `.List_PageSizeOverTheCap_IsBroughtBackToTheCap`, `OrganizersPageTests.Load_AsksTheServerForTheFirstPage` |
| AC14 the domain answers with a `Result` and never throws | `OrganizerTests.Create_BlankOrTooShortName_FailsWithNameRequired` (and the other twelve `OrganizerTests`), `OrganizerEndpointTests.Create_BlankName_IsRefusedWithItsOwnCode` |
| AC15 every text in the three languages | `ResourceParityTests.Every_key_exists_in_every_language`, `RoleResourcesTests.EveryPermissionSystemRoleAndErrorCode_HasAText` (now over `WebPermissions.All` and `CatalogErrorCodes`) |
| BR7 an unknown kind is a coded 400 | `OrganizerEndpointTests.Create_KindThatIsNotOneOfTheThreeNames_IsRefusedWithKindInvalid` |

## Validation script

Start the app host from this worktree and sign in as an Admin. Claude opened `/admin/organizers` through the
app host but could not sign in (its rules forbid entering credentials), so steps 2 to 7 are yours.

Git Bash and PowerShell 7, from `D:/dev/_icontrol/wt/simulab/f-33` (same command in both):

```bash
dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

Expected: `Application host directory is: D:\dev\_icontrol\wt\simulab\f-33\src\Hosts\Simulab.AppHost` and
`Now listening on: https://localhost:17162`. The Web is at `https://localhost:7125`. Ctrl+C stops it.

1. Open `https://localhost:7125/admin/organizers` signed out → the sign-in page. Sign in as an Admin and open it again → the Organizers page, empty, offering **Add**. The **Content** section with **Organizers** is in the side menu.
2. **Add**: name `Centro Brasileiro de Pesquisa em Avaliação`, acronym `cebraspe`, kind *Exam board*, website `cebraspe.org.br` → the website field refuses it. Change it to `https://www.cebraspe.org.br` and save → the row appears, acronym shown as `CEBRASPE`.
3. Add a second one: name `Fundacao Getulio Vargas`, acronym `FGV`, kind *University*. Then try a third with the name `FUNDAÇÃO GETÚLIO VARGAS` → refused on the name field, "Another organizer already has this name". Try acronym `fgv` with a different name → refused on the acronym field.
4. Type `cebraspe` in the search box → only the first row. Type `avaliacao` (no accent) → the same row. Clear it and click the **Name** and **Kind** column headers → the order changes.
5. **Edit** the FGV row: change the kind to *Certifying body* and save → the row shows the new kind. Reopen it → the fields come back filled.
6. **Delete** the FGV row and confirm → it disappears. Add a new organizer named `Fundacao Getulio Vargas` → refused, the name is still taken.
7. Switch the language in the header to pt-PT and then to English → the title, the columns, the kinds and the messages change; the organizers' own names do not. On `/admin/roles`, open the **Admin** role → a **Catalog** group holding **Manage the catalog** / *Gerir o catálogo*, ticked, with its description; nothing there reads `catalog.manage` as its name.
8. Keyboard only, with Tab and Enter: reach **Add**, fill the dialog, save, then reach the row's **Edit** and **Delete**. Esc on a dialog with unsaved changes asks before closing.
9. Sign in as a Student (or remove `catalog.manage` from Admin on `/admin/roles` first) and open `/admin/organizers` → Not Found, and no **Content** section in the menu.

## Delivery
- Branch: feature/F-33 (merged and deleted; it never existed on the remote)
- Merge: 756e505 — `Merge feature/F-33: Catalog module and organizers back office (AB#752)`, 101 files, +3882 -236
- Tests: ship gate GREEN on 2026-09-24 — full suite 1000 passed, 0 failed, 0 skipped, 44 s (budget < 5 min); full build 19 s, 0 warnings, baseline stays empty. Per project: Identity 310, Web 483, Catalog 56, architecture 90, Api 9, Jobs 15, Persistence 15, SharedKernel 12, AppHost 8, Email 2.
- Manual pages: `docs/manual/{en,pt-BR,pt-PT}/organizers.md` (new) and `.../roles.md` (permissions are now grouped by area, Catalog being the second group)
