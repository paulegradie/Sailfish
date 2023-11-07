using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Execution;

namespace Sailfish.Analysis.ScaleFish;

public class ComplexityComputer : IComplexityComputer
{
    private readonly IComplexityEstimator complexityEstimator;
    private readonly IScalefishObservationCompiler scalefishObservationCompiler;

    public ComplexityComputer(
        IComplexityEstimator complexityEstimator,
        IScalefishObservationCompiler scalefishObservationCompiler)
    {
        this.complexityEstimator = complexityEstimator;
        this.scalefishObservationCompiler = scalefishObservationCompiler;
    }

    public IEnumerable<ScalefishClassModel> AnalyzeComplexity(List<IClassExecutionSummary> executionSummaries)
    {
        var finalResult = new Dictionary<Type, ComplexityMethodResult>();
        foreach (var testClassSummary in executionSummaries)
        {
            var observationSet = scalefishObservationCompiler.CompileObservationSet(testClassSummary);
            if (observationSet is null) continue;

            var methodResult = new ComplexityMethodResult();
            foreach (var observationMethodGroup in observationSet.Observations.GroupBy(x => x.MethodName))
            {
                var complexityResultMap = new ComplexityProperty();
                foreach (var observationProperty in observationMethodGroup.ToList())
                {
                    var complexityResult = complexityEstimator.EstimateComplexity(observationProperty.ComplexityMeasurements);
                    complexityResultMap.Add(observationProperty.ToString(), complexityResult);
                }

                methodResult.Add(observationMethodGroup.Key, complexityResultMap);
            }

            finalResult.Add(testClassSummary.TestClass, methodResult);
        }

        return ScalefishClassModel.ParseResults(finalResult);
    }
}