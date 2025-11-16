using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Requests;

public record GetLatestExecutionSummaryResponse
{
    public GetLatestExecutionSummaryResponse(List<IClassExecutionSummary> LatestExecutionSummaries)
    {
        this.LatestExecutionSummaries = LatestExecutionSummaries;
    }

    public List<IClassExecutionSummary> LatestExecutionSummaries { get; init; }

    public void Deconstruct(out List<IClassExecutionSummary> LatestExecutionSummaries)
    {
        LatestExecutionSummaries = this.LatestExecutionSummaries;
    }
}