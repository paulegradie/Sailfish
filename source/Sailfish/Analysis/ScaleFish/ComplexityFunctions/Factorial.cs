using System;
using System.Linq;
using MathNet.Numerics;
using Sailfish.Analysis.ScaleFish.CurveFitting;

namespace Sailfish.Analysis.ScaleFish.ComplexityFunctions;

public class Factorial : ScaleFishModelFunction
{
    public override string Name { get; set; } = nameof(Factorial);

    public override string OName { get; set; } = "O(n!)";

    public override string Quality { get; set; } = "Worst!";

    public override string FunctionDef { get; set; } = "f(x) = {0}x! + {1}";

    public override double Compute(double bias, double scale, double x)
    {
        if (x <= 1)
            return scale * 1 + bias;

        double result = 1;
        for (var i = 2; i <= x; i++) result *= i;

        return scale * result + bias;
    }

    /// <summary>
    /// The raw factorial basis overflows around x ≈ 171. For modest X the default OLS works; once any X
    /// exceeds that threshold we fall back to log-space using lgamma(x+1) = log(x!).
    /// </summary>
    public override FittedCurve SeedFit(ComplexityMeasurement[] data, double[]? weights = null)
    {
        var anyBasisOverflow = data.Any(m => m.X > 170 || !double.IsFinite(Compute(0, 1, m.X)));
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
        var minY = data.Min(m => m.Y);
        var bias = minY > 0 ? minY * 0.5 : 0.0;

        double sumW = 0, sumWLogFact = 0, sumWLogY = 0;
        var n = 0;
        for (var i = 0; i < data.Length; i++)
        {
            var dy = data[i].Y - bias;
            if (dy <= 0 || !double.IsFinite(dy)) continue;

            // log(x!) = lgamma(x+1)
            var logFact = SpecialFunctions.GammaLn(data[i].X + 1.0);
            if (!double.IsFinite(logFact)) continue;

            // Delta-method weight: Var(log(y - b)) ≈ Var(y) / (y - b)^2.
            // Preserve upstream zero weights (point gets excluded) and only repair non-finite values.
            var linW = weights?[i] ?? 1.0;
            var w = linW * dy * dy;
            if (!double.IsFinite(w)) w = 1.0;
            if (w < 0) w = 0;
            if (w == 0) continue;

            sumW += w;
            sumWLogFact += w * logFact;
            sumWLogY += w * Math.Log(dy);
            n++;
        }

        if (n < 1 || sumW <= 0)
            throw new Sailfish.Exceptions.SailfishException("Cannot fit Factorial in log-space: all (y - bias) values non-positive");

        // log(y - bias) ≈ log(scale) + log(x!) → log(scale) = mean(log(dy)) - mean(log(x!)) (weighted)
        var logScale = (sumWLogY - sumWLogFact) / sumW;
        var scale = Math.Exp(logScale);

        if (!double.IsFinite(scale))
            throw new Sailfish.Exceptions.SailfishException("Factorial log-space fit produced non-finite scale");

        return new FittedCurve(scale, bias);
    }
}
