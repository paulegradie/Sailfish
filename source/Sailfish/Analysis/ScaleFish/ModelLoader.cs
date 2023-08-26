using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Sailfish.Analysis.Scalefish;

public static class ModelLoader
{
    /// <summary>
    /// Method to load a file of Scalefish models into a List of TestClassComplexityResults
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static IEnumerable<ITestClassComplexityResult> LoadModelFile(string filePath)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new ComplexityFunctionConverter() },
        };

        var jsonContent = File.ReadAllText(filePath); // Read the JSON content from the file
        return JsonSerializer.Deserialize<List<TestClassComplexityResult>>(jsonContent, options) ??
               throw new SerializationException($"Failed to deserialized models in {filePath}");
    }
}