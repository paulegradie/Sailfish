using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.ComplexityEstimation;
using Sailfish.Execution;
using Sailfish.Statistics;

namespace Sailfish.Analysis.Complexity;

public class ComplexityComputer : IComplexityComputer
{
    private readonly IComplexityEstimator complexityEstimator;

    public ComplexityComputer(IComplexityEstimator complexityEstimator)
    {
        this.complexityEstimator = complexityEstimator;
    }

    // map of type to methods
    // map of methods to ComplexityResult by property
    public Dictionary<Type, Dictionary<string, Dictionary<string, ComplexityResult>>> AnalyzeComplexity(List<IExecutionSummary> executionSummaries)
    {
        var finalResult = new Dictionary<Type, Dictionary<string, Dictionary<string, ComplexityResult>>>();
        foreach (var testClassSummary in executionSummaries)
        {
            // to filter out those we want to assess
            var complexityPropertyNames = testClassSummary
                .Type
                .GetProperties()
                .Where(x => x.GetCustomAttributes<SailfishVariableAttribute>().Single().IsComplexityVariable())
                .Select(x => x.Name)
                .ToList();

            var sailfishVariablesCountTuples = testClassSummary.Type
                .GetProperties()
                .Where(x => x.GetCustomAttributes<SailfishVariableAttribute>().Any())
                .Select(y => (y.Name, y.GetCustomAttributes<SailfishVariableAttribute>().Single().GetVariables().Count()))
                .ToList();

            //                                method name    complexity property  result
            var resultsByMethod = new Dictionary<string, Dictionary<string, ComplexityResult>>();


            var empericalResults = new Dictionary<string, (List<int>, List<ICompiledTestCaseResult>)>();
            foreach (var groupedByTestCaseName in testClassSummary.CompiledTestCaseResults.GroupBy(x => x.TestCaseId!.TestCaseName.Name))
            {
                var testCaseMethodName = groupedByTestCaseName.Key;

                for (var i = 0; i < sailfishVariablesCountTuples.Count; i++)
                {
                    var currentVar = sailfishVariablesCountTuples[i];
                    if (!complexityPropertyNames.Contains(currentVar.Name)) continue;
                    var vars = sailfishVariablesCountTuples
                        .Skip(i + 1)
                        .Select(x => x.Item2)
                        .ToList();
                    var step = vars.Aggregate(1, (a, b) => a * b);
                    var indices = Enumerable.Range(0, currentVar.Item2).Select(j => j * step).ToList();

                    var testResult = indices.Select(index => testClassSummary.CompiledTestCaseResults[index]).ToList();

                    empericalResults.Add(currentVar.Name, (vars.ToList(), testResult));
                }

                var complexityResultMap = new Dictionary<string, ComplexityResult>();
                foreach (var complexityProperty in complexityPropertyNames)
                {
                    var complexityMeasurements = empericalResults[complexityProperty]
                        .Item1
                        .Zip(empericalResults[complexityProperty].Item2)
                        .Select((data) => new ComplexityMeasurement(data.First, data.Second.DescriptiveStatisticsResult!.Mean))
                        .ToArray();

                    var complexityResult = complexityEstimator.EstimateComplexity(complexityMeasurements);

                    complexityResultMap.Add(complexityProperty, complexityResult);
                }

                resultsByMethod.Add(testCaseMethodName, complexityResultMap);
            }

            finalResult.Add(testClassSummary.Type, resultsByMethod);
        }

        return finalResult;
    }
}