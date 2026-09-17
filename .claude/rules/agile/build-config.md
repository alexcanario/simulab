---
paths:
  - "**/*.csproj"
  - "**/*.props"
  - "**/*.targets"
  - "**/.editorconfig"
  - "**/BannedSymbols.txt"
  - "**/global.json"
---
# Build configuration

- Every solution, in every profile, has at its root: `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `BannedSymbols.txt` and `global.json`; `tests/Directory.Build.props` holds test-only settings.
- A `.csproj` holds only what is specific to that project: SDK, references, and properties that differ from the shared file. Never repeat `TargetFramework`, `Nullable` or `ImplicitUsings` there.
- Central Package Management: a `PackageReference` never has `Version=`. Add the version as `PackageVersion` in `Directory.Packages.props`, at the latest stable release, checked at the time of adding.
- A new package needs the owner's yes. Check its license first: FluentAssertions 8+ and MassTransit 9+ are commercial and not allowed.
- Rules the build can check belong in the build, not in prose: style and naming in `.editorconfig` (severity `warning`), forbidden APIs in `BannedSymbols.txt`. The Stop gate fails on new warnings.
- The `.editorconfig` is the owner's. Do not change a severity, add a `NoWarn` or a `#pragma warning disable` to make a warning go away: fix the code, or ask.
- A justified suppression is local (one `#pragma` around one line, with a comment saying why), never project-wide. `AppJson` is the one place allowed to construct `JsonSerializerOptions`.
- `TreatWarningsAsErrors` stays off. The gate, not the compiler, decides what fails, so inherited code still builds.
- When a retro lesson can be checked mechanically, add it to `.editorconfig` or `BannedSymbols.txt` before adding a written rule.
- A change to a `.props`, `.targets` or solution file rebuilds the whole solution at the end of the turn; say so before making it.
