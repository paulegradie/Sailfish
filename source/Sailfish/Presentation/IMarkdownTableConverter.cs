using System;
using System.Collections.Generic;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Execution;

namespace Sailfish.Presentation;

public interface IMarkdownTableConverter
{
    string ConvertToMarkdownTableString(IEnumerable<IExecutionSummary> executionSummaries);
    string ConvertToMarkdownTableString(IEnumerable<IExecutionSummary> executionSummaries, Func<IExecutionSummary, bool> summaryFilter);
    string ConvertScaleFishResultToMarkdown(IEnumerable<ITestClassComplexityResult> testClassComplexityResults);
}