using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;
using Sailfish.Presentation;

namespace Sailfish.Analysis.ScaleFish;

internal interface IScaleFishInternal : IAnalyzeFromFile
{
    /// <summary>
    ///     Analyze the current run's in-memory execution summaries directly. ScaleFish is a single-run
    ///     complexity analysis, so it must not be gated by the (SailDiff-shared) tracking-file retrieval or
    ///     by the presence of a baseline — and analyzing the in-memory summaries (which carry the real
    ///     <see cref="System.Type" /> for each test class) also avoids the file round-trip's
    ///     <c>Type.GetType</c> resolution failures that produced an <c>ArgumentNullException("key")</c>.
    /// </summary>
    Task Analyze(IEnumerable<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken);
}
