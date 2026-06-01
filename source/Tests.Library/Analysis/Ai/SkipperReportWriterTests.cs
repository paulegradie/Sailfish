using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sailfish;
using Sailfish.Analysis.Ai;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.Ai;

public class SkipperReportWriterTests
{
    [Fact]
    public async Task WritesMarkdownReport_WhenPresent()
    {
        var dir = NewTempDir();
        try
        {
            var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(dir).Build();
            var writer = new SkipperReportWriter(settings);
            var review = new SkipperReview(
                SkipperVerdict.Regressed,
                Array.Empty<Finding>(),
                Array.Empty<ProposedAction>(),
                "summary",
                "# Skipper Report\n\nParseHeaders regressed — regex compiled in loop (Parser.cs:88).");

            await writer.WriteAsync(review, CancellationToken.None);

            var file = Directory.GetFiles(dir, "skipper-report_*.md").ShouldHaveSingleItem();
            (await File.ReadAllTextAsync(file)).ShouldContain("Parser.cs:88");
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Fact]
    public async Task WritesNothing_WhenMarkdownReportEmpty()
    {
        var dir = NewTempDir();
        try
        {
            var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(dir).Build();
            var writer = new SkipperReportWriter(settings);
            var review = SkipperReview.Empty with { ConsoleSummary = "summary only" };

            await writer.WriteAsync(review, CancellationToken.None);

            Directory.GetFiles(dir, "skipper-report_*.md").ShouldBeEmpty();
        }
        finally
        {
            Cleanup(dir);
        }
    }

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "skipper-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Cleanup(string dir)
    {
        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch
        {
            /* best effort */
        }
    }
}
