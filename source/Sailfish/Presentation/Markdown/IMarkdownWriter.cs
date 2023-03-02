using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.Markdown;

internal interface IMarkdownWriter
{
    Task Present(IEnumerable<IExecutionSummary> result, string filePath, IRunSettings settings, CancellationToken cancellationToken);
}