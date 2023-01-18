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
            var testCaseSet = combos.Select(CreateTestCase(testType, testCsFilePath, sourceDll, method, propertyNames, methodNameLine, logger));
            testCaseSets.AddRange(testCaseSet);
        }

        return testCaseSets;
    }

    private static string CreateFullyQualifiedName(MemberInfo testType)
    {
        return Assembly.CreateQualifiedName(testType.GetType().Assembly.FullName, testType.Name);
    }

    private static Func<int[], TestCase> CreateTestCase(
        Type testType,
        string testFilePath,
        string sourceDll,
        MemberInfo method,
        string[] propertyNames,
        int methodNameLine,
        IMessageLogger handle)
    {
        var randomId = Guid.NewGuid();
        return variablesForEachPropertyInOrder =>
        {
            handle.SendMessage(TestMessageLevel.Informational, $"Param set for {method.Name}: {string.Join(", ", variablesForEachPropertyInOrder.Select(x => x.ToString()))}");
            // var fullyQualifiedName = CreateFullyQualifiedName(testType);

            var displayName = DisplayNameHelper.CreateTestCaseId(testType, method.Name, propertyNames, variablesForEachPropertyInOrder).TestCaseName.Name;
            handle.SendMessage(TestMessageLevel.Informational, $"DisplayName: {displayName}");
            
            handle.SendMessage(TestMessageLevel.Informational, $"This is the file path!: {testFilePath}");
            var testCase = new TestCase(testType.Name, TestExecutor.ExecutorUri, sourceDll) // a test case is a method
            {
                CodeFilePath = testFilePath,
                DisplayName = DisplayNameHelper.CreateTestCaseId(testType, method.Name, propertyNames, variablesForEachPropertyInOrder).TestCaseName.Name,
                ExecutorUri = TestExecutor.ExecutorUri,
                Id = randomId,
                LineNumber = methodNameLine
            };

            if (testType.FullName is null)
            {
                handle.SendMessage(TestMessageLevel.Informational, $"ERROR!: testType fullname not defined - fullname: {testType.FullName}");
                throw new Exception("Impossible!");
            }

            testCase.Traits.Add(new Trait(TestTypeFullName, testType.FullName));

            return testCase;
        };
    }

    private static int GetMethodNameLine(IReadOnlyList<string> fileLines, MemberInfo method, IMessageLogger logger)
    {
        var lineNumber = fileLines
            .Select(
                (line, index) => line.Contains(method.Name + "()") ? index : -1)
            .SingleOrDefault(x => x >= 0);

        logger.SendMessage(TestMessageLevel.Informational, $"Method discovered on line {lineNumber.ToString()} with signature: {fileLines[lineNumber]}");

        return lineNumber;
    }
}