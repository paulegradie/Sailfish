using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using VeerPerforma.Attributes;
using VeerPerforma.Execution;
using VeerPerforma.Utils;

namespace VeerPerforma.TestAdapter.Utils
{
    internal class TestCaseItemCreator
    {
        private readonly ParameterGridCreator parameterGridCreator;

        public TestCaseItemCreator()
        {
            parameterGridCreator = new ParameterGridCreator(new ParameterCombinator());
        }

        public IEnumerable<TestCase> AssembleTestCases(DataBag bag, string sourceDll)
        {
            var methods = bag.Type.GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>().ToArray();
            if (methods is null) throw new Exception("No method with ExecutePerformanceCheck attribute found -- this shouldn't have made it into the test type scan!");

            var propertyNamesAndCombos = parameterGridCreator.GenerateParameterGrid(bag.Type);
            var propertyNames = propertyNamesAndCombos.Item1.ToArray();
            var combos = propertyNamesAndCombos.Item2;
            var contentLines = bag.CsFileContentString.Split("\r");

            var testCaseSets = new List<TestCase>();
            foreach (var method in methods)
            {
                var methodNameLine = GetMethodNameLine(contentLines, method);
                var testCaseSet = combos.Select(CreateTestCase(bag, sourceDll, method, propertyNames, methodNameLine));
                testCaseSets.AddRange(testCaseSet);
            }

            return testCaseSets;
        }

        private Func<int[], TestCase> CreateTestCase(DataBag bag, string sourceDll, MethodInfo method, string[] propertyNames, int methodNameLine)
        {
            return variablesForEachPropertyInOrder =>
            {
                logger.Verbose("Param set for {Method}: {ParamSet}", method.Name, string.Join(", ", variablesForEachPropertyInOrder.Select(x => x.ToString())));
                var randomId = Guid.NewGuid();
                var paramsDisplayName = DisplayNameHelper.CreateParamsDisplay(propertyNames, variablesForEachPropertyInOrder);
                var fullyQualifiedName = CreateFullyQualifiedName(bag, paramsDisplayName);

                logger.Verbose("This is the file path!: {FilePath}", bag.CsFilePath);
                var testCase = new TestCase(fullyQualifiedName, TestExecutor.ExecutorUri, sourceDll) // a test case is a method
                {
                    CodeFilePath = bag.CsFilePath,
                    DisplayName = DisplayNameHelper.CreateDisplayName(bag.Type, method.Name, paramsDisplayName),
                    ExecutorUri = TestExecutor.ExecutorUri,
                    Id = randomId,
                    LineNumber = methodNameLine
                };

                return testCase;
            };
        }

        private static string CreateFullyQualifiedName(DataBag bag, string paramsDisplayName)
        {
            return string.Join(".", Assembly.CreateQualifiedName(Assembly.GetCallingAssembly().ToString(), bag.Type.Name), paramsDisplayName);
        }

        private int GetMethodNameLine(string[] fileLines, MethodInfo method)
        {
            var lineNumber = fileLines
                .Select(
                    (line, index) => { return line.Contains(method.Name + "()") ? index : -1; })
                .Single(x => x >= 0);

            logger.Verbose(
                "Method discovered on line {lineNumber} with signature: {siggy}",
                lineNumber.ToString(),
                fileLines[lineNumber]);

            return lineNumber;
        }
    }
}