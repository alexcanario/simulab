---
paths:
  - "src/Hosts/Simulab.Web/Components/**"
---
# Project rules

- A component that wraps the router (Routes.razor) resolves WebSessionTokenAccessor, and anything behind Redis, lazily and only for a signed-in user: the Web host tests render Home with no Redis and answered 500 when the gate injected it eagerly. (F-85, 2026-10-21)
- AppSectionCard gives its Id only to the title heading (`{Id}-title`); the section itself has no id, so a bUnit test that checks a card by `#<Id>` passes whatever is drawn: assert on `#<Id>-title` (F-97, F-104 and F-106 each hit it). (F-106, 2026-10-21)
