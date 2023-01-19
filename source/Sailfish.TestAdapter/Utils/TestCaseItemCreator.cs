using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Attributes;
using Sailfish.Execution;
using Sailfish.Utils;

namespace Sailfish.TestAdapter.Utils;

internal class TestCaseItemCreator
{
    public const string TestTypeFullName = "TestTypeFullName";

    private readonly ParameterGridCreator parameterGridCreator = new ParameterGridCreator(new ParameterCombinator(), new IterationVariableRetriever());

    public IEnumerable<TestCase> AssembleTestCases(Type testType, string testCsFileContent, string testCsFilePath, string sourceDll, IMessageLogger logger)
    {
        var testCaseSets = new List<TestCase>();
        var methods = testType.GetMethodsWithAttribute<SailfishMethodAttribute>()?.ToArray();
        if (methods is null)
        {
            logger.SendMessage(TestMessageLevel.Informational, "No method with ExecutePerformanceCheck attribute found -- this shouldn't have made it into the test type scan!");
            return testCaseSets;
        }

        var (item1, combos) = parameterGridCreator.GenerateParameterGrid(testType);
        var propertyNames = item1.ToArray();

        var contentLines = LineSplitter.SplitFileIntoLines(testCsFileContent);

        foreach (var method in methods)
        {
            var methodNameLine = GetMethodNameLine(contentLines, method, logger);
            if (methodNameLine == 0) continue;

            var cnt = 0;
            foreach (var variableCombinations in combos)
            {
                var fullyQualifiedName = CreateFullyQualifiedName(testType);
                var testCaseId = DisplayNameHelper.CreateTestCaseId(testType, method.Name, propertyNames, variableCombinations);
                logger.SendMessage(TestMessageLevel.Informational, $"DisplayName: {testCaseId.DisplayName}");
                var testCase = new TestCase(fullyQualifiedName, TestExecutor.ExecutorUri, sourceDll) // a test case is a method
                {
                    CodeFilePath = testCsFilePath,
                    DisplayName = testCaseId.DisplayName,
                    ExecutorUri = TestExecutor.ExecutorUri,
                    Id = Guid.NewGuid(),
                    LineNumber = methodNameLine + cnt
                };
                cnt++;

                if (testType.FullName is null)
                {
                    logger.SendMessage(TestMessageLevel.Informational, $"ERROR!: testType fullname not defined - fullname: {testType.FullName}");
                    throw new Exception("Impossible!");
                }

                testCase.Traits.Add(new Trait(TestTypeFullName, testType.FullName));
                testCase.Traits.Add(new Trait("DisplayName", testCaseId.DisplayName));
                testCaseSets.Add(testCase);
            }
        }

        return testCaseSets;
    }

    private static string CreateFullyQualifiedName(MemberInfo testType)
    {
        return Assembly.CreateQualifiedName(testType.GetType().Assembly.FullName, testType.Name);
    }

    private static int GetMethodNameLine(IReadOnlyList<string> fileLines, MemberInfo method, IMessageLogger logger)
    {
        var lineNumber = fileLines
            .Select(
                (line, index) =>
                {
                    logger.SendMessage(TestMessageLevel.Informational, $"Line {index}: {line}");
                    var methodKey = $" {method.Name}(";
                    return line.Trim().Contains(methodKey) ? index : -1;
                })
            .SingleOrDefault(x => x >= 0);

        logger.SendMessage(TestMessageLevel.Informational, $"Method discovered on line {lineNumber.ToString()} with signature: {fileLines[lineNumber]}");

        return lineNumber;
    }
}