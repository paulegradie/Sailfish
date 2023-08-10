using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Statistics;

namespace Sailfish.Analysis.Scalefish;

public class ComplexityComputer : IComplexityComputer
{
    private readonly IComplexityEstimator complexityEstimator;

    public ComplexityComputer(IComplexityEstimator complexityEstimator)
    {
        this.complexityEstimator = complexityEstimator;
    }

    public IEnumerable<ITestClassComplexityResult> AnalyzeComplexity(List<IExecutionSummary> executionSummaries)
    {
        var finalResult = new Dictionary<Type, Dictionary<string, Dictionary<string, ComplexityResult>>>();
        foreach (var testClassSummary in executionSummaries)
        {
            var sailfishComplexityVariables = testClassSummary
                .Type
                .GetProperties()
                .Where(x => x.GetCustomAttributes<SailfishVariableAttribute>().Any(a => a.IsComplexityVariable()))
                .ToList();

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
            var observations = new Dictionary<string, (List<int>, List<ICompiledTestCaseResult>)>();

            var testCaseGroups = testClassSummary.CompiledTestCaseResults.GroupBy(x => x.TestCaseId!.TestCaseName.Name);
            foreach (var testCaseGroup in testCaseGroups)
            {
                var testCaseMethodName = testCaseGroup.Key;

                for (var i = 0; i < sailfishVariablesCountTuples.Count; i++)
                {
                    var currentVar = sailfishVariablesCountTuples[i];
                    if (!complexityPropertyNames.Contains(currentVar.Name)) continue;

                    var currentSailfishVariables = sailfishComplexityVariables
                        .Single(x => x.Name == sailfishVariablesCountTuples[i].Name)
                        .GetCustomAttribute<SailfishVariableAttribute>()?
                        .GetVariables()
                        .Cast<int>()
                        .ToList() ?? throw new SailfishException(
                        "Error encountered when executing complexity analysis: SailfishVariable analyzed did not have SailfishVariableAttribute as expected");

                    var step = i < sailfishVariablesCountTuples.Count - 1
                        ? sailfishVariablesCountTuples
                            .Skip(i + 1)
                            .Select(x => x.Item2)
                            .Aggregate(1, (a, b) => a * b)
                        : 1;

                    var indices = Enumerable.Range(0, currentVar.Item2).Select(j => j * step).ToList();
                    var testResult = indices.Select(index => testCaseGroup.ToList()[index]).ToList();

                    observations.Add(string.Join("-", testCaseMethodName, currentVar.Name), (currentSailfishVariables, testResult));
                }

                var complexityResultMap = new Dictionary<string, ComplexityResult>();
                foreach (var complexityProperty in complexityPropertyNames)
                {
                    var observationKey = string.Join("-", testCaseMethodName, complexityProperty);
                    var complexityMeasurements = observations[observationKey]
                        .Item1
                        .Zip(observations[observationKey].Item2)
                        .Select((data) =>
                            new ComplexityMeasurement(data.First, data.Second.DescriptiveStatisticsResult!.Mean))
                        .ToArray();

                    var complexityResult = complexityEstimator.EstimateComplexity(complexityMeasurements);

                    complexityResultMap.Add(observationKey, complexityResult);
                }

                resultsByMethod.Add(testCaseMethodName, complexityResultMap);
            }

            finalResult.Add(testClassSummary.Type, resultsByMethod);
        }

        return TestClassComplexityResult.ParseResults(finalResult);
    }
}