using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Analysis;

internal interface ITrackingFileParser
{
    Task<bool> TryParse(string trackingFile, TrackingFileDataList data, CancellationToken cancellationToken);

    Task<bool> TryParseMany(IEnumerable<string> trackingFiles, TrackingFileDataList data, CancellationToken cancellationToken);
}

internal class TrackingFileParser : ITrackingFileParser
{
    private readonly ILogger logger;
    private readonly ITrackingFileSerialization trackingFileSerialization;

    public TrackingFileParser(ITrackingFileSerialization trackingFileSerialization, ILogger logger)
    {
        this.logger = logger;
        this.trackingFileSerialization = trackingFileSerialization;
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
                var deserializedFile = trackingFileSerialization.Deserialize(serialized)?.ToList();
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
        catch (SerializationException ex)
        {
            logger.Log(
                LogLevel.Warning,
                $"Failed to deserialize data into {nameof(PerformanceRunResultTrackingFormat)}. Please remove any non-V1 (or corrupt) tracking data from your tracking directory.\n\n",
                ex);
            return false;
        }
    }
}