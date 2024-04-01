using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

internal static class Normal
{
    private static readonly double[] InverseP0 =
    [
        -59.96335010141079,
        98.00107541859997,
        -56.67628574690703,
        13.931260938727968,
        -1.2391658386738125
    ];

    private static readonly double[] InverseQ0 =
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

    private static readonly double[] InverseP1 =
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

    private static readonly double[] InverseQ1 =
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

    private static readonly double[] InverseP2 =
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

    private static readonly double[] InverseQ2 =
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
            return (num3 + num3 * (x * Specials.Polevl(x, InverseP0, 4) / Specials.P1Evl(x, InverseQ0, 8))) * num1;
        }

        var d2 = Math.Sqrt(-2.0 * Math.Log(d1));
        var num4 = d2 - Math.Log(d2) / d2;
        var x1 = 1.0 / d2;
        var num5 = d2 >= 8.0
            ? x1 * Specials.Polevl(x1, InverseP2, 8) / Specials.P1Evl(x1, InverseQ2, 8)
            : x1 * Specials.Polevl(x1, InverseP1, 8) / Specials.P1Evl(x1, InverseQ1, 8);
        var num6 = num4 - num5;
        if (num2 != 0)
            num6 = -num6;
        return num6;
    }
}