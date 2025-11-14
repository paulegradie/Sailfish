using System.Collections.Generic;
using System.Text;
using System.Threading;
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
    private readonly ILogger _logger;
    private readonly ISailDiffResultMarkdownConverter _sailDiffResultMarkdownConverter;

    public SailDiffConsoleWindowMessageFormatter(
        ISailDiffResultMarkdownConverter sailDiffResultMarkdownConverter,
        ILogger logger)
    {
        this._sailDiffResultMarkdownConverter = sailDiffResultMarkdownConverter;
        this._logger = logger;
    }

    public string FormConsoleWindowMessageForSailDiff(
        IEnumerable<SailDiffResult> sailDiffResult,
        TestIds testIds,
        SailDiffSettings sailDiffSettings,
        CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, sailDiffSettings);

        // Use enhanced formatting for console output
        var resultsAsMarkdown = _sailDiffResultMarkdownConverter.ConvertToEnhancedMarkdownTable(sailDiffResult, Formatting.OutputContext.Console);

        stringBuilder.AppendLine(resultsAsMarkdown);
        var result = stringBuilder.ToString();
        _logger.Log(LogLevel.Information, result);
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