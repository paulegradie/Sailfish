using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Markdown;

internal interface IMarkdownWriter
{
    Task Present(List<ExecutionSummary> result, string filePath, CancellationToken cancellationToken);
}