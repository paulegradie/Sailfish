using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Public.CsvMaps;
using Serilog;

namespace Sailfish.DefaultHandlers;

public class SailfishReadInBeforeAndAfterDataHandler : IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>
{
    private readonly IFileIo fileIo;
    private readonly ILogger logger;

    public SailfishReadInBeforeAndAfterDataHandler(IFileIo fileIo, ILogger logger)
    {
        this.fileIo = fileIo;
        this.logger = logger;
    }

    public async Task<ReadInBeforeAndAfterDataResponse> Handle(ReadInBeforeAndAfterDataCommand request, CancellationToken cancellationToken)
    {
        var beforeData = new List<DescriptiveStatisticsResult>();
        if (!await TryAddDataFile(request.BeforeFilePath, beforeData)) return new ReadInBeforeAndAfterDataResponse(null, null);

        var afterData = new List<DescriptiveStatisticsResult>();
        if (!await TryAddDataFile(request.AfterFilePath, afterData)) return new ReadInBeforeAndAfterDataResponse(null, null);


        return new ReadInBeforeAndAfterDataResponse(new TestData(request.BeforeFilePath, beforeData), new TestData(request.AfterFilePath, afterData));
    }

    private async Task<bool> TryAddDataFile(IEnumerable<string> fileKeys, List<DescriptiveStatisticsResult> data)
    {
        var temp = new List<DescriptiveStatisticsResult>();
        try
        {
            foreach (var key in fileKeys)
            {
                var datum = await fileIo.ReadCsvFile<TestCaseDescriptiveStatisticsMap, DescriptiveStatisticsResult>(key);
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