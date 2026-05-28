using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace Sailfish.Analysis.ScaleFish.CurveFitting;

/// <summary>
/// Continuous-exponent diagnostic: fits y = a · x^b · (log x)^c + d in log-space so the polynomial-and-log
/// part is linear in the unknowns (log a, b, c). Reports continuous (b, c) exponents independent of the
/// discrete-family model selection — useful when reality is between two textbook curves.
/// </summary>
public class PowerLogFit
{
    /// <summary>
    /// Fits the continuous model. Returns null when there aren't enough usable points
    /// (need at least 4 with x &gt; 1 and y &gt; 0), or when the fit is numerically degenerate.
    /// </summary>
    public static PowerLogResult? TryFit(ComplexityMeasurement[] data, double[]? weights = null)
    {
        if (data is null) return null;

        // Approximate a small bias from below; the rest of the fit assumes (y - d) > 0.
        var positiveYs = data.Where(m => m.Y > 0).Select(m => m.Y).ToArray();
        if (positiveYs.Length == 0) return null;
        var minY = positiveYs.Min();
        var d = minY > 0 ? minY * 0.1 : 0.0;

        var rows = data
            .Select((m, idx) => new
            {
                m.X,
                Dy = m.Y - d,
                W = weights?[idx] ?? 1.0,
                Index = idx
            })
            .Where(r => r.X > 1.0 && r.Dy > 0)
            .Select(r =>
            {
                var logX = Math.Log(r.X);
                if (logX <= 0) return (Usable: false, LogX: 0.0, LogLogX: 0.0, LogDy: 0.0, W: 0.0, Dy: 0.0);
                var logLogX = Math.Log(logX);
                if (!double.IsFinite(logLogX)) return (Usable: false, LogX: 0.0, LogLogX: 0.0, LogDy: 0.0, W: 0.0, Dy: 0.0);
                return (Usable: true, LogX: logX, LogLogX: logLogX, LogDy: Math.Log(r.Dy), W: r.W, r.Dy);
            })
            .Where(r => r.Usable)
            .ToArray();

        if (rows.Length < 4) return null;

        // Weighted normal equations for the linear model:
        //     log(y - d) = log(a) + b·log(x) + c·log(log(x))
        // basis = [1, log(x), log(log(x))]
        // weight per residual in log-space ≈ var(y)·(y - d)^(-2)·... → in practice use linear weight * (y - d)^2
        var ata = Matrix<double>.Build.Dense(3, 3);
        var atb = Matrix<double>.Build.Dense(3, 1);
        foreach (var r in rows)
        {
            var w = r.W * r.Dy * r.Dy;
            if (!double.IsFinite(w) || w <= 0) w = 1.0;

            double[] basis = { 1.0, r.LogX, r.LogLogX };
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    ata[i, j] += w * basis[i] * basis[j];
                }
                atb[i, 0] += w * basis[i] * r.LogDy;
            }
        }

        // Determinant check (Cholesky-friendly SPD matrix should have positive determinant)
        if (Math.Abs(ata.Determinant()) < 1e-30) return null;

        Matrix<double> solution;
        try
        {
            solution = ata.Solve(atb);
        }
        catch
        {
            return null;
        }

        var logA = solution[0, 0];
        var b = solution[1, 0];
        var c = solution[2, 0];
        var a = Math.Exp(logA);

        if (!double.IsFinite(a) || !double.IsFinite(b) || !double.IsFinite(c))
            return null;

        // Compute residual SS and predicted-y R^2 in original y-space using the fitted (a, b, c, d).
        double ssRes = 0, ssTot = 0;
        var yMean = data.Average(m => m.Y);
        foreach (var m in data)
        {
            var pred = a * Math.Pow(m.X, b) * Math.Pow(Math.Log(Math.Max(m.X, 1.0001)), c) + d;
            if (!double.IsFinite(pred)) continue;
            ssRes += (m.Y - pred) * (m.Y - pred);
            ssTot += (m.Y - yMean) * (m.Y - yMean);
        }
        var r2 = ssTot > 0 ? 1.0 - ssRes / ssTot : 0.0;

        return new PowerLogResult(a, b, c, d, r2);
    }
}
