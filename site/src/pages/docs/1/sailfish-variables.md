---
title: Sailfish Variables
---

**Sailfish variables** allow you to create multiple test cases with different test class states.

Create a variable by appling one of two variable attributes to a public property:

## SalifishVariable Attribute

```csharp
[Sailfish]
public class Example
{
    [SailfishVariable(10, 100, 1000)] // params object[]
    public int SleepPeriod { get; set; }

    [SailfishMethod]
    public void Method()
    {
        Thread.Sleep(SleepPeriod)
    }
}
```

## SailfishRangeVariable Attribute

```csharp
[Sailfish]
public class Example
{
    [SailfishRangeVariable(start: 1, count: 3, step: 100)]
    public int SleepPeriod { get; set; }

    [SailfishMethod]
    public void Method()
    {
        Thread.Sleep(SleepPeriod)
    }
}
```

### start
The starting number for the range.

### count
The number of elements to create.

### step
The number of values to skip before taking the next value
---

## Variable Types

Sailfish variables can be any type that is compatible the base **Attribute** class.

Here is an example of a test that defines a single test variable:

```csharp
[SailfishVariable(10, 100, 1000)]
[SailfishVariable(0.24, 1.6)]
[SailfishVariable("ScenarioA", "ScenarioB")]
[SailfishVariable(MyEnum.First, MyEnum.Second)]
```

## Complexity Estimation (ScaleFish)

When applying a variable attribute, you may choose to specify that variable for ScaleFish complexity estimation and modeling. To do so set the first optional parameter to true:

```csharp
[SailfishVariable(scalefish: true, 10, 100, 1000)]
```
**NOTE**: When using Scalefish, variables must be of type (int).