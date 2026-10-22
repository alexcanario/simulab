# Definition of done

An item is `done` only when every line is true. "Almost" is `validating`.

- Every acceptance criterion maps to at least one test in `## Criterion → test`, every row filled at ship; a criterion checked only on screen is in the validation script.
- A bug has a regression test that was seen failing before the fix.
- The build has no new warnings (the gate compares with the committed baseline). Accepting a new warning needs the owner's yes.
- No open critical or high security finding: `agile scan GREEN`, or accepted by the owner, or closed as a false positive by Claude with evidence (ship report).
- Affected tests are green at the end of every turn; the full suite and the architecture tests are green before merge.
- Test time is inside the profile budget. An overrun is reported and becomes a retro finding.
- Every UI text exists in each locale of `UI locales:` and the missing-key test is green.
- Every `paths:`-scoped rule whose files the diff touches holds (API contracts, UI, i18n, build config).
- The owner followed the validation script and said it passed, in this conversation.
- The item file reflects what was built: decisions, change notes, `## Delivery`.
- The app manual is updated in the three languages when visible behavior changed; the generated docs (`docs/architecture/`) pass `DocGen --check`; each page of the item's `## Docs` exists and is indexed (`techdocs.js check`).
- The merge gate passed and was verified, and the board item is closed with evidence.
- Nothing is left uncommitted in any worktree.
- The retro ran: at most three lessons, each one proposed as a rule, a skill change or nothing.
