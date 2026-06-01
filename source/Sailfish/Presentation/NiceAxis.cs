using System;
using System.Collections.Generic;

namespace Sailfish.Presentation;

/// <summary>
/// Result of a "nice number" axis computation: rounded bounds, tick step, the tick values, and the
/// number of decimals to format labels with.
/// </summary>
public readonly record struct NiceAxisResult(double Min, double Max, double Step, IReadOnlyList<double> Ticks, int Decimals);

/// <summary>
/// Produces human-friendly axis bounds and tick marks (Heckbert's "nice numbers for graph labels").
/// Given the observed data range it rounds outward to pleasant round numbers (…, 0.2, 0.5, 1, 2, 5, …)
/// so axis labels read like <c>50.6, 50.8, 51.0</c> rather than <c>50.649, 50.811, …</c>.
/// </summary>
public static class NiceAxis
{
    /// <summary>
    /// Computes a nice axis covering <paramref name="dataMin"/>..<paramref name="dataMax"/> with about
    /// <paramref name="maxTicks"/> ticks (including both endpoints). Degenerate ranges (identical or
    /// non-finite values) expand to a small sensible window centred on the value.
    /// </summary>
    public static NiceAxisResult Compute(double dataMin, double dataMax, int maxTicks = 5)
    {
        if (!double.IsFinite(dataMin) || !double.IsFinite(dataMax))
        {
            dataMin = 0;
            dataMax = 1;
        }

        if (dataMax < dataMin) (dataMin, dataMax) = (dataMax, dataMin);

        maxTicks = Math.Max(2, maxTicks);

        // Identical / near-zero span: build a small symmetric window around the value so the spike
        // lands in the middle of a readable axis instead of dividing by zero.
        if (dataMax - dataMin <= 0)
        {
            var centre = dataMin;
            var magnitude = Math.Abs(centre);
            var pad = magnitude > 0 ? NiceNum(magnitude * 0.05, round: true) : 1.0;
            if (pad <= 0) pad = 1.0;
            dataMin = centre - pad;
            dataMax = centre + pad;
        }

        var range = NiceNum(dataMax - dataMin, round: false);
        var step = NiceNum(range / (maxTicks - 1), round: true);
        if (step <= 0) step = 1.0;

        var niceMin = Math.Floor(dataMin / step) * step;
        var niceMax = Math.Ceiling(dataMax / step) * step;
        var decimals = DecimalsForStep(step);

        var ticks = new List<double>();
        // The +0.5*step guard absorbs floating-point drift so the final tick isn't dropped.
        for (var value = niceMin; value <= niceMax + step * 0.5; value += step)
        {
            ticks.Add(Math.Round(value, 10));
        }

        return new NiceAxisResult(Math.Round(niceMin, 10), Math.Round(niceMax, 10), step, ticks, decimals);
    }

    /// <summary>
    /// Rounds <paramref name="x"/> to a "nice" number. When <paramref name="round"/> is true it rounds
    /// to the nearest of {1,2,5}·10ⁿ; otherwise it rounds up (used for the overall range).
    /// </summary>
    private static double NiceNum(double x, bool round)
    {
        if (x <= 0) return 0;
        var exponent = Math.Floor(Math.Log10(x));
        var fraction = x / Math.Pow(10, exponent);

        double niceFraction;
        if (round)
            niceFraction = fraction < 1.5 ? 1 : fraction < 3 ? 2 : fraction < 7 ? 5 : 10;
        else
            niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;

        return niceFraction * Math.Pow(10, exponent);
    }

    private static int DecimalsForStep(double step)
    {
        if (step >= 1) return 0;
        // e.g. step 0.2 -> 1 decimal, 0.05 -> 2. The tiny epsilon keeps clean powers of ten honest.
        var decimals = (int)Math.Ceiling(-Math.Log10(step) - 1e-9);
        return Math.Clamp(decimals, 0, 6);
    }
}
