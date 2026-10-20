---
feature: F-89
epic: Assessment catalog
status: validating
board: 132
version: 1
---
# Upload the notice PDF of an edition

Technical terms: [glossary](../glossary.md)

## Summary
In the back office, on the exam edition screen (F-35), the curator can upload the edition's notice as a PDF file, besides the official link (`NoticeUrl`) stored today. This is the notice document of the edition itself, not the exam paper uploaded for the AI-assisted import of epic E-9: the owner separated the two on 2026-10-04, which revises the F-35 note "the upload arrives with epic 698" for the notice document.

## Start
- Depends on: F-35 (done: the edition screen and `NoticeUrl`).
- Waits on (to start): nothing.
- Needed to validate: the local app host with Docker running (Azurite starts as a new container) and one real notice PDF of up to 20 MB plus one file over 20 MB and one non-PDF file renamed `.pdf` — the owner.
- Suggested path: `/agile:refine` → `/agile:build`. No `/agile:screen`: the change on each screen is one field or one link inside an existing section.
- Parallel with: any item that does not touch the Catalog module, `ExamEditionForm.razor`, `CatalogExam.razor` or the app host. Conflict risk with F-74 (notice subjects per edition, same screen) and F-64/F-65 (app host and Azure publish model).

## Goal
Keep a copy of each edition's notice inside Simulab, so the curator and the students still have it when the official site moves or goes down (F-37 found notice links answering 403 and 404).

## What exists (checked 2026-10-04)
- `ExamEdition` (`src/Modules/Catalog/Simulab.Catalog.Domain/Entities/ExamEdition.cs`) stores `NoticeUrl` (optional, max 300) and `NoticeReference`; no file.
- Back office: `ExamEditionForm.razor` (policy `catalog.manage`), section "Notice" with reference, link and applied date; API `POST/PUT/DELETE /api/v1/catalog/exams/{examId}/editions[/{id}]` (`ExamEndpoints.cs`).
- Students: `CatalogExam.razor` (policy `catalog.browse`) lists published editions from `GET /api/v1/catalog/published-exams/{id}` and shows the notice link in a new tab.
- No file storage: no `IFileStorage`, no blob client, no Azurite in the app host (`docs/infra.md`: "Planned, added by the first feature that needs it: Azurite"), no upload component in the UI kit. ADR-0001 #23 decides blob storage behind `IFileStorage`, Azurite through Aspire in dev, private containers.

## Users and use cases
- UC1 The curator chooses a PDF on the edition form (new or existing edition) and saves; the edition keeps the file.
- UC2 The curator replaces the PDF with another one, or removes it, and saves.
- UC3 The curator downloads the stored PDF from the edition form.
- UC4 A student opens a published exam and downloads the notice PDF of a published edition.

## Business rules
- BR1 The notice PDF and the notice link are independent and both optional; neither is required to save or publish an edition.
- BR2 An edition has at most one notice PDF. Saving with a new file replaces the stored one; saving with "remove" deletes it. The replaced or removed file is deleted from storage, not kept.
- BR3 Only a PDF is accepted: the content must start with `%PDF-`; the name or extension alone is not enough.
- BR4 The file is at most 20 MB (20 × 1024 × 1024 bytes); a larger file is refused before it is stored.
- BR5 Choosing, replacing or removing the file is a change of the form: it is sent only by the form's Save, together with the other fields, and Cancel discards it.
- BR6 Only `catalog.manage` uploads, replaces or removes the file and downloads it from any edition. A user with `catalog.browse` downloads it only from a `Published` edition of a published exam; a draft edition's file answers as not found.
- BR7 Deleting an edition deletes its notice PDF from storage.
- BR8 The file is kept in a private container; nobody gets a direct storage address. Downloads go through the API (and the Web for the browser) with the permission checks of BR6.

