using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.TestAdapter.TestProperties;
using Sailfish.Utils;

namespace Sailfish.TestAdapter.Discovery;

internal static class TestCaseItemCreator
{
    // categories
    public const string TestTypeFullName = "TestTypeFullName";
    public const string DisplayName = "DisplayName";
    public const string FormedVariableSection = "FormedVariableSection";
    public const string MethodName = "MethodName";

    private static PropertySetGenerator PropertySetGenerator => new(new ParameterCombinator(), new IterationVariableRetriever());

    public static IEnumerable<TestCase> AssembleTestCases(Type testType, string testCsFileContent, string testCsFilePath, string sourceDll, IMessageLogger logger)
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
        var contentLines = LineSplitter.SplitFileIntoLines(testCsFileContent);

        foreach (var method in methods)
        {
            var methodNameLine = GetMethodNameLine(testType, contentLines, method, logger);
            if (methodNameLine == 0) continue;

            if (propertySets.Length > 0)
            {
                var i = 0;
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
                        propertyValues,
                        true); // TODO: Remove this once we have traits and properties sorted. see below
                    i++;
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
                    Array.Empty<int>(),
                    true);
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
        IEnumerable<int> propertyValues,
        bool shouldAddCategories)
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
            logger.SendMessage(TestMessageLevel.Informational, $"ERROR!: testType fullname not defined - fullname: {testType.FullName}");
            throw new Exception("Impossible!");
        }

        if (!shouldAddCategories) return testCase;
        // Traits is not the right way to pass this information, but the Properties property keeps getting cleared on the test case when
        // is passed to the executor -- I'm setting that property incorrectly probably
        testCase.Traits.Add(new Trait(TestTypeFullName, testType.FullName));
        testCase.Traits.Add(new Trait(DisplayName, testCaseId.DisplayName));
        testCase.Traits.Add(new Trait(FormedVariableSection, testCaseId.TestCaseVariables.FormVariableSection()));
        testCase.Traits.Add(new Trait(MethodName, method.Name));

        return testCase;
    }

    private static int GetMethodNameLine(Type testType, IReadOnlyList<string> fileLines, MemberInfo method, IMessageLogger logger)
    {
        // TODO: instead of throwing, keep track of the class and check if it matches the testType
        // var className = testType.Name;

        var lineNumber = fileLines
            .Select(
                (line, index) =>
                {
                    var methodKey = $" {method.Name}(";
                    return line.Trim().Contains(methodKey) ? index : -1;
                })
            .Where(x => x > 0)
            .ToArray();
        if (lineNumber.Length > 1)
        {
            throw new SailfishException("Multiple method with the same name discovered in this file");
        }

        return lineNumber.Single();
    }
}