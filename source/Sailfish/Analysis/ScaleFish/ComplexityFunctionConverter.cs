using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Analysis.Scalefish.ComplexityFunctions;

namespace Sailfish.Analysis.Scalefish;

public class ComplexityFunctionConverter : JsonConverter<List<TestClassComplexityResult>>
{
    public override List<TestClassComplexityResult> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var results = new List<TestClassComplexityResult>();
        foreach (var testClassElement in root.EnumerateArray())
        {
            var testClassName = testClassElement.GetProperty("TestClassName").GetString();
            var testMethodComplexityResults = new List<TestMethodComplexityResult>();

            foreach (var testMethodElement in testClassElement.GetProperty("TestMethodComplexityResults").EnumerateArray())
            {
                var testMethodName = testMethodElement.GetProperty("TestMethodName").GetString();
                var testPropertyComplexityResults = new List<TestPropertyComplexityResult>();

                foreach (var testPropertyElement in testMethodElement.GetProperty("TestPropertyComplexityResults").EnumerateArray())
                {
                    var propertyName = testPropertyElement.GetProperty("PropertyName").GetString();
                    var complexityResultJsonElement = testPropertyElement.GetProperty("ComplexityResult");

                    var complexityFunctionProperty = complexityResultJsonElement.GetProperty("ComplexityFunction");
                    var complexityFunctionTypeName = complexityFunctionProperty.GetProperty("Name").GetString();
                    if (complexityFunctionTypeName is null) throw new SerializationException("Failed to find Name property");
                    var complexityFunction = DeserializeComplexityFunction(complexityFunctionTypeName, complexityFunctionProperty);
                    var goodnessOfFit = complexityResultJsonElement.GetProperty("GoodnessOfFit").GetDouble();

                    var nextBestComplexityFunctionProperty = complexityResultJsonElement.GetProperty("NextClosestComplexityFunction");
                    var nextBestComplexityFunctionTypeName = nextBestComplexityFunctionProperty.GetProperty("Name").GetString();
                    if (nextBestComplexityFunctionTypeName is null) throw new SerializationException("Failed to find Name property");
                    var nextBestComplexityFunction = DeserializeComplexityFunction(nextBestComplexityFunctionTypeName, nextBestComplexityFunctionProperty);
                    var nextBestGoodnessOfFit = complexityResultJsonElement.GetProperty("NextClosestGoodnessOfFit").GetDouble();

                    var complexityResult = new ComplexityResult(
                        complexityFunction,
                        goodnessOfFit,
                        nextBestComplexityFunction,
                        nextBestGoodnessOfFit);

                    testPropertyComplexityResults.Add(new TestPropertyComplexityResult(propertyName, complexityResult));
                }

                testMethodComplexityResults.Add(new TestMethodComplexityResult(testMethodName, testPropertyComplexityResults));
            }

            results.Add(new TestClassComplexityResult(testClassName, testMethodComplexityResults));
        }

        return results;
    }

    private static ComplexityFunction? DeserializeComplexityFunction(string complexityFunctionTypeName, JsonElement complexityFunctionProperty)
    {
        return complexityFunctionTypeName switch
        {
            (nameof(Cubic)) => complexityFunctionProperty.Deserialize<Cubic>(),
            (nameof(Exponential)) => complexityFunctionProperty.Deserialize<Exponential>(),
            (nameof(Factorial)) => complexityFunctionProperty.Deserialize<Factorial>(),
            (nameof(Linear)) => complexityFunctionProperty.Deserialize<Linear>(),
            (nameof(LogLinear)) => complexityFunctionProperty.Deserialize<LogLinear>(),
            (nameof(NLogN)) => complexityFunctionProperty.Deserialize<NLogN>(),
            (nameof(Quadratic)) => complexityFunctionProperty.Deserialize<Quadratic>(),
            (nameof(SqrtN)) => complexityFunctionProperty.Deserialize<SqrtN>(),
            _ => throw new Exception("Failed to identify type")
        };
    }

    public override void Write(Utf8JsonWriter writer, List<TestClassComplexityResult> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}