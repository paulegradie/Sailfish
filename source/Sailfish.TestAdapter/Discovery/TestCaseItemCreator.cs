using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Execution;
using Sailfish.TestAdapter.TestProperties;
using Sailfish.Utils;

namespace Sailfish.TestAdapter.Discovery;

internal static class TestCaseItemCreator
{
    private static PropertySetGenerator PropertySetGenerator => new(new ParameterCombinator(), new IterationVariableRetriever());

    public static IEnumerable<TestCase> AssembleTestCases(Type testType, ClassMetaData classMetaData, string sourceDll, IHashAlgorithm hashAlgorithm, IMessageLogger logger)
    {
        var propertySets = PropertySetGenerator.GenerateSailfishVariableSets(testType, out _).ToArray();

        foreach (var methodMetaData in classMetaData.Methods)
        {
            var numToMake = Math.Max(propertySets.Length, 1);
            for (var i = 0; i < numToMake; i++)
            {
                var propertyNames = propertySets.Length > 0 ? propertySets[i].GetPropertyNames() : Array.Empty<string>();
                var propertyValues = propertySets.Length > 0 ? propertySets[i].GetPropertyValues() : Array.Empty<string>();
                var testCase = CreateTestCase(
                    testType,
                    sourceDll,
                    logger,
                    classMetaData.FilePath,
                    methodMetaData.MethodName,
                    methodMetaData.LineNumber,
                    propertyNames,
                    propertyValues,
                    hashAlgorithm);
                yield return testCase;
            }
        }
    }

    private static TestCase CreateTestCase(
        Type testType,
        string sourceDll,
        IMessageLogger logger,
        string filePath,
        string methodName,
        int lineNumber,
        IEnumerable<string> propertyNames,
        IEnumerable<object> propertyValues,
        IHashAlgorithm hasher)
    {
        var testCaseId = DisplayNameHelper.CreateTestCaseId(testType, methodName, propertyNames.ToArray(), propertyValues.ToArray());
        var fullyQualifiedName = $"{testType.Namespace}.{testType.Name}.{methodName}{testCaseId.TestCaseVariables.FormVariableSection()}";
        var testCase = new TestCase(fullyQualifiedName, TestExecutor.ExecutorUri, sourceDll)
        {
            Id = hasher.GuidFromString(TestExecutor.ExecutorUri + $"{testType.Namespace}.{testType.Name}.{methodName}"),
            DisplayName = testCaseId.DisplayName.Split(".").Last(),
            LineNumber = lineNumber,
            ExecutorUri = TestExecutor.ExecutorUri,
            CodeFilePath = filePath
        };

        if (testType.FullName is null)
        {
            var msg = $"Error: testType fullname not defined - fullname: {testType.FullName}";
            logger.SendMessage(TestMessageLevel.Error, msg);
            throw new Exception(msg);
        }

        // custom properties
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishTypeProperty, testType.FullName);
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty, $"{methodName}");
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty, testCaseId.DisplayName);
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishFormedVariableSectionDefinitionProperty, testCaseId.TestCaseVariables.FormVariableSection());

        return testCase;
    }
}