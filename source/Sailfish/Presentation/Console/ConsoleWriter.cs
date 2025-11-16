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
    private readonly ILogger _logger;
    private readonly IMarkdownTableConverter _markdownTableConverter;

    public ConsoleWriter(IMarkdownTableConverter markdownTableConverter, ILogger logger)
    {
        _logger = logger;
        _markdownTableConverter = markdownTableConverter;
    }

    public string WriteToConsole(IEnumerable<IClassExecutionSummary> results, OrderedDictionary tags)
    {
        // Use enhanced markdown so session-level header (including timer calibration) appears in console as well
        var markdownStringTable = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(results);

        if (tags.Count > 0) System.Console.WriteLine($"{Environment.NewLine}Tags:");
        foreach (var entry in tags) _logger.Log(LogLevel.Information, $"{entry.Key}: {entry.Value}");

        _logger.Log(LogLevel.Information, markdownStringTable);
        return markdownStringTable;
    }

    public void WriteString(string content)
    {
        _logger.Log(LogLevel.Information, content);
    }

    public void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, SailDiffSettings sailDiffSettings)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, sailDiffSettings);
        stringBuilder.AppendLine(markdownBody);
        _logger.Log(LogLevel.Information, stringBuilder.ToString());
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