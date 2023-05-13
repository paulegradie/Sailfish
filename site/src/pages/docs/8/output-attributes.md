---
title: Output Attributes
---

Currently, Sailfish exposes two attributes that you can use to produce output files you can use for things like PR submissions, team reviews, or other scenarios where you need to keep a separate record that you may wish to share around.

## The Markdown Attribute

The write to markdown attribute will cause Sailfish to write out a markdown file containing your performance test and analysis results. If no custom output location is provided via the RunSettings object, then the default tracking directory will be used. Additionally, Sailfish exposes the `WriteTestResultsAsMarkdownCommand` which will be passed when implementing `INotificationHandler<WriteToMarkDownCommand>`. This handler will allow you to customize what is done with the markdown form of your test results. Log them, write them to disk, or email them to someone if you'd like.

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

## The Csv Attribute

The same details from the `WriteToMarkdown` attribute apply to the csv attribute. The only difference is that csv is provided to the handler instead of markdown.

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
