using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analysis;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Serilog;
using Serilog.Core;


namespace Sailfish.TestAdapter.Execution;

internal class ConsoleWriter : IConsoleWriter
{
    private readonly IMarkdownTableConverter markdownTableConverter;
    private IMessageLogger? messageLogger;

    private object objectLock = new();

    private readonly Logger consoleLogger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();

    public ConsoleWriter(
        IMarkdownTableConverter markdownTableConverter,
        IMessageLogger? messageLogger)
    {
        this.markdownTableConverter = markdownTableConverter;
        this.messageLogger = messageLogger;
    }

    public string Present(IEnumerable<IExecutionSummary> results, OrderedDictionary? tags = null)
    {
        var summaryResults = results.ToList();
        foreach (var result in summaryResults)
        {
            foreach (var compiledResult in result.CompiledTestCaseResults)
            {
                if (!compiledResult.Exceptions.Any()) continue;
                foreach (var exception in compiledResult.Exceptions)
                {
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.Message);
                    consoleLogger.Error("{Error}", exception.Message);

                    if (exception.StackTrace == null) continue;
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.StackTrace);
                    consoleLogger.Error("{StackTrace}", exception.Message);

                    if (exception.InnerException is null) continue;
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.InnerException.Message);
                    consoleLogger.Error("{InnerError}", exception.InnerException.Message);

                    if (exception.InnerException.StackTrace == null) continue;
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.InnerException.StackTrace);
                    consoleLogger.Error("{InnerStackTrace}", exception.InnerException.StackTrace);
                }
            }
        }

        var rawData = summaryResults
            .SelectMany(x =>
                x.CompiledTestCaseResults.SelectMany(y =>
                    y.DescriptiveStatisticsResult?.RawExecutionResults ?? Array.Empty<double>()))
            .ToArray();

        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(summaryResults)
                                  + "Raw results: \n"
                                  + string.Join(", ", rawData.OrderBy(x => x));

        messageLogger?.SendMessage(TestMessageLevel.Informational, markdownStringTable);
        consoleLogger.Information("{MarkdownTable}", markdownStringTable);

        return markdownStringTable;
    }

    public void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, TestSettings testSettings)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, testSettings);
        stringBuilder.AppendLine(markdownBody);
        var result = stringBuilder.ToString();
        consoleLogger.Information(result);
        messageLogger?.SendMessage(TestMessageLevel.Informational, result);
    }

    public void WriteString(string content)
    {
        consoleLogger.Information(content);
        messageLogger?.SendMessage(TestMessageLevel.Informational, content);
    }

    private static void BuildHeader(StringBuilder stringBuilder, IEnumerable<string> beforeIds, IEnumerable<string> afterIds, TestSettings testSettings)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"{testSettings.TestType} results comparing:");
        stringBuilder.AppendLine($"Before: {string.Join(", ", beforeIds)}");
        stringBuilder.AppendLine($"After: {string.Join(", ", afterIds)}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: The change in execution time is significant if the PValue is less than {testSettings.Alpha}");
    }
}