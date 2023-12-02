using System;
using System.Collections.Generic;
using System.Text;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Logging;

namespace Sailfish.Presentation.Console;

internal class ConsoleWriter : IConsoleWriter
{
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly ILogger logger;

    public ConsoleWriter(IMarkdownTableConverter markdownTableConverter, ILogger logger)
    {
        this.markdownTableConverter = markdownTableConverter;
        this.logger = logger;
    }

    public string WriteToConsole(IEnumerable<IClassExecutionSummary> results, OrderedDictionary tags)
    {
        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(results);

        if ((tags.Count > 0)) System.Console.WriteLine($"{Environment.NewLine}Tags:");
        foreach (var entry in tags)
        {
            logger.Log(LogLevel.Information, $"{entry.Key}: {entry.Value}");
        }

        logger.Log(LogLevel.Information, markdownStringTable);
        return markdownStringTable;
    }

    public void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, SailDiffSettings sailDiffSettings)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, sailDiffSettings);
        stringBuilder.AppendLine(markdownBody);
        logger.Log(LogLevel.Information, stringBuilder.ToString());
    }

    public void WriteString(string content)
    {
        logger.Log(LogLevel.Information, content);
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