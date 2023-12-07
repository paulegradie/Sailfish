using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;
using System;
using System.Text;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public class Independent<TDist> :
    MultivariateContinuousDistribution,
    ISampleableDistribution<double[]>,
    IDistribution<double[]>,
    IDistribution,
    ICloneable,
    IRandomNumberGenerator<double[]>,
    IFittableDistribution<double[], IndependentOptions>,
    IFittable<double[], IndependentOptions>,
    IFittable<double[]>,
    IFittableDistribution<double[]>
    where TDist : IUnivariateDistribution
{
    private double[,] covariance;
    private double[] mean;
    private double[] variance;

    public Independent(int dimensions)
        : base(dimensions)
    {
        try
        {
            Components = new TDist[dimensions];
            for (var index = 0; index < Components.Length; ++index)
                Components[index] = Activator.CreateInstance<TDist>();
        }
        catch
        {
            throw new ArgumentException(
                "The component distribution needs specific parameters that need to begiven to its constructor. Please specify in the 'initializer' argument of this constructorhow the component distributions should be created.");
        }
    }

    public Independent(int dimensions, Func<TDist> initializer)
        : base(dimensions)
    {
        Components = new TDist[dimensions];
        for (var index = 0; index < Components.Length; ++index)
            Components[index] = initializer();
    }

    public Independent(int dimensions, Func<int, TDist> initializer)
        : base(dimensions)
    {
        Components = new TDist[dimensions];
        for (var index = 0; index < Components.Length; ++index)
            Components[index] = initializer(index);
    }

    public Independent(int dimensions, TDist component)
        : base(dimensions)
    {
        Components = new TDist[dimensions];
        for (var index = 0; index < Components.Length; ++index)
            Components[index] = (TDist)component.Clone();
    }

    public Independent(params TDist[] components)
        : base(components.Length)
    {
        Components = components;
    }

    public TDist[] Components { get; }

    public override double[] Mean
    {
        get
        {
            if (mean == null)
            {
                mean = new double[Components.Length];
                for (var index = 0; index < Components.Length; ++index)
                    mean[index] = Components[index].Mean;
            }

            return mean;
        }
    }

    public override double[] Variance
    {
        get
        {
            if (variance == null)
            {
                variance = new double[Components.Length];
                for (var index = 0; index < Components.Length; ++index)
                    variance[index] = Components[index].Variance;
            }

            return variance;
        }
    }

    public override double[,] Covariance
    {
        get
        {
            covariance ??= Ops.InternalOps.Diagonal(Variance);
            return covariance;
        }
    }

    public void Fit(double[][] observations, double[] weights, IndependentOptions options)
    {
        if (options != null)
        {
            if (!options.Transposed)
                observations = observations.Transpose();
            if (options.InnerOptions != null)
                for (var index = 0; index < Components.Length; ++index)
                    Components[index].Fit(observations[index], weights, options.InnerOptions[index]);
            else
                for (var index = 0; index < Components.Length; ++index)
                    Components[index].Fit(observations[index], weights, options.InnerOption);
        }
        else
        {
            observations = observations.Transpose();
            for (var index = 0; index < Components.Length; ++index)
                Components[index].Fit(observations[index], weights, null);
        }

        Reset();
    }

    public override double[][] Generate(int samples, double[][] result, Random source)
    {
        var sampleableDistributionArray = Components.Apply((Func<TDist, ISampleableDistribution<double>>)(x => (ISampleableDistribution<double>)x));
        for (var index1 = 0; index1 < result.Length; ++index1)
            for (var index2 = 0; index2 < sampleableDistributionArray.Length; ++index2)
                result[index1][index2] = sampleableDistributionArray[index2].Generate(source);
        return result;
    }

    public override object Clone()
    {
        var distributionArray = new TDist[Components.Length];
        for (var index = 0; index < distributionArray.Length; ++index)
            distributionArray[index] = (TDist)Components[index].Clone();
        return new Independent<TDist>(distributionArray);
    }

    protected internal override double InnerDistributionFunction(double[] x)
    {
        var num = 1.0;
        for (var index = 0; index < Components.Length; ++index)
            num *= Components[index].DistributionFunction(x[index]);
        return num;
    }

    protected internal override double InnerProbabilityDensityFunction(double[] x)
    {
        return Math.Exp(LogProbabilityDensityFunction(x));
    }

    protected internal override double InnerLogProbabilityDensityFunction(params double[] x)
    {
        var num = 0.0;
        for (var index = 0; index < Components.Length; ++index)
            num += Components[index].LogProbabilityFunction(x[index]);
        return num;
    }

    public override void Fit(double[][] observations, double[] weights, IFittingOptions options)
    {
        var options1 = options as IndependentOptions;
        if (options != null && options1 == null)
            throw new ArgumentException("The specified options' type is invalid.", nameof(options));
        Fit(observations, weights, options1);
    }

    protected void Reset()
    {
        mean = null;
        variance = null;
        covariance = null;
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("Independent(");
        for (var index = 0; index < Components.Length; ++index)
        {
            stringBuilder.Append("x" + index);
            if (index < Components.Length - 1)
                stringBuilder.Append(", ");
        }

        stringBuilder.Append("; ");
        for (var index = 0; index < Components.Length; ++index)
        {
            var format1 = ((object)Components[index] as IFormattable).ToString(format, formatProvider).Replace("(x", "(x" + index);
            stringBuilder.AppendFormat(format1);
            if (index < Components.Length - 1)
                stringBuilder.Append(" + ");
        }

        stringBuilder.Append(")");
        return stringBuilder.ToString();
    }
}