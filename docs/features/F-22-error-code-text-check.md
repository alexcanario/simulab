---
feature: F-22
epic: Foundation and identity
status: done
board: 738
version: 1
---
# Every error code has a text

## Summary
One test that checks, by reflection over every module's `*ErrorCodes` constants, that each code has a text in pt-BR, pt-PT and en. Today each resource test picks code prefixes by hand, so F-14's `role_change.period_invalid` shipped to review with no text in any language (F-14 retro, 2026-09-21).

## What exists
- **One `*ErrorCodes` class in the whole solution**, not one per module: `Simulab.Identity.Contracts/IdentityErrorCodes.cs`, with **55 public string constants**. The summary's "every module's" is a rule for a future that has not arrived — Catalog, ExamEngine and Plans do not exist yet.
- **Two tests check codes, each with its prefixes typed by hand**: `RoleResourcesTests` (`role.`, `role_assignment.`, `user.`, `role_change.`) and `AccountEventResourcesTests` (`account_event.`, added by F-21 two hours ago — the same pattern the F-14 retro complained about, repeated). Between them they cover 4 of the 12 prefixes in use.
- **`ResourceParityTests`** already proves the three languages hold the same keys, for both resource sets (`SharedResources`, `IdentityResources`). So a check over one language plus parity is enough to prove all three; the existing tests check the three anyway.
- **`ErrorText.For(code)`** (`Components/Ui/ErrorText.cs`) is the only place that turns a code into text: the code is the resource key, and a code with no text silently falls back to `common.unexpected_error`. That silent fallback is exactly what this item wants to make loud.
- **Measured now, over all 55 constants against the three `SharedResources` files: 53 have a text in all three languages, 2 have none in any**: `identity.forbidden` and `mfa_required`. Neither is a gap: `mfa_required` is read as a flow signal (`AuthClient.NeedsTotpCode`) and never displayed, and `identity.forbidden` appears nowhere in the Web — an admin page that gets a 403 shows the table's own error state. So the check needs a way to say "this code is never shown", or it fails on day one.
- **The other direction**: 3 resource keys are shaped like a code but match no constant — `common.not_found`, `common.unexpected_error`, `common.validation_failed`. All three are the Web's own generic messages, not API codes.
- No test today reads the `*ErrorCodes` constants by reflection; `RoleResourcesTests` does read them with `GetFields`, filtered by those four prefixes.

## Goal
Make the silent fallback loud: a new API error code without its three texts fails the build instead of reaching a user as "Something went wrong", and a text left behind by a renamed code is found.

## Users and use cases
- UC1 A developer adds an error code to a module and forgets its texts; the suite fails, naming the code and the languages that miss it, before the change is reviewed.
- UC2 A developer renames or deletes a code and leaves its old text behind; the suite fails, naming the text that matches no code.
- UC3 A developer adds a code the app never shows a person (a flow signal, a status the UI turns into a page); they add it to the exempt list with the reason, and the suite passes.
- UC4 A new module arrives with its own `*ErrorCodes` class; it is covered with no change to the test.

