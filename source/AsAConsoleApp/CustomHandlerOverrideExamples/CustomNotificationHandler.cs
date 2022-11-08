﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;

namespace AsAConsoleApp.CustomHandlerOverrideExamples;

public class CustomNotificationHandler : INotificationHandler<NotifyOnTestResultCommand>
{
    public Task Handle(NotifyOnTestResultCommand notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.TestResultFormats.MarkdownFormat)) return Task.CompletedTask;

        var lines = notification
            .TestResultFormats
            .MarkdownFormat
            .Split(Environment.NewLine);

        Console.WriteLine($"Make believe this handler parses the {notification.TestSettings.TestType.ToString()} result and reports tests that have regressed.");
        var header = lines.Single(x => x.Contains(nameof(TestCaseResults.DisplayName)));
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