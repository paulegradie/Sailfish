---
title: Release Notes
---

- 2.0.268
  - replaces internal MemoryCache with a class to hold a state cache. Fixes issue #180 MemoryCache would evict data

- 2.0.192
  - Code cleanup
  - Update documentation
  - Fix stale vs test documentation link

- 2.0.0

  - **Breaking Change**: Public mediator contracts converted to records and CamelCased property names
  - Improved internal organization
  - Improved logging
  - Improved handling of exceptions in test output window

- 1.6.7

  - Fixed issue where test cases would not run if an earlier test case failed / threw exception
  - Fixed issue where logged test case counts would not increment correctly when a test case threw an exception

- 1.6.4
  - Fixed spinner loading bug in ide which caused multiple spinners to stack when exceptions are thrown
