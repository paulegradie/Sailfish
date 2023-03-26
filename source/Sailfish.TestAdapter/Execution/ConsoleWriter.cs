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
                if (compiledResult.Exception is null) continue;
                messageLogger?.SendMessage(TestMessageLevel.Error, compiledResult.Exception.Message);
                consoleLogger.Error("{Error}", compiledResult.Exception.Message);

                if (compiledResult.Exception.StackTrace != null)
                {
                    messageLogger?.SendMessage(TestMessageLevel.Error, compiledResult.Exception.StackTrace);
                    consoleLogger.Error("{StackTrace}", compiledResult.Exception.Message);

                    if (compiledResult.Exception.InnerException is null) continue;
                    messageLogger?.SendMessage(TestMessageLevel.Error, compiledResult.Exception.InnerException.Message);
                    consoleLogger.Error("{InnerError}", compiledResult.Exception.InnerException.Message);

                    if (compiledResult.Exception.InnerException.StackTrace != null)
                    {
                        messageLogger?.SendMessage(TestMessageLevel.Error, compiledResult.Exception.InnerException.StackTrace);
                        consoleLogger.Error("{InnerStackTrace}", compiledResult.Exception.InnerException.StackTrace);
                    }
                }
            }
        }

        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(summaryResults);

        messageLogger?.SendMessage(TestMessageLevel.Informational, markdownStringTable);
        consoleLogger.Information("{MarkdownTable}", markdownStringTable);
        return markdownStringTable;
    }
}