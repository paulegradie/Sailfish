using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Mediation;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Exceptions;

namespace Sailfish.DefaultHandlers.SailDiff;

internal class SailDiffBeforeAndAfterFileLocationHandler : IRequestHandler<BeforeAndAfterFileLocationRequest, BeforeAndAfterFileLocationResponse>
{
    private readonly IRunSettings _runSettings;
    private readonly ITrackingFileDirectoryReader _trackingFileDirectoryReader;

    public SailDiffBeforeAndAfterFileLocationHandler(IRunSettings runSettings, ITrackingFileDirectoryReader trackingFileDirectoryReader)
    {
        _runSettings = runSettings;
        _trackingFileDirectoryReader = trackingFileDirectoryReader;
    }

    public async Task<BeforeAndAfterFileLocationResponse> Handle(
        BeforeAndAfterFileLocationRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Yield();

        // Explicit opt-in only. A before/after comparison happens solely when the caller has named the
        // 'before' tracking file(s) — via RunSettingsBuilder.WithProvidedBeforeTrackingFile(s), the
        // .sailfish.json SailDiffSettings.ProvidedBeforeTrackingFiles array, or a custom IRequestHandler for
        // this request. Sailfish deliberately does NOT reach back and auto-pick the previous run's tracking
        // file: no 'before' provided means no comparison. To compare against your previous run, resolve it
        // explicitly with TrackingFiles.MostRecentIn(...) and pass it to WithProvidedBeforeTrackingFile(...).
        var providedBeforeTrackingFiles = request.ProvidedBeforeTrackingFiles.ToList();
        if (providedBeforeTrackingFiles.Count == 0)
            return new BeforeAndAfterFileLocationResponse(new List<string>(), new List<string>());

        var missingFiles = providedBeforeTrackingFiles.Where(file => !File.Exists(file)).ToList();
        if (missingFiles.Count > 0)
            throw new SailfishException(
                $"Not all {nameof(BeforeAndAfterFileLocationRequest.ProvidedBeforeTrackingFiles)} were found. Missing: {string.Join("\n - ", missingFiles)}");

        // 'after' = the current run's freshly-written tracking file (the newest in the tracking directory).
        var trackingDirectory = _runSettings.GetRunSettingsTrackingDirectoryPath();
        var trackingFiles = Directory.Exists(trackingDirectory)
            ? _trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(trackingDirectory)
            : new List<string>();
        var afterFiles = trackingFiles.Count > 0 ? new List<string> { trackingFiles.First() } : new List<string>();

        return new BeforeAndAfterFileLocationResponse(providedBeforeTrackingFiles, afterFiles);
    }
}