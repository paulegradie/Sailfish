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

        var testCaseGroups = testClassSummary
            .FilterForSuccessfulTestCases()
            .CompiledTestCaseResults
            .GroupBy(x => x.TestCaseId!.TestCaseName.Name)
            .Select(x => new TestCaseComplexityGroup(x.Key, x.ToList()))
            .ToList();

        var observations = new List<ScaleFishObservation>();
        foreach (var testCaseGroup in testCaseGroups)
        {
            foreach (var (complexityCase, caseIndex) in complexityCases.Zip(Enumerable.Range(0, complexityCases.Count)))
            {
                var complexityMeasurements = ComputeComplexityMeasurements(caseIndex, complexityCase, complexityCases, testCaseGroup);
                observations.Add(new ScaleFishObservation(testCaseGroup.TestCaseMethodName, complexityCase.ComplexityPropertyName, complexityMeasurements.ToArray()));
            }
        }

        return new ObservationSetFromSummaries(testClassSummary.TestClass.FullName ?? $"Unknown-Namespace-{testClassSummary.TestClass.Name}", observations);
    }

    // a bit mind bending here
    private static List<ComplexityMeasurement> ComputeComplexityMeasurements(
        int testCaseGroupIndex,
        ComplexityCase complexityCase,
        IEnumerable<ComplexityCase> complexityCases,
        TestCaseComplexityGroup testCaseGroup)
    {
        var step = testCaseGroupIndex < complexityCase.VariableCount - 1
            ? complexityCases
                .Skip(testCaseGroupIndex + 1)
                .Select(x => x.VariableCount)
                .Aggregate(1, (a, b) => a * b)
            : 1;

        var indices = Enumerable.Range(0, complexityCase.VariableCount).Select(j => j * step).ToList();
        var testResult = indices.Select(idx => testCaseGroup.TestCaseGroup[idx]).ToList();

        var complexityMeasurements = complexityCase
            .Variables
            .Zip(testResult.Select(x => x.PerformanceRunResult!.Mean)).Select(x => new ComplexityMeasurement(x.First, x.Second))
            .ToList();
        return complexityMeasurements;
    }
}