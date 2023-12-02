using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Sailfish.Analysis;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;

namespace Sailfish.Contracts.Public;

public interface ISailDiffResultMarkdownConverter
{
    string ConvertToMarkdownTable(IEnumerable<SailDiffResult> testCaseResults, TestIds testIds, CancellationToken cancellationToken);
}

public class SailDiffResultMarkdownConverter : ISailDiffResultMarkdownConverter
{
    public string ConvertToMarkdownTable(IEnumerable<SailDiffResult> testCaseResults, TestIds testIds, CancellationToken cancellationToken)
    {
        var enumeratedResults = testCaseResults.ToList();
        var nBefore = enumeratedResults.Select(x => x.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore).Distinct().Single();
        var nAfter = enumeratedResults.Select(x => x.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeAfter).Distinct().Single();

        var selectors = new List<Expression<Func<SailDiffResult, object>>>
        {
            m => m.TestCaseId.DisplayName,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanBefore,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanAfter,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianBefore,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianAfter,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.ChangeDescription,
        };

        var headers = new List<string>()
        {
            "Display Name", $"MeanBefore (N={nBefore})", $"MeanAfter (N={nAfter})", "MedianBefore", "MedianAfter", "PValue", "Change Description"
        };
        var columnValueSuffixes = new List<string>()
        {
            "", "ms", "ms", "ms", "ms", "", ""
        };

        if (enumeratedResults.Any(x => !string.IsNullOrEmpty(x.TestResultsWithOutlierAnalysis.ExceptionMessage)))
        {
            selectors.Add(m => m.TestResultsWithOutlierAnalysis.ExceptionMessage);
            headers.Add("Exception");
            columnValueSuffixes.Add("");
        }

        return enumeratedResults.ToStringTable(
            columnValueSuffixes,
            headers,
            selectors.ToArray());
    }
}