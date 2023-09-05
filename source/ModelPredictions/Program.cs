using Sailfish;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.SailDiff;
using PerformanceTests.ExamplePerformanceTests;


Console.Clear();
Console.WriteLine("\nThis demo shows you how run Scalefish, load a model file, and then use it to make predictions at scale");

const string outputDir = "demo_results";

// run scalefish
var res = await SailfishRunner.Run(RunSettingsBuilder.CreateBuilder()
    .WithTestNames(nameof(ScaleFishExample))
    .TestsFromAssembliesFromAnchorTypes(typeof(ScaleFishExample))
    .RegistrationProvidersFromAssembliesFromAnchorTypes(typeof(ScaleFishExample))
    .WithSailDiff(new SailDiffSettings(testType: TestType.TTest))
    .WithScalefish()
    .WithLocalOutputDirectory(outputDir)
    .Build());
if (!res.IsValid) throw new Exception("Sailfish run failed. No Scalefish models produced");

// load models
var loaded = LoadAModelFile(Path.Join(Directory.GetCurrentDirectory(), outputDir));
var model = loaded.GetScalefishModel(nameof(ScaleFishExample), nameof(ScaleFishExample.Quadratic), nameof(ScaleFishExample.N));
if (model is null) throw new Exception("Could not load model - check model name parts");


// make predictions
Console.WriteLine("Model predictions:\n");
var nValues = new List<int>() { 1, 10, 100, 1000, 50_000 };
foreach (var val in nValues)
{
    var result = model.ScaleFishModelFunction.Predict(val);
    Console.WriteLine($"f({val}) = " + Math.Round(result, 3) + " ms");
}

// print some details
Console.WriteLine($"\nFitted Model: {model.ScaleFishModelFunction}");
IEnumerable<ScalefishClassModel> LoadAModelFile(string rootDir)
{
    var file = Directory.GetFiles(rootDir, "ScalefishModels*").LastOrDefault();
    if (file is null) throw new Exception("Run a Sailfish test with a variable with multiple values and enable complexity for that variable to produce a Scalefish model file");
    return ModelLoader.LoadModelFile(file);
}