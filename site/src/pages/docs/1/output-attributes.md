---
title: Output Attributes
---
Sailfish prints to StdOut and writes a tracking file into the calling assemblies **bin** directory by default. You can export addition formatted documents using the following attributes.


## WriteToMarkdown

```csharp
[WriteToMarkdown]
[Sailfish]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}
```

## WriteToCsv

```csharp
[WriteToCsv]
[Sailfish]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}
```

**Note on Extensibility**:
Sailfish exposes the `WriteTestResultsAsMarkdownCommand` which will be passed when implementing `INotificationHandler<WriteToMarkDownCommand>`. This handler will allow you to customize what is done with the markdown form of your test results.