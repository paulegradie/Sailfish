using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.DefaultHandlers.SailDiff;
using Sailfish.Presentation;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.E2EScenarios.Handlers;

public class SailDiffAnalysisCompleteNotificationHandlerTests
{
    [Fact]
    public async Task HandlerHandles()
    {
        var outputDirectory = Some.RandomString();
        var settings = RunSettingsBuilder.CreateBuilder().WithLocalOutputDirectory(outputDirectory).Build();
        var handler = new SailDiffAnalysisCompleteNotificationHandler(settings);

        var testCaseId = Some.SimpleTestCaseId();

        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(new StatisticalTestResult(
            5.0,
            6.0,
            5.0,
            5.0,
            345,
            0.001,
            SailfishChangeDirection.NoChange,
            3,
            3,
            [1.0, 2, 3],
            [9.0, 10, 11],
            new Dictionary<string, object>()), null, null);


        SailDiffResult[] results = [new SailDiffResult(testCaseId, testResultWithOutlierAnalysis)];

        var notification = new SailDiffAnalysisCompleteNotification(results, "This is some markdown");

        await handler.Handle(notification, CancellationToken.None);

        var files = Directory.GetFiles(outputDirectory);
        var mdContent = await File.ReadAllTextAsync(files.Single(x => x.EndsWith(DefaultFileSettings.MarkdownSuffix)));
        var csvContent = await File.ReadAllTextAsync(files.Single(x => x.EndsWith(DefaultFileSettings.CsvSuffix)));
        const string expectedCsv = """
                                   5,6,5,5,345,0.001,No Change,3,3,"1,2,3","9,10,11"
                                   """;
        files.Length.ShouldBe(2);
        mdContent.ShouldBe("This is some markdown");
        csvContent.Trim().ShouldEndWith(expectedCsv);
    }
}