using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Requests;

public class GetLatestExecutionSummaryResponse(List<IClassExecutionSummary> latestExecutionSummaries)
{
    public List<IClassExecutionSummary> LatestExecutionSummaries { get; set; } = latestExecutionSummaries;
}