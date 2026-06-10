using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

public interface IScalefishObservationCompiler
{
    ObservationSetFromSummaries? CompileObservationSet(IClassExecutionSummary testClassSummary);
}

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
            .Select(x => new TestCaseComplexityGroup(x.Key, [.. x]))
            .ToList();

        var observations = new List<ScaleFishObservation>();
        foreach (var testCaseGroup in testCaseGroups)
        foreach (var complexityCase in complexityCases)
        {
            var complexityMeasurements = ComputeComplexityMeasurements(complexityCase, testCaseGroup);
            observations.Add(new ScaleFishObservation(testCaseGroup.TestCaseMethodName, complexityCase.ComplexityPropertyName, [.. complexityMeasurements]));
        }

        return new ObservationSetFromSummaries(testClassSummary.TestClass.FullName ?? $"Unknown-Namespace-{testClassSummary.TestClass.Name}", observations);
    }

    /// <summary>
    /// Selects, for one complexity property, the series of test cases where only that property varies —
    /// every other variable (ScaleFish or not) held at a fixed combination — and converts it into
    /// (X, mean, stddev, raw samples) measurements ordered by the property's declared values.
    ///
    /// <para>
    /// Selection is by the variable <em>values</em> recorded on each case's <see cref="TestCaseId"/>.
    /// The previous implementation indexed positionally into the group with stride arithmetic derived
    /// from the complexity-property counts, which (a) used the wrong stride for middle properties whose
    /// value count was small (it compared the property's index against its own value count rather than
    /// the property count), (b) silently mis-attributed measurements whenever a non-ScaleFish variable
    /// participated in the cartesian product, and (c) shifted off-by-one-or-more whenever a failed case
    /// had been filtered out of the group.
    /// </para>
    /// </summary>
    private static List<ComplexityMeasurement> ComputeComplexityMeasurements(
        ComplexityCase complexityCase,
        TestCaseComplexityGroup testCaseGroup)
    {
        // Annotate each successful case with this property's value and a key identifying the values of
        // every other variable on the case. Cases within one key differ only in this property.
        var annotated = new List<(ICompiledTestCaseResult Result, int Value, string SeriesKey)>(testCaseGroup.TestCaseGroup.Count);
        foreach (var result in testCaseGroup.TestCaseGroup)
        {
            var variables = result.TestCaseId?.TestCaseVariables?.Variables;
            if (variables is null) continue;

            int? ownValue = null;
            var seriesKey = new StringBuilder();
            foreach (var variable in variables)
            {
                if (variable.Name == complexityCase.ComplexityPropertyName)
                {
                    if (TryReadIntVariable(variable.Value, out var parsed)) ownValue = parsed;
                    continue;
                }

                seriesKey.Append(variable.Name).Append('=').Append(variable.Value?.ToString() ?? string.Empty).Append(';');
            }

            if (ownValue is null) continue;
            annotated.Add((result, ownValue.Value, seriesKey.ToString()));
        }

        if (annotated.Count == 0) return [];

        var declaredValues = complexityCase.Variables;

        // Choose the baseline series: the fixed-other-variables slice covering the most declared values
        // of this property. Ties resolve to the slice that appears first in the group, so the choice is
        // deterministic for a given result set. (GroupBy preserves first-appearance key order and
        // OrderByDescending is stable.)
        var series = annotated
            .GroupBy(a => a.SeriesKey)
            .OrderByDescending(g => g.Select(a => a.Value).Distinct().Count(declaredValues.Contains))
            .First();

        // First occurrence wins if duplicate (property, value) cases exist within the series.
        var resultByValue = new Dictionary<int, ICompiledTestCaseResult>();
        foreach (var (result, value, _) in series) resultByValue.TryAdd(value, result);

        var complexityMeasurements = new List<ComplexityMeasurement>(declaredValues.Count);
        foreach (var declaredValue in declaredValues)
        {
            if (!resultByValue.TryGetValue(declaredValue, out var result)) continue;
            var performanceRunResult = result.PerformanceRunResult;
            if (performanceRunResult is null) continue;

            // The bootstrap and weighted-fit paths assume `SampleSize` matches the length of
            // `RawSamples`. `PerformanceRunResult.SampleSize` is the *original* count (pre-outlier
            // removal), while `DataWithOutliersRemoved` is what we actually carry as the raw vector.
            // Use the cleaned vector's length so StandardError = StdDev / √N stays honest.
            var cleaned = performanceRunResult.DataWithOutliersRemoved;
            var effectiveN = cleaned?.Length ?? performanceRunResult.SampleSize;
            complexityMeasurements.Add(new ComplexityMeasurement(
                declaredValue,
                performanceRunResult.Mean,
                performanceRunResult.StdDev,
                effectiveN,
                cleaned));
        }

        return complexityMeasurements;
    }

    /// <summary>
    /// Reads a complexity-variable value from a <see cref="TestCaseVariable.Value"/>. Live runs carry
    /// the boxed int from the attribute; tracking-file round-trips can surface the value as a string or
    /// a JsonElement, so anything else falls back to an invariant-culture parse of its string form.
    /// </summary>
    private static bool TryReadIntVariable(object? value, out int parsed)
    {
        switch (value)
        {
            case int i:
                parsed = i;
                return true;
            case string s:
                return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
            default:
                return int.TryParse(value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }
    }
}
