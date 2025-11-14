using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Diagnostics.Environment;

namespace Sailfish.TestAdapter.Execution.EnvironmentHealth;

internal class EnvironmentHealthCheckRunner
{
    private readonly IEnvironmentHealthChecker _checker;

    public EnvironmentHealthCheckRunner(IEnvironmentHealthChecker checker)
    {
        this._checker = checker;
    }

    public async Task<(EnvironmentHealthReport Report, string Summary)> RunAsync(EnvironmentHealthCheckContext? context, CancellationToken token)
    {
        var report = await _checker.CheckAsync(context, token).ConfigureAwait(false);
        var summary = BuildSummaryString(report);
        return (report, summary);
    }

    private static string BuildSummaryString(EnvironmentHealthReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Sailfish Environment Health: {report.Score}/100 ({report.SummaryLabel})");
        foreach (var e in report.Entries.Take(6))
        {
            var rec = string.IsNullOrWhiteSpace(e.Recommendation) ? string.Empty : $" — {e.Recommendation}";
            sb.AppendLine($" - {e.Name}: {e.Status} ({e.Details}){rec}");
        }
        return sb.ToString();
    }

    public async Task<string> RunAndFormatSummaryAsync(EnvironmentHealthCheckContext? context, CancellationToken token)
    {
        var (report, summary) = await RunAsync(context, token).ConfigureAwait(false);
        return summary;
    }
}

