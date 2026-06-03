using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish.Analysis;

internal interface ITrackingFileParser
{
    Task<bool> TryParse(string trackingFile, TrackingFileDataList data, CancellationToken cancellationToken);

    Task<bool> TryParseMany(IEnumerable<string> trackingFiles, TrackingFileDataList data, CancellationToken cancellationToken);
}

internal class TrackingFileParser : ITrackingFileParser
{
    private readonly ILogger _logger;
    private readonly ITrackingFileSerialization _trackingFileSerialization;

    public TrackingFileParser(ITrackingFileSerialization trackingFileSerialization, ILogger logger)
    {
        _logger = logger;
        _trackingFileSerialization = trackingFileSerialization;
    }

    public async Task<bool> TryParse(string trackingFile, TrackingFileDataList data, CancellationToken cancellationToken)
    {
        return await TryParseMany(new List<string>
        {
            trackingFile
        }, data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns a list of deserialized IExecutionSummaries, where each element represents a tracking file. Useful for
    ///     searching prior executions for prior results.
    /// </summary>
    /// <param name="trackingFiles"></param>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SerializationException"></exception>
    public async Task<bool> TryParseMany(IEnumerable<string> trackingFiles, TrackingFileDataList data, CancellationToken cancellationToken)
    {
        var trackingFormatData = new TrackingFileDataList();
        try
        {
            foreach (var trackingFile in trackingFiles)
            {
                var serialized = await File.ReadAllTextAsync(trackingFile, cancellationToken);
                var deserializedFile = _trackingFileSerialization.Deserialize(serialized)?.ToList();
                if (deserializedFile is null) throw new SerializationException($"Failed to deserialize {trackingFile}");
                if (!deserializedFile.Any()) continue;
                try
                {
                    trackingFormatData.Add(deserializedFile.ToSummaryFormat().ToList());
                }
                catch (ArgumentException)
                {
                    // failed to convert all test cases to summary format
                }
            }

            data.AddRange(trackingFormatData);
            return true;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is a control-flow signal — let it propagate to the caller.
            throw;
        }
        catch (Exception ex)
        {
            // Genuinely non-throwing: any failure (corrupt/non-V1 data, an I/O error, or a serializer that
            // throws something other than SerializationException) is reported as a parse failure rather than
            // propagated. The post-measurement error boundary in SailfishExecutor relies on this so a bad
            // tracking file can never crash the run. (#294 refines this to per-file resilience.)
            _logger.Log(
                LogLevel.Warning,
                ex,
                "Failed to deserialize data into {TrackingFormat}. Please remove any non-V1 (or corrupt) tracking data from your tracking directory.",
                nameof(PerformanceRunResultTrackingFormat));
            return false;
        }
    }
}