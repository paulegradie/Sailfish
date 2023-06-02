using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
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
    private readonly IMessageLogger? messageLogger;

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
            foreach (var compiledResult in result.CompiledResults)
            {
                if (!compiledResult.Exceptions.Any()) continue;
                foreach (var exception in compiledResult.Exceptions)
                {
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.Message);
                    consoleLogger.Error("{Error}", exception.Message);

                    if (exception.StackTrace != null)
                    {
                        messageLogger?.SendMessage(TestMessageLevel.Error, exception.StackTrace);
                        consoleLogger.Error("{StackTrace}", exception.Message);

                        if (exception.InnerException is null) continue;
                        messageLogger?.SendMessage(TestMessageLevel.Error, exception.InnerException.Message);
                        consoleLogger.Error("{InnerError}", exception.InnerException.Message);

                        if (exception.InnerException.StackTrace != null)
                        {
                            messageLogger?.SendMessage(TestMessageLevel.Error, exception.InnerException.StackTrace);
                            consoleLogger.Error("{InnerStackTrace}", exception.InnerException.StackTrace);
                        }
                    }
                }
            }
        }

        var rawData = summaryResults
            .SelectMany(x =>
                x.CompiledResults.SelectMany(y =>
                    y.DescriptiveStatisticsResult?.RawExecutionResults ?? Array.Empty<double>()))
            .ToArray();
        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(summaryResults) + "Raw results: \n" + string.Join(", ", rawData.OrderBy(x => x));

        messageLogger?.SendMessage(TestMessageLevel.Informational, markdownStringTable);
        consoleLogger.Information("{MarkdownTable}", markdownStringTable);

        return markdownStringTable;
    }
}