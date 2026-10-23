---
paths:
  - "tests/Hosts/Simulab.Web.Tests/**"
---
# Project rules

- BunitContext.DefaultWaitTimeout is static (bUnit 2.10.3): a test that changes it joins the SerialDebounceWait collection and restores the value in finally; never set it from a test that runs in parallel. (B-31, 2026-10-20)
- In QuestionForm tests, `.app-form-card` also wraps the history cards: count the form sections with `.app-form-layout .app-form-card section.app-section-card` (F-126 counted 7 and a locked test had to be unlocked). (F-126, 2026-10-23)
