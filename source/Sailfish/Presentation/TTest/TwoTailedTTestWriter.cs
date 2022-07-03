using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sailfish.ExtensionMethods;
using Sailfish.Presentation.Console;
using Sailfish.Statistics.StatisticalAnalysis;
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

    public async Task<string> ComputeAndConvertToStringContent(BeforeAndAfterTrackingFiles beforeAndAfter, TTestSettings settings)
    {
        await Task.CompletedTask;

        var results = tTestComputer.ComputeTTest(beforeAndAfter, settings);
        if (results.Count == 0)
        {
            logger.Information("No prior test results found for the current set");
            return string.Empty;
        }

        var table = results.ToStringTable(
            new List<string>() { "", "ms", "ms", "", "", "", "" },
            m => m.TestName,
            m => m.MeanOfBefore,
            m => m.MeanOfAfter,
            m => m.PValue,
            m => m.DegreesOfFreedom,
            m => m.TStatistic,
            m => m.ChangeDescription
        );
        PrintHeader(
            Path.GetFileName(beforeAndAfter.BeforeFilePath),
            Path.GetFileName(beforeAndAfter.AfterFilePath),
            settings.Alpha);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(table);

        return stringBuilder.Build();
    }

    private void PrintHeader(string beforeId, string afterId, double alpha)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"T-Test results comparing:");
        stringBuilder.AppendLine($"Before: {beforeId}");
        stringBuilder.AppendLine($"After: {afterId}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: The change in execution time is significant if the PValue is less than {alpha}");
    }
}