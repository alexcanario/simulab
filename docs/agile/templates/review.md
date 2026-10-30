# Review: what fails here and what to ignore

The `reviewer` agent reads this file on every `/agile:review`. One line each, at most 30 lines in all. A finding a `## Failures` line matches is at least `major`; one an `## Ignore` line matches is left out of the report and only counted. No line silences a `security` or `locks` blocker, nor a criterion without a test.
The owner writes it, or the retro on the owner's yes: a finding rejected in two items becomes an `## Ignore` line; a real finding seen in two items becomes a project rule (`.claude/rules/agile/project.md`), not a line here.

## Failures
<!-- One line each: what counts as a failure in this project. Example: - A doc in docs/ that contradicts the code. -->

## Ignore
<!-- One line each: what not to report. Example: - Formatting of the scripts in tools/ (no formatter runs there). -->
