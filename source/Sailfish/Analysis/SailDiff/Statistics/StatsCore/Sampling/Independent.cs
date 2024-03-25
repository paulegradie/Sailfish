// using System;
// using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
// using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;
//
// namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Sampling;
//
// [Serializable]
// public class Independent<TDistribution, TObservation> :
//     Independent<TDistribution>,
//     IMultivariateDistribution<TObservation[]>,
//     ISampleableDistribution<TObservation[]> where TDistribution : IDistribution<TObservation>, IUnivariateDistribution
// {
//     public Independent(params TDistribution[] components) : base(components)
//     {
//     }
//
//     public double DistributionFunction(TObservation[] x)
//     {
//         var num = 1.0;
//         for (var index = 0; index < Components.Length; ++index)
//             num *= Components[index].DistributionFunction(x[index]);
//         return num;
//     }
//
//     public double ProbabilityFunction(TObservation[] x)
//     {
//         throw new NotImplementedException();
//     }
//
//     public double LogProbabilityFunction(TObservation[] x)
//     {
//         throw new NotImplementedException();
//     }
//
//     public double ComplementaryDistributionFunction(TObservation[] x)
//     {
//         var num = 1.0;
//         for (var index = 0; index < Components.Length; ++index)
//             num *= Components[index].ComplementaryDistributionFunction(x[index]);
//         return num;
//     }
//
//     public override object Clone()
//     {
//         var distributionArray = new TDistribution[Components.Length];
//         for (var index = 0; index < distributionArray.Length; ++index)
//             distributionArray[index] = (TDistribution)Components[index].Clone();
//         return new Independent<TDistribution, TObservation>(distributionArray);
//     }
//
//     public TObservation[] Generate(TObservation[] result, Random source)
//     {
//         for (var index = 0; index < Components.Length; ++index)
//             result[index] = ((ISampleableDistribution<TObservation>)Components[index]).Generate(source);
//         return result;
//     }
//
//     public TObservation[][] Generate(int samples, TObservation[][] result, Random source)
//     {
//         for (var index = 0; index < Components.Length; ++index)
//             result.SetColumn(index, ((ISampleableDistribution<TObservation>)Components[index]).Generate(samples, source));
//         return result;
//     }
//
//     TObservation[][] ISampleableDistribution<TObservation[]>.Generate(int samples, Random source)
//     {
//         return Generate(samples, InternalOps.Zeros<TObservation>(samples, Components.Length), source);
//     }
//
//     TObservation[] ISampleableDistribution<TObservation[]>.Generate(Random source)
//     {
//         return Generate(new TObservation[Components.Length], source);
//     }
// }