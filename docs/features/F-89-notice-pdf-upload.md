---
feature: F-89
epic: Assessment catalog
status: building
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

Tests locked: 59c2c106eee4845e47a5e99694b9f44de99de10b (13 files); unlocks: 0

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

## Events

- 2026-10-20 question — asked 3, from context 0
- 2026-10-20 blocked — test wrong: two tests of mine expect the wrong exception type and omit the status the Api requires on update; criteria unchanged

## Delivery
