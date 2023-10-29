using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.Markdown;

internal interface IMarkdownWriter
{
    Task Write(IEnumerable<IClassExecutionSummary> result, string filePath, IRunSettings settings, CancellationToken cancellationToken);
}