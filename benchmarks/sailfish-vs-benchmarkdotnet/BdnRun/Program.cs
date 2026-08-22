using System.Globalization;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Running;
using ToolCompare;

var outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "compare-output");
Directory.CreateDirectory(outDir);

Console.WriteLine("=== Running BenchmarkDotNet suite ===");
var summary = BenchmarkRunner.Run<BdnSuite>();
var fullJson = Directory
    .GetFiles(summary.ResultsDirectoryPath, "*-report-full.json")
    .Single();
Console.WriteLine($"Parsing {fullJson}");

var csv = new StringBuilder();
csv.AppendLine("tool,workload,sample_index,value_ns");
using var doc = JsonDocument.Parse(File.ReadAllText(fullJson));
foreach (var bench in doc.RootElement.GetProperty("Benchmarks").EnumerateArray())
{
    var method = bench.GetProperty("Method").GetString()!;
    // Job id is not a top-level property in the full JSON; it's the suffix of DisplayInfo ("Class.Method: JobId").
    var jobId = bench.GetProperty("DisplayInfo").GetString()!.Split(": ")[^1];
    var idx = 0;
    foreach (var m in bench.GetProperty("Measurements").EnumerateArray())
    {
        if (m.GetProperty("IterationMode").GetString() != "Workload") continue;
        if (m.GetProperty("IterationStage").GetString() != "Actual") continue;
        var perOpNs = m.GetProperty("Nanoseconds").GetDouble() / m.GetProperty("Operations").GetInt64();
        csv.AppendLine($"{jobId},{method},{idx++},{perOpNs.ToString("G17", CultureInfo.InvariantCulture)}");
    }

    Console.WriteLine($"{jobId} {method}: n={idx}");
}

var csvPath = Path.Combine(outDir, "samples_bdn.csv");
File.WriteAllText(csvPath, csv.ToString());
Console.WriteLine($"Wrote {csvPath}");
return 0;
