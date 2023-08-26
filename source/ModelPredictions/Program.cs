using Sailfish.Analysis.Scalefish;

const string rootDir = @"C:\Users\paule\code\Sailfish\source\PerformanceTests\bin\Release\net6.0\SailfishTestOutput";
var file = Directory.GetFiles(rootDir, "ScalefishModels*").LastOrDefault();

if (file is null) throw new Exception("Run a Sailfish test with a variable with multiple values and enable complexity for that variable to produce a Scalefish model file");

var models = ModelLoader.LoadModelFile(file);

var someComplexityModel = models.First();
Console.WriteLine("Models for class: " + someComplexityModel.TestClassName);

var modelForSomeMethodInTheClass = someComplexityModel.TestMethodComplexityResults.First();
Console.WriteLine("Models for method: " + modelForSomeMethodInTheClass.TestMethodName);

var bestFitModelForMethod = modelForSomeMethodInTheClass.TestPropertyComplexityResults.First();
Console.WriteLine("Model with respect to: " + bestFitModelForMethod.PropertyName);
var nValues = Enumerable.Range(1, 100).ToList();


Console.WriteLine(
    $"Predictions for {someComplexityModel.TestClassName}.{modelForSomeMethodInTheClass.TestMethodName}.{bestFitModelForMethod.PropertyName}:");

foreach (var val in nValues)
{
    var result = bestFitModelForMethod.ComplexityResult.ComplexityFunction.Predict(val);
    Console.WriteLine($"f({val}) = " + Math.Round(result, 3));
}

Console.WriteLine("AT 50K: " + Math.Round(bestFitModelForMethod.ComplexityResult.ComplexityFunction.Predict(50_000)));