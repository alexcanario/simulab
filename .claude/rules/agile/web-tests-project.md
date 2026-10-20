---
paths:
  - "tests/Hosts/Simulab.Web.Tests/**"
---
# Project rules

- BunitContext.DefaultWaitTimeout is static (bUnit 2.10.3): a test that changes it joins the SerialDebounceWait collection and restores the value in finally; never set it from a test that runs in parallel. (B-31, 2026-10-20)
