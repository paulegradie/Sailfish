---
title: Getting Started
---

Sailfish is designed to be a user friendly and familiar framework for writing performance style tests in C#.

If you are familiar with BenchmarkDotNet, you will find Sailfish very familiar.

There are a couple different intended ways to use Sailfish:

- Writing local performance tests
- Writing performance tests for execution in a production performance regression monitoring system

# Local Performance Tests

These are the sorts of you tests you might write to collect data before and after a set of changes for sumbmissions with a pull/merge request.

You can imagine a scenario where you write a few tests, execute them against your main/master branch, and then switch to your development branch and run them again.

In this scenario, you will produce a before and after result that can be pasted into a pull/merge request description and shared with your team.

# Production Performance Regression monitoring system

In this scenario, you might have dynamic or statically provisioned infrastructure and code against which you'll run performance tests.

Sailfish might be used to write test result tracking data to a cloud storage container, which would then be used for regular analysis to determine if regressions have occured in newer versions of your software.

# What this documentation covers

Inside these docs, you'll find all of the information that you will need to make the most of Sailfish for your given scenario.
