using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;
using Serilog;

namespace Sailfish.Analysis;

internal class TrackingFileParser : ITrackingFileParser
{
    private readonly ITrackingFileSerialization trackingFileSerialization;
    private readonly ILogger logger;

    public TrackingFileParser(ITrackingFileSerialization trackingFileSerialization, ILogger logger)
    {
        this.trackingFileSerialization = trackingFileSerialization;
        this.logger = logger;
    }

    public async Task<bool> TryParse(string trackingFile, TrackingFileDataList data, CancellationToken cancellationToken)
    {
        return await TryParse(new List<string>() { trackingFile }, data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a list of deserialized IExecutionSummaries, where each element represents a tracking file. Useful for searching prior executions for prior results. 
    /// </summary>
    /// <param name="trackingFiles"></param>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SerializationException"></exception>
    public async Task<bool> TryParse(IEnumerable<string> trackingFiles, TrackingFileDataList data, CancellationToken cancellationToken)
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
                    trackingFormatData.Add(deserializedFile.ToSummaryFormat().ToList()); // only add if all files present succeed
                }
                catch (ArgumentException)
                {
                    // failed to deserialize the file, but continue trying files
                }
            }

            data.AddRange(trackingFormatData);
            return true;
        }
        catch (SerializationException ex)
        {
            logger.Warning(ex,
                $"Failed to deserialize data into {nameof(PerformanceRunResultTrackingFormat)}. Please remove any non-V1 (or corrupt) tracking data from your tracking directory.\n\n");
            return false;
        }
    }
}