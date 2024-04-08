using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Contracts.Public.Serialization.JsonConverters;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.ScaleFish;

public class ComplexityFunctionConverter : JsonConverter<List<ScalefishClassModel>>
{
    public override List<ScalefishClassModel> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var results = new List<ScalefishClassModel>();
        foreach (var testClassElement in root.EnumerateArray())
        {
            var testNameSpace = testClassElement.GetProperty(nameof(ScalefishClassModel.NameSpace)).GetString() ??
                                throw new SailfishException($"Failed to find '{nameof(ScalefishClassModel.NameSpace)}'");
            var testClassName = testClassElement.GetProperty(nameof(ScalefishClassModel.TestClassName)).GetString() ??
                                throw new SailfishException($"Failed to find property: '{nameof(ScalefishClassModel.TestClassName)}'");
            var testMethodComplexityResults = new List<ScaleFishMethodModel>();

            foreach (var testMethodElement in testClassElement.GetProperty(nameof(ScalefishClassModel.ScaleFishMethodModels)).EnumerateArray())
            {
                var testMethodName = testMethodElement.GetProperty(nameof(ScaleFishMethodModel.TestMethodName)).GetString() ??
                                     throw new SailfishException($"Failed to find property '{nameof(ScaleFishMethodModel.TestMethodName)}'");

                var testPropertyComplexityResults = new List<ScaleFishPropertyModel>();

                foreach (var testPropertyElement in testMethodElement.GetProperty(nameof(ScaleFishMethodModel.ScaleFishPropertyModels)).EnumerateArray())
                {
                    var propertyName = testPropertyElement.GetProperty(nameof(ScaleFishPropertyModel.PropertyName)).GetString() ??
                                       throw new SailfishException($"Failed to find property '{nameof(ScaleFishPropertyModel.PropertyName)}'");
                    var complexityResultJsonElement = testPropertyElement.GetProperty(nameof(ScaleFishPropertyModel.ScaleFishModel));

                    var complexityFunctionProperty = complexityResultJsonElement.GetProperty(nameof(ScaleFishModel.ScaleFishModelFunction));
                    var complexityFunctionTypeName = complexityFunctionProperty.GetProperty(nameof(ScaleFishModelFunction.Name)).GetString();
                    if (complexityFunctionTypeName is null) throw new SerializationException($"Failed to find {nameof(ScaleFishModelFunction.Name)} property");
                    var complexityFunction = DeserializeComplexityFunction(complexityFunctionTypeName, complexityFunctionProperty);
                    var goodnessOfFit = complexityResultJsonElement.GetProperty(nameof(ScaleFishModel.GoodnessOfFit)).GetDouble();

                    var nextBestComplexityFunctionProperty = complexityResultJsonElement.GetProperty(nameof(ScaleFishModel.NextClosestScaleFishModelFunction));
                    var nextBestComplexityFunctionTypeName = nextBestComplexityFunctionProperty.GetProperty(nameof(ScaleFishModelFunction.Name)).GetString();
                    if (nextBestComplexityFunctionTypeName is null) throw new SerializationException($"Failed to find {nameof(ScaleFishModelFunction.Name)} property");
                    var nextBestComplexityFunction = DeserializeComplexityFunction(nextBestComplexityFunctionTypeName, nextBestComplexityFunctionProperty);
                    var nextBestGoodnessOfFit = complexityResultJsonElement.GetProperty(nameof(ScaleFishModel.NextClosestGoodnessOfFit)).GetDouble();

                    var complexityResult = new ScaleFishModel(
                        complexityFunction,
                        goodnessOfFit,
                        nextBestComplexityFunction,
                        nextBestGoodnessOfFit);

                    testPropertyComplexityResults.Add(new ScaleFishPropertyModel(propertyName, complexityResult));
                }

                testMethodComplexityResults.Add(new ScaleFishMethodModel(testMethodName, testPropertyComplexityResults));
            }

            results.Add(new ScalefishClassModel(testNameSpace, testClassName, testMethodComplexityResults));
        }

        return results;
    }

    public override void Write(Utf8JsonWriter writer, List<ScalefishClassModel> value, JsonSerializerOptions options)
    {
        var opts = new JsonSerializerOptions();
        var converters = new List<JsonConverter>
        {
            new JsonNanConverter(),
            new ExecutionSummaryTrackingFormatConverter(),
            new TypePropertyConverter()
        };
        foreach (var converter in converters) opts.Converters.Add(converter);

        JsonSerializer.Serialize(writer, value, opts);
    }

    private static ScaleFishModelFunction DeserializeComplexityFunction(string complexityFunctionTypeName, JsonElement complexityFunctionProperty)
    {
        ScaleFishModelFunction? deserialized = complexityFunctionTypeName switch
        {
            nameof(Cubic) => complexityFunctionProperty.Deserialize<Cubic>(),
            nameof(Exponential) => complexityFunctionProperty.Deserialize<Exponential>(),
            nameof(Factorial) => complexityFunctionProperty.Deserialize<Factorial>(),
            nameof(Linear) => complexityFunctionProperty.Deserialize<Linear>(),
            nameof(LogLinear) => complexityFunctionProperty.Deserialize<LogLinear>(),
            nameof(NLogN) => complexityFunctionProperty.Deserialize<NLogN>(),
            nameof(Quadratic) => complexityFunctionProperty.Deserialize<Quadratic>(),
            nameof(SqrtN) => complexityFunctionProperty.Deserialize<SqrtN>(),
            _ => throw new SailfishException($"Failed to identify complexity function type: {complexityFunctionTypeName}")
        };

        if (deserialized is null) throw new SerializationException($"Failed to deserialize {complexityFunctionTypeName}");
        return deserialized;
    }
}