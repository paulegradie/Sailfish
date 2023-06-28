using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analyzers.Discovery;
using Sailfish.Execution;
using Sailfish.TestAdapter.TestProperties;
using Sailfish.Utils;

namespace Sailfish.TestAdapter.Discovery;

internal static class TestCaseItemCreator
{
    private static PropertySetGenerator PropertySetGenerator => new(new ParameterCombinator(), new IterationVariableRetriever());

    public static IEnumerable<TestCase> AssembleTestCases(Type testType, ClassMetaData classMetaData, string sourceDll, IMessageLogger logger)
    {
        var testCaseSets = new List<TestCase>();

        var propertySets = PropertySetGenerator.GeneratePropertySets(testType).ToArray();
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
                    methodMetaData.MethodName,
                    propertyNames,
                    propertyValues);
                testCase.CodeFilePath = classMetaData.FilePath;
                testCase.ExecutorUri = TestExecutor.ExecutorUri;
                testCase.LineNumber = methodMetaData.LineNumber;
                testCaseSets.Add(testCase);
            }
        }

        return testCaseSets;
    }

    private static TestCase CreateTestCase(
        Type testType,
        string sourceDll,
        IMessageLogger logger,
        string methodName,
        IEnumerable<string> propertyNames,
        IEnumerable<object> propertyValues)
    {
        var testCaseId = DisplayNameHelper.CreateTestCaseId(testType, methodName, propertyNames.ToArray(), propertyValues.ToArray());
        var fullyQualifiedName = $"{testType.Namespace}.{testType.Name}.{methodName}{testCaseId.TestCaseVariables.FormVariableSection()}";
        var testCase = new TestCase(fullyQualifiedName, TestExecutor.ExecutorUri, sourceDll)
        {
            DisplayName = testCaseId.DisplayName
        };

        testCase.SetPropertyValue(SailfishDisplayNameDefinition.SailfishDisplayNameDefinitionProperty, testCaseId.DisplayName);

        if (testType.FullName is null)
        {
            var msg = $"Error: testType fullname not defined - fullname: {testType.FullName}";
            logger.SendMessage(TestMessageLevel.Error, msg);
            throw new Exception(msg);
        }

        testCase.SetPropertyValue(SailfishTestTypeFullNameDefinition.SailfishTestTypeFullNameDefinitionProperty, testType.FullName);
        testCase.SetPropertyValue(SailfishDisplayNameDefinition.SailfishDisplayNameDefinitionProperty, testCaseId.DisplayName);
        testCase.SetPropertyValue(SailfishFormedVariableSectionDefinition.SailfishFormedVariableSectionDefinitionProperty, testCaseId.TestCaseVariables.FormVariableSection());
        testCase.SetPropertyValue(SailfishMethodNameDefinition.SailfishMethodNameDefinitionProperty, methodName);

        return testCase;
    }
}