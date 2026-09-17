# Git

- The main branch is the one named in `CLAUDE.md` (default `main`). Never edit it directly: every item has its branch, `feature/F-<n>` or `bug/B-<n>`.
- Never push, merge or rebase onto the main branch without the owner's explicit authorization in the current conversation.
- Never force-push, reset a shared branch, delete a branch or skip hooks (`--no-verify`) without asking.
- Commits are Conventional Commits in English with the item id: `feat(F-3): add exam board list`.
- Commit in small steps on the item branch. Nothing stays only on disk: before a pause, commit with `wip(F-<n>): ...`.
- A commit message says what the diff really does. Check `git diff --staged` before committing.
- Start of session: `git status` and `git log` on the main checkout and on **every** worktree (`git worktree list`). Report uncommitted work before anything else.
- If git and the board or the files disagree, stop and ask.
- Use a worktree only when the owner asks for parallel work. One worktree per agent; two writers never share one (the index races).
- Worktrees live in a short sibling folder named `<type>-<n>` (e.g. `D:\wt\<repo>\feature-3`). Never inside the repository.
- Before merging or removing a worktree, ask the owner to close any IDE or app host running from it.
- Bring the item branch up to date with the main branch before the full suite, not after.
- Merge with `--no-ff` and a message that references the item and the board id.
- After a merge verify: `git rev-list --left-right --count main...origin/main` shows `0 0`, the branch is gone locally and remotely, the worktree folder is gone.
- A merge commit with an empty diff is a defect. Check `git show --stat` after merging.
- Never commit secrets, connection strings with passwords, or local settings. Use user secrets or environment variables.
- `.claude/agile/warnings-baseline.json` is committed; build output, `bin/`, `obj/` and IDE folders are not.
- Generated files (migrations, snapshots, OpenAPI documents) are committed with the change that produced them.
