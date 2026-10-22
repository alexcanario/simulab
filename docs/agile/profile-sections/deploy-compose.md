An environment whose Deploy command in `docs/infra.md` is not an `aspire deploy` line runs the app's own container images and a compose file kept in the repository, on a host that runs Docker (#151). The AppHost stays a local development tool (`aspire run`): this recipe never references it and changes nothing in it, and an environment's recipe changes only by its Deploy command, never by a flag. Azure Container Apps without Aspire is not a recipe. For the profiles `/agile:publish` supports, `/agile:publish <environment>` runs the line like any declared command (`microservices` is not one of them yet: these lines only prepare its compose file); it runs no Aspire version check, and a secret of "Expected secrets" is the variable named in its row, as is, with no `Parameters__` prefix (measured on .NET SDK 10.0.401 and Docker 29.8.2: two deploys of two released tags, the same database, the same signed-in session):
- The Deploy command is one portable line, valid in cmd.exe and in /bin/sh: `dotnet publish <project> -c Release -t:PublishContainer` for each deployable web or API project, joined by `&&`, then `docker compose -f deploy/compose.yaml --env-file deploy/<environment>.env up -d --wait`. It uses no shell variable syntax: compose reads `AGILE_ENVIRONMENT` and `AGILE_VERSION` from the environment `/agile:publish` sets. In Git Bash the switch is written `-t:` (`/t:` is rewritten as a path). `--env-file` is required (without it compose reports `ASPNETCORE_ENVIRONMENT` and `HOST_PORT` missing), and `--wait` returns when the containers run, since a service without a healthcheck counts as healthy: the Check URL of the environment is what proves the app answers.
- Each deployable project sets `<ContainerRepository>` (its name, lowercase) and `<ContainerImageTag>$(Version)</ContainerImageTag>` in its `.csproj`: without the tag the SDK publishes `latest`, not the version of `Directory.Build.props`. `PublishContainer` is part of the .NET SDK, so no package is added; the image goes to the Docker daemon the machine's `docker` CLI points at, and a deploy of a version that already exists builds it again and moves its tag.
- One `deploy/compose.yaml` serves every environment, with `name: <app>-${AGILE_ENVIRONMENT:?}` (staging and production on one host never replace each other, each with its own containers and volumes), the database service pinned to a major version (`postgres:17`, with `POSTGRES_DB`, `pg_isready` as its healthcheck and `depends_on: condition: service_healthy` on each service that uses it; SQL Server `mcr.microsoft.com/mssql/server:2022-latest` with `MSSQL_SA_PASSWORD` and `ACCEPT_EULA: "Y"`), the version the app's AppHost runs locally, with a named data volume and no published port, and each web or API service on `image: <repository>:${AGILE_VERSION:?}` (`<repository>` exactly as the project's `ContainerRepository`) with its connection string in `ConnectionStrings__<name>` (the name the app reads, `appdb`).
- `deploy/<environment>.env`, committed, holds only what is not secret: `ASPNETCORE_ENVIRONMENT` in the environment's case (`Staging`: `appsettings.Staging.json` is read case-sensitively on Linux, else the app logs `Production`) and `HOST_PORT`, the port of the URL in `docs/infra.md`, the only published port (`"${HOST_PORT:?}:8080"`, the port the .NET images listen on).
- The database password, and every other secret, is a variable listed in "Expected secrets" of `docs/infra.md` as an environment variable (`DB_PASSWORD`), referenced as `${DB_PASSWORD:?DB_PASSWORD is not set}` in the compose file: the same value at every deploy, random, 22 or more letters and digits (a `$` or a quote breaks the connection string), generated and kept by the owner, never written to a file of the repository or of the host. `/agile:publish` stops before anything runs when it is unset; run by hand, `dotnet publish` builds the images first and compose then refuses before starting anything with `required variable DB_PASSWORD is missing a value: DB_PASSWORD is not set`. The image applies the password only when its data is created: to change it, run `ALTER USER` (`ALTER LOGIN` on SQL Server) signed in with the old one, then change the variable.
- Each web or API service sets `DataProtection__KeysPath: /home/app/keys` and mounts a named keys volume at `/home/app` (another folder is not writable by the `app` user), declared on the service and at the top level, and `ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"` when a proxy sits in front of it; the app calls `AddDataProtection().PersistKeysToFileSystem(...).SetApplicationName("<App>")` only when `DataProtection:KeysPath` is set, as in the Aspire recipe, so a redeploy keeps cookies and antiforgery tokens valid and local development is unchanged; the keys sit unencrypted on the volume (the log warns `No XML encryptor configured`). `docker compose down -v` is never run: it deletes the keys volume and the data.
- Telemetry: the compose file sets no OpenTelemetry endpoint and runs no dashboard; logs go to the console (`docker compose -f deploy/compose.yaml logs <service>`). A project that wants export sets `OTEL_EXPORTER_OTLP_ENDPOINT` in `deploy/<environment>.env` (its headers are a secret: "Expected secrets").
- An environment that already ran the Aspire compose recipe starts on new volumes when it switches (the compose project name differs): its data and signed-in sessions do not carry over, and moving them is by hand; no plugin command does it.
```yaml
# deploy/compose.yaml (a web app and PostgreSQL; the API of web-api has the same shape)
name: app-${AGILE_ENVIRONMENT:?}
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_DB: appdb
      POSTGRES_PASSWORD: ${DB_PASSWORD:?DB_PASSWORD is not set}
    volumes:
      - data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d appdb"]
      interval: 5s
      timeout: 3s
      retries: 20
  web:
    image: app-web:${AGILE_VERSION:?}
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:?}
      ConnectionStrings__appdb: Host=postgres;Database=appdb;Username=postgres;Password=${DB_PASSWORD:?DB_PASSWORD is not set}
      DataProtection__KeysPath: /home/app/keys
    ports:
      - "${HOST_PORT:?}:8080"
    volumes:
      - keys:/home/app
volumes:
  data:
  keys:
```
