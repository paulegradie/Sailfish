using Sailfish;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.SailDiff;
using PerformanceTests.ExamplePerformanceTests;


Console.Clear();
Console.WriteLine("\nThis demo shows you how run Scalefish, load a model file, and then use it to make predictions at scale" +
                  "\nPlease wait while we generate some example results...\n");

const string outputDir = "demo_results";

// load models
var model = await LoadAModelFile(Path.Join(Directory.GetCurrentDirectory(), outputDir));
Console.Clear();

// make predictions
Console.WriteLine($"\nFitted Model: {model.ScaleFishModelFunction}");
Console.WriteLine("Model predictions:\n");
var nValues = new List<int>() { 10, 100, 1000, 50_000 };
foreach (var val in nValues)
{
    var result = model.ScaleFishModelFunction.Predict(val);
    Console.WriteLine($"f({val}) = " + Math.Round(result, 3) + " ms");
}

// print some details
async Task<ScalefishModel> LoadAModelFile(string rootDir)
{
    try
    {
        var file = Directory.GetFiles(rootDir, "ScalefishModels*").LastOrDefault();
        if (file is null) throw new Exception("Run a Sailfish test with a variable with multiple values and enable complexity for that variable to produce a Scalefish model file");
        var loaded = ModelLoader.LoadModelFile(file);
        var model = loaded.GetScalefishModel(nameof(ScaleFishExample), nameof(ScaleFishExample.Linear), nameof(ScaleFishExample.N));
        if (model is null) throw new Exception("Could not load model - is your method disabled?");
        return model;
    }
    catch
    {
        var runTask = SailfishRunner.Run(RunSettingsBuilder.CreateBuilder()
            .WithTestNames(nameof(ScaleFishExample))
            .TestsFromAssembliesContaining(typeof(ScaleFishExample))
            .ProvidersFromAssembliesContaining(typeof(ScaleFishExample))
            .WithSailDiff(new SailDiffSettings(testType: TestType.TTest))
            .WithScalefish()
            .WithLocalOutputDirectory(outputDir)
            .Build());

        Thread.Sleep(5000);
        Console.WriteLine("\nHows your day going?");
        Thread.Sleep(10000);
        Console.WriteLine("\nThis won't take too much longer...");
        Thread.Sleep(5000);
        Console.WriteLine("\nOnce this finishes, the model will load and print out some predictions...");

        var res = await runTask;
        if (!res.IsValid) throw new Exception("Sailfish run failed. No Scalefish models produced");
        return await LoadAModelFile(rootDir);
    }
}