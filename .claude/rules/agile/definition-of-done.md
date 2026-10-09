# Definition of done

An item is `done` only when every line is true. "Almost" is `validating`.

- Every acceptance criterion maps to at least one test, kept in the item's `## Criterion → test`, every row filled at ship. A criterion checked only on screen is in the validation script.
- A bug has a regression test that was seen failing before the fix.
- The build has no new warnings (the gate compares with the committed baseline). Accepting a new warning needs the owner's yes.
- No open critical or high security finding: the scan (`scan.js`) ends `agile scan GREEN`, or the owner accepted each finding with a reason in `docs/security/findings.md`.
- Affected tests are green at the end of every turn; the full suite and the architecture tests are green before merge.
- Test time is inside the profile budget. An overrun is reported and becomes a retro finding.
- Every UI text exists in pt-BR, pt-PT and en, and the missing-key test is green.
- API changes follow the API contract rules: `/api/v1/`, stable error codes, nullable optional ids, shared JSON options, OpenAPI document returns 200.
- Schema changes have a migration, and unique indexes over nullable tenant columns are `NULLS NOT DISTINCT`.
- Permissions are checked by the endpoint and by the screen (menu, page and action), by permission and not by role name.
- A screen that calls the API was opened through the app host by Claude before handing over the validation script.
- The owner followed the validation script and said it passed, in this conversation.
- The item file reflects what was built: decisions, change notes, `## Delivery`.
- The app manual is updated in the three languages when visible behavior changed; the generated technical docs (`docs/architecture/`) pass `DocGen --check` when the project has them.
- The merge was authorized by the owner (typing `/agile:ship <id>`; in `/agile:autopilot`, possibly in the validation message naming the merge), verified (`0 0`, branch and worktree gone) and the board item is closed with evidence.
- Nothing is left uncommitted in any worktree.
- The retro ran: at most three lessons, each one proposed as a rule, a skill change or nothing.
