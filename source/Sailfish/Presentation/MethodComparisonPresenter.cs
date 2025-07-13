using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Extensions.Methods;
using Sailfish.Presentation.Console;

namespace Sailfish.Presentation;

internal class MethodComparisonPresenter : INotificationHandler<MethodComparisonCompletedNotification>
{
    private readonly IConsoleWriter consoleWriter;

    public MethodComparisonPresenter(IConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
    }

    public async Task Handle(MethodComparisonCompletedNotification notification, CancellationToken cancellationToken)
    {
        var result = notification.ComparisonResult;
        var output = new StringBuilder();

        output.AppendLine();
        output.AppendLine($"=== Method Comparison Results: {result.ComparisonGroup.GroupName} ===");
        output.AppendLine();

        // Display rankings
        output.AppendLine("Performance Rankings:");
        foreach (var ranking in result.MethodRankings.OrderBy(r => r.Rank))
        {
            var performanceIndicator = ranking.RelativePerformance switch
            {
                < 0.9 => "🚀 Fastest",
                < 1.1 => "⚡ Fast", 
                < 2.0 => "🐌 Slow",
                _ => "🐢 Slowest"
            };

            output.AppendLine($"  {ranking.Rank}. {ranking.MethodName} - {ranking.MedianExecutionTime:F2}ms " +
                            $"({ranking.RelativePerformance:F2}x) {performanceIndicator}");
        }

        output.AppendLine();

        // Display significant differences
        var significantComparisons = result.PairwiseComparisons
            .Where(c => c.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue < result.ComparisonGroup.SignificanceLevel)
            .ToList();

        if (significantComparisons.Any())
        {
            output.AppendLine("Statistically Significant Differences:");
            foreach (var comparison in significantComparisons)
            {
                output.AppendLine($"  • {comparison.TestCaseId.TestCaseName.GetMethodPart()} vs baseline: " +
                                $"p-value = {comparison.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue:F4}");
            }
        }
        else
        {
            output.AppendLine("No statistically significant differences found.");
        }

        output.AppendLine();
        output.AppendLine("=".PadRight(60, '='));

        consoleWriter.WriteString(output.ToString());
        await Task.CompletedTask;
    }
}
