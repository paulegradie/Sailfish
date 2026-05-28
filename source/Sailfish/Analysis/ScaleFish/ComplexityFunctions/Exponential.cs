using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish.CurveFitting;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Exponential : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(Exponential);
    public override string OName { get; set; } = "O(2^n)";
    public override string Quality { get; set; } = "Very Bad";
    public override string FunctionDef { get; set; } = "f(x) = {0}*(2^x) + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        return scale * Math.Pow(2, x) + bias;
    }

    /// <summary>
    /// At modest X the default OLS on the 2^x basis works. Once any X is large enough that 2^x overflows
    /// (x ≳ 1023) the default fit produces non-finite values; in that regime we fit in log-space
    /// log(y - bias) ≈ log(scale) + x·ln(2), which stays well-conditioned.
    /// </summary>
    public override FittedCurve SeedFit(ComplexityMeasurement[] data, double[]? weights = null)
    {
        var anyBasisOverflow = data.Any(m => !double.IsFinite(Math.Pow(2, m.X)));
        if (!anyBasisOverflow)
        {
            try
            {
                return base.SeedFit(data, weights);
            }
            catch
            {
                // fall through to log-space
            }
        }

        return LogSpaceFit(data, weights);
    }

    private static FittedCurve LogSpaceFit(ComplexityMeasurement[] data, double[]? weights)
    {
        // Approximate bias from the smallest observed Y (assumed small relative to scale·2^x_max).
        var minY = data.Min(m => m.Y);
        var bias = minY > 0 ? minY * 0.5 : 0.0;

        double sumW = 0, sumWx = 0, sumWlogY = 0;
        var n = 0;
        for (var i = 0; i < data.Length; i++)
        {
            var dy = data[i].Y - bias;
            if (dy <= 0 || !double.IsFinite(dy)) continue;

            // Delta-method weight: Var(log(y - b)) ≈ Var(y) / (y - b)^2 → weight = (y - b)^2 / Var(y)
            var linW = weights?[i] ?? 1.0;
            var w = linW * dy * dy;
            if (!double.IsFinite(w) || w <= 0) w = 1.0;

            sumW += w;
            sumWx += w * data[i].X;
            sumWlogY += w * Math.Log(dy);
            n++;
        }

        if (n < 1 || sumW <= 0)
            throw new Sailfish.Exceptions.SailfishException("Cannot fit Exponential in log-space: all (y - bias) values non-positive");

        var xMean = sumWx / sumW;
        var logYMean = sumWlogY / sumW;
        var logScale = logYMean - Math.Log(2.0) * xMean;
        var scale = Math.Exp(logScale);

        if (!double.IsFinite(scale))
            throw new Sailfish.Exceptions.SailfishException("Exponential log-space fit produced non-finite scale");

        return new FittedCurve(scale, bias);
    }
}
