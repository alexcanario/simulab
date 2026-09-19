---
feature: F-13
epic: Foundation and identity
status: idea
board: 723
version: 1
---
# Identity emails through the job queue

## Summary
Send the identity emails (verification, password reset, password changed) through the job table run by the worker in the `Api` host, instead of inside the request. The anonymous endpoints would then answer in the same time whether an account exists or not (today only real accounts wait for SMTP), and a slow mail server would not slow a page. Found by `/agile:review` on F-7 (2026-09-19); needs the job table, which does not exist yet.
