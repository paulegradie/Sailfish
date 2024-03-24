using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

public sealed class BrentSearch(
    Func<double, double> function,
    double lowerBound,
    double upperBound,
    double tol = 1E-06,
    int maxIterations = 500)
{
    public double Tolerance { get; set; } = tol;

    public double LowerBound { get; set; } = lowerBound;

    public double UpperBound { get; set; } = upperBound;

    public int MaxIterations { get; set; } = maxIterations;

    public BrentSearchStatus Status { get; private set; }

    public Func<double, double> Function { get; } = function;

    public int NumberOfVariables
    {
        get => 1;
        set
        {
            if (value != 1)
                throw new InvalidOperationException("Brent Search supports only one variable.");
        }
    }

    public double Solution { get; set; }

    public double Value { get; private set; }

    public bool Minimize()
    {
        var brentSearchResult = MinimizeInternal(Function, LowerBound, UpperBound, Tolerance, MaxIterations);
        Solution = brentSearchResult.Solution;
        Value = brentSearchResult.Value;
        Status = brentSearchResult.Status;
        return Status == BrentSearchStatus.Success;
    }

    public bool Maximize()
    {
        var brentSearchResult =
            MinimizeInternal(x => -Function(x), LowerBound, UpperBound, Tolerance, MaxIterations);
        Solution = brentSearchResult.Solution;
        Value = -brentSearchResult.Value;
        Status = brentSearchResult.Status;
        return Status == BrentSearchStatus.Success;
    }

    public static double Minimize(
        Func<double, double> function,
        double lowerBound,
        double upperBound,
        double tol = 1E-06,
        int maxIterations = 500)
    {
        return HandleResult(MinimizeInternal(function, lowerBound, upperBound, tol, maxIterations));
    }

    public static double Maximize(
        Func<double, double> function,
        double lowerBound,
        double upperBound,
        double tol = 1E-06,
        int maxIterations = 500)
    {
        return Minimize(x => -function(x), lowerBound, upperBound, tol, maxIterations);
    }

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

    private static BrentSearchResult MinimizeInternal(
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
        if (maxIterations == 0)
            maxIterations = int.MaxValue;
        if (upperBound < lowerBound)
        {
            (lowerBound, upperBound) = (upperBound, lowerBound);
        }

        var num1 = lowerBound + 0.831966011250105 * (upperBound - lowerBound);
        var num2 = function(num1);
        var solution = num1;
        var num3 = num2;
        var num4 = num1;
        var num5 = num2;
        for (var index = 0; index < maxIterations; ++index)
        {
            var num6 = upperBound - lowerBound;
            var num7 = lowerBound / 2.0 + upperBound / 2.0;
            var num8 = Math.Sqrt(1.1102230246251565E-16) * Math.Abs(solution) + tol / 3.0;
            if (Math.Abs(solution - num7) + num6 / 2.0 <= 2.0 * num8)
                return new BrentSearchResult(solution, num3, BrentSearchStatus.Success);
            var num9 = 0.831966011250105 * (solution < num7 ? upperBound - solution : lowerBound - solution);
            if (Math.Abs(solution - num4) >= num8)
            {
                var num10 = (solution - num4) * (num3 - num2);
                var num11 = (solution - num1) * (num3 - num5);
                var num12 = (solution - num1) * num11 - (solution - num4) * num10;
                var num13 = 2.0 * (num11 - num10);
                if (num13 > 0.0)
                    num12 = -num12;
                else
                    num13 = -num13;
                if (Math.Abs(num12) < Math.Abs(num9 * num13) && num12 > num13 * (lowerBound - solution + 2.0 * num8) &&
                    num12 < num13 * (upperBound - solution - 2.0 * num8))
                    num9 = num12 / num13;
            }

            if (Math.Abs(num9) < num8)
                num9 = num9 > 0.0 ? num8 : -num8;
            var num14 = solution + num9;
            var d = function(num14);
            if (double.IsNaN(d) || double.IsInfinity(d))
                return new BrentSearchResult(solution, num3, BrentSearchStatus.FunctionNotFinite);
            if (d <= num3)
            {
                if (num14 < solution)
                    upperBound = solution;
                else
                    lowerBound = solution;
                num1 = num4;
                num2 = num5;
                num4 = solution;
                num5 = num3;
                solution = num14;
                num3 = d;
            }
            else
            {
                if (num14 < solution)
                    lowerBound = num14;
                else
                    upperBound = num14;
                if (d <= num5 || num4 == solution)
                {
                    num1 = num4;
                    num2 = num5;
                    num4 = num14;
                    num5 = d;
                }
                else if (d <= num2 || num1 == solution || num1 == num4)
                {
                    num1 = num14;
                    num2 = d;
                }
            }
        }

        return new BrentSearchResult(solution, num3, BrentSearchStatus.MaxIterationsReached);
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
            return new BrentSearchResult(upperBound, d, BrentSearchStatus.RootNotBracketed);
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
                return new BrentSearchResult(upperBound, d, BrentSearchStatus.Success);
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
                return new BrentSearchResult(upperBound, d, BrentSearchStatus.FunctionNotFinite);
            if ((d > 0.0 && num3 > 0.0) || (d < 0.0 && num3 < 0.0))
            {
                num2 = lowerBound;
                num3 = num1;
            }
        }

        return new BrentSearchResult(upperBound, d, BrentSearchStatus.MaxIterationsReached);
    }

    private static double HandleResult(BrentSearchResult result) => result.Status switch
    {
        BrentSearchStatus.Success => result.Solution,
        BrentSearchStatus.RootNotBracketed => throw new ConvergenceException("Root must be enclosed between bounds once and only once."),
        BrentSearchStatus.FunctionNotFinite => throw new ConvergenceException("Function evaluation didn't return a finite number."),
        BrentSearchStatus.MaxIterationsReached => throw new ConvergenceException("The maximum number of iterations was reached."),
        _ => throw new ArgumentOutOfRangeException(nameof(result), "Argument type error"),
    };

    private readonly struct BrentSearchResult
    {
        public BrentSearchResult(double solution, double value, BrentSearchStatus status)
            : this()
        {
            Solution = solution;
            Value = value;
            Status = status;
        }

        public double Solution { get; }

        public double Value { get; }

        public BrentSearchStatus Status { get; }
    }
}