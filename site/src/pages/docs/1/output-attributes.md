---
title: Output Attributes
---

Sailfish provides flexible output options to suit different reporting needs. By default, results are printed to StdOut and written to a tracking file in the calling assembly's **bin** directory.

{% info-callout title="Default Output" %}
Sailfish automatically provides console output and tracking files. Use output attributes to generate additional formatted documents for reporting and analysis.
{% /info-callout %}

## 📊 Available Output Formats

{% feature-grid columns=2 %}
{% feature-card title="Markdown Output" description="Generate formatted markdown reports perfect for documentation and GitHub." /%}

{% feature-card title="CSV Output" description="Export data to CSV format for analysis in Excel, R, or other data tools." /%}
{% /feature-grid %}

## 📝 WriteToMarkdown

{% success-callout title="Documentation-Ready Reports" %}
Generate beautifully formatted markdown reports that are perfect for documentation, GitHub README files, or technical reports.
{% /success-callout %}

```csharp
[WriteToMarkdown]
[Sailfish]
public class PerformanceTest
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Your performance test logic
    }
}
```

**Use cases:**
- **Documentation**: Include performance results in project documentation
- **GitHub Reports**: Add results to pull request descriptions
- **Technical Reports**: Generate formatted reports for stakeholders

## 📈 WriteToCsv

{% code-callout title="Data Analysis Ready" %}
Export raw performance data to CSV format for detailed analysis in spreadsheet applications or statistical tools.
{% /code-callout %}

```csharp
[WriteToCsv]
[Sailfish]
public class DataAnalysisTest
{
    [SailfishMethod]
    public void TestMethod()
    {
        // Your performance test logic
    }
}
```

**Use cases:**
- **Statistical Analysis**: Import data into R, Python, or MATLAB
- **Spreadsheet Analysis**: Open in Excel for charts and pivot tables
- **Data Warehousing**: Import into databases for historical tracking

## 🔧 Extensibility

{% tip-callout title="Custom Output Handlers" %}
Sailfish exposes extensibility points that allow you to create custom output formats and processing logic.
{% /tip-callout %}

Sailfish exposes the `WriteTestResultsAsMarkdownCommand` which will be passed when implementing `INotificationHandler<WriteToMarkDownCommand>`. This handler allows you to customize what is done with the markdown form of your test results.

### Custom Handler Example

```csharp
public class CustomMarkdownHandler : INotificationHandler<WriteToMarkdownCommand>
{
    public async Task Handle(WriteToMarkdownCommand notification, CancellationToken cancellationToken)
    {
        // Custom processing of markdown results
        var customReport = ProcessMarkdown(notification.MarkdownContent);
        await SaveToCustomLocation(customReport);
    }
}
```

{% note-callout title="Advanced Usage" %}
For more advanced output customization and extensibility options, check out our [Extensibility Guide](/docs/3/extensibility).
{% /note-callout %}