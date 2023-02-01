using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Collections;
using Sailfish.Execution;

namespace Sailfish.Presentation.Console;

internal class ConsoleWriter : IConsoleWriter
{
    private readonly IMarkdownTableConverter markdownTableConverter;

    public ConsoleWriter(IMarkdownTableConverter markdownTableConverter)
    {
        this.markdownTableConverter = markdownTableConverter;
    }

    public string Present(IEnumerable<IExecutionSummary> results, OrderedDictionary<string, string> tags)
    {
        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(results);

        if (tags.Any()) System.Console.WriteLine($"{Environment.NewLine}Tags:");
        foreach (var (key, value) in tags)
        {
            System.Console.WriteLine($"{key}: {value}");
        }

        System.Console.WriteLine(markdownStringTable);
        return markdownStringTable;
    }
}