## Screens and API
- `/admin/exams/{examId}/editions/{id}` and `/admin/exams/{examId}/editions/new` (`ExamEditionForm.razor`), section "Notice": a file field "Notice PDF" with hint (PDF, up to 20 MB). Without a stored file: "Choose PDF". With a stored file: its name, size, a download link, "Replace" and "Remove". A chosen file not yet saved shows its name and "Discard choice". Errors appear inline in the field and in the error summary, as the other fields.
- `/catalog/exams/{id}` (`CatalogExam.razor`): beside the official link of each edition, a "Notice (PDF)" download link when the edition has a file; the accessible name says the edition and that it is a PDF download.
- `PUT /api/v1/catalog/exams/{examId}/editions/{id}/notice-document` — multipart, one file; replaces (`catalog.manage`).
- `DELETE /api/v1/catalog/exams/{examId}/editions/{id}/notice-document` — removes (`catalog.manage`).
- `GET /api/v1/catalog/exams/{examId}/editions/{id}/notice-document` — downloads, any status (`catalog.manage`).
- `GET /api/v1/catalog/published-exams/{examId}/editions/{id}/notice-document` — downloads a published edition's file (`catalog.browse`).
- `ExamEditionResponse` and `PublishedExamEditionResponse` gain `NoticeDocument` (nullable: file name, size in bytes, upload time).
- Error codes: `exam_edition.notice_document_not_pdf`, `exam_edition.notice_document_too_large`, `exam_edition.notice_document_not_found`.

## Acceptance criteria
- AC1 (UC1, BR1) Given an edition with no link and no file, when the curator chooses a valid PDF and saves, then the edition shows the file's name and size and the file can be downloaded with the same bytes.
- AC2 (BR1) Given an edition with neither link nor file, when it is saved as `Published`, then it is saved.
- AC3 (UC2, BR2) Given an edition with a stored file, when the curator saves a new PDF, then the edition holds only the new one and the old blob no longer exists in storage.
- AC4 (UC2, BR2) Given an edition with a stored file, when the curator chooses "Remove" and saves, then the edition has no file and the blob no longer exists.
- AC5 (BR3) Given a file named `notice.pdf` whose content does not start with `%PDF-`, when it is sent, then the API answers `exam_edition.notice_document_not_pdf` and nothing is stored.
- AC6 (BR4) Given a PDF of 20 MB + 1 byte, when it is sent, then it is refused with `exam_edition.notice_document_too_large` and nothing is stored; a PDF of exactly 20 MB is accepted.
- AC7 (BR5) Given a chosen file on the form, when the curator cancels, then nothing is uploaded and the stored file (if any) is unchanged.
- AC8 (UC1, BR5) Given a new edition with a chosen file, when the curator saves, then the edition is created and holds the file.
- AC9 (BR6) Given a user without `catalog.manage`, when they call the upload, remove or back-office download endpoint, then the answer is 403; the form shows no file actions to them.
- AC10 (UC4, BR6) Given a published exam with a published edition holding a file, when a student downloads it, then they get the PDF (`application/pdf`); given a draft edition holding a file, the student endpoint answers `exam_edition.notice_document_not_found` (404).
- AC11 (UC4) Given a published edition with a file, when a student opens the exam page, then the "Notice (PDF)" link is shown; with no file, it is not.
- AC12 (BR7) Given an edition holding a file, when it is deleted, then the blob no longer exists.
- AC13 (BR8) Given the stored file, then its container is private (anonymous access off) and no response carries a storage address.
- AC14 All new texts appear in pt-BR, pt-PT and en.

## Criterion → test

Tests locked: 281448a6904ecefd8ed9df1f6bec1e588b8d257d (13 files); unlocks: 3

