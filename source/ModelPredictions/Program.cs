using Sailfish.Analysis.Scalefish;

Console.Clear();
Console.WriteLine("\nThis demo shows you how to load a model file and then use it to make predictions on scale.");
Console.WriteLine("\nThis is V1, so over the next number of iterations, we'll add more tooling around processing these models to make it easier to make predictions.");

var (cm, mm, pm) = 
    GetBestFitModelForFirstMethod();

Console.WriteLine($"\nModel for : {cm.TestClassName}.{mm.TestMethodName}");
Console.WriteLine($"With respect to {pm.PropertyName.Split(".").Last()}\n");

Console.WriteLine("Model Details:\n");
var function = pm.ComplexityResult.ComplexityFunction;
Console.WriteLine($"Type:    {function.Name} - that's {function.OName}...");
Console.WriteLine($"Quality: {function.Quality}\n");

Console.WriteLine("Model predictions:\n");
var nValues = new List<int>() { 1, 10, 100, 1000, 50_000 };
foreach (var val in nValues)
{
    var result = pm.ComplexityResult.ComplexityFunction.Predict(val);
    Console.WriteLine($"f({val}) = " + Math.Round(result, 3) + " ms");
}

var scale =  Math.Round(pm.ComplexityResult.ComplexityFunction.FunctionParameters.Scale, 5);
var bias = Math.Round(pm.ComplexityResult.ComplexityFunction.FunctionParameters.Bias, 8);
Console.WriteLine($"\nFitted Model: f(x) = {scale}x + {bias}");

(TestClassComplexityResult, TestMethodComplexityResult, TestPropertyComplexityResult) GetBestFitModelForFirstMethod()
{
    const string rootDir = @"C:\Users\paule\code\Sailfish\source\PerformanceTests\bin\Release\net6.0\SailfishTestOutput";
    var file = Directory.GetFiles(rootDir, "ScalefishModels*").LastOrDefault();
    if (file is null) throw new Exception("Run a Sailfish test with a variable with multiple values and enable complexity for that variable to produce a Scalefish model file");

    var models = ModelLoader.LoadModelFile(file);
    var classModel = models.First();
    var methodModel = classModel.TestMethodComplexityResults.First();
    var propertyModel = methodModel.TestPropertyComplexityResults.First();
    return ((TestClassComplexityResult, TestMethodComplexityResult, TestPropertyComplexityResult))(classModel, methodModel, propertyModel);
}