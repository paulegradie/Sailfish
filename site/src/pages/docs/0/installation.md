---
title: Installation
---

Install Sailfish packages from NuGet to get started with performance testing in your .NET applications.

## 📦 Available Packages

{% feature-grid columns=3 %}
{% feature-card title="Sailfish" description="Core library for performance testing and statistical analysis." /%}

{% feature-card title="Sailfish.TestAdapter" description="Test adapter for IDE integration and test runner support." /%}

{% feature-card title="Sailfish.Analyzers" description="Code analyzers to help write better performance tests." /%}
{% /feature-grid %}

## ⚡ Package Installation

{% success-callout title="Quick Start" %}
For most users, installing both `Sailfish` and `Sailfish.TestAdapter` packages will provide the best experience with full IDE integration.
{% /success-callout %}

### 🖥️ Using .NET CLI

**Core Library** - Install the main Sailfish package:
```bash
dotnet add package Sailfish
```

**Test Adapter (Recommended)** - For IDE integration and test runner support:
```bash
dotnet add package Sailfish.TestAdapter
```

{% tip-callout title="IDE Integration" %}
The Test Adapter allows you to run Sailfish tests directly from Visual Studio, VS Code, or Rider test explorers.
{% /tip-callout %}

**Code Analyzers (Optional)** - For enhanced development experience:
```bash
dotnet add package Sailfish.Analyzers
```

### 📋 Using Package Manager Console

```powershell
# Core library
Install-Package Sailfish

# Test adapter
Install-Package Sailfish.TestAdapter

# Code analyzers
Install-Package Sailfish.Analyzers
```

### 📄 Using PackageReference

{% code-callout title="Project File Integration" %}
Add these references directly to your `.csproj` file for version control and reproducible builds.
{% /code-callout %}

```xml
<PackageReference Include="Sailfish" Version="*" />
<PackageReference Include="Sailfish.TestAdapter" Version="*" />
<PackageReference Include="Sailfish.Analyzers" Version="*" />
```

## 📊 Package Status

![GitHub Workflow Status (with branch)](https://img.shields.io/github/actions/workflow/status/paulegradie/sailfish/publish.yml)
![Nuget](https://img.shields.io/nuget/dt/Sailfish)

{% info-callout title="Package Links" %}
- [Sailfish](https://www.nuget.org/packages/Sailfish/) - Core performance testing library
- [Sailfish.TestAdapter](https://www.nuget.org/packages/Sailfish.TestAdapter/) - IDE and test runner integration
- [Sailfish.Analyzers](https://www.nuget.org/packages/Sailfish.Analyzers/) - Code analysis and suggestions
{% /info-callout %}

## 🚀 Next Steps

{% note-callout title="Ready to Start Testing?" %}
After installation, check out our [Quick Start Guide](/docs/0/quick-start) to write your first performance test, or dive into [Getting Started](/docs/0/getting-started) for a more comprehensive introduction.
{% /note-callout %}
