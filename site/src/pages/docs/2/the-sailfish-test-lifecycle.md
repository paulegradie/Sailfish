---
title: The Sailfish Test Lifecycle
---

Each Sailfish test class is considered separately from any other Sailfish test at run time. When the internal execution engine fires up, it will collect all of your test classes and registrations and create an test case provider for each class. When iteration begins, each test case provider will produce a test case enumerator. Each iteration, the enumerator will `yield` a new test case.

The yield behavior is quite important, but at the moment we materialize the result of the enumerator via the `yield`, we'll create the instance of the class, inject any dependencies to the constructor, and invoke it. If there is complex behavior initialized from within the constructor, it will only happen we we need to use the type.

__**Warning**: We do not instantiate all Sailfish classes at the beginning of the run! We instatiate them as we `yield` them from the iterator.__

This means that it safe to implement complexe behavior in your constructor. What follows will depend on how you have applied the `SailfishAttribute` to the test class, and which of the test lifecycle methods you have implemented.
