using System;
using System.Globalization;

namespace Sailfish.Analysis.ScaleFish.CurveFitting;

/// <summary>
/// Result of fitting the continuous power-log model y = a · x^b · (log x)^c + d.
/// The (b, c) pair gives a continuous characterization of complexity, complementing the discrete-family label.
/// </summary>
public class PowerLogResult
{
    public PowerLogResult(double a, double b, double c, double d, double rSquared)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        RSquared = rSquared;
    }

    /// <summary>Scale parameter (coefficient on x^b · (log x)^c).</summary>
    public double A { get; init; }

    /// <summary>Continuous power exponent. 1 ≈ linear, 2 ≈ quadratic, 3 ≈ cubic, 0.5 ≈ √n.</summary>
    public double B { get; init; }

    /// <summary>Continuous log-power exponent. 0 ≈ pure polynomial, 1 ≈ n·log(n)-like.</summary>
    public double C { get; init; }

    /// <summary>Bias estimate (constant offset).</summary>
    public double D { get; init; }

    /// <summary>R^2 of the continuous model on the original y-space data.</summary>
    public double RSquared { get; init; }

    /// <summary>
    /// Returns the discrete family name whose (b, c) reference point is closest to this fit.
    /// Uses Chebyshev (L^∞) distance in (b, c) space.
    /// </summary>
    public string NearestDiscreteFamily()
    {
        var candidates = new (string Name, double B, double C)[]
        {
            ("SqrtN",     0.5, 0.0),
            ("Linear",    1.0, 0.0),
            ("NLogN",     1.0, 1.0),
            ("Quadratic", 2.0, 0.0),
            ("Cubic",     3.0, 0.0)
        };

        var bestName = candidates[0].Name;
        var bestDist = double.PositiveInfinity;
        foreach (var (name, bRef, cRef) in candidates)
        {
            var dist = Math.Max(Math.Abs(B - bRef), Math.Abs(C - cRef));
            if (dist < bestDist)
            {
                bestDist = dist;
                bestName = name;
            }
        }
        return bestName;
    }

    /// <summary>
    /// A human-friendly description of the continuous exponents — e.g. "b=1.03, c=0.05".
    /// </summary>
    public string Describe()
    {
        return $"b={B.ToString("F2", CultureInfo.InvariantCulture)}, c={C.ToString("F2", CultureInfo.InvariantCulture)}";
    }
}
