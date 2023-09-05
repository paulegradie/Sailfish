using System;
using System.Collections.Generic;
using System.Text;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Execution;
using Sailfish.Extensions.Types;

namespace Sailfish.Presentation.Console;

internal class ConsoleWriter : IConsoleWriter
{
    private readonly IMarkdownTableConverter markdownTableConverter;

    public ConsoleWriter(IMarkdownTableConverter markdownTableConverter)
    {
        this.markdownTableConverter = markdownTableConverter;
    }

    public string Present(IEnumerable<IClassExecutionSummary> results, OrderedDictionary tags)
    {
        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(results);

        if ((tags.Count > 0)) System.Console.WriteLine($"{Environment.NewLine}Tags:");
        foreach (var entry in tags)
        {
            System.Console.WriteLine($"{entry.Key}: {entry.Value}");
        }

        System.Console.WriteLine(markdownStringTable);
        return markdownStringTable;
    }

    public void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, SailDiffSettings sailDiffSettings)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, sailDiffSettings);
        stringBuilder.AppendLine(markdownBody);
        System.Console.WriteLine(stringBuilder.ToString());
    }

    public void WriteString(string content)
    {
        System.Console.Write(content);
    }

    private static void BuildHeader(StringBuilder stringBuilder, IEnumerable<string> beforeIds, IEnumerable<string> afterIds, SailDiffSettings sailDiffSettings)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"{sailDiffSettings.TestType} results comparing:");
        stringBuilder.AppendLine($"Before: {string.Join(", ", beforeIds)}");
        stringBuilder.AppendLine($"After: {string.Join(", ", afterIds)}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: The change in execution time is significant if the PValue is less than {sailDiffSettings.Alpha}");
    }
}