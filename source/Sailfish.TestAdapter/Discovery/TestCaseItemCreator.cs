using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;
using Sailfish.Execution;
using Sailfish.TestAdapter.TestProperties;
using Sailfish.Utils;

namespace Sailfish.TestAdapter.Discovery;

internal static class TestCaseItemCreator
{
    private static PropertySetGenerator PropertySetGenerator => new(new ParameterCombinator(), new IterationVariableRetriever());

    public static IEnumerable<TestCase> AssembleTestCases(Type testType, List<string> testCsFileContent, string testCsFilePath, string sourceDll, IMessageLogger logger)
    {
        var testCaseSets = new List<TestCase>();
        var methods = testType.GetMethodsWithAttribute<SailfishMethodAttribute>()?.ToArray();
        if (methods is null)
        {
            logger.SendMessage(TestMessageLevel.Informational,
                $"No method with {nameof(SailfishMethodAttribute)} attribute found -- this shouldn't have made it into the test type scan!");
            return testCaseSets;
        }

        var propertySets = PropertySetGenerator.GeneratePropertySets(testType).ToArray();

        foreach (var method in methods)
        {
            var methodNameLine = GetMethodNameLine(testType, testCsFileContent, method, logger);
            if (methodNameLine == 0) continue;

            if (propertySets.Length > 0)
            {
                foreach (var propertySet in propertySets)
                {
                    var propertyNames = propertySet.GetPropertyNames();
                    var propertyValues = propertySet.GetPropertyValues();
                    var testCase = CreateTestCase(
                        testType,
                        sourceDll,
                        logger,
                        method,
                        propertyNames,
                        propertyValues);
                    testCase.CodeFilePath = testCsFilePath;
                    testCase.ExecutorUri = TestExecutor.ExecutorUri;
                    testCase.LineNumber = methodNameLine;
                    testCaseSets.Add(testCase);
                }
            }
            else
            {
                var testCase = CreateTestCase(
                    testType,
                    sourceDll,
                    logger,
                    method,
                    Array.Empty<string>(),
                    Array.Empty<object>());
                testCase.CodeFilePath = testCsFilePath;
                testCase.ExecutorUri = TestExecutor.ExecutorUri;
                testCase.LineNumber = methodNameLine;
                testCaseSets.Add(testCase);
            }
        }

        return testCaseSets;
    }

    private static TestCase CreateTestCase(
        Type testType,
        string sourceDll,
        IMessageLogger logger,
        MemberInfo method,
        IEnumerable<string> propertyNames,
        IEnumerable<object> propertyValues)
    {
        var testCaseId = DisplayNameHelper.CreateTestCaseId(testType, method.Name, propertyNames.ToArray(), propertyValues.ToArray());
        var fullyQualifiedName = $"{testType.Namespace}.{testType.Name}.{method.Name}{testCaseId.TestCaseVariables.FormVariableSection()}";
        var testCase = new TestCase(fullyQualifiedName, TestExecutor.ExecutorUri, sourceDll)
        {
            DisplayName = testCaseId.DisplayName
        };

        testCase.SetPropertyValue(SailfishDisplayNameDefinition.SailfishDisplayNameDefinitionProperty, testCaseId.DisplayName);

        if (testType.FullName is null)
        {
            var msg = $"ERROR!: testType fullname not defined - fullname: {testType.FullName}";
            logger.SendMessage(TestMessageLevel.Informational, msg);
            throw new Exception(msg);
        }

        testCase.SetPropertyValue(SailfishTestTypeFullNameDefinition.SailfishTestTypeFullNameDefinitionProperty, testType.FullName);
        testCase.SetPropertyValue(SailfishDisplayNameDefinition.SailfishDisplayNameDefinitionProperty, testCaseId.DisplayName);
        testCase.SetPropertyValue(SailfishFormedVariableSectionDefinition.SailfishFormedVariableSectionDefinitionProperty, testCaseId.TestCaseVariables.FormVariableSection());
        testCase.SetPropertyValue(SailfishMethodNameDefinition.SailfishMethodNameDefinitionProperty, method.Name);

        return testCase;
    }

    private static int GetMethodNameLine(MemberInfo testType, List<string> fileLines, MemberInfo method, IMessageLogger logger)
    {
        var currentlyInTheRightClass = false;
        var index = 0;
        var methodLine = 0;
        foreach (var fileLine in fileLines)
        {
            if (fileLine.Contains($"class {testType.Name}"))
            {
                currentlyInTheRightClass = true;
            }
            else if (fileLine.Contains("class"))
            {
                currentlyInTheRightClass = false;
            }

            var methodKey = $" {method.Name}(";
            if (currentlyInTheRightClass && fileLine.Trim().Contains(methodKey))
            {
                methodLine = index;
            }

            index++;
        }

        if (methodLine != 0) return methodLine;
        logger.SendMessage(TestMessageLevel.Error, $"Failed to discover the test method for {method.Name} in type {testType.Name} amongst these file lines:\n");
        foreach (var line in fileLines)
        {
            logger.SendMessage(TestMessageLevel.Error, line);
        }

        return methodLine;
    }
}