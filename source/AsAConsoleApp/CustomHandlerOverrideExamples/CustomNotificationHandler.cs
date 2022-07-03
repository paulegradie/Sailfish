using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace AsAConsoleApp.CustomHandlerOverrideExamples;

public class CustomNotificationHandler : INotificationHandler<NotifyOnTestResultCommand>
{
    public Task Handle(NotifyOnTestResultCommand notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.TTestContent)) return Task.CompletedTask;

        var lines = notification.TTestContent
            .Split(Environment.NewLine);

        Console.WriteLine("Make believe this handler parses the ttest result and reports tests that have regressed.");
        var header = lines.Single(x => x.Contains("TestName"));
        lines = lines
            .Where(x => x.Contains("*"))
            .ToArray();

        if (lines.Count() == 0)
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