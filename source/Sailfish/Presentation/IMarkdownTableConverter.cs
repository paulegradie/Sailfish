using System;
using System.Collections.Generic;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Execution;

namespace Sailfish.Presentation;

public interface IMarkdownTableConverter
{
    string ConvertToMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries);
    string ConvertToMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries, Func<IClassExecutionSummary, bool> summaryFilter);
    string ConvertScaleFishResultToMarkdown(IEnumerable<IScalefishClassModels> testClassComplexityResults);
}