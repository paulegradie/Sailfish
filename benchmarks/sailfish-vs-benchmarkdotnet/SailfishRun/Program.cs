using System.Globalization;
using System.Text;
using Sailfish;
using Sailfish.Logging;
using ToolCompare;

var outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "compare-output");
Directory.CreateDirectory(outDir);

Console.WriteLine("=== Running Sailfish suite ===");
var settings = RunSettingsBuilder
    .CreateBuilder()
    .TestsFromAssembliesContaining(typeof(SailfishSuite))
    .WithTestNames(typeof(SailfishSuite).FullName!)
    .CreateTrackingFiles(false)
    .WithMinimumLogLevel(LogLevel.Warning)
    .WithLocalOutputDirectory(Path.Combine(outDir, "sailfish"))
    .Build();

var result = await SailfishRunner.Run(settings);
if (!result.IsValid)
{
    foreach (var ex in result.Exceptions ?? []) Console.WriteLine(ex);
    return 1;
}

var csv = new StringBuilder();
csv.AppendLine("tool,workload,sample_index,value_ns");
foreach (var summary in result.ExecutionSummaries)
foreach (var tc in summary.GetSuccessfulTestCases())
{
    var perf = tc.PerformanceRunResult!;
    var name = tc.TestCaseId!.TestCaseName.Name.Split('.').Last().TrimEnd('(', ')');
    for (var i = 0; i < perf.RawExecutionResults.Length; i++)
    {
        var ns = perf.RawExecutionResults[i] * 1_000_000.0; // ms -> ns
        csv.AppendLine($"Sailfish,{name},{i},{ns.ToString("G17", CultureInfo.InvariantCulture)}");
    }

    Console.WriteLine($"Sailfish {name}: n={perf.RawExecutionResults.Length} mean={perf.Mean:F4}ms median={perf.Median:F4}ms");
}

var csvPath = Path.Combine(outDir, "samples_sailfish.csv");
File.WriteAllText(csvPath, csv.ToString());
Console.WriteLine($"Wrote {csvPath}");
return 0;
