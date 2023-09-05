using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Commands;

public class SailfishGetLatestExecutionSummaryResponse
{
    public SailfishGetLatestExecutionSummaryResponse(List<IClassExecutionSummary> latestExecutionSummaries) => LatestExecutionSummaries = latestExecutionSummaries;
    public List<IClassExecutionSummary> LatestExecutionSummaries { get; set; }
}