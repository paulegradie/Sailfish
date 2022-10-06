using System.Collections.Generic;
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

    public async Task<TTestResultFormats> ComputeAndConvertToStringContent(TestData beforeTestData, TestData afterTestData, TTestSettings settings)
    {
        await Task.CompletedTask;

        var results = tTestComputer.ComputeTTest(beforeTestData, afterTestData, settings);
        if (results.Count == 0)
        {
            logger.Information("No prior test results found for the current set");
            return new TTestResultFormats("", new List<NamedTTestResult>());
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
            beforeTestData.TestId,
            afterTestData.TestId,
            settings.Alpha);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(table);

        return new TTestResultFormats(stringBuilder.Build(), results);
    }

    private void PrintHeader(IEnumerable<string> beforeId, IEnumerable<string> afterId, double alpha)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"T-Test results comparing:");
        stringBuilder.AppendLine($"Before: {string.Join(", ", beforeId)}");
        stringBuilder.AppendLine($"After: {string.Join(", ", afterId)}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: The change in execution time is significant if the PValue is less than {alpha}");
    }
}