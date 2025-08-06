using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library;

public class RunSettingsBuilderTests
{
    [Fact]
    public void CreateBuilder_ReturnsNewInstance()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<RunSettingsBuilder>();
    }

    [Fact]
    public void CreateBuilder_ReturnsNewInstanceEachTime()
    {
        var builder1 = RunSettingsBuilder.CreateBuilder();
        var builder2 = RunSettingsBuilder.CreateBuilder();
        
        builder1.ShouldNotBeSameAs(builder2);
    }

    [Fact]
    public void WithMinimumLogLevel_SetsLogLevel_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithMinimumLogLevel(LogLevel.Error);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.MinimumLogLevel.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public void WithCustomLogger_SetsLogger_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var mockLogger = Substitute.For<ILogger>();
        
        var result = builder.WithCustomLogger(mockLogger);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.CustomLogger.ShouldBeSameAs(mockLogger);
    }

    [Fact]
    public void DisableLogging_SetsDisableLoggingTrue_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.DisableLogging();
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.DisableLogging.ShouldBeTrue();
    }

    [Fact]
    public void DisableStreamingTrackingUpdates_SetsStreamTrackingUpdatesFalse_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.DisableStreamingTrackingUpdates();
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.StreamTrackingUpdates.ShouldBeFalse();
    }

    [Fact]
    public void WithTestNames_SingleName_AddsToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithTestNames("TestClass1");
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.TestNames.ShouldContain("TestClass1");
        settings.TestNames.Count().ShouldBe(1);
    }

    [Fact]
    public void WithTestNames_MultipleNames_AddsAllToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithTestNames("TestClass1", "TestClass2", "TestClass3");
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.TestNames.ShouldContain("TestClass1");
        settings.TestNames.ShouldContain("TestClass2");
        settings.TestNames.ShouldContain("TestClass3");
        settings.TestNames.Count().ShouldBe(3);
    }

    [Fact]
    public void WithTestNames_MultipleCalls_AccumulatesNames_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        builder.WithTestNames("TestClass1")
               .WithTestNames("TestClass2", "TestClass3");
        
        var settings = builder.Build();
        settings.TestNames.Count().ShouldBe(3);
        settings.TestNames.ShouldContain("TestClass1");
        settings.TestNames.ShouldContain("TestClass2");
        settings.TestNames.ShouldContain("TestClass3");
    }

    [Fact]
    public void WithLocalOutputDirectory_SetsDirectory_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var customDir = "custom_output";
        
        var result = builder.WithLocalOutputDirectory(customDir);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.LocalOutputDirectory.ShouldBe(customDir);
    }

    [Fact]
    public void CreateTrackingFiles_True_SetsCreateTrackingFilesTrue_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.CreateTrackingFiles(true);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.CreateTrackingFiles.ShouldBeTrue();
    }

    [Fact]
    public void CreateTrackingFiles_False_SetsCreateTrackingFilesFalse_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.CreateTrackingFiles(false);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.CreateTrackingFiles.ShouldBeFalse();
    }

    [Fact]
    public void CreateTrackingFiles_DefaultParameter_SetsCreateTrackingFilesTrue_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.CreateTrackingFiles();
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.CreateTrackingFiles.ShouldBeTrue();
    }

    [Fact]
    public void WithSailDiff_NoParameters_EnablesSailDiff_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithSailDiff();
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.RunSailDiff.ShouldBeTrue();
        settings.SailDiffSettings.ShouldNotBeNull();
    }

    [Fact]
    public void WithSailDiff_WithSettings_EnablesSailDiffWithCustomSettings_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var customSettings = new SailDiffSettings(alpha: 0.05, round: 5);
        
        var result = builder.WithSailDiff(customSettings);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.RunSailDiff.ShouldBeTrue();
        settings.SailDiffSettings.ShouldBeSameAs(customSettings);
    }

    [Fact]
    public void WithScaleFish_EnablesScaleFish_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithScaleFish();
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.RunScaleFish.ShouldBeTrue();
    }

    [Fact]
    public void TestsFromAssembliesContaining_SingleType_AddsToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.TestsFromAssembliesContaining(typeof(string));
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.TestLocationAnchors.ShouldContain(typeof(string));
    }

    [Fact]
    public void TestsFromAssembliesContaining_MultipleTypes_AddsAllToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.TestsFromAssembliesContaining(typeof(string), typeof(int), typeof(DateTime));
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.TestLocationAnchors.ShouldContain(typeof(string));
        settings.TestLocationAnchors.ShouldContain(typeof(int));
        settings.TestLocationAnchors.ShouldContain(typeof(DateTime));
    }

    [Fact]
    public void ProvidersFromAssembliesContaining_SingleType_AddsToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.ProvidersFromAssembliesContaining(typeof(string));
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.RegistrationProviderAnchors.ShouldContain(typeof(string));
    }

    [Fact]
    public void WithTag_AddsTagToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithTag("environment", "test");
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.Tags["environment"].ShouldBe("test");
    }

    [Fact]
    public void WithTag_MultipleCalls_AccumulatesTags_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        builder.WithTag("environment", "test")
               .WithTag("version", "1.0");
        
        var settings = builder.Build();
        settings.Tags["environment"].ShouldBe("test");
        settings.Tags["version"].ShouldBe("1.0");
        settings.Tags.Count.ShouldBe(2);
    }

    [Fact]
    public void WithArg_AddsArgToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithArg("config", "debug");
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.Args["config"].ShouldBe("debug");
    }

    [Fact]
    public void WithArgs_AddsAllArgsToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var args = new OrderedDictionary { { "arg1", "value1" }, { "arg2", "value2" } };
        
        var result = builder.WithArgs(args);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.Args["arg1"].ShouldBe("value1");
        settings.Args["arg2"].ShouldBe("value2");
        settings.Args.Count.ShouldBe(2);
    }

    [Fact]
    public void WithProvidedBeforeTrackingFile_AddsFileToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var trackingFile = "tracking1.json";
        
        var result = builder.WithProvidedBeforeTrackingFile(trackingFile);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.ProvidedBeforeTrackingFiles.ShouldContain(trackingFile);
    }

    [Fact]
    public void WithProvidedBeforeTrackingFiles_AddsAllFilesToCollection_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var trackingFiles = new[] { "tracking1.json", "tracking2.json" };
        
        var result = builder.WithProvidedBeforeTrackingFiles(trackingFiles);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.ProvidedBeforeTrackingFiles.ShouldContain("tracking1.json");
        settings.ProvidedBeforeTrackingFiles.ShouldContain("tracking2.json");
    }

    [Fact]
    public void WithTimeStamp_SetsTimeStamp_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var timestamp = new DateTime(2023, 1, 1, 12, 0, 0);
        
        var result = builder.WithTimeStamp(timestamp);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.TimeStamp.ShouldBe(timestamp);
    }

    [Fact]
    public void InDebugMode_True_SetsDebugTrue_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.InDebugMode(true);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.Debug.ShouldBeTrue();
    }

    [Fact]
    public void InDebugMode_False_SetsDebugFalse_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.InDebugMode(false);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.Debug.ShouldBeFalse();
    }

    [Fact]
    public void InDebugMode_DefaultParameter_SetsDebugFalse_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.InDebugMode();
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.Debug.ShouldBeFalse();
    }

    [Fact]
    public void DisableOverheadEstimation_SetsDisableOverheadEstimationTrue_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.DisableOverheadEstimation();
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.DisableOverheadEstimation.ShouldBeTrue();
    }

    [Fact]
    public void WithAnalysisDisabledGlobally_SetsDisableAnalysisGloballyTrue_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithAnalysisDisabledGlobally();
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.DisableAnalysisGlobally.ShouldBeTrue();
    }

    [Fact]
    public void WithGlobalSampleSize_PositiveValue_SetsSampleSize_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithGlobalSampleSize(50);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.SampleSizeOverride.ShouldBe(50);
    }

    [Fact]
    public void WithGlobalSampleSize_ZeroValue_SetsToOne_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithGlobalSampleSize(0);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.SampleSizeOverride.ShouldBe(1);
    }

    [Fact]
    public void WithGlobalSampleSize_NegativeValue_SetsToOne_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithGlobalSampleSize(-5);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.SampleSizeOverride.ShouldBe(1);
    }

    [Fact]
    public void WithGlobalNumWarmupIterations_PositiveValue_SetsWarmupIterations_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithGlobalNumWarmupIterations(10);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.NumWarmupIterationsOverride.ShouldBe(10);
    }

    [Fact]
    public void WithGlobalNumWarmupIterations_ZeroValue_SetsToOne_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        
        var result = builder.WithGlobalNumWarmupIterations(0);
        
        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.NumWarmupIterationsOverride.ShouldBe(1);
    }

    [Fact]
    public void WithGlobalNumWarmupIterations_NegativeValue_SetsToOne_ReturnsThis()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var result = builder.WithGlobalNumWarmupIterations(-3);

        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.NumWarmupIterationsOverride.ShouldBe(1);
    }

    // Default Value Tests
    [Fact]
    public void Build_WithoutConfiguration_UsesDefaultValues()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var settings = builder.Build();

        settings.TestNames.ShouldBeEmpty();
        settings.LocalOutputDirectory.ShouldBe(DefaultFileSettings.DefaultOutputDirectory);
        settings.CreateTrackingFiles.ShouldBeTrue(); // Default is true
        settings.RunSailDiff.ShouldBeFalse(); // Default is false
        settings.RunScaleFish.ShouldBeFalse(); // Default is false
        settings.SailDiffSettings.ShouldNotBeNull(); // Should create default instance
        settings.Tags.ShouldBeEmpty();
        settings.Args.ShouldBeEmpty();
        settings.ProvidedBeforeTrackingFiles.ShouldBeEmpty();
        settings.TestLocationAnchors.ShouldNotBeEmpty(); // Should contain builder's type
        settings.RegistrationProviderAnchors.ShouldNotBeEmpty(); // Should contain builder's type
        settings.CustomLogger.ShouldBeNull();
        settings.DisableOverheadEstimation.ShouldBeFalse();
        settings.DisableAnalysisGlobally.ShouldBeFalse();
        settings.SampleSizeOverride.ShouldBeNull();
        settings.NumWarmupIterationsOverride.ShouldBeNull();
        settings.StreamTrackingUpdates.ShouldBeTrue(); // Default is true
        settings.DisableLogging.ShouldBeFalse();
        settings.MinimumLogLevel.ShouldBe(LogLevel.Verbose); // Default
        settings.Debug.ShouldBeFalse();
        settings.TimeStamp.ShouldNotBe(default(DateTime)); // Should be set to current time
    }

    [Fact]
    public void Build_WithoutLocalOutputDirectory_UsesDefaultOutputDirectory()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var settings = builder.Build();

        settings.LocalOutputDirectory.ShouldBe(DefaultFileSettings.DefaultOutputDirectory);
    }

    [Fact]
    public void Build_WithoutSailDiffSettings_CreatesDefaultSailDiffSettings()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var settings = builder.Build();

        settings.SailDiffSettings.ShouldNotBeNull();
        settings.SailDiffSettings.ShouldBeOfType<SailDiffSettings>();
    }

    [Fact]
    public void Build_WithoutTestLocationAnchors_UsesBuilderType()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var settings = builder.Build();

        settings.TestLocationAnchors.ShouldContain(typeof(RunSettingsBuilder));
        settings.TestLocationAnchors.Count().ShouldBe(1);
    }

    [Fact]
    public void Build_WithoutRegistrationProviderAnchors_UsesBuilderType()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var settings = builder.Build();

        settings.RegistrationProviderAnchors.ShouldContain(typeof(RunSettingsBuilder));
        settings.RegistrationProviderAnchors.Count().ShouldBe(1);
    }

    [Fact]
    public void Build_WithoutMinimumLogLevel_UsesVerboseDefault()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var settings = builder.Build();

        settings.MinimumLogLevel.ShouldBe(LogLevel.Verbose);
    }

    // Edge Cases and Integration Tests
    [Fact]
    public void Build_WithAllConfigurationOptions_SetsAllValues()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var mockLogger = Substitute.For<ILogger>();
        var customSailDiffSettings = new SailDiffSettings(alpha: 0.01);
        var timestamp = new DateTime(2023, 6, 15, 10, 30, 0);

        var settings = builder
            .WithMinimumLogLevel(LogLevel.Warning)
            .WithCustomLogger(mockLogger)
            .DisableLogging()
            .DisableStreamingTrackingUpdates()
            .WithTestNames("Test1", "Test2")
            .WithLocalOutputDirectory("custom_output")
            .CreateTrackingFiles(false)
            .WithSailDiff(customSailDiffSettings)
            .WithScaleFish()
            .TestsFromAssembliesContaining(typeof(string))
            .ProvidersFromAssembliesContaining(typeof(int))
            .WithTag("env", "test")
            .WithArg("config", "debug")
            .WithProvidedBeforeTrackingFile("before.json")
            .WithTimeStamp(timestamp)
            .InDebugMode(true)
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .WithGlobalSampleSize(100)
            .WithGlobalNumWarmupIterations(5)
            .Build();

        settings.MinimumLogLevel.ShouldBe(LogLevel.Warning);
        settings.CustomLogger.ShouldBeSameAs(mockLogger);
        settings.DisableLogging.ShouldBeTrue();
        settings.StreamTrackingUpdates.ShouldBeFalse();
        settings.TestNames.ShouldContain("Test1");
        settings.TestNames.ShouldContain("Test2");
        settings.LocalOutputDirectory.ShouldBe("custom_output");
        settings.CreateTrackingFiles.ShouldBeFalse();
        settings.RunSailDiff.ShouldBeTrue();
        settings.SailDiffSettings.ShouldBeSameAs(customSailDiffSettings);
        settings.RunScaleFish.ShouldBeTrue();
        settings.TestLocationAnchors.ShouldContain(typeof(string));
        settings.RegistrationProviderAnchors.ShouldContain(typeof(int));
        settings.Tags["env"].ShouldBe("test");
        settings.Args["config"].ShouldBe("debug");
        settings.ProvidedBeforeTrackingFiles.ShouldContain("before.json");
        settings.TimeStamp.ShouldBe(timestamp);
        settings.Debug.ShouldBeTrue();
        settings.DisableOverheadEstimation.ShouldBeTrue();
        settings.DisableAnalysisGlobally.ShouldBeTrue();
        settings.SampleSizeOverride.ShouldBe(100);
        settings.NumWarmupIterationsOverride.ShouldBe(5);
    }

    [Fact]
    public void FluentInterface_AllMethods_ReturnSameInstance()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var mockLogger = Substitute.For<ILogger>();
        var customSailDiffSettings = new SailDiffSettings();
        var timestamp = DateTime.Now;

        var result = builder
            .WithMinimumLogLevel(LogLevel.Information)
            .WithCustomLogger(mockLogger)
            .DisableLogging()
            .DisableStreamingTrackingUpdates()
            .WithTestNames("Test1")
            .WithLocalOutputDirectory("output")
            .CreateTrackingFiles(true)
            .WithSailDiff()
            .WithSailDiff(customSailDiffSettings)
            .WithScaleFish()
            .TestsFromAssembliesContaining(typeof(string))
            .ProvidersFromAssembliesContaining(typeof(int))
            .WithTag("key", "value")
            .WithArg("arg", "value")
            .WithProvidedBeforeTrackingFile("file.json")
            .WithTimeStamp(timestamp)
            .InDebugMode(true)
            .DisableOverheadEstimation()
            .WithAnalysisDisabledGlobally()
            .WithGlobalSampleSize(50)
            .WithGlobalNumWarmupIterations(10);

        result.ShouldBeSameAs(builder);
    }

    // Bug Tests - Testing the WithTags method bug
    [Fact]
    public void WithTags_HasBugInImplementation_ThrowsArgumentException()
    {
        var builder = RunSettingsBuilder.CreateBuilder();
        var tags = new OrderedDictionary { { "key1", "value1" } };

        // This test documents the current bug in WithTags method
        // The method adds to the parameter 'tags' instead of 'this.tags'
        // This causes an ArgumentException because it tries to add the same key twice to the same dictionary

        var exception = Should.Throw<ArgumentException>(() => builder.WithTags(tags));
        exception.Message.ShouldContain("Item has already been added");
        exception.Message.ShouldContain("key1");
    }

    [Fact]
    public void WithCustomLogger_Null_SetsNullLogger()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var result = builder.WithCustomLogger(null!);

        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.CustomLogger.ShouldBeNull();
    }

    [Fact]
    public void WithTestNames_EmptyArray_DoesNotAddNames()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var result = builder.WithTestNames();

        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.TestNames.ShouldBeEmpty();
    }

    [Fact]
    public void TestsFromAssembliesContaining_EmptyArray_DoesNotAddTypes()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var result = builder.TestsFromAssembliesContaining();

        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        // Should still contain the default builder type
        settings.TestLocationAnchors.ShouldContain(typeof(RunSettingsBuilder));
        settings.TestLocationAnchors.Count().ShouldBe(1);
    }

    [Fact]
    public void ProvidersFromAssembliesContaining_EmptyArray_DoesNotAddTypes()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var result = builder.ProvidersFromAssembliesContaining();

        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        // Should still contain the default builder type
        settings.RegistrationProviderAnchors.ShouldContain(typeof(RunSettingsBuilder));
        settings.RegistrationProviderAnchors.Count().ShouldBe(1);
    }

    [Fact]
    public void WithProvidedBeforeTrackingFiles_EmptyCollection_DoesNotAddFiles()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var result = builder.WithProvidedBeforeTrackingFiles(new string[0]);

        result.ShouldBeSameAs(builder);
        var settings = builder.Build();
        settings.ProvidedBeforeTrackingFiles.ShouldBeEmpty();
    }
}