| Criterion | Test |
| --- | --- |
| AC1 | `NoticeDocumentEndpointTests.Upload_AValidPdf_KeepsItOnTheEdition_AndTheDownloadReturnsTheSameBytes`; `ExamEditionNoticeDocumentTests.Render_WithAStoredFile_ShowsNameSizeDownloadReplaceAndRemove`; validation step (the browser saves the same bytes) |
| AC2 | `NoticeDocumentEndpointTests.Save_PublishedWithNeitherLinkNorFile_IsSaved` |
| AC3 | `NoticeDocumentEndpointTests.Upload_OverAnExistingFile_LeavesOnlyTheNewOneInTheStorage`; `ExamEditionNoticeDocumentTests.Save_AReplacementFile_SendsTheFieldsThenTheNewFile`; `Attach_ToAnEditionWithAFile_ReturnsTheBlobNameItReplaced` |
| AC4 | `NoticeDocumentEndpointTests.Remove_DeletesTheFileFromTheEditionAndTheStorage`; `ExamEditionNoticeDocumentTests.Save_AfterRemove_DeletesTheFileAndUploadsNothing` |
| AC5 | `NoticeDocumentEndpointTests.Upload_AFileNamedPdfThatIsNotOne_IsRefusedWithNotPdfAndStoresNothing`; `Upload_AnEmptyFile_IsRefusedWithNotPdf`; `Upload_WithoutAFilePart_IsRefusedWithNotPdf` |
| AC6 | `NoticeDocumentEndpointTests.Upload_ExactlyTwentyMegabytes_IsAccepted`; `Upload_OverTwentyMegabytes_IsRefusedWithTooLargeAndStoresNothing`; `ExamEditionNoticeDocumentTests.Choose_AFileOverTwentyMegabytes_IsRefusedOnTheFieldAndNothingIsSent`; `NoticeDocumentColumnsTests.AFileOver20Megabytes_IsRefusedByTheDatabase` |
| AC7 | `ExamEditionNoticeDocumentTests.Cancel_WithAChosenFile_UploadsNothingAndLeavesTheStoredFile`; `Choose_AFile_ShowsItsNameAndDiscard_AndSendsNothing` |
| AC8 | `ExamEditionNoticeDocumentTests.Save_ANewEditionWithAChosenFile_CreatesTheEditionThenUploadsTheFileToIt` |
| AC9 | `NoticeDocumentEndpointTests.BackOfficeRoutes_WithoutTheManagePermission_AreForbidden`; validation step (the form is reachable only with `catalog.manage`) |
| AC10 | `NoticeDocumentEndpointTests.Student_DownloadsTheFileOfAPublishedEdition`; `Student_AskingForTheFileOfADraftEdition_GetsNotFound`; `Student_AskingForAPublishedEditionWithoutAFile_GetsNotFound` |
| AC11 | `CatalogExamNoticePdfTests.Render_AnEditionWithAFile_ShowsTheNoticePdfDownloadLink`; `Render_AnEditionWithoutAFile_ShowsNoPdfLink`; `NoticeDocumentEndpointTests.PublishedExam_ListsTheNoticeDocumentOnlyOnTheEditionThatHasOne` |
| AC12 | `NoticeDocumentEndpointTests.DeletingTheEdition_DeletesItsFileFromTheStorage` |
| AC13 | `BlobFileStorageTests.Container_IsPrivate_AndAnAnonymousRequestForAFileIsRefused`; `NoticeDocumentEndpointTests.Responses_NeverCarryTheBlobNameOrAStorageAddress` |
| AC14 | `NoticeDocumentResourcesTests.EveryNewText_ExistsInTheThreeLanguages` |

