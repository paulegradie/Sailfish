using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Sailfish.Analysis.ScaleFish;

public static class ModelLoader
{
    /// <summary>
    /// Method to load a file of Scalefish models into a List of TestClassComplexityResults
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static IEnumerable<ScalefishClassModel> LoadModelFile(string filePath)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new ComplexityFunctionConverter() },
        };

        var jsonContent = File.ReadAllText(filePath); // Read the JSON content from the file
        return JsonSerializer.Deserialize<List<ScalefishClassModel>>(jsonContent, options) ??
               throw new SerializationException($"Failed to deserialized models in {filePath}");
    }

    public static ScalefishClassModel? GetModelsForTestClass(this IEnumerable<ScalefishClassModel> testClassComplexityResults, string testClass)
    {
        return testClassComplexityResults.SingleOrDefault(x => x.TestClassName == testClass);
    }

    public static ScalefishClassModel? GetModelsForTestClass(this IEnumerable<ScalefishClassModel> testClassComplexityResults, Type testClass)
    {
        return GetModelsForTestClass(testClassComplexityResults, testClass.Name);
    }

    public static ScaleFishMethodModel? GetModelsForMethod(this IEnumerable<ScaleFishMethodModel> testMethodComplexityResults, string testMethod)
    {
        return testMethodComplexityResults.SingleOrDefault(x => x.TestMethodName == testMethod);
    }

    public static ScaleFishMethodModel? GetModelsForMethod(this IEnumerable<ScaleFishMethodModel> testMethodComplexityResults, MemberInfo testMethod)
    {
        return GetModelsForMethod(testMethodComplexityResults, testMethod.Name);
    }

    public static ScaleFishPropertyModel? GetModelsForProperty(this IEnumerable<ScaleFishPropertyModel> testPropertyComplexityResults, string testProperty)
    {
        return testPropertyComplexityResults.SingleOrDefault(x => x.PropertyName.EndsWith($".{testProperty}"));
    }

    public static ScaleFishPropertyModel? GetModelsForProperty(this IEnumerable<ScaleFishPropertyModel> testPropertyComplexityResults, MemberInfo testProperty)
    {
        return GetModelsForProperty(testPropertyComplexityResults, testProperty.Name);
    }

    /// <summary>
    ///  Easiest way to load a Scalefish model for making predictions
    /// </summary>
    /// <param name="classModels"></param>
    /// <param name="testClass"></param>
    /// <param name="method"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    public static ScalefishModel? GetScalefishModel(
        this IEnumerable<ScalefishClassModel> classModels,
        string testClass,
        string method,
        string property)
    {
        return classModels
            .GetModelsForTestClass(testClass)?
            .ScaleFishMethodModels
            .GetModelsForMethod(method)?
            .ScaleFishPropertyModels
            .GetModelsForProperty(property)?
            .ScalefishModel;
    }

    public static ScalefishModel? GetScalefishModel(
        this IEnumerable<ScalefishClassModel> classModels,
        Type testClass,
        MemberInfo method,
        MemberInfo property)
    {
        return classModels
            .GetModelsForTestClass(testClass.Name)?
            .ScaleFishMethodModels
            .GetModelsForMethod(method.Name)?
            .ScaleFishPropertyModels
            .GetModelsForProperty(property.Name)?
            .ScalefishModel;
    }
}