using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public static class Normal
{
    private static readonly double[] inverse_P0 =
    [
        -59.96335010141079,
        98.00107541859997,
        -56.67628574690703,
        13.931260938727968,
        -1.2391658386738125
    ];

    private static readonly double[] inverse_Q0 =
    [
        1.9544885833814176,
        4.676279128988815,
        86.36024213908905,
        -225.46268785411937,
        200.26021238006066,
        -82.03722561683334,
        15.90562251262117,
        -1.1833162112133
    ];

    private static readonly double[] inverse_P1 =
    [
        4.0554489230596245,
        31.525109459989388,
        57.16281922464213,
        44.08050738932008,
        14.684956192885803,
        2.1866330685079025,
        -0.1402560791713545,
        -0.03504246268278482,
        -0.0008574567851546854
    ];

    private static readonly double[] inverse_Q1 =
    [
        15.779988325646675,
        45.39076351288792,
        41.3172038254672,
        15.04253856929075,
        2.504649462083094,
        -0.14218292285478779,
        -0.03808064076915783,
        -0.0009332594808954574
    ];

    private static readonly double[] inverse_P2 =
    [
        3.2377489177694603,
        6.915228890689842,
        3.9388102529247444,
        1.3330346081580755,
        0.20148538954917908,
        0.012371663481782003,
        0.00030158155350823543,
        2.6580697468673755E-06,
        6.239745391849833E-09
    ];

    private static readonly double[] inverse_Q2 =
    [
        6.02427039364742,
        3.6798356385616087,
        1.3770209948908132,
        0.21623699359449663,
        0.013420400608854318,
        0.00032801446468212774,
        2.8924786474538068E-06,
        6.790194080099813E-09
    ];

    private static readonly double[] BVND_WN20 =
    [
        0.01761400713915212,
        0.04060142980038694,
        0.06267204833410905,
        0.08327674157670475,
        0.1019301198172404,
        0.1181945319615184,
        0.1316886384491766,
        0.1420961093183821,
        0.1491729864726037,
        0.1527533871307259
    ];

    private static readonly double[] BVND_XN20 =
    [
        -0.9931285991850949,
        -0.9639719272779138,
        -0.912234428251326,
        -0.8391169718222188,
        -0.7463319064601508,
        -0.636053680726515,
        -0.5108670019508271,
        -0.3737060887154196,
        -0.2277858511416451,
        -0.07652652113349732
    ];

    private static readonly double[] BVND_WN12 =
    [
        0.04717533638651177,
        0.1069393259953183,
        0.1600783285433464,
        0.2031674267230659,
        0.2334925365383547,
        0.2491470458134029
    ];

    private static readonly double[] BVND_XN12 =
    [
        -0.9815606342467191,
        -0.904117256370475,
        -0.769902674194305,
        -0.5873179542866171,
        -0.3678314989981802,
        -0.1252334085114692
    ];

    private static readonly double[] BVND_WN6 =
    [
        0.1713244923791705,
        0.3607615730481384,
        0.4679139345726904
    ];

    private static readonly double[] BVND_XN6 =
    [
        -0.9324695142031522,
        -0.6612093864662647,
        -0.238619186083197
    ];

    public static double Function(double value)
    {
        return 0.5 + 0.5 * Specials.Erf(value / 1.4142135623730951);
    }

    public static double Complemented(double value)
    {
        return 0.5 * Specials.Erfc(value / 1.4142135623730951);
    }

    public static double Inverse(double y0)
    {
        if (y0 <= 0.0)
        {
            if (y0 == 0.0)
                return double.NegativeInfinity;
            throw new ArgumentOutOfRangeException(nameof(y0));
        }

        if (y0 >= 1.0)
        {
            if (y0 == 1.0)
                return double.PositiveInfinity;
            throw new ArgumentOutOfRangeException(nameof(y0));
        }

        var num1 = Math.Sqrt(2.0 * Math.PI);
        var num2 = 1;
        var d1 = y0;
        if (d1 > 0.8646647167633873)
        {
            d1 = 1.0 - d1;
            num2 = 0;
        }

        if (d1 > 0.1353352832366127)
        {
            var num3 = d1 - 0.5;
            var x = num3 * num3;
            return (num3 + num3 * (x * Specials.Polevl(x, inverse_P0, 4) / Specials.P1evl(x, inverse_Q0, 8))) * num1;
        }

        var d2 = Math.Sqrt(-2.0 * Math.Log(d1));
        var num4 = d2 - Math.Log(d2) / d2;
        var x1 = 1.0 / d2;
        var num5 = d2 >= 8.0
            ? x1 * Specials.Polevl(x1, inverse_P2, 8) / Specials.P1evl(x1, inverse_Q2, 8)
            : x1 * Specials.Polevl(x1, inverse_P1, 8) / Specials.P1evl(x1, inverse_Q1, 8);
        var num6 = num4 - num5;
        if (num2 != 0)
            num6 = -num6;
        return num6;
    }

    public static double Bivariate(double x, double y, double rho)
    {
        return BVND(-x, -y, rho);
    }

    public static double BivariateComplemented(double x, double y, double rho)
    {
        return BVND(x, y, rho);
    }

    private static double BVND(double dh, double dk, double r)
    {
        double[] numArray1;
        double[] numArray2;
        if (Math.Abs(r) < 0.3)
        {
            numArray1 = BVND_XN6;
            numArray2 = BVND_WN6;
        }
        else if (Math.Abs(r) < 0.75)
        {
            numArray1 = BVND_XN12;
            numArray2 = BVND_WN12;
        }
        else
        {
            numArray1 = BVND_XN20;
            numArray2 = BVND_WN20;
        }

        var val1 = dh;
        var val2 = dk;
        var num1 = val1 * val2;
        var num2 = 0.0;
        if (Math.Abs(r) < 0.925)
        {
            if (Math.Abs(r) > 0.0)
            {
                var num3 = (val1 * val1 + val2 * val2) / 2.0;
                var num4 = Math.Asin(r);
                for (var index1 = 0; index1 < numArray1.Length; ++index1)
                    for (var index2 = -1; index2 <= 1; index2 += 2)
                    {
                        var num5 = Math.Sin(num4 * (index2 * numArray1[index1] + 1.0) / 2.0);
                        num2 += numArray2[index1] * Math.Exp((num5 * num1 - num3) / (1.0 - num5 * num5));
                    }

                num2 = num2 * num4 / (4.0 * Math.PI);
            }

            return num2 + Function(-val1) * Function(-val2);
        }

        if (r < 0.0)
        {
            val2 = -val2;
            num1 = -num1;
        }

        if (Math.Abs(r) < 1.0)
        {
            var d1 = (1.0 - r) * (1.0 + r);
            var num6 = Math.Sqrt(d1);
            var num7 = val1 - val2;
            var d2 = num7 * num7;
            var num8 = (4.0 - num1) / 8.0;
            var num9 = (12.0 - num1) / 16.0;
            var d3 = -(d2 / d1 + num1) / 2.0;
            if (d3 > -100.0)
                num2 = num6 * Math.Exp(d3) * (1.0 - num8 * (d2 - d1) * (1.0 - num9 * d2 / 5.0) / 3.0 + num8 * num9 * d1 * d1 / 5.0);
            if (-num1 < 100.0)
            {
                var num10 = Math.Sqrt(d2);
                num2 -= Math.Exp(-num1 / 2.0) * Math.Sqrt(2.0 * Math.PI) * Function(-num10 / num6) * num10 * (1.0 - num8 * d2 * (1.0 - num9 * d2 / 5.0) / 3.0);
            }

            var num11 = num6 / 2.0;
            for (var index3 = 0; index3 < numArray1.Length; ++index3)
                for (var index4 = -1; index4 <= 1; index4 += 2)
                {
                    var num12 = num11 * (index4 * numArray1[index3] + 1.0);
                    var num13 = num12 * num12;
                    var num14 = Math.Sqrt(1.0 - num13);
                    var d4 = -(d2 / num13 + num1) / 2.0;
                    if (d4 > -100.0)
                        num2 += num11 * numArray2[index3] * Math.Exp(d4) *
                                (Math.Exp(-num1 * num13 / (2.0 * (1.0 + num14) * (1.0 + num14))) / num14 - (1.0 + num8 * num13 * (1.0 + num9 * num13)));
                }

            num2 = -num2 / (2.0 * Math.PI);
        }

        if (r > 0.0)
            return num2 + Function(-Math.Max(val1, val2));
        var num15 = -num2;
        if (val2 <= val1)
            return num15;
        return val1 < 0.0 ? num15 + Function(val2) - Function(val1) : num15 + Function(-val1) - Function(-val2);
    }
}