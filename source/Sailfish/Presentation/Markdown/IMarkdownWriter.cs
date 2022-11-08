using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.Markdown;

internal interface IMarkdownWriter
{
    Task Present(List<ExecutionSummary> result, string filePath, RunSettings settings, CancellationToken cancellationToken);
}