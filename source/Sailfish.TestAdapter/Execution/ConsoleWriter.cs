using System.Collections.Generic;
using Accord.Collections;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Serilog;
using Serilog.Core;

namespace Sailfish.TestAdapter.Execution;

internal class ConsoleWriter : IConsoleWriter
{
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IMessageLogger? messageLogger;
    private readonly Logger consoleLogger;

    public ConsoleWriter(
        IMarkdownTableConverter markdownTableConverter,
        IMessageLogger? messageLogger)
    {
        this.markdownTableConverter = markdownTableConverter;
        this.messageLogger = messageLogger;
        consoleLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }

    public string Present(IEnumerable<IExecutionSummary> results, OrderedDictionary<string, string>? tags = null)
    {
        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(results);

        messageLogger?.SendMessage(TestMessageLevel.Informational, markdownStringTable);
        consoleLogger.Information(markdownStringTable);
        return markdownStringTable;
    }
}