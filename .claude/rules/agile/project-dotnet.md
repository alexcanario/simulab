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
