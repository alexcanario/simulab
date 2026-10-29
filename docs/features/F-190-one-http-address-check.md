---
feature: F-190
epic: Data type controls
status: building
board: 260
version: 1
---
# One http(s) address check for the URL fields

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-29)
- Stopped at: Approved by autopilot --assume (2026-10-29).
- Next action: Build it (autopilot, lane nanda).
- Waiting on: nothing

## Summary
The rule "only `http` and `https`, with a host" now lives in four places: `AppUrlField.IsValid` (the kit) and the private checks of `ExamEditionForm.razor` (around line 708), `IssuingAuthorityDialog.razor` (around line 108) and `OrganizerDialog.razor` (around line 147). Have the three pages call `AppUrlField.IsValid` and keep their own module error codes, so the rule has one copy. The review estimated about 9 lines less. Seen in F-183 review #7; the pages and the control behave the same today, so this is a cleanup with no visible change.

## Start
- Depends on: nothing (F-183 is done).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (lane assignment of the lanes run; the change is small).
- Suggested path: `/agile:autopilot F-190` (small and clear).
- Parallel with: F-189.

## Goal
The rule for a valid web address has one copy, in the kit control, so a change to it (for example a new scheme) is made once and the pages cannot drift from the control.

## Users and use cases
- UC1 An administrator types a website or notice address on the exam edition form, the issuing authority dialog or the organizer dialog and sees the same result as today: a valid address is kept, any other is refused with the module's own message.

## Business rules
- BR1 A web address is valid when it is absolute, its scheme is `http` or `https` and it has a host. The only copy of this rule is `AppUrlField.IsValid`.
- BR2 The three pages call `AppUrlField.IsValid` and keep their own module error codes (`ExamEditionNoticeUrlInvalid`, `IssuingAuthorityWebsiteInvalid`, `OrganizerWebsiteInvalid`) and the exam edition length check (`ExamEditionNoticeUrlMaxLength`).
- BR3 No visible change: for every address the pages accepted or refused before, they accept or refuse it now, with the same message. The Api and the Domain checks (`ExamEdition`, `IssuingAuthority`, `Organizer`) are not touched; the Domain cannot reach the Web project.

## Screens and API
- No new route, endpoint, screen or error code. Three private methods lose their copy of the scheme test: `NoticeUrlError` (`ExamEditionForm.razor`), `WebsiteError` (`IssuingAuthorityDialog.razor`, `OrganizerDialog.razor`).

## Risks
- A page accepts or refuses an address differently after the change — `IsValid` adds only "host not empty", and an absolute `http`/`https` address that `Uri` parses always has a host — proven by the page tests of AC1 to AC3 — low impact — two-way door.

## Docs
- none — internal cleanup with no visible change; `docs/flows/text-kind-controls.md` already describes the rule of `AppUrlField`.

## Acceptance criteria
- AC1 Given the exam edition form, when the notice address is `ftp://x.gov.br`, `mailto:a@b.c` or too long, then the form shows `ExamEditionNoticeUrlInvalid` as before, and a valid `https://x.gov.br/edital` or an empty address shows no error.
- AC2 Given the issuing authority dialog, when the website is `ftp://x.gov.br`, then the dialog shows `IssuingAuthorityWebsiteInvalid` as before, and a valid or empty website shows no error.
- AC3 Given the organizer dialog, when the website is `ftp://x.gov.br`, then the dialog shows `OrganizerWebsiteInvalid` as before, and a valid or empty website shows no error.
- AC4 Given the Catalog pages, when their sources are scanned, then none of them tests `Uri.UriSchemeHttp` or `Uri.UriSchemeHttps` itself; only `AppUrlField.razor` does.
- AC5 No new text is added, so the locale files do not change and the missing-key test stays green.

## Criterion → test
| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC2 | pending |
| AC3 | pending |
| AC4 | pending |
| AC5 | pending |

## Decisions
- 2026-10-29 — Scope is the three pages only; the Domain and Api checks stay — the Domain cannot reference the Web project and a shared copy there is another item — assumed: scope (CLAUDE.md, modular-monolith profile)
- 2026-10-29 — The exam edition form keeps its length check before calling `IsValid` — it is a separate rule (`ExamEditionNoticeUrlMaxLength`) — assumed: scope
- 2026-10-29 — AC4 is a source-scan test in `Simulab.Web.Tests`, so the single copy cannot come back — the cheapest way to hold "one copy" — assumed: tests
- 2026-10-29: plan — 1 `WebAddressRuleTests` red on the three pages → run it; 2 the three pages call `AppUrlField.IsValid` → the new test and the Catalog page tests green; 3 gate, coverage, validation script → `agile gate GREEN`
- 2026-10-29 — No question reached the owner: no package, permission, schema, money or screen change — assumed by autopilot (`--assume`) as lane nanda of the lanes run

## Out of scope
- The Api and Domain copies of the address check.
- Any new scheme or a stricter address rule.

## Open questions
- (none)

## Change notes

## Events

- 2026-10-29 assumed — 4 answers
