using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public class KolmogorovSmirnovDistribution([Positive] double samples) : UnivariateContinuousDistribution, IFormattable
{
    public double NumberOfSamples { get; private set; } = samples > 0.0 ? samples : throw new ArgumentOutOfRangeException(nameof(samples), "The number of samples must be positive.");

    public override DoubleRange Support => new(0.5 / NumberOfSamples, 1.0);

    public override double Mean => 0.8687311606361592 / Math.Sqrt(NumberOfSamples);

    public override double Mode => throw new NotSupportedException();

    public override double Variance => (0.8224670334241132 - Mean * Mean) / NumberOfSamples;

    public override double Entropy => throw new NotSupportedException();

    public override string ToString(string format, IFormatProvider formatProvider)
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

    public override object Clone()
    {
        return new KolmogorovSmirnovDistribution(NumberOfSamples);
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
        if (n <= 140.0)
        {
            if (num < 0.754693)
                return Durbin(n1, x);
            return num < 4.0 ? Pomeranz(n1, x) : 1.0 - ComplementaryDistributionFunction(n, x);
        }

        return n <= 100000.0 && n * num * x <= 1.96 ? Durbin(n1, x) : PelzGood(n, x);
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
        var num8 = 1E-12;
        var num9 = 0.0;
        for (var index = k; index <= num4; ++index)
        {
            var d = index / n + x;
            var num10 = Math.Exp(num6 + (index - 1) * Math.Log(d) + (n - index) * Specials.Log1p(-d));
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
            var num13 = Math.Exp(num12 + (index - 1) * Math.Log(d) + (n - index) * Specials.Log1p(-d));
            num9 += num13;
            num12 += Math.Log(index / (n - index + 1.0));
            if (num13 <= num9 * num8)
                break;
        }

        return num9 * x + Math.Exp(n * Specials.Log1p(-x));
    }

    public static double Pomeranz(int n, double x)
    {
        var num1 = 1E-15;
        var y = 350;
        var num2 = Math.Pow(2.0, y);
        var t = n * x;
        var A = new double[2 * n + 3];
        var floors = new double[2 * n + 3];
        var ceilings = new double[2 * n + 3];
        var numArray = new double[2][];
        for (var index = 0; index < numArray.Length; ++index)
            numArray[index] = new double[n + 2];
        var H = new double[4][];
        for (var index = 0; index < H.Length; ++index)
            H[index] = new double[n + 2];
        var limits = computeLimits(t, floors, ceilings);
        computeA(n, A, limits);
        computeH(n, A, H);
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
            var num8 = (A[index3] - A[index3 - 1]) / n;
            var index4 = -1;
            for (var index5 = 0; index5 < 4; ++index5)
                if (Math.Abs(num8 - H[index5][1]) <= num1)
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
                    num11 += numArray[index1][index7] * H[index4][index6 - index7];
                numArray[index2][index6] = num11;
                if (num11 < num9)
                    num9 = num11;
            }

            if (num9 < 1E-280)
            {
                for (var index8 = num4; index8 <= num5; ++index8)
                    numArray[index2][index8] *= num2;
                ++num3;
            }
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
        var A = new double[length, length];
        var V = new double[length, length];
        var B = new double[length, length];
        for (var index1 = 0; index1 < length; ++index1)
            for (var index2 = 0; index2 < length; ++index2)
                if (index1 - index2 + 1 >= 0)
                    A[index1, index2] = 1.0;
        for (var index = 0; index < length; ++index)
        {
            A[index, 0] -= Math.Pow(x, index + 1);
            A[length - 1, index] -= Math.Pow(x, length - index);
        }

        A[length - 1, 0] += 2.0 * x - 1.0 > 0.0 ? Math.Pow(2.0 * x - 1.0, length) : 0.0;
        for (var index3 = 0; index3 < length; ++index3)
            for (var index4 = 0; index4 < length; ++index4)
                if (index3 - index4 + 1 > 0)
                    for (var index5 = 1; index5 <= index3 - index4 + 1; ++index5)
                        A[index3, index4] /= index5;
        var eV = 0;
        matrixPower(A, 0, V, ref eV, length, n, B);
        var num2 = V[num1 - 1, num1 - 1];
        for (var index = 1; index <= n; ++index)
        {
            num2 *= index / (double)n;
            if (num2 < 1E-140)
            {
                num2 *= 1E+140;
                eV -= 140;
            }
        }

        return num2 * Math.Pow(10.0, eV);
    }

    private static void matrixPower(
        double[,] A,
        int eA,
        double[,] V,
        ref int eV,
        int m,
        int n,
        double[,] B)
    {
        if (n == 1)
        {
            for (var index1 = 0; index1 < m; ++index1)
                for (var index2 = 0; index2 < m; ++index2)
                    V[index1, index2] = A[index1, index2];
            eV = eA;
        }
        else
        {
            matrixPower(A, eA, V, ref eV, m, n / 2, B);
            V.Dot(V, B);
            var num = 2 * eV;
            if (B[m / 2, m / 2] > 1E+140)
            {
                for (var index3 = 0; index3 < m; ++index3)
                    for (var index4 = 0; index4 < m; ++index4)
                        B[index3, index4] *= 1E-140;
                num += 140;
            }

            if (n % 2 == 0)
            {
                for (var index5 = 0; index5 < m; ++index5)
                    for (var index6 = 0; index6 < m; ++index6)
                        V[index5, index6] = B[index5, index6];
                eV = num;
            }
            else
            {
                A.Dot(B, V);
                eV = eA + num;
            }

            if (V[m / 2, m / 2] <= 1E+140)
                return;
            for (var index7 = 0; index7 < m; ++index7)
                for (var index8 = 0; index8 < m; ++index8)
                    V[index7, index8] *= 1E-140;
            eV += 140;
        }
    }

    private static double computeLimits(double t, double[] floors, double[] ceilings)
    {
        var num1 = Math.Floor(t);
        var num2 = Math.Ceiling(t);
        var limits = t - num1;
        var num3 = t;
        var num4 = num2 - num3;
        if (limits > 0.5)
        {
            for (var index = 1; index < floors.Length; index += 2)
                floors[index] = index / 2 - 1 - num1;
            for (var index = 2; index < floors.Length; index += 2)
                floors[index] = index / 2 - 2 - num1;
            for (var index = 1; index < ceilings.Length; index += 2)
                ceilings[index] = index / 2 + 1 + num1;
            for (var index = 2; index < ceilings.Length; index += 2)
                ceilings[index] = index / 2 + num1;
        }
        else if (limits > 0.0)
        {
            ceilings[1] = 1.0 + num1;
            for (var index = 1; index < floors.Length; ++index)
                floors[index] = index / 2 - 1 - num1;
            for (var index = 2; index < ceilings.Length; ++index)
                ceilings[index] = index / 2 + num1;
        }
        else
        {
            for (var index = 1; index < floors.Length; index += 2)
                floors[index] = index / 2 - num1;
            for (var index = 2; index < floors.Length; index += 2)
                floors[index] = index / 2 - 1 - num1;
            for (var index = 1; index < ceilings.Length; index += 2)
                ceilings[index] = index / 2 + num1;
            for (var index = 2; index < ceilings.Length; index += 2)
                ceilings[index] = index / 2 - 1 + num1;
        }

        if (num4 < limits)
            limits = num4;
        return limits;
    }

    private static void computeA(int n, double[] A, double z)
    {
        A[0] = 0.0;
        A[1] = 0.0;
        A[2] = z;
        A[3] = 1.0 - z;
        for (var index = 4; index < A.Length - 1; ++index)
            A[index] = A[index - 2] + 1.0;
        A[A.Length - 1] = n;
    }

    private static double computeH(int n, double[] A, double[][] H)
    {
        H[0][0] = 1.0;
        var num1 = 2.0 * A[2] / n;
        for (var index = 1; index <= n + 1; ++index)
            H[0][index] = num1 * H[0][index - 1] / index;
        H[1][0] = 1.0;
        var num2 = (1.0 - 2.0 * A[2]) / n;
        for (var index = 1; index <= n + 1; ++index)
            H[1][index] = num2 * H[1][index - 1] / index;
        H[2][0] = 1.0;
        var h = A[2] / n;
        for (var index = 1; index <= n + 1; ++index)
            H[2][index] = h * H[2][index - 1] / index;
        H[3][0] = 1.0;
        for (var index = 1; index <= n + 1; ++index)
            H[3][index] = 0.0;
        return h;
    }
}