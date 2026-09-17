---
paths:
  - "**/*.cs"
  - "**/*.razor"
---
# API contracts

- Every route lives under `/api/v<major>/`, starting at `/api/v1/`. The client builds URLs from one base-address setting, never from a hardcoded scheme, host or port.
- Routes are lowercase kebab-case plural nouns (`/api/v1/catalog/exam-boards/{id}`). An action that is not CRUD is `POST .../{id}/<verb>`.
- Inside a version only add optional fields. A breaking change is a new version.
- Errors are RFC 9457 problem details plus a stable `code`: `<entity>.<reason>` in snake_case (`exam_board.acronym_taken`).
- Error codes are constants, in one place per module. A shipped code is never renamed or reused.
- Clients branch on `code`, never on the message text. The UI localizes by code (resource key = code).
- Status: 400 validation (one entry per field, each with a code), 401, 403, 404, 409 conflict or concurrency, 422 business rule.
- Requests and responses are records in the contracts folder. Never expose entities or EF types.
- Optional ids are `Guid? Id = null`, never `Guid Id = default`: a struct default cannot be written to OpenAPI and the whole document returns 500.
- The same applies to every struct (`DateOnly`, `TimeSpan`, enums): nullable with `= null`.
- One shared `JsonSerializerOptions` instance (`AppJson.Options`) is used by the API, by every `HttpClient` call and by the tests.
- Never write `new JsonSerializerOptions` elsewhere, and never call `ReadFromJsonAsync` / `PostAsJsonAsync` without passing the shared instance.
- Enums travel as strings. Instants are ISO 8601 UTC (`DateTimeOffset`); calendar dates are `DateOnly`.
- Polymorphic payloads use a `type` discriminator registered in the shared options.
- Lists are paged: `page` and `pageSize` (cap 100) in, `{ items, total }` out.
- Endpoints authorize by permission or policy, never by role name.
- Global data in a tenant table (`TenantId` null): the unique index includes `TenantId` and is `NULLS NOT DISTINCT` (PostgreSQL 15+; Npgsql `.AreNullsDistinct(false)`). Without it duplicates pass.
- Every tenant-scoped endpoint has a test proving tenant A cannot read or change tenant B's data.
- An integration test fetches `/openapi/v1.json` and expects 200.
- Each feature has at least one test through the real HTTP pipeline (`WebApplicationFactory`) using the shared JSON options.
- A screen that calls the API is smoke-tested through the app host before the validation script is handed over: green tests do not prove the Web → API wiring.
