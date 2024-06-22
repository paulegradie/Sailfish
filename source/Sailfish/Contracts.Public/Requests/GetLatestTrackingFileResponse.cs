using System.Collections.Generic;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Requests;

public record GetLatestExecutionSummaryResponse(List<IClassExecutionSummary> LatestExecutionSummaries);