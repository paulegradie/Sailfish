using System;
using System.Collections.Generic;

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

    public string Present(IEnumerable<IExecutionSummary> results, OrderedDictionary tags)
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
}