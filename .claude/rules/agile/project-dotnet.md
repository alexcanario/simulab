---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.props"
  - "**/*.targets"
  - "**/*.razor"
  - "**/*.cshtml"
  - "**/*.xaml"
---
# Project rules

- Run dotnet build on the project that holds the migrations before dotnet ef migrations add or remove, and never use --no-build with remove: against a stale assembly remove deleted the previous item's migration (restored with git checkout), so check git status for a D line after each ef command (F-108, 2026-10-24)
- Before test-lock.js lock, build the test projects with dotnet build --no-incremental and clear every warning (IDE0062 static local function, CA1305 culture on Parse): an incremental build hides them, and a locked file needs the owner's unlock to fix them (F-109). (F-109, 2026-10-25)
