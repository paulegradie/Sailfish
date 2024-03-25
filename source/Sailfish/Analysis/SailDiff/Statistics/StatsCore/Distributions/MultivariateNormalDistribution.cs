using System;
using System.Runtime.CompilerServices;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Decompositions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public class MultivariateNormalDistribution : MultivariateContinuousDistribution
{
    private CholeskyDecomposition chol;
    private double[,] covariance;
    private double lnconstant;
    private double[] mean;
    private SingularValueDecomposition svd;
    private double[] variance;

    public MultivariateNormalDistribution(int dimension)
        : this(dimension, true)
    {
    }

    private MultivariateNormalDistribution(int dimension, bool init)
        : base(dimension)
    {
        if (!init)
            return;
        var m = new double[dimension];
        var cov = InternalOps.Identity(dimension);
        var cd = new CholeskyDecomposition(cov);
        Initialize(m, cov, cd, null);
    }

    public MultivariateNormalDistribution(double[] mean, double[][] covariance)
        : this(mean, covariance.ToMatrix())
    {
    }

    public MultivariateNormalDistribution(double[] mean)
        : this(mean, InternalOps.Identity(mean.Length))
    {
    }

    public MultivariateNormalDistribution(double[] mean, double[,] covariance)
        : base(mean.Length)
    {
        var length1 = covariance.GetLength(0);
        var length2 = covariance.GetLength(1);
        if (length1 != length2)
            throw new DimensionMismatchException(nameof(covariance), "Covariance matrix should be square.");
        if (mean.Length != length1)
            throw new DimensionMismatchException(nameof(covariance), "Covariance matrix should have the same dimensions as mean vector's length.");
        var cd = new CholeskyDecomposition(covariance);
        if (!cd.IsPositiveDefinite)
            throw new NonPositiveDefiniteMatrixException(
                "Covariance matrix is not positive definite. If are trying to estimate a distribution from data, please try using the Estimate method.");
        Initialize(mean, covariance, cd, null);
    }

    public override double[] Mean => mean;

    public override double[] Variance => variance;

    public override double[,] Covariance => covariance;

    public void Fit(double[][] observations, double[]? weights, NormalOptions? options)
    {
        double[] numArray;
        double[,] cov1;
        if (weights != null)
        {
            numArray = observations.WeightedMean(weights);
            if (options == null || !options.Diagonal)
            {
                cov1 = observations.WeightedCovariance(weights, numArray);
            }
            else
            {
                cov1 = InternalOps.Diagonal(observations.WeightedVariance(weights, numArray));
            }
        }
        else
        {
            numArray = observations.Mean(0);
            if (options == null || !options.Diagonal)
            {
                cov1 = observations.Covariance(numArray).ToMatrix();
            }
            else
            {
                cov1 = InternalOps.Diagonal(observations.Variance(numArray));
            }
        }

        if (options is { Shared: true })
        {
            options.Postprocessing = (components, pi) =>
            {
                var vector = components.To<MultivariateNormalDistribution[]>();
                var cov2 = InternalOps.PooledCovariance(
                    vector.Apply(x => x.covariance), pi);
                Decompose(options, cov2, out chol, out svd);
                foreach (var normalDistribution in vector)
                    normalDistribution.Initialize(normalDistribution.mean, cov2, chol, svd);
            };
            Initialize(numArray, cov1, null, null);
        }
        else
        {
            Decompose(options, cov1, out chol, out svd);
            Initialize(numArray, cov1, chol, svd);
        }
    }

    public override object Clone()
    {
        var normalDistribution = new MultivariateNormalDistribution(Dimension, false);
        normalDistribution.lnconstant = lnconstant;
        normalDistribution.covariance = (double[,])covariance.Clone();
        normalDistribution.mean = (double[])mean.Clone();
        if (chol != null)
            normalDistribution.chol = (CholeskyDecomposition)chol.Clone();
        if (svd != null)
            normalDistribution.svd = (SingularValueDecomposition)svd.Clone();
        return normalDistribution;
    }

    public override double[][] Generate(int samples, double[][] result, Random source)
    {
        var matrix = chol != null ? chol.LeftTriangularFactor : throw new NonPositiveDefiniteMatrixException("Covariance matrix is not positive definite.");
        var numArray = new double[Dimension];
        var mean = Mean;
        for (var index = 0; index < samples; ++index)
        {
            NormalDistribution.Random(Dimension, numArray, source);
            matrix.Dot(numArray, result[index]);
            result[index].Add(mean, result[index]);
        }

        return result;
    }

    private void Initialize(
        double[] m,
        double[,] cov,
        CholeskyDecomposition cd,
        SingularValueDecomposition svd)
    {
        var length = m.Length;
        mean = m;
        covariance = cov;
        chol = cd;
        this.svd = svd;
        if (chol == null && svd == null)
            return;
        var num = cd != null ? cd.LogDeterminant : svd.LogPseudoDeterminant;
        lnconstant = -(1.8378770664093456 * length + num) * 0.5;
    }

    protected internal override double InnerDistributionFunction(double[] x)
    {
        if (Dimension == 1)
        {
            var num = Math.Sqrt(Covariance[0, 0]);
            return num == 0.0 ? x[0] == mean[0] ? 1.0 : 0.0 : Normal.Function((x[0] - mean[0]) / num);
        }

        if (Dimension != 2)
            throw new NotSupportedException("The cumulative distribution function is only available for up to two dimensions.");
        var num1 = Math.Sqrt(Covariance[0, 0]);
        var num2 = Math.Sqrt(Covariance[1, 1]);
        var num3 = Covariance[0, 1] / (num1 * num2);
        return double.IsNaN(num3)
            ? x.IsEqual(mean) ? 1.0 : 0.0
            : Normal.Bivariate((x[0] - mean[0]) / num1, (x[1] - mean[1]) / num2, num3);
    }

    protected internal override double InnerComplementaryDistributionFunction(params double[] x)
    {
        if (Dimension == 1)
        {
            var num1 = Math.Sqrt(Covariance[0, 0]);
            var num2 = (x[0] - mean[0]) / num1;
            return num1 == 0.0 ? x[0] == mean[0] ? 0.0 : 1.0 : Normal.Complemented(num2);
        }

        if (Dimension != 2)
            throw new NotSupportedException("The cumulative distribution function is only available for up to two dimensions.");
        var num3 = Math.Sqrt(Covariance[0, 0]);
        var num4 = Math.Sqrt(Covariance[1, 1]);
        var num5 = Covariance[0, 1] / (num3 * num4);
        return double.IsNaN(num5)
            ? x.IsEqual(mean) ? 0.0 : 1.0
            : Normal.BivariateComplemented((x[0] - mean[0]) / num3, (x[1] - mean[1]) / num4, num5);
    }

    protected internal override double InnerProbabilityDensityFunction(double[] x)
    {
        return Math.Exp(-0.5 * Mahalanobis(x) + lnconstant);
    }

    protected internal override double InnerLogProbabilityDensityFunction(params double[] x)
    {
        return -0.5 * Mahalanobis(x) + lnconstant;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Mahalanobis(double[] x)
    {
        if (x.Length != Dimension)
            throw new DimensionMismatchException(nameof(x), "The vector should have the same dimension as the distribution.");
        var numArray1 = new double[mean.Length];
        for (var index = 0; index < x.Length; ++index)
            numArray1[index] = x[index] - mean[index];
        var numArray2 = chol == null ? svd.Solve(numArray1) : chol.Solve(numArray1);
        var num = 0.0;
        for (var index = 0; index < numArray1.Length; ++index)
            num += numArray2[index] * numArray1[index];
        return num;
    }

    public override void Fit(double[][] observations, double[]? weights, IFittingOptions options)
    {
        var options1 = options as NormalOptions;
        if (options != null && options1 == null)
            throw new ArgumentException("The specified options' type is invalid.", nameof(options));
        Fit(observations, weights, options1);
    }

    private static void Decompose(
        NormalOptions? options,
        double[,] cov,
        out CholeskyDecomposition chol,
        out SingularValueDecomposition svd)
    {
        svd = null;
        chol = null;
        if (options == null)
        {
            chol = new CholeskyDecomposition(cov);
            if (!chol.IsPositiveDefinite)
                throw new NonPositiveDefiniteMatrixException(
                    "Covariance matrix is not positive definite. Try specifying a regularization constant in the fitting options (there is an example in the Multivariate Normal Distribution documentation).");
        }
        else if (options.Robust)
        {
            svd = new SingularValueDecomposition(cov, true, true, true);
        }
        else
        {
            chol = new CholeskyDecomposition(cov);
            var regularization = options.Regularization;
            if (regularization > 0.0)
            {
                var num = cov.Rows();
                while (!chol.IsPositiveDefinite)
                {
                    for (var index1 = 0; index1 < num; ++index1)
                    {
                        for (var index2 = 0; index2 < num; ++index2)
                            if (double.IsNaN(cov[index1, index2]) || double.IsInfinity(cov[index1, index2]))
                                cov[index1, index2] = 0.0;

                        cov[index1, index1] += regularization;
                    }

                    chol = new CholeskyDecomposition(cov, inPlace: true);
                }
            }

            if (!chol.IsPositiveDefinite)
                throw new NonPositiveDefiniteMatrixException(
                    "Covariance matrix is not positive definite. Try specifying a regularization constant in the fitting options (there is an example in the Multivariate Normal Distribution documentation).");
        }
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        return string.Format(formatProvider, "Normal(X; μ, Σ)");
    }
}