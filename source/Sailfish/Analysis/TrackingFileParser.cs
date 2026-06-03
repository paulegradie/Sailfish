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
    ///     Parses each tracking file into the supplied <paramref name="data" /> list, where each element
    ///     represents one tracking file. Useful for searching prior executions for prior results.
    ///     <para>
    ///         Resilient by design: an individual file that cannot be parsed (corrupt, 0-byte/partial, or
    ///         non-V1) is logged and skipped rather than failing the whole batch, so valid runs alongside it
    ///         are still returned. Returns <c>false</c> only when every candidate file failed (and even then
    ///         <paramref name="data" /> is simply left empty — the method never throws except to propagate
    ///         cancellation).
    ///     </para>
    /// </summary>
    /// <param name="trackingFiles"></param>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><c>true</c> if at least one file parsed or there were no failures; otherwise <c>false</c>.</returns>
    public async Task<bool> TryParseMany(IEnumerable<string> trackingFiles, TrackingFileDataList data, CancellationToken cancellationToken)
    {
        var trackingFormatData = new TrackingFileDataList();
        var anyFailures = false;

        foreach (var trackingFile in trackingFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<ClassExecutionSummaryTrackingFormat>? deserializedFile;
            try
            {
                var serialized = await File.ReadAllTextAsync(trackingFile, cancellationToken).ConfigureAwait(false);
                deserializedFile = _trackingFileSerialization.Deserialize(serialized)?.ToList();
                if (deserializedFile is null) throw new SerializationException($"Failed to deserialize {trackingFile}");
            }
            catch (OperationCanceledException)
            {
                // Cancellation is a control-flow signal — let it propagate to the caller.
                throw;
            }
            catch (Exception ex)
            {
                // One unparseable file (corrupt, 0-byte/partial, non-V1, or an I/O error) must not take down
                // the whole batch. Log a single warning naming the file, skip it, and keep going so valid
                // runs alongside it are still retrieved.
                anyFailures = true;
                _logger.Log(
                    LogLevel.Warning,
                    ex,
                    "Skipping tracking file '{TrackingFile}' — it could not be parsed (corrupt, empty/partial, or non-V1). Continuing with the remaining tracking files.",
                    trackingFile);
                continue;
            }

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

        // Succeed if we parsed at least one file, or if nothing failed at all (e.g. an empty directory or
        // only valid-but-empty files). Only "every candidate file failed" reports an overall failure — and
        // even then we degrade gracefully (data is simply empty); we never throw or abort the process.
        return trackingFormatData.Count > 0 || !anyFailures;
    }
}