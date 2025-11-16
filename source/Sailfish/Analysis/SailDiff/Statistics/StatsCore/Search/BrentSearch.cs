using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

internal static class BrentSearch
{
    public static double FindRoot(
        Func<double, double> function,
        double lowerBound,
        double upperBound,
        double tol = 1E-06,
        int maxIterations = 500)
    {
        return HandleResult(FindRootInternal(function, lowerBound, upperBound, tol, maxIterations));
    }

    public static double Find(
        Func<double, double> function,
        double value,
        double lowerBound,
        double upperBound,
        double tol = 1E-06,
        int maxIterations = 500)
    {
        return FindRoot(x => function(x) - value, lowerBound, upperBound, tol, maxIterations);
    }

    private static BrentSearchResult FindRootInternal(
        Func<double, double> function,
        double lowerBound,
        double upperBound,
        double tol,
        int maxIterations)
    {
        if (double.IsInfinity(lowerBound))
            throw new ArgumentOutOfRangeException(nameof(lowerBound), "Bounds must be finite");
        if (double.IsInfinity(upperBound))
            throw new ArgumentOutOfRangeException(nameof(upperBound), "Bounds must be finite");
        if (tol < 0.0)
            throw new ArgumentOutOfRangeException(nameof(tol), "Tolerance must be positive.");
        var num1 = function(lowerBound);
        var d = function(upperBound);
        var num2 = lowerBound;
        var num3 = num1;
        if (Math.Sign(num1) == Math.Sign(d))
            return new BrentSearchResult(upperBound, BrentSearchStatus.RootNotBracketed);
        for (var index = 0; index < maxIterations; ++index)
        {
            var num4 = upperBound - lowerBound;
            if (Math.Abs(num3) < Math.Abs(d))
            {
                lowerBound = upperBound;
                num1 = d;
                upperBound = num2;
                d = num3;
                num2 = lowerBound;
                num3 = num1;
            }

            var num5 = 2.220446049250313E-16 * Math.Abs(upperBound) + tol / 2.0;
            var num6 = (num2 - upperBound) / 2.0;
            if (Math.Abs(num6) <= num5 || d == 0.0)
                return new BrentSearchResult(upperBound, BrentSearchStatus.Success);
            if (Math.Abs(num4) >= num5 && Math.Abs(num1) > Math.Abs(d))
            {
                var num7 = num2 - upperBound;
                double num8;
                double num9;
                if (lowerBound == num2)
                {
                    var num10 = d / num1;
                    num8 = num7 * num10;
                    num9 = 1.0 - num10;
                }
                else
                {
                    var num11 = num1 / num3;
                    var num12 = d / num3;
                    var num13 = d / num1;
                    num8 = num13 * (num7 * num11 * (num11 - num12) - (upperBound - lowerBound) * (num12 - 1.0));
                    num9 = (num11 - 1.0) * (num12 - 1.0) * (num13 - 1.0);
                }

                if (num8 > 0.0)
                    num9 = -num9;
                else
                    num8 = -num8;
                if (num8 < 0.75 * num7 * num9 - Math.Abs(num5 * num9) / 2.0 && num8 < Math.Abs(num4 * num9 / 2.0))
                    num6 = num8 / num9;
            }

            if (Math.Abs(num6) < num5)
                num6 = num6 > 0.0 ? num5 : -num5;
            lowerBound = upperBound;
            num1 = d;
            upperBound += num6;
            d = function(upperBound);
            if (double.IsNaN(d) || double.IsInfinity(d))
                return new BrentSearchResult(upperBound, BrentSearchStatus.FunctionNotFinite);
            if ((d > 0.0 && num3 > 0.0) || (d < 0.0 && num3 < 0.0))
            {
                num2 = lowerBound;
                num3 = num1;
            }
        }

        return new BrentSearchResult(upperBound, BrentSearchStatus.MaxIterationsReached);
    }

    private static double HandleResult(BrentSearchResult result)
    {
        return result.Status switch
        {
            BrentSearchStatus.Success => result.Solution,
            BrentSearchStatus.RootNotBracketed => throw new ConvergenceException("Root must be enclosed between bounds once and only once."),
            BrentSearchStatus.FunctionNotFinite => throw new ConvergenceException("Function evaluation didn't return a finite number."),
            BrentSearchStatus.MaxIterationsReached => throw new ConvergenceException("The maximum number of iterations was reached."),
            _ => throw new ArgumentOutOfRangeException(nameof(result), "Argument type error")
        };
    }

    private readonly struct BrentSearchResult
    {
        public BrentSearchResult(double solution, BrentSearchStatus status)
        {
            Solution = solution;
            Status = status;
        }

        public double Solution { get; }

        public BrentSearchStatus Status { get; }
    }
}