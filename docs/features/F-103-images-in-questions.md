---
feature: F-103
epic: Question bank
status: building
board: 149
version: 1
---
# Images in questions

Technical terms: [glossary](../glossary.md)

## Summary
The curator adds images to the statement, the base text and the options; the files are stored through `IFileStorage` (ADR-0001 #23) and served to the back office. Simulae has no file storage: its images were inline HTML. Source: epic Question bank (E-4), brief capability 3 "base text and images".

## Start
- Depends on: F-95 (done); F-89 (creates `IFileStorage` and Azurite; `building` on 2026-10-20, its `Simulab.Storage` block is not merged yet).
- Waits on (to start): the merge of F-89.
- Needed to validate: the local app host with Docker running and a few images (a PNG, a JPEG, one over 2 MB, one SVG, one text file renamed `.png`) — owner.
- Model: opus (a door: an upload, and a new table).
- Suggested path: `/agile:refine` → `/agile:build`. No `/agile:screen`: one dialog and one toolbar button, drawn below.
- Parallel with: any item that does not touch the question editor, the sanitizer or the app host.

## Goal
Let a curator put a figure, a chart or a picture in the statement, the base text or an option, so questions that depend on an image (maps, graphs, drawings, alternatives that are pictures) can be authored in the bank.

## What exists (verified in the code, 2026-10-20)
- Question text is sanitized HTML. `QuestionHtmlSanitizer` allows `p br strong b em i u s sub sup ul ol li blockquote table thead tbody tr th td a`; `img` is not on the list, `data:` and external addresses are dropped (F-95 BR8, ADR-0005 #5). The Api sanitizes, the Web shows what is stored.
- The editor is Quill through `Blazored.TextEditor` behind `AppRichTextField`; the formats it offers are the list at `wwwroot/js/rich-text.js:10`, without `image`. The compact editor (options) has an inline toolbar only.
- The base text is its own entity since F-102 (`BaseText.Html`); a question holds the id. Each published question version copies the statement, base text and option HTML (F-98 BR1) and is never edited.
- Question permissions: `questions.manage` and `questions.review` only; no student screen reads a question yet (E-5, E-6).
- `IFileStorage` does not exist on the main branch. F-89 builds `Simulab.Storage` (an Azure Blob client behind `IFileStorage`, Azurite in the app host, private container `documents`, connection `blobs`) in its worktree. This item uses it as it merges.
- The Web holds the tokens server-side (B-3): a browser link to the Api cannot authenticate, so downloads go through a Web route that calls the Api with the signed-in user's token (the F-89 pattern).
- The job table has no recurring schedule; `JobCleanup` is an hourly hosted sweep in the Api host. The image sweep follows that shape.
- Blazor Server messages from the browser are limited to 32 KB (no `MaximumReceiveMessageSize` is set), so an image inside the HTML the editor hands to the page, as base64, would break the circuit.

## Users and use cases
- UC1 The curator, writing a statement, a base text or an option, chooses "Insert image", picks a file, types its alternative text (or marks it decorative) and inserts it; the picture appears in the editor.
- UC2 The curator removes an image from a text in the editor and saves; the picture is gone from the text and, when nothing else keeps it, from storage.
- UC3 The curator or the reviewer sees the pictures in the preview, the review screen and the base text screen.

## Business rules
- BR1 Images go in the statement, the base text and the options. Not in the explanation, the base text title or the change reasons.
- BR2 Accepted formats: PNG, JPEG, WebP, GIF. The format is decided by the first bytes of the file; the name, the extension and the sent content type are not trusted. SVG and any other content are refused. At most 2 MB (2 × 1024 × 1024 bytes); a larger file is refused before it is stored.
- BR3 The file is stored when it is chosen, not at the form's Save: the editor needs an address to show the picture. A stored image that no saved text holds yet is pending, and has no owner.
- BR4 Every image in a text carries its alternative text (WCAG 2.2 AA): an `img` with no `alt` attribute is refused on save; an empty `alt` means decorative and is accepted. The text is at most 250 characters.
- BR5 The sanitizer's allow-list gains `img` with `src` and `alt` only. `src` must be exactly `/question-images/<guid>`; any other `src` (`http`, `https`, `data:`, `javascript:`, another path) removes the `img`; every other attribute is dropped. The list stays one list, pinned by a test.
- BR6 On a save of a question or a base text, the Api reads the image ids in the sanitized HTML. Each id must exist and be pending or already owned by this same question or base text; it then becomes owned by it. An id that does not exist, or that belongs to another question or base text, is refused. A question's statement and options together belong to the question; the base text's images belong to the base text.
- BR7 At most 10 images per question (statement and options together) and 10 per base text, counted in the stored HTML.
- BR8 An image counts as content: a statement, an option or a base text that holds only images is not empty. The plain text (list excerpt, search) carries the alternative text of the non-decorative images.
- BR9 A published question version copies the HTML as it is and so keeps pointing at its images (F-98 BR1, BR5; F-102 BR4 for the base text). An image is deleted only when the live text and every version of the owner, and of the questions linked to a base text, no longer hold its id.
- BR10 Removing an image from a text and saving deletes its file (blob and row, soft delete) when BR9 allows; otherwise the file stays. Soft-deleting a question or a base text keeps its files.
- BR11 A pending image older than 24 hours is deleted (blob and row) by a sweep that runs hourly in the Api host. A failed blob delete is logged and tried again on the next run; a row never points at a missing blob.
- BR12 Only `questions.manage` uploads an image. `questions.manage` and `questions.review` read one. Nobody gets a storage address: the container is private and the picture is served by the Api, and to the browser by a Web route that calls the Api with the user's token. The student route arrives with the simulator (E-5).
- BR13 The Api answers an image with the stored content type, `X-Content-Type-Options: nosniff`, `Content-Security-Policy: sandbox` and `Cache-Control: private`. The blob name is `question-images/{new guid}`, never the user's file name.
- BR14 The editor shows an "Insert image" button in both toolbars (full and compact). A file pasted or dropped into the editor is not inserted: only the dialog inserts.
- BR15 In the preview and the review screens the picture is scaled down to the width of its column, never up.

## Screens and API
- Editor toolbar (`AppRichTextField`, used by `QuestionForm` and `BaseTextForm`): new button "Insert image" (`AppIcons.Image`), in the full and the compact toolbar. It opens `AppRichTextImageDialog` [new], like the link dialog.
- `AppRichTextImageDialog`: a file field (`Choose image`, hint "PNG, JPEG, WebP or GIF, up to 2 MB"), the chosen file's name and size, a preview, `Alternative text` (required, max 250), a `Decorative image` checkbox that disables the text, `Insert` and `Cancel`. `Insert` sends the file, then puts the picture at the caret. Errors are inline in the field and in the error summary.
- Question form, base text form, preview dialog and review screen show the picture as part of the text (`app-question-content`), `max-width: 100%`.
- `POST /api/v1/question-bank/images` — multipart, one file; `questions.manage`; answers 201 `QuestionImageResponse` (`Id`, `Url` = `/question-images/{id}`, `ContentType`, `SizeBytes`).
- `GET /api/v1/question-bank/images/{id}` — streams the file; `questions.manage` or `questions.review`.
- Web `GET /question-images/{id}` — `RequireAuthorization` by permission (`questions.manage` or `questions.review`), calls the Api with the signed-in user's token, streams the answer.
- `POST/PUT /api/v1/question-bank/questions` and `/base-texts`: the contracts do not change; the HTML carries the images.
- Error codes: `question_image.not_an_image`, `question_image.too_large`, `question_image.not_found`; on save `question.image_alt_required`, `question.image_not_found`, `question.images_too_many`, `base_text.image_alt_required`, `base_text.image_not_found`, `base_text.images_too_many`.
- Data: table `question_bank.question_images` (id, blob name, content type, size in bytes, owner kind and owner id, both null while pending, created at, audit and soft-delete columns); a migration.

### Wireframe: Insert image dialog
```text
+--------------------------------------------------------+
| Insert image [new]                                  X  |
+--------------------------------------------------------+
| Image *                                                |
| [ Choose image ]  map-of-brazil.png  (184 KB)          |
| PNG, JPEG, WebP or GIF, up to 2 MB                     |
| +----------------------------------------------------+ |
| |                  (preview)                         | |
| +----------------------------------------------------+ |
| Alternative text *                                     |
| [ Map of Brazil with the five regions coloured      ]  |
| [ ] Decorative image (no alternative text)             |
|                                                        |
|                        [ Cancel ]   [ Insert ]         |
+--------------------------------------------------------+
Error: "File is not a PNG, JPEG, WebP or GIF image" under
the file field; "Alternative text is required" under the text.
```

### Wireframe: Question form, statement
```text
+--------------------------------------------------------+
| Statement *                                            |
| [B][I][U][S][x2][x2][link][image] [changed]            |
| +----------------------------------------------------+ |
| | Look at the map and answer:                        | |
| | +------------------------------+                   | |
| | | (image, scaled to the column)|                   | |
| | +------------------------------+                   | |
| +----------------------------------------------------+ |
| Options                                                |
| (A) [B][I][link][image] [changed]  [ (image) ]         |
+--------------------------------------------------------+
```

## Security
### Surfaces
- S1 `POST /api/v1/question-bank/images` and the Web upload from the dialog — `questions.manage`.
- S2 `GET /api/v1/question-bank/images/{id}` and Web `GET /question-images/{id}` — `questions.manage` or `questions.review`.
- S3 The `img` tag in the HTML of a question or base text sent to `POST/PUT` — `questions.manage`.
- S4 The pending-image sweep (no caller; storage cost).

### Threats
- T1 (S1, S) A caller without a token, or without `questions.manage`, uploads a file → AC1 [sec]
- T2 (S1, T) A file named `.png` whose content is HTML, a script, SVG or an executable is stored and later served → AC2 [sec]
- T3 (S1, D) A file over 2 MB, or more than 10 images in a text, fills the storage or the memory → AC3 [sec]
- T4 (S1, T) A file name such as `../x` chooses the blob path → AC4 [sec]
- T5 (S2, I) A user without either permission, or the public internet, reads an image → AC5 [sec]
- T6 (S2, T) An image served inline runs script in the app's origin → AC6 [sec]
- T7 (S3, T) A text carries an `img` with an external, `data:` or `javascript:` address, or an `onerror` → AC7 [sec]
- T8 (S3, I) A text names the id of an image that belongs to another question, showing a draft's picture → AC8 [sec]
- T9 (S4, D) Images chosen and never saved pile up → AC9 [sec]

## Docs
- flow: new docs/flows/images-in-questions.md — insert, remove and see an image; the limits; the versions that keep an image.
- flow: update docs/flows/shared-base-text.md#Steps — the base text editor gains the image button.
- architecture: update docs/architecture/QuestionBank/data-dictionary.md — `question_images` (generated by DocGen).

## Acceptance criteria
- AC1 [sec] (T1) Given no token, or a user without `questions.manage`, when they `POST` an image, then the answer is 401 or 403 and nothing is stored; a user with only `questions.review` also gets 403.
- AC2 [sec] (T2, BR2) Given a file named `a.png` that holds HTML, one that holds SVG, and one that holds text, when each is sent, then the answer is `question_image.not_an_image` and nothing is stored; real PNG, JPEG, WebP and GIF files are accepted.
- AC3 [sec] (T3, BR2, BR7) Given a file of 2 MB + 1 byte, then `question_image.too_large` and nothing stored, and exactly 2 MB is accepted; given a question whose HTML holds 11 images, then `question.images_too_many`, 10 is accepted; the same for a base text.
- AC4 [sec] (T4, BR13) Given a file named `../../x.png`, when it is stored, then the blob name is `question-images/<guid>` and the file name appears nowhere in the stored name.
- AC5 [sec] (T5, BR12) Given a user without `questions.manage` and without `questions.review`, when they `GET` an image through the Api or the Web route, then 403; given a user with either, then the bytes; given the stored blob, its container is private and no response carries a storage address.
- AC6 [sec] (T6, BR13) Given a stored image, when it is served, then the response has the stored content type, `nosniff`, `Content-Security-Policy: sandbox` and `Cache-Control: private`.
- AC7 [sec] (T7, BR5) Given HTML with `<img src="https://x/y.png">`, `<img src="data:image/png;base64,...">`, `<img src="javascript:...">` and `<img src="/question-images/<id>" onerror="...">`, when it is saved in any of the four fields, then the first three are gone and the fourth keeps only `src` and `alt`; the allow-list pin test lists `img`.
- AC8 [sec] (T8, BR6) Given an image owned by question A, when question B (or a base text) saves HTML that holds its id, then `question.image_not_found` (`base_text.image_not_found`) and B is unchanged; an id that does not exist is refused the same way.
- AC9 [sec] (T9, BR11) Given a pending image older than 24 hours and one younger, when the sweep runs, then the old one is deleted (blob and row) and the young one stays; an image owned by a text is never swept.
- AC10 (UC1, BR3, BR6) Given a curator on the question form, when they insert an image in the statement and in two options and save, then the stored question holds the three `img` and the three images are owned by it.
- AC11 (BR4) Given an `img` with no `alt`, when it is saved, then `question.image_alt_required`; with `alt=""` it is accepted; with an `alt` over 250 characters it is refused.
- AC12 (BR8) Given an option that holds only an image, when the question is published with two such options, then it is accepted; a statement with nothing but empty tags is still refused.
- AC13 (UC2, BR9, BR10) Given a draft question with an image, when the curator removes it from the text and saves, then the blob and the row are gone; given a published question, whose latest version holds the image, when the curator removes it and saves, then the file stays; and when no version holds it any more, it is deleted.
- AC14 (BR9) Given a base text image used by a published question, when the base text is edited without that image, then the question's version keeps pointing at it and the file stays.
- AC15 (UC3, BR15) Given a question with images, when the curator opens the preview, then the pictures show scaled to the column, from the Web route, with their alternative text.
- AC16 (BR14) Given the editor, when a curator pastes an image from the clipboard, then nothing is inserted; the "Insert image" button is in the full and in the compact toolbar.
- AC17 All new texts appear in pt-BR, pt-PT and en.

## Criterion → test

Tests locked: 8d35a0d661b6f988b717ab9264b3afd04de1b249 (8 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | QuestionImageUploadTests: Upload_WithoutAToken_Answers401_AndStoresNothing, Upload_ByAStudent_Answers403_AndStoresNothing, Upload_ByAHolderOfReviewOnly_Answers403_AndStoresNothing |
| AC2 | QuestionImageUploadTests: Upload_OfEachAcceptedFormat_Answers201_WithTheTypeOfItsBytes, Upload_OfAFileNamedPngThatIsNotAnImage_AnswersNotAnImage_AndStoresNothing, Upload_WithNoFilePart_AnswersNotAnImage |
| AC3 | QuestionImageUploadTests: Upload_OfExactlyTwoMegabytes_IsAccepted, Upload_OfOneByteOverTwoMegabytes_AnswersTooLarge_AndStoresNothing; QuestionImageOwnershipTests: Save_AQuestionWithTenImages_IsAccepted_AndWithElevenIsRefused_CountingTheOptionsToo, Save_ABaseTextWithTenImages_IsAccepted_AndWithElevenIsRefused |
| AC4 | QuestionImageUploadTests: Upload_OfAFileNamedWithATraversal_StoresItUnderAGeneratedName |
| AC5 | QuestionImageDownloadTests: Download_ByAHolderOfManage_AnswersTheBytes, Download_ByAHolderOfReviewOnly_AnswersTheBytes, Download_ByAStudent_Answers403, Download_WithoutAToken_Answers401, Download_AnswerCarriesNoStorageAddress; Web route: pending (step 7) |
| AC6 | QuestionImageDownloadTests: Download_AnswersTheStoredTypeAndTheHardeningHeaders |
| AC7 | QuestionImageSanitizerTests: Sanitize_AnImgWithAnAddressOtherThanAStoredImage_IsRemoved, Sanitize_AStoredImageWithExtraAttributes_KeepsOnlySrcAndAlt, Sanitize_AStoredImageInsideAListItemATableCellOrAlone_IsKept; QuestionImageOwnershipTests: Save_AStatementWithForeignAddressesAndAStoredImageWithAnEventAttribute_KeepsOnlyTheCleanStoredImage, Save_AnOptionAndABaseTextWithForeignAddresses_DropThemToo; QuestionHtmlSanitizerTests: AllowedTags_AreExactlyTheListOfTheRule (explanation drops images: Save_AnExplanationWithAStoredImage_DropsTheImage) |
| AC8 | QuestionImageOwnershipTests: Save_AQuestionNamingAnImageOfAnotherQuestion_AnswersImageNotFound_AndStaysUnchanged, Save_ANewQuestionNamingAnImageOfAnotherQuestion_AnswersImageNotFound, Save_ABaseTextNamingAnImageOfAQuestion_AnswersImageNotFound, Save_ABaseTextNamingAnImageOfAnotherBaseText_AnswersImageNotFound, Save_NamingAnImageThatDoesNotExist_AnswersImageNotFound_ForAQuestionAndForABaseText |
| AC9 | QuestionImageSweepTests: Sweep_DeletesAPendingImageOlderThan24Hours_AndKeepsTheYoungOneAndTheOwnedOne, Sweep_LeavesAPendingImageYoungerThan24Hours |
| AC10 | QuestionImageOwnershipTests: Save_AQuestionWithAnImageInTheStatementAndInTwoOptions_StoresThemAndOwnsThemByTheQuestion, Save_ABaseTextWithAnImage_OwnsItByTheBaseText, Save_AgainWithTheImageThatTheQuestionOwns_IsAccepted; editor side: pending (step 7) |
| AC11 | QuestionImageSanitizerTests: Sanitize_AnImgWithNoAlt_IsCountedAsWithoutAlt, Sanitize_AnImgWithAnEmptyAlt_IsDecorativeAndNotCounted, Sanitize_AnAltOverTheLimit_IsCountedAsTooLong; QuestionImageOwnershipTests: Save_AQuestionImageWithNoAlt_AnswersAltRequired_WithAnEmptyAltIsAccepted_AndWithALongAltIsRefused, Save_ABaseTextImageWithNoAlt_AnswersAltRequired_WithAnEmptyAltIsAccepted_AndWithALongAltIsRefused |
| AC12 | QuestionImageSanitizerTests: Sanitize_ATextWithOnlyAnImage_IsNotEmpty, Sanitize_PlainText_CarriesTheAltOfANonDecorativeImageOnly; QuestionImageOwnershipTests: Publish_AQuestionWhoseOptionsAreOnlyImages_IsAccepted; empty tags still refused: QuestionEndpointTests (existing, QuestionStatementRequired) |
| AC13 | QuestionImageReleaseTests: Save_ADraftWithoutItsImageAnymore_DeletesTheFileAndTheRow, Save_APublishedQuestionWithoutItsImageAnymore_KeepsTheFileBecauseAVersionHoldsIt, Save_AnUnpublishedQuestionThatAddedAnImageAfterItsVersionsAndDropsIt_DeletesTheFile, Delete_ADraftQuestionWithAnImage_KeepsTheFile, Version_OfAPublishedQuestion_KeepsTheImageAddressAfterTheTextDroppedIt |
| AC14 | QuestionImageReleaseTests: Save_ABaseTextWithoutItsImageAnymore_KeepsTheFileWhileAPublishedQuestionVersionHoldsIt, Save_ABaseTextNoQuestionVersionHolds_DeletesTheFileWhenTheTextDropsTheImage |
| AC15 | pending |
| AC16 | pending |
| AC17 | pending |

## Decisions
- 2026-10-20: files go through `IFileStorage`, in a private container, no public address, as decided in ADR-0001 #23 and `project.md` — confirmed: docs/decisions/ADR-0001-foundation.md.
- 2026-10-20: the Api sanitizes every text, the Web shows what is stored, one allow-list pinned by a test (F-95 BR8); the list gains `img` with a stored-image address only — confirmed: docs/features/F-95-single-choice-questions.md.
- 2026-10-20: the image is sent when it is chosen, not at the form's Save — changed: the file is sent only by the form's Save (docs/features/F-89-notice-pdf-upload.md). Reason: the editor needs an address to show the picture, and the HTML with the image embedded breaks the 32 KB limit of the Blazor messages; an unsaved image is pending and swept after 24 hours — owner (asked).
- 2026-10-20: images in the statement, base text and options; not in the explanation — owner (asked).
- 2026-10-20: alternative text required, with a "decorative image" option — owner (asked): WCAG 2.2 AA is a project rule.
- 2026-10-20: PNG, JPEG, WebP, GIF, up to 2 MB, no SVG — owner (asked).
- 2026-10-20: at most 10 images per question and 10 per base text — owner (asked).
- 2026-10-20: only curators and reviewers read an image now; the student route comes with the simulator (E-5) — owner (asked): no student screen exists to validate it.
- 2026-10-20: only `questions.manage` uploads — owner (asked).
- 2026-10-20: nothing becomes a later item now — owner (asked): reuse of an image, resize and compress, and images in the transcription stay out of scope.
- 2026-10-20: size M judged, kept whole — one chain (choose, store, show, release); the plan has about six steps; no part is usable alone — Claude.
- 2026-10-20: no new package. The image type is read from the first bytes by hand (no imaging library: dimensions are not checked, the picture is scaled by CSS); the Azurite test container comes from F-89 — Claude.
- 2026-10-20: the image belongs to a question or a base text through `owner kind` and `owner id` set on save from the ids found in the sanitized HTML; the live-or-versioned check searches the version HTML for the id — Claude: no extra link table to keep in step with the HTML.
- 2026-10-20: build spikes, settled by a test during the build: Quill 2 writes `alt` for an `img` (its image format lists `alt`); the editor drops a pasted or dropped image file; the sanitizer keeps `img` when it sits inside an option, a list item and a table cell — Claude.
- 2026-10-20: the plan has no layer alone: 1 table, upload and download endpoints, the sanitizer `img`, ownership on save, the sweep (needs F-89 merged) → AC1 to AC9, AC11, AC12; 2 the editor button, the dialog and the Web route → AC10, AC15, AC16, AC17; 3 removal and versions → AC13, AC14 — Claude.
- 2026-10-20: blocked (depends on F-89 (building)) — unblocked by the delivery of F-89
- 2026-10-21: blocked (depends on F-89 (building)), unblocked (F-89 delivered)
- 2026-10-21: design passes (system-design, architect) verified against the files. Accepted: the 8-step slice, the `question_images` table (create only, no foreign key on the owner), `IQuestionImageFiles` port in Application with the adapter in Infrastructure (the architecture test lets Application reference only its Domain, Contracts and SharedKernel), a shared `QuestionImageOwnership` for both save handlers with a `FOR UPDATE` read, magic bytes by hand, blob first and row second, blob delete after the commit. Dropped: the inline `RequireClaim` on the Api route (the Api token carries no permission claim: `TokenEndpoints.cs:392-395`; the Api checks by `PermissionAuthorizationHandler`) — Claude.
- 2026-10-21: design coverage — contracts, data, simplicity, rules, scope, layout (read: item, profile, project rules, Catalog notice-document pattern, Identity authorization, architecture tests) — Claude.
- 2026-10-21: traps — T1 link table instead of ids read from the HTML: drift from the HTML; T2 the kit field calling the module client: every rich-text field gets the button; T3 trusting the content type or extension: stored SVG/HTML served in the app origin — Claude.
- 2026-10-21: design trade-off 1 (manage-or-review on the Api image route) — owner chose as proposed: the endpoint asks `IPermissionQueryService` (Identity.Contracts) and answers 403 without either permission; no change to the Identity contract.
- 2026-10-21: design trade-off 2 (alt over 250 characters) — owner chose as proposed: two new codes `question.image_alt_too_long` and `base_text.image_alt_too_long` (change note v2).
- 2026-10-21: design trade-off 3 (Cache-Control of the Web route) — owner chose as proposed: `private`, as BR13.
- 2026-10-21: design trade-off 4 (where the sweep runs) — owner chose as proposed: a hosted service of the module, switch `QuestionBank:ImageSweepEnabled` (default true, false in `SimulabApiFactory`).
- 2026-10-21: swept pending rows are soft-deleted (project rule "soft delete only"); the BR9 search for a base-text image reads every version's `base_text_html` — Claude.
- 2026-10-21: the explanation takes no image (BR1): the Api drops every `img` of the explanation after sanitizing, so an image only the explanation names is never owned; AC7's "any of the four fields" is proved for the statement, an option and the base text, and the explanation proves BR1 — Claude (found at the test phase: the sanitizer is shared by all fields).
- 2026-10-21: tests unlocked twice (owner chose unlock and fix, both times): `QuestionImageApiTests.RowAsync` aliased columns in PascalCase while the EF convention maps snake_case, and `QuestionImageSweepTests` did not sign in again after moving the clock 25 hours. No assertion changed; locked again at the commit after — owner.
- 2026-10-21: plan — 1 upload: table, entity, endpoint, magic bytes → AC1, AC2, AC3 (file), AC4; 2 download with headers and the manage-or-review check → AC5 (Api), AC6; 3 sanitizer `img` and entity rules (alt, count, empty) → AC7, AC11, AC12; 4 ownership on save → AC8, AC10 (Api), AC3 (count); 5 release and versions → AC13, AC14; 6 pending sweep → AC9; 7 Web: route, editor button, dialog, preview, resources → AC5 (Web), AC10, AC15, AC16, AC17; 8 flow pages and data dictionary → techdocs check, DocGen --check — Claude.

## Out of scope
- Images in the explanation.
- Reusing an uploaded image in another question (an image library).
- Resizing or compressing on upload; checking the dimensions.
- SVG and any format outside PNG, JPEG, WebP, GIF.
- Pasting or dropping an image file into the editor.
- A student route to read an image (E-5).
- Images in the transcription of a paper (F-99) and in the import of Simulae questions.

## Open questions
- (none)

## Change notes
- v2 (2026-10-21, owner answered at the build's design round): AC11 gains two error codes for an `alt` over 250 characters, `question.image_alt_too_long` and `base_text.image_alt_too_long` (400); the error-code list under "Screens and API" gains them. Affected criterion: AC11 only, re-approved by the owner's answer. The Api route `GET /api/v1/question-bank/images/{id}` checks "manage or review" in the endpoint through `IPermissionQueryService`, not by a policy.

## Validation script

## Events

- 2026-10-20 question — asked 7, from context 2
- 2026-10-20 override — F-89 Q5 (file sent only at Save) changed to send on choose: the editor needs an address and the HTML would pass the 32 KB message limit
- 2026-10-20 blocked — depends on F-89
- 2026-10-20 blocked — depends on F-89
- 2026-10-21 blocked — test wrong: RowAsync used PascalCase aliases in raw SQL; the EF convention maps snake_case columns

## Delivery
