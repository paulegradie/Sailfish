using Sailfish.Execution;
using System.Collections.Generic;

namespace Sailfish.Contracts.Public.Requests;

public class GetLatestExecutionSummaryResponse(List<IClassExecutionSummary> latestExecutionSummaries)
{
    public List<IClassExecutionSummary> LatestExecutionSummaries { get; set; } = latestExecutionSummaries;
}