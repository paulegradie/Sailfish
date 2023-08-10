using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Serilog;

namespace Sailfish.DefaultHandlers;

internal class SailfishReadInBeforeAndAfterDataHandler : IRequestHandler<ReadInBeforeAndAfterDataCommand, ReadInBeforeAndAfterDataResponse>
{
    private readonly ITrackingFileParser trackingFileParser;
    private readonly ILogger logger;

    public SailfishReadInBeforeAndAfterDataHandler(ITrackingFileParser trackingFileParser, ILogger logger)
    {
        this.trackingFileParser = trackingFileParser;
        this.logger = logger;
    }

    public async Task<ReadInBeforeAndAfterDataResponse> Handle(ReadInBeforeAndAfterDataCommand request, CancellationToken cancellationToken)
    {
        var beforeData = new List<DescriptiveStatisticsResult>();
        if (!await trackingFileParser.TryParse(request.BeforeFilePaths, beforeData, cancellationToken).ConfigureAwait(false))
            return new ReadInBeforeAndAfterDataResponse(null, null);

        var afterData = new List<DescriptiveStatisticsResult>();
        if (!await trackingFileParser.TryParse(request.AfterFilePaths, afterData, cancellationToken).ConfigureAwait(false)) return new ReadInBeforeAndAfterDataResponse(null, null);

        if (!beforeData.Any() || !afterData.Any()) return new ReadInBeforeAndAfterDataResponse(null, null);

        return new ReadInBeforeAndAfterDataResponse(new TestData(request.BeforeFilePaths, beforeData), new TestData(request.AfterFilePaths, afterData));
    }
}