using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityComputer
{
    IEnumerable<ScalefishClassModel> AnalyzeComplexity(List<IClassExecutionSummary> executionSummaries);

    /// <summary>
    /// Same analysis as <see cref="AnalyzeComplexity(List{IClassExecutionSummary})"/> but additionally
    /// returns the per-property <see cref="ComplexityMeasurement"/> arrays that fed the estimator,
    /// keyed by <c>MethodName.PropertyName</c> to match <see cref="ScaleFishPropertyModel.PropertyName"/>.
    /// Used by the HTML report renderer to plot empirical points alongside the fitted curves.
    /// </summary>
    ComplexityAnalysisResult AnalyzeComplexityWithMeasurements(List<IClassExecutionSummary> executionSummaries);
}

/// <summary>
/// Bundles the analysis output (classifications) with the per-property measurement vectors that were
/// passed to the estimator.
/// </summary>
public sealed class ComplexityAnalysisResult
{
    public ComplexityAnalysisResult(
        IReadOnlyList<ScalefishClassModel> classes,
        IReadOnlyDictionary<string, ComplexityMeasurement[]> measurementsByPropertyKey)
    {
        Classes = classes;
        MeasurementsByPropertyKey = measurementsByPropertyKey;
    }

    /// <summary>Per-class classification models — same shape as <see cref="IComplexityComputer.AnalyzeComplexity"/>.</summary>
    public IReadOnlyList<ScalefishClassModel> Classes { get; }

    /// <summary>
    /// Per-property measurements, keyed by <c>MethodName.PropertyName</c> (same string as
    /// <see cref="ScaleFishPropertyModel.PropertyName"/>) so callers can join models to their inputs.
    /// </summary>
    public IReadOnlyDictionary<string, ComplexityMeasurement[]> MeasurementsByPropertyKey { get; }
}

public class ComplexityComputer : IComplexityComputer
{
    private readonly IComplexityEstimator _complexityEstimator;
    private readonly IScalefishObservationCompiler _scalefishObservationCompiler;

    public ComplexityComputer(IComplexityEstimator complexityEstimator,
        IScalefishObservationCompiler scalefishObservationCompiler)
    {
        _complexityEstimator = complexityEstimator ?? throw new ArgumentNullException(nameof(complexityEstimator));
        _scalefishObservationCompiler = scalefishObservationCompiler ?? throw new ArgumentNullException(nameof(scalefishObservationCompiler));
    }

    public IEnumerable<ScalefishClassModel> AnalyzeComplexity(List<IClassExecutionSummary> executionSummaries)
    {
        return AnalyzeComplexityWithMeasurements(executionSummaries).Classes;
    }

    public ComplexityAnalysisResult AnalyzeComplexityWithMeasurements(List<IClassExecutionSummary> executionSummaries)
    {
        var finalResult = new Dictionary<Type, ComplexityMethodResult>();
        var measurementsByKey = new Dictionary<string, ComplexityMeasurement[]>(StringComparer.Ordinal);

        foreach (var testClassSummary in executionSummaries)
        {
            // A summary whose TestClass could not be resolved (e.g. loaded from a tracking file where
            // Type.GetType(assemblyQualifiedName) returned null because the type isn't loadable in this
            // process) would otherwise crash Dictionary<Type, ...>.Add with ArgumentNullException("key").
            // Skip it so analysis degrades to "no model for this class" instead of taking down the run.
            // The normal path now feeds ScaleFish the current run's in-memory summaries (real Type), so this
            // guard is defensive for the file-loaded path.
            if (testClassSummary.TestClass is null) continue;

            var observationSet = _scalefishObservationCompiler.CompileObservationSet(testClassSummary);
            if (observationSet is null) continue;

            var resultsByMethod = ComputeComplexityMethodResult(observationSet, measurementsByKey);

            finalResult[testClassSummary.TestClass] = resultsByMethod;
        }

        var classes = ScalefishClassModel.ParseResults(finalResult).ToList();
        return new ComplexityAnalysisResult(classes, measurementsByKey);
    }

    private ComplexityMethodResult ComputeComplexityMethodResult(
        ObservationSetFromSummaries observationSet,
        Dictionary<string, ComplexityMeasurement[]> measurementsByKey)
    {
        var methodResult = new ComplexityMethodResult();
        foreach (var observationMethodGroup in observationSet.Observations.GroupBy(x => x.MethodName))
        {
            var complexityResultMap = ComputeComplexityPropertyResult(observationMethodGroup, measurementsByKey);

            methodResult.Add(observationMethodGroup.Key, complexityResultMap);
        }

        return methodResult;
    }

    private ComplexityProperty ComputeComplexityPropertyResult(
        IEnumerable<ScaleFishObservation> observationMethodGroup,
        Dictionary<string, ComplexityMeasurement[]> measurementsByKey)
    {
        var complexityResultMap = new ComplexityProperty();
        foreach (var observationProperty in observationMethodGroup.ToList())
        {
            var complexityResult = _complexityEstimator.EstimateComplexity(observationProperty.ComplexityMeasurements);
            if (complexityResult is null) continue;
            var key = observationProperty.ToString();
            complexityResultMap.Add(key, complexityResult);
            measurementsByKey[key] = observationProperty.ComplexityMeasurements;
        }

        return complexityResultMap;
    }
}
