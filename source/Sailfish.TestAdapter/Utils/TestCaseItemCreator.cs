using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Attributes;
using Sailfish.Execution;
using Sailfish.Utils;

namespace Sailfish.TestAdapter.Utils;

internal class TestCaseItemCreator
{
    public const string TestCaseId = "TestCaseId";

    private readonly ParameterGridCreator parameterGridCreator;

    public TestCaseItemCreator()
    {
        parameterGridCreator = new ParameterGridCreator(new ParameterCombinator(), new IterationVariableRetriever());
        TestProperty.Register(TestCaseId, "TestType", typeof(Type), TestPropertyAttributes.None, typeof(Type));
    }

    public IEnumerable<TestCase> AssembleTestCases(Type testType, string testCsFileContent, string testCsFilePath, string sourceDll)
    {
        var testCaseSets = new List<TestCase>();
        var methods = testType.GetMethodsWithAttribute<SailfishMethodAttribute>()?.ToArray();
        if (methods is null)
        {
            CustomLogger.VerbosePadded("No method with ExecutePerformanceCheck attribute found -- this shouldn't have made it into the test type scan!");
            return testCaseSets;
        }

        var (item1, combos) = parameterGridCreator.GenerateParameterGrid(testType);
        var propertyNames = item1.ToArray();

        var contentLines = LineSplitter.SplitFileIntoLines(testCsFileContent);

        foreach (var method in methods)
        {
            var methodNameLine = GetMethodNameLine(contentLines, method);
            if (methodNameLine == 0) continue;
            var testCaseSet = combos.Select(CreateTestCase(testType, testCsFilePath, sourceDll, method, propertyNames, methodNameLine));
            testCaseSets.AddRange(testCaseSet);
        }

        return testCaseSets;
    }

    private static Func<int[], TestCase> CreateTestCase(
        Type testType,
        string testFilePath,
        string sourceDll,
        MemberInfo method,
        string[] propertyNames,
        int methodNameLine)
    {
        var randomId = Guid.NewGuid();
        return variablesForEachPropertyInOrder =>
        {
            CustomLogger.Verbose("Param set for {Method}: {ParamSet}", method.Name, string.Join(", ", variablesForEachPropertyInOrder.Select(x => x.ToString())));
            var fullyQualifiedName = CreateFullyQualifiedName(testType);
                
            CustomLogger.Verbose("This is the file path!: {FilePath}", testFilePath);
            var testCase = new TestCase(fullyQualifiedName, TestExecutor.ExecutorUri, sourceDll) // a test case is a method
            {
                CodeFilePath = testFilePath,
                DisplayName = DisplayNameHelper.CreateTestCaseId(testType, method.Name, propertyNames, variablesForEachPropertyInOrder).TestCaseName.Name,
                ExecutorUri = TestExecutor.ExecutorUri,
                Id = randomId,
                LineNumber = methodNameLine
            };

            var property = TestProperty.Find(TestCaseId)!;
            testCase.SetPropertyValue(property, testType);

            return testCase;
        };
    }

    private static string CreateFullyQualifiedName(Type testType)
    {
        return Assembly.CreateQualifiedName(Assembly.GetCallingAssembly().ToString(), testType.Name);
    }

    private static int GetMethodNameLine(IReadOnlyList<string> fileLines, MemberInfo method)
    {
        var lineNumber = fileLines
            .Select(
                (line, index) => line.Contains(method.Name + "()") ? index : -1)
            .SingleOrDefault(x => x >= 0);

        CustomLogger.Verbose(
            "Method discovered on line {lineNumber} with signature: {siggy}",
            lineNumber.ToString(),
            fileLines[lineNumber]);

        return lineNumber;
    }
}