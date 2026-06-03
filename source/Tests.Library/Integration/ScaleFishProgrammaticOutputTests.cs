using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sailfish;
using Sailfish.Attributes;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.Integration;

/// <summary>
/// Issue 4 repro/guard: a benchmark with a <c>scaleFish</c> variable, run through the programmatic
/// <see cref="SailfishRunner"/> API with <c>WithScaleFish()</c>, must write ScaleFish output
/// (<c>Scalefish_*.md</c> + <c>ScalefishModels_*.json</c>) in a single run.
/// </summary>
public class ScaleFishProgrammaticOutputTests
{
    [Fact]
    public async Task ScaleFishEnabledVariable_ViaProgrammaticApi_WritesScaleFishOutput()
    {
        var outputDir = Some.RandomString();
        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(outputDir)
            .TestsFromAssembliesContaining(typeof(ScaleFishReproBenchmark))
            .WithTestNames(typeof(ScaleFishReproBenchmark).FullName!)
            .DisableOverheadEstimation()
            .WithScaleFish()
            .Build();

        var result = await SailfishRunner.Run(runSettings);

        result.IsValid.ShouldBeTrue();
        result.Exceptions.Count().ShouldBe(0);

        Directory.Exists(outputDir).ShouldBeTrue();
        var allFiles = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(outputDir, f)).ToList();
        allFiles.Any(f => Path.GetFileName(f).Contains("scalefish", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue($"expected a ScaleFish output file under '{outputDir}'; found (recursive): {string.Join(" | ", allFiles)}");
    }
}

[Sailfish(SampleSize = 3, NumWarmupIterations = 1)]
public class ScaleFishReproBenchmark
{
    [SailfishVariable(scaleFish: true, 100, 1_000, 10_000)]
    public int N { get; set; }

    [SailfishMethod]
    public void ScalesLinearlyWithN()
    {
        var acc = 0.0;
        for (var i = 0; i < N * 50; i++) acc += Math.Sqrt(i);
        // Defeat dead-code elimination so the loop is actually measured.
        if (acc < 0) throw new InvalidOperationException("unreachable");
    }
}
