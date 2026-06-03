using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

public class TestClassCompletedNotificationHandlerTests : IDisposable
{
    private readonly string _outputDirectory;
    private readonly string _trackingDirectory;
    private readonly IRunSettings _runSettings = Substitute.For<IRunSettings>();

    public TestClassCompletedNotificationHandlerTests()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "sailfish-test-" + Guid.NewGuid().ToString("N"));
        _trackingDirectory = Path.Combine(_outputDirectory, DefaultFileSettings.DefaultExecutionSummaryTrackingDirectory);
        Directory.CreateDirectory(_trackingDirectory);

        _runSettings.StreamTrackingUpdates.Returns(true);
        _runSettings.CreateTrackingFiles.Returns(true);
        _runSettings.LocalOutputDirectory.Returns(_outputDirectory);
        _runSettings.GetRunSettingsTrackingDirectoryPath().Returns(_trackingDirectory);
        _runSettings.TimeStamp.Returns(DateTime.UtcNow);
        _runSettings.Tags.Returns(new OrderedDictionary());
    }

    private static TestClassCompletedNotification Notification()
    {
        var summary = ClassExecutionSummaryTrackingFormatBuilder.Create().Build(); // has one successful case
        return new TestClassCompletedNotification(summary, null!, Enumerable.Empty<dynamic>());
    }

    [Fact]
    public async Task WhenSerializationFails_NoTrackingFileOrTempIsLeftBehind()
    {
        // #293: a serializer failure (e.g. reflection serialization disabled) must not leave a 0-byte /
        // partial tracking file — nor a stray temp file — behind.
        var serialization = Substitute.For<ITrackingFileSerialization>();
        serialization.Deserialize(Arg.Any<string>()).Returns((System.Collections.Generic.IEnumerable<ClassExecutionSummaryTrackingFormat>?)null);
        serialization
            .Serialize(Arg.Any<System.Collections.Generic.IEnumerable<ClassExecutionSummaryTrackingFormat>>())
            .Returns(_ => throw new InvalidOperationException("serialization disabled"));

        var handler = new TestClassCompletedNotificationHandler(serialization, _runSettings, Substitute.For<ILogger>());

        await Should.ThrowAsync<InvalidOperationException>(() => handler.Handle(Notification(), CancellationToken.None));

        Directory.GetFiles(_trackingDirectory).ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenSerializationSucceeds_WritesExactlyOneCompleteTrackingFile()
    {
        var serialization = new TrackingFileSerialization(Substitute.For<ILogger>());
        var handler = new TestClassCompletedNotificationHandler(serialization, _runSettings, Substitute.For<ILogger>());

        await handler.Handle(Notification(), CancellationToken.None);

        var files = Directory.GetFiles(_trackingDirectory);
        files.Length.ShouldBe(1);
        files.Single().ShouldEndWith(DefaultFileSettings.TrackingSuffix);
        new FileInfo(files.Single()).Length.ShouldBeGreaterThan(0);
        // No leftover temp files.
        Directory.GetFiles(_trackingDirectory, "*.tmp").ShouldBeEmpty();

        // And it round-trips back into the V1 graph.
        var roundTripped = serialization.Deserialize(await File.ReadAllTextAsync(files.Single()))?.ToList();
        roundTripped.ShouldNotBeNull();
        roundTripped!.Count.ShouldBe(1);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_outputDirectory)) Directory.Delete(_outputDirectory, recursive: true);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
