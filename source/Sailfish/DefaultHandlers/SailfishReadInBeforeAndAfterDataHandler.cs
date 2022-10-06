using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Presentation;
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
        var afterData = new List<DescriptiveStatisticsResult>();
        try
        {
            var data = await fileIo.ReadCsvFile<TestCaseDescriptiveStatisticsMap, DescriptiveStatisticsResult>(request.BeforeFilePath);
            beforeData.AddRange(data);
        }
        catch (Exception ex)
        {
            logger.Fatal("Unable to read tracking files before and after: {Message}", ex.Message);
            return new ReadInBeforeAndAfterDataResponse(null, null);
        }

        try
        {
            var data = await fileIo
                .ReadCsvFile<TestCaseDescriptiveStatisticsMap, DescriptiveStatisticsResult>(request.AfterFilePath);
            afterData.AddRange(data);
        }
        catch (Exception ex)
        {
            logger.Fatal("Unable to read tracking files before and after: {Message}", ex.Message);
            return new ReadInBeforeAndAfterDataResponse(null, null);
        }

        return new ReadInBeforeAndAfterDataResponse(new TestData(request.BeforeFilePath, beforeData), new TestData(request.AfterFilePath, afterData));
    }
}