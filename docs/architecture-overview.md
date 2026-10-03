# Simulab — architecture overview

Hand-written (F-23). The generated docs (modules, entities, routes) are in [`docs/architecture/`](architecture/README.md).
Dashed = planned, not built yet. Dotted = optional, off by default.

`ArchitectureOverviewTests` keeps the containers diagram in step with the app host
(`src/Hosts/Simulab.AppHost/AppHost.cs`): every resource it starts is a node with the resource name as its id, and a
node in the `planned` class is not a resource yet. When a planned piece is built, move it to `container` or `external`.

## 1. System context

```mermaid
%%{init: {"flowchart": {"nodeSpacing": 24, "rankSpacing": 40}}}%%
flowchart TB
    student["<b>Student</b><br/>[Person]<br/>Practises exams,<br/>follows progress"]
    curator["<b>Curator</b><br/>[Person]<br/>Imports and<br/>reviews content"]
    admin["<b>Admin</b><br/>[Person]<br/>Catalog, users,<br/>roles, plans"]
    simulab["<b>Simulab</b><br/>[Software system]<br/>Practice exams<br/>and a study coach"]
    email["<b>Email provider</b><br/>[SMTP]<br/>Mailpit locally"]
    claude["<b>Claude API</b><br/>[planned]<br/>Extraction, coach"]
    google["<b>Google</b><br/>[optional, off]<br/>Sign-in"]
    student -- "HTTPS" --> simulab
    curator -- "HTTPS" --> simulab
    admin -- "HTTPS" --> simulab
    simulab -- "Emails" --> email
    simulab -. "AI calls" .-> claude
    simulab -. "OAuth" .-> google
    classDef person fill:#08427b,stroke:#052e56,color:#fff
    classDef system fill:#1168bd,stroke:#0b4884,color:#fff
    classDef external fill:#999999,stroke:#6b6b6b,color:#fff
    classDef planned fill:#fff,stroke:#6b6b6b,color:#1f2328,stroke-dasharray:6 4
    classDef optional fill:#999999,stroke:#6b6b6b,color:#fff,stroke-dasharray:2 3
    class student,curator,admin person
    class simulab system
    class email external
    class claude planned
    class google optional
```

## 2. Containers

```mermaid
%%{init: {"flowchart": {"nodeSpacing": 24, "rankSpacing": 40}}}%%
flowchart TB
    people["<b>Student, Curator,<br/>Admin</b><br/>[Person]"]
    subgraph simulab["Simulab"]
        web["<b>Web</b><br/>[Blazor Server,<br/>MudBlazor]<br/>Every screen"]
        redis[("<b>Redis</b><br/>[Cache]<br/>Sessions,<br/>revoked tokens")]
        api["<b>Api</b><br/>[ASP.NET Core]<br/>/api/v1, modules,<br/>job worker"]
        postgres[("<b>PostgreSQL</b><br/>[Database]<br/>One schema<br/>per module")]
        blob[("<b>File storage</b><br/>[Blob, planned]<br/>Azurite locally")]
    end
    mailpit["<b>Email provider</b><br/>[SMTP]<br/>Mailpit locally"]
    claude["<b>Claude API</b><br/>[planned]"]
    people -- "HTTPS" --> web
    web -- "JSON, tokens" --> api
    web -- "Sessions" --> redis
    api -- "Tokens" --> redis
    api -- "EF Core" --> postgres
    api -. "IFileStorage" .-> blob
    api -- "Emails" ---> mailpit
    api -. "IAiGateway" ..-> claude
    classDef person fill:#08427b,stroke:#052e56,color:#fff
    classDef container fill:#438dd5,stroke:#2e6295,color:#fff
    classDef external fill:#999999,stroke:#6b6b6b,color:#fff
    classDef planned fill:#fff,stroke:#6b6b6b,color:#1f2328,stroke-dasharray:6 4
    class people person
    class web,api,postgres,redis container
    class mailpit external
    class claude,blob planned
```

- **Web** — every screen, in pt-BR, pt-PT and en. Talks to the Api only over `/api/v1`; keeps each browser's tokens in Redis (B-3).
- **Api** — the modules (Identity, Catalog), the OpenIddict token endpoint and the job worker that sends the emails (F-13).
- **PostgreSQL** — one database, one schema per module: `identity`, `catalog`, `jobs`.
- **Redis** — refresh-token sessions and the access-token revocation set (F-5), and the Web's server-side sessions (B-3).
- **File storage** (planned) — blob containers behind `IFileStorage`, Azurite locally; added by the first feature that stores files.
- **Claude API** (planned) — reached only through `IAiGateway`, which checks the plan and records usage and cost.
- **In the cloud** (ADR-0002, F-62) — the same containers run on Azure Container Apps, Brazil South, with PostgreSQL Flexible Server, Key Vault and, in production, Azure Cache for Redis (staging keeps Redis as a container). These resources are added only when the app host publishes (`AzureDeployment.cs`), so they are not nodes of this diagram.
- **Google** (optional) — sign-in with Google, off unless the OAuth client is configured (F-20); shown in the context diagram only.

## 3. Inside the Api

The modules and the references between their projects are generated: [`docs/architecture/modules.md`](architecture/modules.md).