## Business rules
- BR1 The codes checked are every public `const string` of every static class whose name ends in `ErrorCodes`, in every `*.Contracts` assembly of the solution. Nothing is listed by hand, and a class added later is found by the same rule.
- BR2 Every such code has a text in `SharedResources` for en, pt-BR and pt-PT — the resource set `ErrorText.For` reads, where the code is the key. A missing text fails, naming the code and each language that misses it.
- BR3 A code the app never shows a person is exempt, and is listed in the test with the reason it is never shown. Today: `identity.forbidden` (no page reads it; a 403 on an admin list shows the table's error state) and `mfa_required` (read as a flow signal by `AuthClient.NeedsTotpCode`). The list is the only way to be exempt; an empty reason is not allowed.
- BR4 The other direction: a key in `SharedResources` shaped like a code — lowercase, with a dot, matching the code shape — that matches no constant fails, naming the key. Keys under `common.` are the Web's own messages, not API codes, and are out of it.
- BR5 The check itself is tested: given a code with no text it fails, given an exempt code it passes, given an orphan key it fails. A guard nobody has seen fail is not a guard.
- BR6 The test names what to do in its failure message: the file to edit (`SharedResources*.resx`) and the exempt list as the alternative.

## Screens and API
No screen, no route, no new error code: this item is a test. It reads `src/Hosts/Simulab.Web/Resources/SharedResources*.resx` and the `*.Contracts` assemblies.

## Acceptance criteria
- AC1 Given the solution as it stands, when the suite runs, then the check passes: 55 codes, 53 with texts and 2 exempt with their reason. (BR1, BR2, BR3)
- AC2 Given a code with no text in one language only, when the check runs, then it fails naming that code and that language. (BR2, BR5)
- AC3 Given a code with no text in any language and not on the exempt list, when the check runs, then it fails naming the code and the three languages. (BR2, BR5)
- AC4 Given a code on the exempt list, when the check runs, then it passes; and an entry with no reason fails. (BR3, BR5)
- AC5 Given a resource key shaped like a code that matches no constant, when the check runs, then it fails naming the key; a key under `common.` never fails. (BR4, BR5)
- AC6 Given a second static class ending in `ErrorCodes`, in any `*.Contracts` assembly, when the check runs, then its codes are checked too, with no change to the test. (BR1, UC4)
- AC7 Given the failure message of any of these, then it names the resource file to edit and the exempt list as the alternative. (BR6)
- AC8 Given `RoleResourcesTests` and `AccountEventResourcesTests`, then neither still walks the error codes by prefix, and both still cover what only they cover: permission and system role names, and the event, method and reason labels. (decision 5)
- AC9 This item adds no UI text, so there is nothing new to translate; the check it adds is what proves every error text exists in pt-BR, pt-PT and en.

## Decisions
- 2026-09-23 — The codes come from reflection over every `*ErrorCodes` class in the `*.Contracts` assemblies, not from a name typed in the test — owner, question 1 — the defect this item fixes is a list kept by hand, so a new module must be covered without anyone remembering.
- 2026-09-23 — A code that is never shown is exempt through a list in the test, each entry with its reason — owner, question 2 — growing the list goes through the test's own review, and no text is written for something nobody reads. The two of today are `identity.forbidden` and `mfa_required`.
- 2026-09-23 — The check also runs the other way: a code-shaped key with no constant fails, except under `common.` — owner, question 3 — it catches the text left behind by a rename; `common.*` is the Web's own namespace (3 keys today).
- 2026-09-23 — The test lives in `Simulab.Web.Tests`, next to `ResourceParityTests` — owner, question 4 — that is where `SharedResources` and its localizer are already wired.
- 2026-09-23 — `RoleResourcesTests` and `AccountEventResourcesTests` lose their error-code sweep and keep the rest — owner, question 5 — the duplication is exactly the hand-kept list this item removes.
- 2026-09-23 — No new packages, for code or tests — owner, question 6.
- 2026-09-23 — Claude: the check is a plain class over inputs (the codes, the exempt list, the resource keys) so BR5 can test it with made-up data; the test that runs it over the real solution is one more case. Reading the resource keys uses `ResourceManager` per culture, as `ResourceParityTests` does, not the `.resx` XML.
- 2026-09-23 — Approved by the owner ("aprovo f-22").
- 2026-09-23 — Claude: `IdentityResources` (the email texts) is out of it — `ErrorText` reads `SharedResources`, and a code is never looked up anywhere else.

## Out of scope
- Writing the missing texts for `identity.forbidden` and `mfa_required`: they are exempt, not gaps.
- Checking the quality or the wording of a text; only that it exists.
- Checking resource keys that are not error codes (labels, titles, messages): `ResourceParityTests` covers their parity, and the two resource tests cover the named sets they own.
- The email resource set (`IdentityResources`).
- An analyzer that fails the build in the IDE instead of the suite.

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
This item has no screen: what you are validating is that the guard catches what it promises. Every step runs from
`D:\dev\_icontrol\wt\simulab\f-22`; the command is the same in Git Bash and in PowerShell 7, and both were run here.

1. The check is green as it stands: `dotnet test tests/Hosts/Simulab.Web.Tests/Simulab.Web.Tests.csproj --filter "FullyQualifiedName~Localization"` → `Passed! - Failed: 0, Passed: 23`.
2. Take a text away: delete the line `<data name="account_event.period_invalid" ...>` from `src/Hosts/Simulab.Web/Resources/SharedResources.pt-PT.resx` and run step 1 again → it fails, naming the code, the language (`has no text in pt-PT`), the file to edit and the exempt list. Put the line back and run step 1 → green.
3. Add a code with no text: in `src/Modules/Identity/Simulab.Identity.Contracts/IdentityErrorCodes.cs`, add `public const string TryMe = "try_me.no_text";` and run step 1 → it fails naming `try_me.no_text` in the three languages. Remove the line and run step 1 → green.
4. Leave a text behind: add `<data name="role.renamed_away" xml:space="preserve"><value>x</value></data>` before `</root>` in the three `SharedResources*.resx` files and run step 1 → it fails, listing `role.renamed_away` once per language, each saying the text answers to no error code. Remove the three lines and run step 1 → green.
5. Read the exempt list at the top of `tests/Hosts/Simulab.Web.Tests/Localization/ErrorCodeTextTests.cs`: two codes, each with the reason the app never shows it. Decide whether you agree with both reasons — that list is the only way out of the rule.
6. Nothing else changed: `dotnet test tests/Hosts/Simulab.Web.Tests/Simulab.Web.Tests.csproj` → `Passed! - Failed: 0, Passed: 469`.

## Delivery
- Branch: `feature/F-22` (merged with `--no-ff` into `main`, AB#738, and deleted; it was never pushed, so there was no remote branch to delete)
- Merge: `e1df7eb`
- Tests: 923, 50 s (full suite, architecture tests included); full build 13 s, 0 warnings, baseline still empty
- Manual pages: none — the item changes no visible behavior; only `docs/infra.md` (measured times) was touched
- Validated by the owner on 2026-09-23.

## Coverage
| Criterion | Test(s) |
|---|---|
| AC1 | `ErrorCodeTextTests.EveryErrorCode_HasATextInEveryLanguage_AndEveryCodeShapedTextAnswersToACode` (the real solution); `ErrorCodeTextCheckTests.EveryCodeWithItsTexts_Passes` |
| AC2 | `ErrorCodeTextCheckTests.CodeMissingInOneCultureOnly_FailsNamingThatCulture` |
| AC3 | `ErrorCodeTextCheckTests.CodeMissingEverywhereAndNotExempt_FailsNamingTheThreeCultures` |
| AC4 | `ErrorCodeTextCheckTests.ExemptCode_Passes_AndAnExemptionWithNoReason_Fails`, `ExemptionForACodeThatNoLongerExists_Fails`; `ErrorCodeTextTests.EveryExemption_NamesACodeAndGivesAReason` |
| AC5 | `ErrorCodeTextCheckTests.OrphanText_Fails_AndTheWebsOwnPrefix_DoesNot`, `KeyThatIsNotCodeShaped_IsNotAnOrphan`, `CodeShape_IsSnakeCaseWordsSeparatedByDots` |
| AC6 | `ErrorCodeTextTests.TheScan_FindsTheCodesOfEveryContractsAssembly` (scans `Simulab.*.Contracts.dll`, asserts it found `IdentityErrorCodes` and its codes — the rule of presence, so an empty scan fails) |
| AC7 | `ErrorCodeTextCheckTests.MissingTextFailure_NamesTheResourceFileAndTheExemptList` |
| AC8 | `NamedKeyTestsOwnNoErrorCodeTests.NoHandKeptList_HoldsAnErrorCode`, `EveryHandKeptList_StillCoversItsOwnKeys`; `RoleResourcesTests.EveryPermissionSystemRoleAndActionName_HasAText` and `AccountEventResourcesTests.EveryEventMethodAndReasonLabel_HasAText` still green |
| AC9 | No new UI text in this item; `ErrorCodeTextTests.TheTexts_AreReadForTheThreeLanguages` pins that the check reads all three, and `ResourceParityTests` still covers parity |
