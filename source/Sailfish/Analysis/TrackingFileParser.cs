using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.CsvMaps;
using Serilog;

namespace Sailfish.Analysis;

internal class TrackingFileParser : ITrackingFileParser
{
    private readonly IFileIo fileIo;
    private readonly ILogger logger;

    public TrackingFileParser(IFileIo fileIo, ILogger logger)
    {
        this.fileIo = fileIo;
        this.logger = logger;
    }

    public async Task<bool> TryParse(string fileKey, List<DescriptiveStatisticsResult> data, CancellationToken cancellationToken)
    {
        return await TryParse(new List<string>() { fileKey }, data, cancellationToken).ConfigureAwait(false);
    }


    public async Task<bool> TryParse(IEnumerable<string> fileKeys, List<DescriptiveStatisticsResult> data, CancellationToken cancellationToken)
    {
        var temp = new List<DescriptiveStatisticsResult>();
        try
        {
            foreach (var key in fileKeys)
            {
                var datum = await fileIo.ReadCsvFile<DescriptiveStatisticsResultCsvMap, DescriptiveStatisticsResult>(key, cancellationToken).ConfigureAwait(false);
                foreach (var d in datum)
                {
                    d.SetNumIterations(d.RawExecutionResults.Length);
                }

                temp.AddRange(datum);
            }

            data.AddRange(temp); // only add if all succeed
            return true;
        }
        catch (Exception ex)
        {
            logger.Fatal("Unable to read tracking files for after: {Message}", ex.Message);
            return false;
        }
    }
}