## Decisions
- 2026-10-04 — The notice PDF belongs to E-2 (Assessment catalog), not E-9: the notice document and the exam paper for the AI import are two different uploads — owner (idea capture).
- 2026-10-04 — Curator and student both see the PDF; the student only on published editions — owner (Q1): the student already sees the official link, and a notice is a public document.
- 2026-10-04 — Link and PDF are independent and optional — owner (Q2): the PDF covers the case of an official site that moved or went down.
- 2026-10-04 — Maximum 20 MB — owner (Q3): notices are usually 1 to 10 MB; room for scanned ones.
- 2026-10-04 — One file per edition, replace deletes the old one, no history — owner (Q4): simple, no orphan files.
- 2026-10-04 — The file is sent by the form's Save, not on choose — owner (Q5): rule "one way to edit an item".
- 2026-10-04 — The seed editions (F-37) get no automatic download of their PDFs; the curator uploads by hand — owner (Q6): some links answered 403/404 in F-37, and a deploy must not depend on external sites.
- 2026-10-04 — Packages approved — owner (Q7): see `## Approved list`.
- 2026-10-04 — Nothing out of scope becomes a new item now — owner (Q8).
- 2026-10-04 — New building block `Simulab.Storage` with `IFileStorage` (save, open for read, delete, exists), implemented over Azure Blob through `Aspire.Azure.Storage.Blobs`; E-9 reuses it — Claude: ADR-0001 #23, and the profile keeps cross-cutting infrastructure in building blocks.
- 2026-10-04 — The app host adds Azurite locally (named volume `simulab-azurite-data`) and an Azure Storage account in publish mode (F-62 model), connection name `blobs` as `docs/infra.md` already lists — Claude: a deploy must not break when the Api asks for the client.
- 2026-10-04 — The edition stores the file's metadata (blob name, original file name, size, upload time) in the `catalog` schema; the blob name is `exam-editions/{editionId}/notice/{new guid}.pdf`, never the user's file name — Claude: avoids path injection and lets a replace write before deleting.
- 2026-10-04 — Replace order: store the new blob, save the row, then delete the old blob; a failed delete is logged and leaves an orphan, never a lost file — Claude: the row must never point at a missing blob.
- 2026-10-04 — The browser downloads through a Web endpoint that calls the Api with the signed-in user's token and streams the answer (`Content-Disposition: attachment`) — Claude: the tokens live server-side in the Web (B-3), so a plain link to the Api cannot authenticate.
- 2026-10-04 — Blazor Server reads the file with `IBrowserFile.OpenReadStream(maxAllowedSize: 20 MB)` and the Web forwards it as multipart; the Api endpoint sets its own request size limit to 20 MB plus multipart overhead — Claude: each layer refuses an oversize file before buffering it.
- 2026-10-04 — Glossary row: "PDF do edital" → `NoticeDocument`, added in this item before its first use in code — Claude: naming rule.
- 2026-10-20: size L judged, kept whole — the four use cases are one chain (store, replace or remove, download), the plan has 7 steps, no part is usable alone — asked (approved whole at refinement).
- 2026-10-20: design passes (system-design, architect) verified against the files; accepted: the storage resource `blob`, connection `blobs`, container `documents`, a keyed client beside the unkeyed key-ring client of ServiceDefaults; `NoticeDocument` as an owned value object with four nullable columns and a CHECK that writes every required column as `IS NOT NULL`; the port `INoticeDocumentFiles` in Application (Application may reference only Contracts, Domain and SharedKernel); `DisableAntiforgery` on the token-authenticated multipart endpoint; the student guard as one Infrastructure query; the `download` attribute checked on screen. Dropped: `Aspire.Hosting.Azure.Storage` and `Aspire.Azure.Storage.Blobs` are not new (already pinned at 13.6.1 since F-64), so only `Testcontainers.Azurite` 4.15.0 is added — Claude.
- 2026-10-20: design coverage — scope, simplicity, layout, rules, contracts, data checked against the item file, the profile, the project rules and the architecture tests (`ModuleBoundaryTests`, `InfraAccessTests`, `ArchitectureOverviewTests`, `TableDescriptionTests`, `FieldLengthTests`); not checked: the exact Aspire 13.6.1 method name `AddBlobs`, Blazor's handling of an anchor with `download`, .NET 10 antiforgery for `IFormFile` (all three are settled by a test or a validation step during the build) — Claude.
- 2026-10-20: traps — T1 one multipart request with the edition JSON and the file (changes the JSON contract and the bUnit readers); T2 reuse the F-16 download path (20 MB through SignalR); T3 the Api refusing to start without `blobs` everywhere (an Azurite container in four test projects) — all three avoided.
- 2026-10-20: design trade-off 5 (Web buffers the browser stream, then one short Api call) — owner chose as proposed.
- 2026-10-20: design trade-off 6 (Azurite only in Storage.Tests; Catalog.Tests use an in-memory `INoticeDocumentFiles`) — owner chose as proposed.
- 2026-10-20: open point 1 (new code `storage.not_configured`; the Api starts without `blobs` in Development and refuses outside it) — owner chose as proposed.
- 2026-10-20: architect findings accepted — the plan has no layer alone (schema folds into the upload step; downloads come before replace and remove); `docs/infra.md` (line 13, line 300 and an "Access" row with `InfraAccessTests.ApprovedAccessRows`), the profile line on `tests/Simulab.Testing` and the three resource files get the changes in step 1; `StorageErrorCodes` lives in `Simulab.Storage`; the Web routes carry `RequireAuthorization` by permission (`Manage`, `Browse`); a body above the Kestrel limit answers `exam_edition.notice_document_too_large` with a test, not a bare 413; the CHECK and the string lengths are explicit — Claude.
- 2026-10-20: tests unlocked (owner's yes, asked: two of my tests expected the wrong exception type and sent a PUT without the status the Api requires on update; the criteria did not change), fixed and locked again.
- 2026-10-20: tests unlocked a second time (owner's yes, asked: the Cancel test did not render the discard dialog the unsaved-changes rule opens, and the F-22 error text scan only reads `*.Contracts` projects, so `StorageErrorCodes` moved to a new `Simulab.Storage.Contracts` beside `Simulab.Ai.Contracts`; two tests gained a `using`; the criteria did not change), fixed and locked again.
- 2026-10-20: tests unlocked a third time (owner's yes, asked: the Cancel test passed alone and failed once in the whole suite, because its wait is driven by renders and the navigation that follows the confirmation renders nothing; it now waits with `SpinWait.SpinUntil` and a 15 s ceiling; proof: 12 of 12 alone and 4 of 4 whole-project runs green; the criteria did not change), fixed and locked again.
- 2026-10-20: the Api hosts that tests start outside Development get a storage connection that is never opened (`SimulabApiFactory` after `ConfigureHost`, `ApiFactory` always), and the blob client has no health check of its own — Claude: trap T3 avoided in the tests with one line per factory, not an Azurite container in four projects; a storage that is down must not take the Api out of rotation.
- 2026-10-20: Reuse: the Aspire client integration `AddKeyedAzureBlobServiceClient` and the Azurite emulator (`RunAsEmulator`), `Testcontainers` through the `RedisServer` shape (`AzuriteServer`), `ApiProblem` for every answer, the `Simulab.Ai.Contracts` pattern for a code the Web localizes, `AzureDeployment` account settings (`WithLocallyRedundantStorage` shared with the key rings), the form kit (`AppSectionCard`, `AppFormGrid`, `AppFormActions`, `AppLink`, `ErrorText`) and `MudButton HtmlTag="label"` for the picker — Claude.
- 2026-10-20: Shortcuts: 1 marked (src/Hosts/Simulab.Web/Components/Pages/Catalog/ExamEditionForm.razor, the chosen PDF held whole in the circuit's memory) — Claude.
- 2026-10-20: a new project, `Simulab.Storage.Contracts` (only `StorageErrorCodes`), referenced by `Simulab.Storage` and the Web — Claude: the error-code text check (F-22) finds codes only in `*.Contracts` assemblies, as it found `ai.not_configured` in `Simulab.Ai.Contracts`.
- 2026-10-20: plan — 1 Storage block, Azurite in the app host and in the publish model, infra docs, overview and boundary tests → build, Storage.Tests, architecture and app host tests; 2 Upload end to end: migration, `NoticeDocument`, port and adapter, handler, `PUT` → AC1 (upload half), AC5, AC6, AC13; 3 Downloads: back office and student `GET` → AC1, AC9 (API), AC10; 4 Replace, remove and delete the edition → AC3, AC4, AC12; 5 Web plumbing: client, buffered upload, the two download routes with policies → Web host tests; 6 `AppFileField` and the edition form with the three locales → AC2, AC7, AC8, AC9 (screen), AC14; 7 Student page link → AC11.

## Approved list
Packages (owner, Q7), versions checked on NuGet on 2026-10-04, all MIT:
1. `Aspire.Hosting.Azure.Storage` 13.6.0 — app host: Azurite locally, Azure Storage in publish mode.
2. `Aspire.Azure.Storage.Blobs` 13.6.0 — Api blob client (brings `Azure.Storage.Blobs` 12.30.0).
3. `Testcontainers.Azurite` 4.15.0 — integration tests against a real blob emulator.

## Out of scope
- The exam paper upload and extraction for the AI import: epic E-9.
- An inline PDF preview inside the screen.
- Antivirus scanning of uploaded files.
- Downloading the seed editions' PDFs automatically (Q6).
- Several files per edition (rectifications) and a file history.

## Open questions
- (none)

## Change notes

## Validation script
### English
Needed to validate: the local app host with Docker running (Azurite, the local blob storage emulator, starts as a new container the first time), three files of yours — one real notice PDF of up to 20 MB, one file over 20 MB, and one file that is not a PDF renamed to end in `.pdf` — and a second account made at `/sign-up` (confirm its email in Mailpit, the local mail catcher the dashboard links) — owner; the app host and the database are in place now, the files and the second account are yours to bring.

Whose data: the app runs on its own database `simulab_f89` inside the shared local PostgreSQL volume (the migration `AddExamEditionNoticeDocument` is already applied there, never to `simulab`), with the seeded exams; the uploaded files live in the new volume `simulab-azurite-data`.

1. [ ] Start the app host from the item's worktree `D:\wt\simulab\f-89-notice-pdf-upload` (stop any other app host first). Open https://localhost:7125 and sign in as `admin@simulab.local` → you land signed in; the console has no `fail` line.

   PowerShell 7:
   ```powershell
   $env:Database__Name = "simulab_f89"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   Git Bash:
   ```bash
   Database__Name=simulab_f89 dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. In the sidebar (left menu) open Content → Exams, click the exam "Guarda Municipal" of Curitiba, and in its "Editions" section click an edition (the edition page, a form in cards).
   - [ ] 2.1 In the card "Notice and application", below "Application date", the file field (a field that holds one PDF) "Notice PDF" shows "Choose PDF" and the hint "PDF, up to 20 MB."
   - [ ] 2.2 Press **Choose PDF** and pick your real PDF → the line "Chosen file, not saved yet: <name>" and **Discard choice** appear, and the field's other fields did not change.
   - [ ] 2.3 Press **Save** (bottom right) → "Edition saved."; the field now shows the file's name, its size (for example "2.5 MB"), **Download**, **Replace** and **Remove**.
   - [ ] 2.4 Press **Download** → the browser saves a file with the same name; open it and compare its size with the original: the same bytes, nothing else.
   - [ ] 2.5 The storage is private. Run (it prints the status of an anonymous request to the container; the port changes on each start):

   PowerShell 7:
   ```powershell
   $p = (docker port simulab-azurite 10000 | Select-Object -First 1).Split(":")[-1]; curl.exe -s -o NUL -w "%{http_code}`n" "http://127.0.0.1:$p/devstoreaccount1/documents?restype=container&comp=list"
   ```
   Git Bash:
   ```bash
   PORT=$(docker port simulab-azurite 10000 | head -1 | sed 's/.*://'); curl -s -o /dev/null -w "%{http_code}\n" "http://127.0.0.1:$PORT/devstoreaccount1/documents?restype=container&comp=list"
   ```
   → it prints `403` (anonymous access is refused). Repeat it any time.
3. Replace and remove, on the same edition page.
   - [ ] 3.1 Press **Replace**, pick another PDF, press **Save** → the name and size are the new file's; after the save the old file is gone (nothing else to do: the list of files is not shown).
   - [ ] 3.2 Press **Remove** → the line "The file will be removed when you save." and **Keep file** appear; press **Keep file** → the file is back as it was.
   - [ ] 3.3 Press **Remove** again, then **Save** → "Edition saved." and the field shows "Choose PDF" again.
   - [ ] 3.4 Press **Choose PDF**, pick a file, then press **Cancel** (bottom, text button) → a dialog "Discard changes?" appears; confirm → you return to the exam page; open the edition again → no file was stored.
4. Refusals on the same page.
   - [ ] 4.1 Choose the file that is not a PDF (renamed `.pdf`) and press **Save** → the fields are saved, the field shows "This file is not a PDF." in red below it, the error summary (the panel at the top of the form) lists "Notice PDF", and the chosen file stays so you can discard it.
   - [ ] 4.2 Press **Discard choice**, then choose the file over 20 MB → "The file is over 20 MB." appears at once on the field and nothing is chosen.
   - [ ] 4.3 The link and the PDF are independent: clear "Notice link", set the status to **Published** with no file and press **Save** → it saves (neither is required).
5. Add a new edition with a file: on the exam page press **Add edition**, pick an organizer and a notice year, press **Choose PDF** and pick your PDF, press **Save** → "Edition saved.", the address changes to the edition's own page, and the field shows the file (the edition was created first, then the file went to it).
6. As a student: sign out, sign in with the second account (a student), then in the sidebar open Study → Catalog and click the exam "Guarda Municipal" of Curitiba.
   - [ ] 6.1 Beside "Official notice", the editions that are published and hold a file show the link "Notice (PDF)"; an edition with no file shows no such link.
   - [ ] 6.2 Press **Notice (PDF)** → the browser saves the PDF and the page does not navigate away (the link has the `download` attribute; if the page navigates or goes blank, tell me).
   - [ ] 6.3 As the administrator set that edition back to **Draft** and **Save**, then reload the student page → its "Notice (PDF)" link is gone.
   - [ ] 6.4 As the student open `/admin/exams` → Page not found (the student cannot reach the back office or upload).
7. Language: in the app bar (the top strip) press the globe and choose Português (Brasil) → the field reads "PDF do edital", "Escolher PDF", "Substituir", "Remover", "Descartar escolha"; choose Português (Portugal) → "PDF do aviso" and "Transferir"; go back to English.
8. Keyboard only, as the administrator on the edition page: press Tab until the focus ring surrounds "Choose PDF" → press Enter or Space and the file dialog opens (Esc closes it); Tab on to **Save** and **Cancel**; the order is logical.

### pt-BR
Necessário para validar: o app host local com o Docker ligado (o Azurite, o emulador local do armazenamento de blobs, sobe como um contêiner novo na primeira vez), três arquivos seus — um PDF de edital de verdade de até 20 MB, um arquivo com mais de 20 MB e um arquivo que não é PDF renomeado para terminar em `.pdf` — e uma segunda conta criada em `/sign-up` (confirme o e-mail no Mailpit, o capturador de e-mail local que o painel mostra) — dono; o app host e o banco já estão no lugar, os arquivos e a segunda conta são você que traz.

De quem são os dados: o app roda no banco próprio `simulab_f89`, dentro do volume compartilhado do PostgreSQL local (a migração `AddExamEditionNoticeDocument` já foi aplicada ali, nunca em `simulab`), com as provas semeadas; os arquivos enviados ficam no volume novo `simulab-azurite-data`.

1. [ ] Suba o app host a partir da worktree do item `D:\wt\simulab\f-89-notice-pdf-upload` (pare qualquer outro app host antes). Abra https://localhost:7125 e entre como `admin@simulab.local` → você entra logado; o console não tem nenhuma linha `fail`.

   PowerShell 7:
   ```powershell
   $env:Database__Name = "simulab_f89"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   Git Bash:
   ```bash
   Database__Name=simulab_f89 dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. Na sidebar (menu lateral esquerdo) abra Content → Exams, clique na prova "Guarda Municipal" de Curitiba e, na seção "Editions", clique numa edição (a página da edição, um formulário em cartões).
   - [ ] 2.1 No cartão "Notice and application", abaixo de "Application date", o campo de arquivo (um campo que guarda um PDF) "Notice PDF" mostra "Choose PDF" e a dica "PDF, up to 20 MB."
   - [ ] 2.2 Aperte **Choose PDF** e escolha o seu PDF → aparecem a linha "Chosen file, not saved yet: <nome>" e **Discard choice**, e os outros campos não mudaram.
   - [ ] 2.3 Aperte **Save** (canto inferior direito) → "Edition saved."; o campo agora mostra o nome do arquivo, o tamanho (por exemplo "2.5 MB"), **Download**, **Replace** e **Remove**.
   - [ ] 2.4 Aperte **Download** → o navegador salva um arquivo com o mesmo nome; abra-o e compare o tamanho com o original: os mesmos bytes, nada além.
   - [ ] 2.5 O armazenamento é privado. Rode (ele imprime o status de um pedido anônimo ao contêiner; a porta muda a cada subida):

   PowerShell 7:
   ```powershell
   $p = (docker port simulab-azurite 10000 | Select-Object -First 1).Split(":")[-1]; curl.exe -s -o NUL -w "%{http_code}`n" "http://127.0.0.1:$p/devstoreaccount1/documents?restype=container&comp=list"
   ```
   Git Bash:
   ```bash
   PORT=$(docker port simulab-azurite 10000 | head -1 | sed 's/.*://'); curl -s -o /dev/null -w "%{http_code}\n" "http://127.0.0.1:$PORT/devstoreaccount1/documents?restype=container&comp=list"
   ```
   → imprime `403` (o acesso anônimo é recusado). Repita quando quiser.
3. Troca e remoção, na mesma página da edição.
   - [ ] 3.1 Aperte **Replace**, escolha outro PDF e aperte **Save** → o nome e o tamanho são os do novo arquivo; depois do salvamento o arquivo antigo some (não há mais nada a fazer: a lista de arquivos não é mostrada).
   - [ ] 3.2 Aperte **Remove** → aparecem a linha "The file will be removed when you save." e **Keep file**; aperte **Keep file** → o arquivo volta como estava.
   - [ ] 3.3 Aperte **Remove** de novo e depois **Save** → "Edition saved." e o campo volta a mostrar "Choose PDF".
   - [ ] 3.4 Aperte **Choose PDF**, escolha um arquivo e depois aperte **Cancel** (embaixo, botão de texto) → aparece o diálogo "Discard changes?"; confirme → você volta à página da prova; abra a edição de novo → nenhum arquivo foi guardado.
4. Recusas, na mesma página.
   - [ ] 4.1 Escolha o arquivo que não é PDF (renomeado `.pdf`) e aperte **Save** → os campos são salvos, o campo mostra "This file is not a PDF." em vermelho embaixo, o painel de erros (o quadro no alto do formulário) lista "Notice PDF" e o arquivo escolhido continua, para você descartá-lo.
   - [ ] 4.2 Aperte **Discard choice** e depois escolha o arquivo com mais de 20 MB → "The file is over 20 MB." aparece na hora no campo e nada fica escolhido.
   - [ ] 4.3 O link e o PDF são independentes: apague o "Notice link", ponha o status em **Published** sem arquivo e aperte **Save** → salva (nenhum dos dois é obrigatório).
5. Adicione uma edição nova com arquivo: na página da prova aperte **Add edition**, escolha uma banca e um ano do edital, aperte **Choose PDF** e escolha o seu PDF, aperte **Save** → "Edition saved.", o endereço muda para o da própria edição e o campo mostra o arquivo (a edição foi criada primeiro, depois o arquivo foi para ela).
6. Como aluno: saia, entre com a segunda conta (de aluno) e, na sidebar, abra Study → Catalog e clique na prova "Guarda Municipal" de Curitiba.
   - [ ] 6.1 Ao lado de "Official notice", as edições publicadas que têm arquivo mostram o link "Notice (PDF)"; uma edição sem arquivo não mostra esse link.
   - [ ] 6.2 Aperte **Notice (PDF)** → o navegador salva o PDF e a página não sai do lugar (o link tem o atributo `download`; se a página navegar ou ficar em branco, me avise).
   - [ ] 6.3 Como administrador, volte essa edição para **Draft** e **Save**; recarregue a página do aluno → o link "Notice (PDF)" dela sumiu.
   - [ ] 6.4 Como aluno abra `/admin/exams` → Page not found (o aluno não alcança o back office nem envia arquivo).
7. Idioma: na barra superior (a faixa no alto) aperte o globo e escolha Português (Brasil) → o campo diz "PDF do edital", "Escolher PDF", "Substituir", "Remover", "Descartar escolha"; escolha Português (Portugal) → "PDF do aviso" e "Transferir"; volte para o inglês.
8. Só com o teclado, como administrador na página da edição: aperte Tab até o anel de foco cercar "Choose PDF" → aperte Enter ou Espaço e o diálogo de arquivo abre (Esc fecha); siga com Tab até **Save** e **Cancel**; a ordem é lógica.

## Events

- 2026-10-20 question — asked 3, from context 0
- 2026-10-20 blocked — test wrong: two tests of mine expect the wrong exception type and omit the status the Api requires on update; criteria unchanged
- 2026-10-20 blocked — test wrong: Cancel test did not render the discard dialog providers; StorageErrorCodes moves to a Contracts project so the F-22 error text scan sees it; criteria unchanged
- 2026-10-20 blocked — test wrong: Cancel test waits on the page render but the navigation after the confirmation does not render it: flaky under load; criteria unchanged
- 2026-10-20 test-fail — tests failed (Simulab.slnx): Simulab.Api.Tests.ForwardedHeadersHostTests.ProxyListed_ForwardedHeaderFromAnUnlistedSender, 2 reds
- 2026-10-20 question — owner card: unlock of a locked test (round 1)
- 2026-10-20 question — owner card: unlock of a locked test (round 2)
- 2026-10-20 question — owner card: unlock of a locked test (round 3)

## Delivery
