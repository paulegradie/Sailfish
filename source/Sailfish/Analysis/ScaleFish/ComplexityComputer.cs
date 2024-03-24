using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

public interface IComplexityComputer
{
    IEnumerable<ScalefishClassModel> AnalyzeComplexity(List<IClassExecutionSummary> executionSummaries);
}

public class ComplexityComputer(
    IComplexityEstimator complexityEstimator,
    IScalefishObservationCompiler scalefishObservationCompiler) : IComplexityComputer
{
    private readonly IComplexityEstimator complexityEstimator = complexityEstimator;
    private readonly IScalefishObservationCompiler scalefishObservationCompiler = scalefishObservationCompiler;

    public IEnumerable<ScalefishClassModel> AnalyzeComplexity(List<IClassExecutionSummary> executionSummaries)
    {
        var finalResult = new Dictionary<Type, ComplexityMethodResult>();
        foreach (var testClassSummary in executionSummaries)
        {
            var observationSet = scalefishObservationCompiler.CompileObservationSet(testClassSummary);
            if (observationSet is null) continue;

            var resultsByMethod = ComputeComplexityMethodResult(observationSet);

            finalResult.Add(testClassSummary.TestClass, resultsByMethod);
        }

        return ScalefishClassModel.ParseResults(finalResult);
    }

    private ComplexityMethodResult ComputeComplexityMethodResult(ObservationSetFromSummaries observationSet)
    {
        var methodResult = new ComplexityMethodResult();
        foreach (var observationMethodGroup in observationSet.Observations.GroupBy(x => x.MethodName))
        {
            var complexityResultMap = ComputeComplexityPropertyResult(observationMethodGroup);

            methodResult.Add(observationMethodGroup.Key, complexityResultMap);
        }

        return methodResult;
    }

    private ComplexityProperty ComputeComplexityPropertyResult(IEnumerable<ScaleFishObservation> observationMethodGroup)
    {
        var complexityResultMap = new ComplexityProperty();
        foreach (var observationProperty in observationMethodGroup.ToList())
        {
            var complexityResult = complexityEstimator.EstimateComplexity(observationProperty.ComplexityMeasurements);
            if (complexityResult is not null) complexityResultMap.Add(observationProperty.ToString(), complexityResult);
        }

        return complexityResultMap;
    }
}