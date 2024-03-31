using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public class KolmogorovSmirnovDistribution([Positive] double samples) : UnivariateContinuousDistribution, IFormattable
{
    public double NumberOfSamples { get; private set; } = samples > 0.0 ? samples : throw new ArgumentOutOfRangeException(nameof(samples), "The number of samples must be positive.");

    public override DoubleRange Support => new(0.5 / NumberOfSamples, 1.0);

    public override double Mean => 0.8687311606361592 / Math.Sqrt(NumberOfSamples);

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "KS(x; n = {0})", NumberOfSamples.ToString(format, formatProvider));
    }

    protected internal override double InnerDistributionFunction(double x)
    {
        return CumulativeFunction(NumberOfSamples, x);
    }

    protected internal override double InnerProbabilityDensityFunction(double x)
    {
        throw new NotSupportedException();
    }

    protected internal override double InnerComplementaryDistributionFunction(double x)
    {
        return ComplementaryDistributionFunction(NumberOfSamples, x);
    }

    public double OneSideDistributionFunction(double x)
    {
        return OneSideUpperTail(NumberOfSamples, x);
    }

    public static double CumulativeFunction(double n, double x)
    {
        if (double.IsNaN(x))
            throw new ArgumentOutOfRangeException(nameof(x));
        var num = n * x * x;
        var n1 = (int)Math.Ceiling(n);
        if (x >= 1.0 || num >= 18.0)
            return 1.0;
        if (x <= 0.5 / n)
            return 0.0;
        if (n == 1.0)
            return 2.0 * x - 1.0;
        if (x <= 1.0 / n)
            return n > 20.0 ? Math.Exp(Specials.LogFactorial(n) + n * Math.Log(2.0 * x - 1.0 / n)) : Specials.Factorial(n) * Math.Pow(2.0 * x - 1.0 / n, n);
        if (x >= 1.0 - 1.0 / n)
            return 1.0 - 2.0 * Math.Pow(1.0 - x, n);
        if (!(n <= 140.0)) return n <= 100000.0 && n * num * x <= 1.96 ? Durbin(n1, x) : PelzGood(n, x);
        if (num < 0.754693) return Durbin(n1, x);
        return num < 4.0 ? Pomeranz(n1, x) : 1.0 - ComplementaryDistributionFunction(n, x);

    }

    public static double ComplementaryDistributionFunction(double n, double x)
    {
        var num = n * x * x;
        if (x >= 1.0 || num >= 370.0)
            return 0.0;
        if (x <= 0.5 / n || num <= 0.0274)
            return 1.0;
        if (n == 1.0)
            return 2.0 - 2.0 * x;
        if (x <= 1.0 / n)
            return n > 20.0 ? 1.0 - Math.Exp(Specials.LogFactorial(n) + n * Math.Log(2.0 * x - 1.0 / n)) : 1.0 - Specials.Factorial(n) * Math.Pow(2.0 * x - 1.0 / n, n);
        if (x >= 1.0 - 1.0 / n)
            return 2.0 * Math.Pow(1.0 - x, n);
        return n <= 140.0 ? num >= 4.0 ? 2.0 * OneSideUpperTail(n, x) : 1.0 - CumulativeFunction(n, x) : num >= 2.2 ? 2.0 * OneSideUpperTail(n, x) : 1.0 - CumulativeFunction(n, x);
    }

    public static double PelzGood(double n, double x)
    {
        var num1 = Math.Sqrt(n);
        var num2 = num1 * x;
        var num3 = num2 * num2;
        var num4 = num3 * num2;
        var num5 = num3 * num3;
        var num6 = num5 * num3;
        var num7 = num5 * num4;
        var num8 = num5 * num5;
        var num9 = num8 * num3;
        var num10 = -9.869604401089358 / (2.0 * num3);
        var num11 = 0.0;
        for (var index = 0; index <= 20; ++index)
        {
            var num12 = index + 0.5;
            double num13;
            num11 += num13 = Math.Exp(num12 * num12 * num10);
            if (num13 <= 1E-10 * num11)
                break;
        }

        var num14 = 0.0;
        for (var index = 0; index <= 20; ++index)
        {
            var num15 = (index + 0.5) * (index + 0.5);
            double num16;
            num14 += num16 = (9.869604401089358 * num15 - num3) * Math.Exp(num15 * num10);
            if (Math.Abs(num16) <= 1E-10 * Math.Abs(num14))
                break;
        }

        var num17 = 0.0;
        for (var index = 0; index <= 20; ++index)
        {
            var num18 = (index + 0.5) * (index + 0.5);
            double num19;
            num17 += num19 = (6.0 * num6 + 2.0 * num5 + 9.869604401089358 * (2.0 * num5 - 5.0 * num3) * num18 + 97.40909103400243 * (1.0 - 2.0 * num3) * num18 * num18) *
                             Math.Exp(num18 * num10);
            if (Math.Abs(num19) <= 1E-10 * Math.Abs(num17))
                break;
        }

        var num20 = 0.0;
        for (var index = 1; index <= 20; ++index)
        {
            double num21 = index * index;
            double num22;
            num20 += num22 = 9.869604401089358 * num21 * Math.Exp(num21 * num10);
            if (num22 <= 1E-10 * num20)
                break;
        }

        var num23 = 0.0;
        for (var index = 0; index <= 20; ++index)
        {
            var num24 = (index + 0.5) * (index + 0.5);
            double num25;
            num23 += num25 = (-30.0 * num6 - 90.0 * num8 + 9.869604401089358 * (135.0 * num5 - 96.0 * num6) * num24 +
                              97.40909103400243 * (212.0 * num5 - 60.0 * num3) * num24 * num24 + 961.3891935753043 * num24 * num24 * num24 * (5.0 - 30.0 * num3)) *
                             Math.Exp(num24 * num10);
            if (Math.Abs(num25) <= 1E-10 * Math.Abs(num23))
                break;
        }

        var num26 = 0.0;
        for (var index = 1; index <= 20; ++index)
        {
            double num27 = index * index;
            double num28;
            num26 += num28 = (29.608813203268074 * num27 * num3 - 97.40909103400243 * num27 * num27) * Math.Exp(num27 * num10);
            if (Math.Abs(num28) <= 1E-10 * Math.Abs(num26))
                break;
        }

        return num11 * (2.5066282746310007 / num2) + num14 * (1.2533141373155003 / (num1 * 3.0 * num5)) + num17 * (1.2533141373155003 / (n * 36.0 * num7)) -
            num20 * (1.2533141373155003 / (n * 18.0 * num4)) + num23 * (1.2533141373155003 / (n * num1 * 3240.0 * num9)) + num26 * (1.2533141373155003 / (n * num1 * 108.0 * num6));
    }

    public static double OneSideUpperTail(double n, double x)
    {
        if (n > 200000.0)
        {
            var num1 = 6.0 * n * x + 1.0;
            var num2 = num1 * num1 / (18.0 * n);
            var num3 = (1.0 - (2.0 * num2 * num2 - 4.0 * num2 - 1.0) / (18.0 * n)) * Math.Exp(-num2);
            if (num3 <= 0.0)
                return 0.0;
            return num3 >= 1.0 ? 1.0 : 1.0 * num3;
        }

        var num4 = (int)(n * (1.0 - x));
        if (1.0 - x - num4 / n <= 0.0)
            --num4;
        var num5 = n > 3000.0 ? 2 : 3;
        var k = num4 / num5 + 1;
        var num6 = Specials.LogBinomial(n, k);
        var num7 = num6;
        const double num8 = 1E-12;
        var num9 = 0.0;
        for (var index = k; index <= num4; ++index)
        {
            var d = index / n + x;
            var num10 = Math.Exp(num6 + (index - 1) * Math.Log(d) + (n - index) * Specials.Log1P(-d));
            num9 += num10;
            num6 += Math.Log((n - index) / (index + 1));
            if (num10 <= num9 * num8)
                break;
        }

        var num11 = num4 / num5;
        var num12 = num7 + Math.Log((num11 + 1) / (n - num11));
        for (var index = num11; index > 0; --index)
        {
            var d = index / n + x;
            var num13 = Math.Exp(num12 + (index - 1) * Math.Log(d) + (n - index) * Specials.Log1P(-d));
            num9 += num13;
            num12 += Math.Log(index / (n - index + 1.0));
            if (num13 <= num9 * num8)
                break;
        }

        return num9 * x + Math.Exp(n * Specials.Log1P(-x));
    }

    public static double Pomeranz(int n, double x)
    {
        const double num1 = 1E-15;
        const int y = 350;
        var num2 = Math.Pow(2.0, y);
        var t = n * x;
        var a = new double[2 * n + 3];
        var floors = new double[2 * n + 3];
        var ceilings = new double[2 * n + 3];
        var numArray = new double[2][];
        for (var index = 0; index < numArray.Length; ++index)
            numArray[index] = new double[n + 2];
        var h = new double[4][];
        for (var index = 0; index < h.Length; ++index)
            h[index] = new double[n + 2];
        var limits = ComputeLimits(t, floors, ceilings);
        ComputeA(n, a, limits);
        ComputeH(n, a, h);
        numArray[1][1] = num2;
        var num3 = 1;
        var index1 = 0;
        var index2 = 1;
        for (var index3 = 2; index3 <= 2 * n + 2; ++index3)
        {
            var num4 = (int)(2.0 + floors[index3]);
            if (num4 < 1)
                num4 = 1;
            var num5 = (int)ceilings[index3];
            if (num5 > n + 1)
                num5 = n + 1;
            var num6 = (int)(2.0 + floors[index3 - 1]);
            if (num6 < 1)
                num6 = 1;
            var num7 = (int)ceilings[index3 - 1];
            var num8 = (a[index3] - a[index3 - 1]) / n;
            var index4 = -1;
            for (var index5 = 0; index5 < 4; ++index5)
                if (Math.Abs(num8 - h[index5][1]) <= num1)
                {
                    index4 = index5;
                    break;
                }

            var num9 = num2;
            index1 = (index1 + 1) & 1;
            index2 = (index2 + 1) & 1;
            for (var index6 = num4; index6 <= num5; ++index6)
            {
                var num10 = num7;
                if (num10 > index6)
                    num10 = index6;
                var num11 = 0.0;
                for (var index7 = num10; index7 >= num6; --index7)
                    num11 += numArray[index1][index7] * h[index4][index6 - index7];
                numArray[index2][index6] = num11;
                if (num11 < num9)
                    num9 = num11;
            }

            if (!(num9 < 1E-280)) continue;
            for (var index8 = num4; index8 <= num5; ++index8)
                numArray[index2][index8] *= num2;
            ++num3;
        }

        var d1 = numArray[index2][n + 1];
        var d2 = Specials.LogFactorial(n) - num3 * y * 0.6931471805599453 + Math.Log(d1);
        return d2 >= 0.0 ? 1.0 : Math.Exp(d2);
    }

    public static double Durbin(int n, double d)
    {
        var num1 = (int)(n * d) + 1;
        var length = 2 * num1 - 1;
        var x = num1 - n * d;
        var a = new double[length, length];
        var v = new double[length, length];
        var b = new double[length, length];
        for (var index1 = 0; index1 < length; ++index1)
            for (var index2 = 0; index2 < length; ++index2)
                if (index1 - index2 + 1 >= 0)
                    a[index1, index2] = 1.0;
        for (var index = 0; index < length; ++index)
        {
            a[index, 0] -= Math.Pow(x, index + 1);
            a[length - 1, index] -= Math.Pow(x, length - index);
        }

        a[length - 1, 0] += 2.0 * x - 1.0 > 0.0 ? Math.Pow(2.0 * x - 1.0, length) : 0.0;
        for (var index3 = 0; index3 < length; ++index3)
            for (var index4 = 0; index4 < length; ++index4)
                if (index3 - index4 + 1 > 0)
                    for (var index5 = 1; index5 <= index3 - index4 + 1; ++index5)
                        a[index3, index4] /= index5;
        var eV = 0;
        MatrixPower(a, 0, v, ref eV, length, n, b);
        var num2 = v[num1 - 1, num1 - 1];
        for (var index = 1; index <= n; ++index)
        {
            num2 *= index / (double)n;
            if (!(num2 < 1E-140)) continue;
            num2 *= 1E+140;
            eV -= 140;
        }

        return num2 * Math.Pow(10.0, eV);
    }

    private static void MatrixPower(
        double[,] a,
        int eA,
        double[,] v,
        ref int eV,
        int m,
        int n,
        double[,] b)
    {
        if (n == 1)
        {
            for (var index1 = 0; index1 < m; ++index1)
                for (var index2 = 0; index2 < m; ++index2)
                    v[index1, index2] = a[index1, index2];
            eV = eA;
        }
        else
        {
            MatrixPower(a, eA, v, ref eV, m, n / 2, b);
            v.Dot(v, b);
            var num = 2 * eV;
            if (b[m / 2, m / 2] > 1E+140)
            {
                for (var index3 = 0; index3 < m; ++index3)
                    for (var index4 = 0; index4 < m; ++index4)
                        b[index3, index4] *= 1E-140;
                num += 140;
            }

            if (n % 2 == 0)
            {
                for (var index5 = 0; index5 < m; ++index5)
                    for (var index6 = 0; index6 < m; ++index6)
                        v[index5, index6] = b[index5, index6];
                eV = num;
            }
            else
            {
                a.Dot(b, v);
                eV = eA + num;
            }

            if (v[m / 2, m / 2] <= 1E+140)
                return;
            for (var index7 = 0; index7 < m; ++index7)
                for (var index8 = 0; index8 < m; ++index8)
                    v[index7, index8] *= 1E-140;
            eV += 140;
        }
    }

    private static double ComputeLimits(double t, IList<double> floors, IList<double> ceilings)
    {
        var num1 = Math.Floor(t);
        var num2 = Math.Ceiling(t);
        var limits = t - num1;
        var num4 = num2 - t;
        switch (limits)
        {
            case > 0.5:
            {
                for (var index = 1; index < floors.Count; index += 2)
                    floors[index] = index / 2 - 1 - num1;
                for (var index = 2; index < floors.Count; index += 2)
                    floors[index] = index / 2 - 2 - num1;
                for (var index = 1; index < ceilings.Count; index += 2)
                    ceilings[index] = index / 2 + 1 + num1;
                for (var index = 2; index < ceilings.Count; index += 2)
                    ceilings[index] = index / 2 + num1;
                break;
            }
            case > 0.0:
            {
                ceilings[1] = 1.0 + num1;
                for (var index = 1; index < floors.Count; ++index)
                    floors[index] = index / 2 - 1 - num1;
                for (var index = 2; index < ceilings.Count; ++index)
                    ceilings[index] = index / 2 + num1;
                break;
            }
            default:
            {
                for (var index = 1; index < floors.Count; index += 2)
                    floors[index] = index / 2 - num1;
                for (var index = 2; index < floors.Count; index += 2)
                    floors[index] = index / 2 - 1 - num1;
                for (var index = 1; index < ceilings.Count; index += 2)
                    ceilings[index] = index / 2 + num1;
                for (var index = 2; index < ceilings.Count; index += 2)
                    ceilings[index] = index / 2 - 1 + num1;
                break;
            }
        }

        if (num4 < limits)
            limits = num4;
        return limits;
    }

    private static void ComputeA(int n, IList<double> a, double z)
    {
        a[0] = 0.0;
        a[1] = 0.0;
        a[2] = z;
        a[3] = 1.0 - z;
        for (var index = 4; index < a.Count - 1; ++index)
            a[index] = a[index - 2] + 1.0;
        a[^1] = n;
    }

    private static double ComputeH(int n, IReadOnlyList<double> a, IReadOnlyList<double[]> hInput)
    {
        hInput[0][0] = 1.0;
        var num1 = 2.0 * a[2] / n;
        for (var index = 1; index <= n + 1; ++index)
            hInput[0][index] = num1 * hInput[0][index - 1] / index;
        hInput[1][0] = 1.0;
        var num2 = (1.0 - 2.0 * a[2]) / n;
        for (var index = 1; index <= n + 1; ++index)
            hInput[1][index] = num2 * hInput[1][index - 1] / index;
        hInput[2][0] = 1.0;
        var h = a[2] / n;
        for (var index = 1; index <= n + 1; ++index)
            hInput[2][index] = h * hInput[2][index - 1] / index;
        hInput[3][0] = 1.0;
        for (var index = 1; index <= n + 1; ++index)
            hInput[3][index] = 0.0;
        return h;
    }
}