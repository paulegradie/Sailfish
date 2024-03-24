using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public static class Gamma
{
    private static readonly double[] gamma_P =
    [
        0.00016011952247675185,
        0.0011913514700658638,
        0.010421379756176158,
        0.04763678004571372,
        0.20744822764843598,
        0.4942148268014971,
        1.0
    ];

    private static readonly double[] gamma_Q =
    [
        -2.3158187332412014E-05,
        0.0005396055804933034,
        -0.004456419138517973,
        0.011813978522206043,
        0.035823639860549865,
        -0.23459179571824335,
        0.0714304917030273,
        1.0
    ];

    private static readonly double[] STIR =
    [
        0.0007873113957930937,
        -0.00022954996161337813,
        -0.0026813261780578124,
        1.0 / 288.0,
        1.0 / 12.0
    ];

    private static readonly double[] log_A =
    [
        0.0008116141674705085,
        -0.0005950619042843014,
        0.0007936503404577169,
        -1.0 / 360.0,
        1.0 / 12.0
    ];

    private static readonly double[] log_B =
    [
        -1378.2515256912086,
        -38801.631513463784,
        -331612.9927388712,
        -1162370.974927623,
        -1721737.0082083966,
        -853555.6642457654
    ];

    private static readonly double[] log_C =
    [
        -351.81570143652345,
        -17064.210665188115,
        -220528.59055385445,
        -1139334.4436798252,
        -2532523.0717758294,
        -2018891.4143353277
    ];

    public static double Function(double x)
    {
        var num1 = Math.Abs(x);
        if (num1 > 33.0)
        {
            if (x >= 0.0)
                return Stirling(x);
            var num2 = Math.Floor(num1);
            if (num2 == num1)
                throw new OverflowException();
            var num3 = num1 - num2;
            if (num3 > 0.5)
            {
                var num4 = num2 + 1.0;
                num3 = num1 - num4;
            }

            var num5 = num1 * Math.Sin(Math.PI * num3);
            if (num5 == 0.0)
                throw new OverflowException();
            return -(Math.PI / (Math.Abs(num5) * Stirling(num1)));
        }

        var num6 = 1.0;
        while (x >= 3.0)
        {
            --x;
            num6 *= x;
        }

        for (; x < 0.0; ++x)
        {
            if (x == 0.0)
                throw new ArithmeticException();
            if (x > -1E-09)
                return num6 / ((1.0 + 0.5772156649015329 * x) * x);
            num6 /= x;
        }

        for (; x < 2.0; ++x)
        {
            if (x == 0.0)
                throw new ArithmeticException();
            if (x < 1E-09)
                return num6 / ((1.0 + 0.5772156649015329 * x) * x);
            num6 /= x;
        }

        if (x is 2.0 or 3.0)
            return num6;
        x -= 2.0;
        var num7 = Specials.Polevl(x, gamma_P, 6);
        var num8 = Specials.Polevl(x, gamma_Q, 7);
        return num6 * num7 / num8;
    }

    public static double Stirling(double x)
    {
        var num1 = 143.01608;
        var x1 = 1.0 / x;
        var d1 = Math.Exp(x);
        var num2 = 1.0 + x1 * Specials.Polevl(x1, STIR, 4);
        double num3;
        if (x > num1)
        {
            var d2 = Math.Pow(x, 0.5 * x - 0.25);
            num3 = !double.IsPositiveInfinity(d2) || !double.IsPositiveInfinity(d1) ? d2 * (d2 / d1) : double.PositiveInfinity;
        }
        else
        {
            num3 = Math.Pow(x, x - 0.5) / d1;
        }

        return 2.5066282746310007 * num3 * num2;
    }

    public static double UpperIncomplete(double a, double x)
    {
        if (x <= 0.0 || a <= 0.0)
            return 1.0;
        if (x < 1.0 || x < a)
            return 1.0 - LowerIncomplete(a, x);
        if (double.IsPositiveInfinity(x))
            return 0.0;
        var d = a * Math.Log(x) - x - Log(a);
        if (d < -709.782712893384)
            return 0.0;
        var num1 = Math.Exp(d);
        var num2 = 1.0 - a;
        var num3 = x + num2 + 1.0;
        var num4 = 0.0;
        var num5 = 1.0;
        var num6 = x;
        var num7 = x + 1.0;
        var num8 = num3 * x;
        var num9 = num7 / num8;
        double num10;
        do
        {
            ++num4;
            ++num2;
            num3 += 2.0;
            var num11 = num2 * num4;
            var num12 = num7 * num3 - num5 * num11;
            var num13 = num8 * num3 - num6 * num11;
            if (num13 != 0.0)
            {
                var num14 = num12 / num13;
                num10 = Math.Abs((num9 - num14) / num14);
                num9 = num14;
            }
            else
            {
                num10 = 1.0;
            }

            num5 = num7;
            num7 = num12;
            num6 = num8;
            num8 = num13;
            if (Math.Abs(num12) > 4503599627370496.0)
            {
                num5 *= 2.220446049250313E-16;
                num7 *= 2.220446049250313E-16;
                num6 *= 2.220446049250313E-16;
                num8 *= 2.220446049250313E-16;
            }
        } while (num10 > 1.1102230246251565E-16);

        return num9 * num1;
    }

    public static double LowerIncomplete(double a, double x)
    {
        if (a <= 0.0)
            return 1.0;
        if (x <= 0.0)
            return 0.0;
        if (x > 1.0 && x > a)
            return 1.0 - UpperIncomplete(a, x);
        var d = a * Math.Log(x) - x - Log(a);
        if (d < -709.782712893384)
            return 0.0;
        var num1 = Math.Exp(d);
        var num2 = a;
        var num3 = 1.0;
        var num4 = 1.0;
        do
        {
            ++num2;
            num3 *= x / num2;
            num4 += num3;
        } while (num3 / num4 > 1.1102230246251565E-16);

        return num4 * num1 / a;
    }

    public static double Log(double x)
    {
        if (x == 0.0)
            return double.PositiveInfinity;
        if (x < -34.0)
        {
            var num1 = -x;
            var num2 = Log(num1);
            var num3 = Math.Floor(num1);
            if (num3 == num1)
                throw new OverflowException();
            var num4 = num1 - num3;
            if (num4 > 0.5)
                num4 = num3 + 1.0 - num1;
            var d = num1 * Math.Sin(Math.PI * num4);
            if (d == 0.0)
                throw new OverflowException();
            return 1.1447298858494002 - Math.Log(d) - num2;
        }

        if (x < 13.0)
        {
            var d = 1.0;
            while (x >= 3.0)
            {
                --x;
                d *= x;
            }

            for (; x < 2.0; ++x)
            {
                if (x == 0.0)
                    throw new OverflowException();
                d /= x;
            }

            if (d < 0.0)
                d = -d;
            if (x == 2.0)
                return Math.Log(d);
            x -= 2.0;
            var num = x * Specials.Polevl(x, log_B, 5) / Specials.P1evl(x, log_C, 6);
            return Math.Log(d) + num;
        }

        if (x > 2.556348E+305)
            throw new OverflowException();
        var num5 = (x - 0.5) * Math.Log(x) - x + 0.9189385332046728;
        if (x > 100000000.0)
            return num5;
        var x1 = 1.0 / (x * x);
        return x < 1000.0 ? num5 + Specials.Polevl(x1, log_A, 4) / x : num5 + ((0.0007936507936507937 * x1 - 1.0 / 360.0) * x1 + 1.0 / 12.0) / x;
    }
}