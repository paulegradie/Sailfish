using System;
using System.Collections.Generic;
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
    private readonly struct Row
    {
        public Row(double x, double y, double dy, double logX, double logLogX, double logDy, double weight)
        {
            X = x;
            Y = y;
            Dy = dy;
            LogX = logX;
            LogLogX = logLogX;
            LogDy = logDy;
            Weight = weight;
        }

        public double X { get; }
        public double Y { get; }
        public double Dy { get; }
        public double LogX { get; }
        public double LogLogX { get; }
        public double LogDy { get; }
        public double Weight { get; }
    }

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

        var rows = new List<Row>(data.Length);
        for (var idx = 0; idx < data.Length; idx++)
        {
            var m = data[idx];
            var dy = m.Y - d;
            if (m.X <= 1.0 || dy <= 0 || !double.IsFinite(dy)) continue;

            var logX = Math.Log(m.X);
            if (logX <= 0) continue;

            var logLogX = Math.Log(logX);
            if (!double.IsFinite(logLogX)) continue;

            rows.Add(new Row(m.X, m.Y, dy, logX, logLogX, Math.Log(dy), weights?[idx] ?? 1.0));
        }

        if (rows.Count < 4) return null;

        // Weighted normal equations for the linear model:
        //     log(y - d) = log(a) + b·log(x) + c·log(log(x))
        // basis = [1, log(x), log(log(x))]
        // weight per residual in log-space ≈ var(y)·(y - d)^(-2)·... → in practice use linear weight * (y - d)^2
        var ata = Matrix<double>.Build.Dense(3, 3);
        var atb = Matrix<double>.Build.Dense(3, 1);
        var usedRows = new List<Row>(rows.Count);
        foreach (var r in rows)
        {
            var w = r.Weight * r.Dy * r.Dy;
            // Preserve upstream zero weights (excluded points) and only repair non-finite values.
            if (!double.IsFinite(w)) w = 1.0;
            if (w <= 0) continue;

            usedRows.Add(r);
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

        if (usedRows.Count < 4) return null;

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

        // Compute R^2 on the same sample that was actually fit. Mixing in filtered-out points (x ≤ 1
        // or non-finite predictions) makes yMean/ssTot inconsistent with ssRes and can collapse R^2.
        double ssRes = 0, ssTot = 0;
        var yMean = usedRows.Average(r => r.Y);
        foreach (var r in usedRows)
        {
            var pred = a * Math.Pow(r.X, b) * Math.Pow(r.LogX, c) + d;
            if (!double.IsFinite(pred)) continue;
            ssRes += (r.Y - pred) * (r.Y - pred);
            ssTot += (r.Y - yMean) * (r.Y - yMean);
        }
        var r2 = ssTot > 0 ? 1.0 - ssRes / ssTot : 0.0;

        return new PowerLogResult(a, b, c, d, r2);
    }
}
