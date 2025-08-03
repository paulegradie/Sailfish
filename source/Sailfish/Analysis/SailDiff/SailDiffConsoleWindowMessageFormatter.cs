using System.Collections.Generic;
using System.Text;
using System.Threading;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;

namespace Sailfish.Analysis.SailDiff;

public interface ISailDiffConsoleWindowMessageFormatter
{
    string FormConsoleWindowMessageForSailDiff(
        IEnumerable<SailDiffResult> sailDiffResult,
        TestIds testIds,
        SailDiffSettings sailDiffSettings,
        CancellationToken cancellationToken);
}

public class SailDiffConsoleWindowMessageFormatter : ISailDiffConsoleWindowMessageFormatter
{
    private readonly ILogger logger;
    private readonly ISailDiffResultMarkdownConverter sailDiffResultMarkdownConverter;

    public SailDiffConsoleWindowMessageFormatter(
        ISailDiffResultMarkdownConverter sailDiffResultMarkdownConverter,
        ILogger logger)
    {
        this.sailDiffResultMarkdownConverter = sailDiffResultMarkdownConverter;
        this.logger = logger;
    }

    public string FormConsoleWindowMessageForSailDiff(
        IEnumerable<SailDiffResult> sailDiffResult,
        TestIds testIds,
        SailDiffSettings sailDiffSettings,
        CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, sailDiffSettings);

        // Try to use enhanced formatting if available
        var resultsAsMarkdown = sailDiffResultMarkdownConverter is ISailDiffResultMarkdownConverter enhancedConverter
            ? enhancedConverter.ConvertToEnhancedMarkdownTable(sailDiffResult, Formatting.OutputContext.Console)
            : sailDiffResultMarkdownConverter.ConvertToMarkdownTable(sailDiffResult);

        stringBuilder.AppendLine(resultsAsMarkdown);
        var result = stringBuilder.ToString();
        logger.Log(LogLevel.Information, result);
        return result;
    }

    private static void BuildHeader(
        StringBuilder stringBuilder,
        IEnumerable<string> beforeIds,
        IEnumerable<string> afterIds,
        SailDiffSettings sailDiffSettings)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"{sailDiffSettings.TestType} results comparing:");
        stringBuilder.AppendLine($"Before: {string.Join(", ", beforeIds)}");
        stringBuilder.AppendLine($"After: {string.Join(", ", afterIds)}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: Changes are significant if the PValue is less than {sailDiffSettings.Alpha}");
    }
}