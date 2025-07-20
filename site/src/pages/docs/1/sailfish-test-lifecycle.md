---
title: The Sailfish Test Lifecycle
---

Understanding the Sailfish test lifecycle is crucial for writing effective performance tests. Sailfish provides six lifecycle methods that give you fine-grained control over test execution.

{% info-callout title="Lifecycle Overview" %}
Sailfish allows you to implement multiple `SailfishMethods` in a single class, as well as multiple lifecycle methods for expressive control over your tests.
{% /info-callout %}

Test cases are built for each variable combination (if any) for each test method. Each test case is iterated `(int)SampleSize` times.

## 🔄 Test Execution Flow

{% success-callout title="Predictable Execution Order" %}
For each test case, the test lifecycle follows a predictable pattern that ensures consistent setup and teardown.
{% /success-callout %}

1. **Test Class instantiation**
2. **GlobalSetup** (once per class)
3. **Method Setup**
4. **Iteration Setup**
5. **SailfishMethod** ⏱️ **(This is what gets measured)**
6. **IterationTeardown** (return to `SailfishIterationSetup` when SampleSize > 1)
7. **MethodTeardown** (return to `SailfishMethodSetup` when Method count > 1)
8. **GlobalTeardown** (once per class)

## 🏷️ Lifecycle Method Attributes

{% tip-callout title="Six Lifecycle Methods" %}
Sailfish exposes six lifecycle attributes that give you fine-grained control within your test class. Each serves a specific purpose in the test execution flow.
{% /tip-callout %}

### 🚀 The Setup Phase

{% code-callout title="Preparation Methods" %}
Setup methods prepare your test environment at different levels of granularity.
{% /code-callout %}

**Global Setup** - Called once per test class at the beginning of execution:
```csharp
[SailfishGlobalSetup]
public async Task GlobalSetup(CancellationToken ct) => ...
```

**Method Setup** - Called once before each test method per variable set:
```csharp
[SailfishMethodSetup]
public async Task MethodSetup(CancellationToken ct) => ...
```

**Iteration Setup** - Called once before each test method invocation:
```csharp
[SailfishIterationSetup]
public async Task IterationSetup(CancellationToken ct) => ...
```

### 🧹 The Teardown Phase

{% code-callout title="Cleanup Methods" %}
Teardown methods clean up resources and reset state at different levels of granularity.
{% /code-callout %}

**Iteration Teardown** - Called once after each test method invocation:
```csharp
[SailfishIterationTeardown]
public async Task IterationTeardown(CancellationToken ct) => ...
```

**Method Teardown** - Called once after each test method per variable set:
```csharp
[SailfishMethodTeardown]
public async Task MethodTeardown(CancellationToken ct) => ...
```

**Global Teardown** - Called once per test class at the end of all execution:
```csharp
[SailfishGlobalTeardown]
public async Task GlobalTeardown(CancellationToken ct) => ...
```

## 🔄 Multiple Lifecycle Methods

{% warning-callout title="Execution Order" %}
You may implement more than one of any lifecycle method. The order of execution within a given class is not guaranteed, however methods implemented in base classes will always be executed before child class methods.
{% /warning-callout %}

## 🎯 Targeting Specific SailfishMethods

{% tip-callout title="Selective Lifecycle Methods" %}
You can optionally provide a params array of method names to target specific SailfishMethods. If no names are provided, the lifecycle method is applied to all methods.
{% /tip-callout %}

```csharp
[SailfishMethodSetup(nameof(TestMethod))]  // <- target specific method
public async Task MethodSetup(CancellationToken ct) => ...

[SailfishMethod]
public void TestMethod() => ...
```

**Supported targeting attributes:**
- **SailfishMethodSetup**
- **SailfishMethodTeardown**
- **SailfishIterationSetup**
- **SailfishIterationTeardown**

## 🔧 Property and Field Management

{% info-callout title="Instance Management" %}
When multiple test cases are created for a class, distinct instances of the class are created. Properties and fields that are set in the global lifecycle methods must be cloned to new instances.
{% /info-callout %}

The following modifiers are allowed when creating a property or field where the data is set during lifecycle invocation:

### 📝 Properties

{% feature-grid columns=2 %}
{% feature-card title="Public Properties" description="Accessible from anywhere, commonly used for test data." /%}

{% feature-card title="Protected Properties" description="Accessible from derived classes, useful for inheritance scenarios." /%}
{% /feature-grid %}

```csharp
public Type Public { get; set; }
protected Type Protected { get; set; }
```

### 🏷️ Fields

{% feature-grid columns=2 %}
{% feature-card title="Public/Internal Fields" description="Direct field access for simple data storage." /%}

{% feature-card title="Protected/Private Fields" description="Encapsulated field access for internal test state." /%}
{% /feature-grid %}

```csharp
internal Type InternalField;
protected Type ProtectedField;
private Type PrivateField;
public Type PublicField;
```

{% note-callout title="Next Steps" %}
Now that you understand the test lifecycle, explore [Output Attributes](/docs/1/output-attributes) to learn how to customize test results, or check out [Test Dependencies](/docs/1/test-dependencies) for dependency injection scenarios.
{% /note-callout %}
