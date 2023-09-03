using PerformanceTests.ExamplePerformanceTests.Discoverable;
using Sailfish.Analysis.ScaleFish;

Console.Clear();
Console.WriteLine("\nThis demo shows you how to load a model file and then use it to make predictions on scale.");
Console.WriteLine("\nThis is V1, so over the next number of iterations, we'll add more tooling around processing these models to make it easier to make predictions.");

const string rootDir = @"C:\Users\paule\code\Sailfish\source\PerformanceTests\bin\Debug\net6.0\SailfishIDETestOutput";
var loaded = LoadAModelFile(rootDir);
var model = loaded.GetScalefishModel(nameof(AllTheFeatures), nameof(AllTheFeatures.FasterMethod), nameof(AllTheFeatures.Delay));

Console.WriteLine("Model predictions:\n");
var nValues = new List<int>() { 1, 10, 100, 1000, 50_000 };
foreach (var val in nValues)
{
    var result = model.ScaleFishModelFunction.Predict(val);
    Console.WriteLine($"f({val}) = " + Math.Round(result, 3) + " ms");
}

var scale = Math.Round(model.ScaleFishModelFunction.FunctionParameters?.Scale ?? 0, 5);
var bias = Math.Round(model.ScaleFishModelFunction.FunctionParameters?.Bias ?? 0, 8);
Console.WriteLine($"\nFitted Model: f(x) = {scale}x + {bias}");

IEnumerable<ScalefishClassModel> LoadAModelFile(string rootDir)
{
    var file = Directory.GetFiles(rootDir, "ScalefishModels*").LastOrDefault();
    if (file is null) throw new Exception("Run a Sailfish test with a variable with multiple values and enable complexity for that variable to produce a Scalefish model file");

    return ModelLoader.LoadModelFile(file);
}