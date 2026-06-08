using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Diagnostics.Environment;
using Sailfish.Execution;
using Sailfish.TestAdapter.Display.VSTestFramework;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Handlers.FrameworkHandlers;

public class FrameworkTestCaseEndNotificationHandlerTests
{
    private static readonly Uri ExecutorUri = new("executor://sailfishexecutor/v1");

    private static (ITestFrameworkWriter Writer, Func<TestResult?> Recorded) WriterCapturingResult()
    {
        TestResult? recorded = null;
        var writer = Substitute.For<ITestFrameworkWriter>();
        writer.When(w => w.RecordResult(Arg.Any<TestResult>())).Do(call => recorded = call.Arg<TestResult>());
        return (writer, () => recorded);
    }

    private static FrameworkTestCaseEndNotification SuccessNotification(string report)
    {
        var testCase = new TestCase("My.Ns.MyClass.MyMethod", ExecutorUri, "source.dll") { DisplayName = "MyMethod" };
        return new FrameworkTestCaseEndNotification(report, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1.0, testCase, StatusCode.Success, null);
    }

    [Fact]
    public async Task Handle_AttachesTheFormattedReportAsAFileOnTheResult()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "sf_attach_" + Guid.NewGuid().ToString("N")[..8]);
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.LocalOutputDirectory.Returns(tempDir);
        runSettings.TimeStamp.Returns(new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc));

        var (writer, recorded) = WriterCapturingResult();
        var handler = new FrameworkTestCaseEndNotificationHandler(writer, Substitute.For<IEnvironmentHealthReportProvider>(), runSettings);

        try
        {
            await handler.Handle(SuccessNotification("Descriptive Statistics\n| Mean | 1.0 |"), CancellationToken.None);

            var result = recorded();
            result.ShouldNotBeNull();
            var attachment = result!.Attachments.ShouldHaveSingleItem().Attachments.ShouldHaveSingleItem();
            File.Exists(attachment.Uri.LocalPath).ShouldBeTrue();
            File.ReadAllText(attachment.Uri.LocalPath).ShouldContain("Descriptive Statistics");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task Handle_WithoutRunSettings_RecordsResultWithNoAttachment()
    {
        var (writer, recorded) = WriterCapturingResult();
        // 1-arg ctor → no run settings → report can't be written, so the result is recorded without an attachment.
        var handler = new FrameworkTestCaseEndNotificationHandler(writer);

        await handler.Handle(SuccessNotification("some report"), CancellationToken.None);

        var result = recorded();
        result.ShouldNotBeNull();
        result!.Attachments.ShouldBeEmpty();
    }
}
