---
title: Getting Started
---

There are two ways to hold Sailfish:

- Test Project
- Library (that may or may not depend on a test project)

# Test Projet

When you install the [Sailfish TestAdapter](https://www.nuget.org/packages/Sailfish.TestAdapter) nuget package, you can write and run Sailfish tests directly from the IDE as if it were a test project using xUnit or NUnit. This is a great way to get started with Sailfish and get it integrated into your workflow.

# Library

If you don't need a test project, then your application can install the [Sailfish](https://www.nuget.org/packages/Sailfish) nuget package. The library exposes various tools as well as an entry point into the Sailfish execution program. This entry point requires an IRunSettings, which can be created using the [RunSettingsBuilder](https://github.com/paulegradie/Sailfish/blob/main/source/Sailfish/RunSettingsBuilder.cs).

You can optionally place your tests in a separate project, which installs the test adapter. When your application's main project depends on the test project, you get access to the Sailfish library.
