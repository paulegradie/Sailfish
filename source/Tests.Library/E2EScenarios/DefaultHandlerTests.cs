using Sailfish;
using Sailfish.Contracts.Public.Serialization;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Presentation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tests.E2E.TestSuite;
using Tests.E2E.TestSuite.Discoverable;
using Tests.Library.Utils;
using Xunit;

namespace Tests.Library.E2EScenarios;

public class DefaultHandlerTests
{
    [Fact]
    public async Task TestCaseCompleteNotificationIsInvoked()
    {
        var outputDirectory = Some.RandomString();
        var timeStamp = DateTime.Now;
        const int sampleSizeOverride = 4;
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(outputDirectory)
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .WithTestNames(typeof(MinimalTest).FullName!)
            .CreateTrackingFiles()
            .DisableOverheadEstimation()
            .WithTimeStamp(timeStamp)
            .WithGlobalSampleSize(sampleSizeOverride)
            .WithAnalysisDisabledGlobally()
            .Build();

        await SailfishRunner.Run(runSettings);

        var trackingPath = Path.Join(outputDirectory, DefaultFileSettings.DefaultExecutionSummaryTrackingDirectory, DefaultFileSettings.DefaultTrackingFileName(timeStamp));
        File.Exists(trackingPath).ShouldBeTrue();

        var content = GetFileContent(trackingPath);
        var trackingData = SailfishSerializer.Deserialize<IEnumerable<ClassExecutionSummaryTrackingFormat>>(content)?.ToList();

        trackingData.ShouldNotBeNull();
        trackingData.Count.ShouldBe(1);

        var result = trackingData.Single();
        result.TestClass.ShouldBe(typeof(MinimalTest));
        result.CompiledTestCaseResults.Count().ShouldBe(1);
        result.CompiledTestCaseResults.Single().Exception.ShouldBeNull();
        result.CompiledTestCaseResults.Single().PerformanceRunResult.ShouldNotBeNull();
        result.CompiledTestCaseResults.Single().PerformanceRunResult!.RawExecutionResults.Length.ShouldBe(sampleSizeOverride);
    }

    [Fact]
    public async Task TestRunCompleteIsInvoked()
    {
        var outputDirectory = Some.RandomString();
        var timeStamp = DateTime.Now;
        const int sampleSizeOverride = 4;
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(outputDirectory)
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .WithTestNames(typeof(MinimalTest).FullName!)
            .CreateTrackingFiles()
            .DisableOverheadEstimation()
            .WithTimeStamp(timeStamp)
            .WithGlobalSampleSize(sampleSizeOverride)
            .WithAnalysisDisabledGlobally()
            .Build();

        await SailfishRunner.Run(runSettings);

        var content = GetFileContent(Path.Join(outputDirectory, "TestRunCompleted.txt"));
        content.ShouldBe("TestRunComplete");
    }

    [Fact]
    public async Task OutputsAreWritten()
    {
        var outputDirectory = Some.RandomString();
        var timeStamp = DateTime.Now;
        const int sampleSizeOverride = 4;
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(outputDirectory)
            .ProvidersFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .TestsFromAssembliesContaining(typeof(E2ETestRegistrationProvider))
            .WithTestNames(typeof(MinimalTest).FullName!)
            .DisableOverheadEstimation()
            .WithTimeStamp(timeStamp)
            .WithGlobalSampleSize(sampleSizeOverride)
            .WithAnalysisDisabledGlobally()
            .Build();

        await SailfishRunner.Run(runSettings);

        var files = Directory.GetFiles(outputDirectory);
        var csv = files.Single(x => x.EndsWith(DefaultFileSettings.CsvSuffix));
        var md = files.Single(x => x.EndsWith(DefaultFileSettings.MarkdownSuffix));

        var csvContent = GetFileContent(csv);
        var mdContent = GetFileContent(md);

        csvContent.ShouldContain(nameof(MinimalTest));
        mdContent.ShouldContain(nameof(MinimalTest));
    }

    private static string GetFileContent(string md)
    {
        using var stream = new StreamReader(md);
        return stream.ReadToEnd();
    }
}