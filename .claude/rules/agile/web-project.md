---
paths:
  - "src/Hosts/Simulab.Web/Components/**"
---
# Project rules

- A component that wraps the router (Routes.razor) resolves WebSessionTokenAccessor, and anything behind Redis, lazily and only for a signed-in user: the Web host tests render Home with no Redis and answered 500 when the gate injected it eagerly. (F-85, 2026-10-21)
