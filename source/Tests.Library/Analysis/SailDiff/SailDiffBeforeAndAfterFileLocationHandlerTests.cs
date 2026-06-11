using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.DefaultHandlers.SailDiff;
using Sailfish.Exceptions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class SailDiffBeforeAndAfterFileLocationHandlerTests
{
    private readonly IRunSettings _runSettings = Substitute.For<IRunSettings>();
    private readonly ITrackingFileDirectoryReader _reader = Substitute.For<ITrackingFileDirectoryReader>();

    private SailDiffBeforeAndAfterFileLocationHandler CreateSut() => new(_runSettings, _reader);

    [Fact]
    public async Task NoProvidedBeforeFiles_DoesNotAutoPickPreviousRun_ReturnsEmpty()
    {
        // The core of this change: even with multiple tracking files on disk, nothing is auto-selected.
        _runSettings.GetRunSettingsTrackingDirectoryPath().Returns("/some/tracking/dir");
        _reader.FindTrackingFilesInDirectoryOrderedByLastModified(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(new List<string> { "newest.json.tracking", "previous.json.tracking" });

        var response = await CreateSut().Handle(
            new BeforeAndAfterFileLocationRequest(new List<string>()),
            CancellationToken.None);

        response.BeforeFilePaths.ShouldBeEmpty();
        response.AfterFilePaths.ShouldBeEmpty();
        // No reach-back into history whatsoever.
        _reader.DidNotReceive().FindTrackingFilesInDirectoryOrderedByLastModified(Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task ProvidedBeforeFileThatExists_BeforeIsProvided_AfterIsNewestTrackingFile()
    {
        var beforeFile = Path.GetTempFileName();
        var trackingDir = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "sf_handler_" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            _runSettings.GetRunSettingsTrackingDirectoryPath().Returns(trackingDir);
            _reader.FindTrackingFilesInDirectoryOrderedByLastModified(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(new List<string> { "current_run.json.tracking", "older.json.tracking" });

            var response = await CreateSut().Handle(
                new BeforeAndAfterFileLocationRequest(new List<string> { beforeFile }),
                CancellationToken.None);

            response.BeforeFilePaths.ShouldBe(new[] { beforeFile });
            response.AfterFilePaths.ShouldBe(new[] { "current_run.json.tracking" });
        }
        finally
        {
            File.Delete(beforeFile);
            Directory.Delete(trackingDir, true);
        }
    }

    [Fact]
    public async Task ProvidedBeforeFileMissing_Throws()
    {
        _runSettings.GetRunSettingsTrackingDirectoryPath().Returns("/some/tracking/dir");
        var missing = Path.Combine(Path.GetTempPath(), "definitely_missing_" + Guid.NewGuid().ToString("N") + ".json.tracking");

        var sut = CreateSut();

        await Should.ThrowAsync<SailfishException>(async () =>
            await sut.Handle(new BeforeAndAfterFileLocationRequest(new List<string> { missing }), CancellationToken.None));
    }

    [Fact]
    public async Task ProvidedBeforeFile_NoTrackingDirectory_ReturnsBeforeWithEmptyAfter()
    {
        var beforeFile = Path.GetTempFileName();
        var nonexistentDir = Path.Combine(Path.GetTempPath(), "sf_missing_" + Guid.NewGuid().ToString("N"));
        try
        {
            _runSettings.GetRunSettingsTrackingDirectoryPath().Returns(nonexistentDir);

            var response = await CreateSut().Handle(
                new BeforeAndAfterFileLocationRequest(new List<string> { beforeFile }),
                CancellationToken.None);

            response.BeforeFilePaths.ShouldBe(new[] { beforeFile });
            response.AfterFilePaths.ShouldBeEmpty();
            // Missing directory is handled gracefully, not by crashing in the reader.
            _reader.DidNotReceive().FindTrackingFilesInDirectoryOrderedByLastModified(Arg.Any<string>(), Arg.Any<bool>());
        }
        finally
        {
            File.Delete(beforeFile);
        }
    }
}
