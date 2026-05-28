---
title: Installation
---

Sailfish targets **.NET 9** and **.NET 10**. Older runtimes are not supported.

Packages are available on NuGet:

 - [Sailfish](https://www.nuget.org/packages/Sailfish/) — the core runner, used when you embed Sailfish in a console app or library.
 - [Sailfish.TestAdapter](https://www.nuget.org/packages/Sailfish.TestAdapter/) — the VSTest adapter; install this in a test project so Visual Studio / Rider Test Explorer discovers `[Sailfish]` classes.
 - [Sailfish.Analyzers](https://www.nuget.org/packages/Sailfish.Analyzers/) — Roslyn analyzers (e.g. SF1001/SF1002/SF1003) that catch dead-code-elimination hazards. Pulled in transitively by the core package.

```bash
# Class library / programmatic runner
dotnet add package Sailfish

# Test project discovered by VSTest
dotnet add package Sailfish.TestAdapter
```
