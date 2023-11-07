using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

internal class ScalefishObservationCompiler : IScalefishObservationCompiler
{
    public ObservationSetFromSummaries? CompileObservationSet(IClassExecutionSummary testClassSummary)
    {
        var complexityCases = testClassSummary
            .TestClass
            .GetProperties()
            .Where(propertyInfo => propertyInfo.IsSailfishComplexityVariable())
            .Select(propertyInfo => new ComplexityCase(
                propertyInfo.Name,
                propertyInfo,
                propertyInfo.GetSailfishVariableAttributeOrThrow().GetVariables().Count(),
                propertyInfo.GetSailfishVariableAttributeOrThrow().GetVariables().Cast<int>().ToList()
            ))
            .ToList();

        if (complexityCases.Count == 0) return null;

        // TODO: for public interface
        // throw new SailfishException($"Failed to discover any complexity cases for {testClassSummary.TestClass.FullName}");

        var testCaseGroups = testClassSummary
            .FilterForSuccessfulTestCases()
            .CompiledTestCaseResults
            .GroupBy(x => x.TestCaseId!.TestCaseName.Name)
            .Select(x => new TestCaseComplexityGroup(x.Key, x.ToList()))
            .ToList();

        var observations = new List<ScaleFishObservation>();
        foreach (var testCaseGroup in testCaseGroups)
        {
            foreach (var (complexityCase, i) in complexityCases.Zip(Enumerable.Range(0, complexityCases.Count)))
            {
                var step = i < complexityCase.VariableCount - 1
                    ? complexityCases
                        .Skip(i + 1)
                        .Select(x => x.VariableCount)
                        .Aggregate(1, (a, b) => a * b)
                    : 1;

                var indices = Enumerable.Range(0, complexityCase.VariableCount).Select(j => j * step).ToList();
                var testResult = indices.Select(index => testCaseGroup.TestCaseGroup[index]).ToList();

                var complexityMeasurements = complexityCase
                    .Variables
                    .Zip(testResult.Select(x => x.PerformanceRunResult!.Mean)).Select(x => new ComplexityMeasurement(x.First, x.Second))
                    .ToList();

                observations.Add(new ScaleFishObservation(testCaseGroup.TestCaseMethodName, complexityCase.ComplexityPropertyName, complexityMeasurements.ToArray()));
            }
        }

        return new ObservationSetFromSummaries(testClassSummary.TestClass.FullName ?? $"Unknown-Namespace-{testClassSummary.TestClass.Name}", observations);
    }
}