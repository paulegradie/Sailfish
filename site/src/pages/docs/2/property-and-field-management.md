---
title: Property and Field Management
---

When multiple test cases are created for a class, distinct instances of the class are created. Properties and Fields that are set in the global lifecycle methods must therefore be cloned to new instances that do not execute the global lifecycle method.

The following modifiers are allowed when creating a property or field where the data is a set during lifecycle invocation:

## Properties

```csharp
    public Type Public { get; set; }
    protected Type Protected { get; set; }
```

## Fields

```csharp
    internal Type InternalField;
    protected Type ProtectedField;
    private Type PrivateField;
    public Type PublicField;
```
