using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

public static class Specials
{
    private static readonly double[] ErfcP =
    [
        2.461969814735305E-10,
        0.5641895648310689,
        7.463210564422699,
        48.63719709856814,
        196.5208329560771,
        526.4451949954773,
        934.5285271719576,
        1027.5518868951572,
        557.5353353693994
    ];

    private static readonly double[] ErfcQ =
    [
        13.228195115474499,
        86.70721408859897,
        354.9377788878199,
        975.7085017432055,
        1823.9091668790973,
        2246.3376081871097,
        1656.6630919416134,
        557.5353408177277
    ];

    private static readonly double[] ErfcR =
    [
        0.5641895835477551,
        1.275366707599781,
        5.019050422511805,
        6.160210979930536,
        7.4097426995044895,
        2.9788666537210022
    ];

    private static readonly double[] ErfcS =
    [
        2.2605286322011726,
        9.396035249380015,
        12.048953980809666,
        17.08144507475659,
        9.608968090632859,
        3.369076451000815
    ];

    private static readonly double[] ErfcT =
    [
        9.604973739870516,
        90.02601972038427,
        2232.005345946843,
        7003.325141128051,
        55592.30130103949
    ];

    private static readonly double[] ErfcU =
    [
        33.56171416475031,
        521.3579497801527,
        4594.323829709801,
        22629.000061389095,
        49267.39426086359
    ];

    private static double[]? _lnfcache;

    public static double Erfc(double value)
    {
        var x = value >= 0.0 ? value : -value;
        if (x < 1.0)
            return 1.0 - Erf(value);
        var d = -value * value;
        if (d < -709.782712893384)
            return value < 0.0 ? 2.0 : 0.0;
        var num1 = Math.Exp(d);
        double num2;
        double num3;
        if (x < 8.0)
        {
            num2 = Polevl(x, ErfcP, 8);
            num3 = P1Evl(x, ErfcQ, 8);
        }
        else
        {
            num2 = Polevl(x, ErfcR, 5);
            num3 = P1Evl(x, ErfcS, 6);
        }

        var num4 = num1 * num2 / num3;
        if (value < 0.0)
            num4 = 2.0 - num4;
        if (num4 != 0.0)
            return num4;
        return value < 0.0 ? 2.0 : 0.0;
    }

    public static double Erf(double x)
    {
        if (Math.Abs(x) > 1.0)
            return 1.0 - Erfc(x);
        var x1 = x * x;
        return x * Polevl(x1, ErfcT, 4) / P1Evl(x1, ErfcU, 5);
    }

    public static double Polevl(double x, double[] coefficient, int n)
    {
        var firstCoefficient = coefficient[0];
        for (var index = 1; index <= n; ++index)
            firstCoefficient = firstCoefficient * x + coefficient[index];
        return firstCoefficient;
    }

    public static double P1Evl(double x, double[] coefficient, int n)
    {
        var firstCoefficient = x + coefficient[0];
        for (var index = 1; index < n; ++index)
            firstCoefficient = firstCoefficient * x + coefficient[index];
        return firstCoefficient;
    }

    public static double Binomial(int n, int k)
    {
        return Math.Round(Math.Exp(LogFactorial(n) - LogFactorial(k) - LogFactorial(n - k)));
    }

    public static double LogBinomial(double n, double k)
    {
        return LogFactorial(n) - LogFactorial(k) - LogFactorial(n - k);
    }

    public static double Factorial(double n)
    {
        return Gamma.Function(n + 1.0);
    }

    public static double LogFactorial(double n)
    {
        return Gamma.Log(n + 1.0);
    }

    public static double LogFactorial(int n)
    {
        _lnfcache ??= new double[101];
        if (n < 0)
            throw new ArgumentException("Argument cannot be negative.", nameof(n));
        if (n <= 1)
            return 0.0;
        if (n > 100)
            return Gamma.Log(n + 1.0);
        return _lnfcache[n] <= 0.0 ? _lnfcache[n] = Gamma.Log(n + 1.0) : _lnfcache[n];
    }

    public static double Log1P(double x)
    {
        if (x <= -1.0)
            return double.NaN;
        return Math.Abs(x) > 0.0001 ? Math.Log(1.0 + x) : (-0.5 * x + 1.0) * x;
    }
}