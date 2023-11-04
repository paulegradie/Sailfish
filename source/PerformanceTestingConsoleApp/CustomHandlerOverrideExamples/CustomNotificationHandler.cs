using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Serilog;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public class CustomNotificationHandler : INotificationHandler<NotifyOnTestResultNotification>
{
    private readonly ILogger logger;

    public CustomNotificationHandler(ILogger logger)
    {
        this.logger = logger;
    }

    public Task Handle(NotifyOnTestResultNotification notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.TestResultFormats.MarkdownFormat)) return Task.CompletedTask;

        var lines = notification
            .TestResultFormats
            .MarkdownFormat
            .Split(Environment.NewLine);

        Console.WriteLine($"Make believe this handler parses the {notification.SailDiffSettings.TestType.ToString()} result and reports tests that have regressed.");
        var header = lines.Single(x => x.Contains(nameof(TestCaseResults.TestCaseId.DisplayName)));
        lines = lines
            .Where(x => x == SailfishChangeDirection.Regressed)
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