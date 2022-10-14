using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;

namespace AsAConsoleApp.CustomHandlerOverrideExamples;

public class CustomNotificationHandler : INotificationHandler<NotifyOnTestResultCommand>
{
    public Task Handle(NotifyOnTestResultCommand notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.TTestContent.MarkdownTable)) return Task.CompletedTask;

        var lines = notification.TTestContent.MarkdownTable
            .Split(Environment.NewLine);

        Console.WriteLine("Make believe this handler parses the t-test result and reports tests that have regressed.");
        var header = lines.Single(x => x.Contains(nameof(NamedTTestResult.DisplayName)));
        lines = lines
            .Where(x => x.Contains('*'))
            .ToArray();

        if (!lines.Any())
        {
            Console.WriteLine("No regressions or improvements found.");
            return Task.CompletedTask;
        }

        Console.WriteLine(header);
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        return Task.CompletedTask;
    }
}