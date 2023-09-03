using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Commands;

public class SailfishGetLatestExecutionSummaryResponse
{
    public SailfishGetLatestExecutionSummaryResponse(List<IExecutionSummary> latestExecutionSummaries) => LatestExecutionSummaries = latestExecutionSummaries;
    public List<IExecutionSummary> LatestExecutionSummaries { get; set; }
}