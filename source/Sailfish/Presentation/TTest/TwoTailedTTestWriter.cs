using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.ExtensionMethods;
using Sailfish.Presentation.Console;
using Serilog;

namespace Sailfish.Presentation.TTest;

internal class TwoTailedTTestWriter : ITwoTailedTTestWriter
{
    private readonly ITTestComputer tTestComputer;
    private readonly ILogger logger;
    private readonly IPresentationStringConstructor stringBuilder;

    public TwoTailedTTestWriter(
        ITTestComputer tTestComputer,
        ILogger logger,
        IPresentationStringConstructor stringBuilder)
    {
        this.tTestComputer = tTestComputer;
        this.logger = logger;
        this.stringBuilder = stringBuilder;
    }

    public Task<TestResultFormats> ComputeAndConvertToStringContent(TestData beforeTestData, TestData afterTestData, TestSettings settings, CancellationToken cancellationToken)
    {
        var testIds = new TestIds(beforeTestData.TestId, afterTestData.TestId);
        var results = tTestComputer.ComputeTTest(beforeTestData, afterTestData, settings);
        if (results.Count == 0)
        {
            logger.Information("No prior test results found for the current set");
            return Task.FromResult(new TestResultFormats("", new List<NamedTTestResult>(), testIds));
        }

        var table = results.ToStringTable(
            new List<string>() { "", "ms", "ms", "", "", "", "" },
            m => m.DisplayName,
            m => m.MeanOfBefore,
            m => m.MeanOfAfter,
            m => m.PValue,
            m => m.DegreesOfFreedom,
            m => m.TStatistic,
            m => m.ChangeDescription
        );
        PrintHeader(
            testIds.BeforeTestIds,
            testIds.AfterTestIds,
            settings.Alpha);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(table);

        return Task.FromResult(new TestResultFormats(stringBuilder.Build(), results, testIds));
    }

    private void PrintHeader(IEnumerable<string> beforeIds, IEnumerable<string> afterIds, double alpha)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"T-Test results comparing:");
        stringBuilder.AppendLine($"Before: {string.Join(", ", beforeIds)}");
        stringBuilder.AppendLine($"After: {string.Join(", ", afterIds)}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: The change in execution time is significant if the PValue is less than {alpha}");
    }
}