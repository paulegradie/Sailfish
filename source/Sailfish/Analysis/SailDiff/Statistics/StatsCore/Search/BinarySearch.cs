using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

public class BinarySearch(Func<int, double> function, int a, int b)
{
    public int LowerBound { get; set; } = a;

    public int UpperBound { get; set; } = b;

    public int Solution { get; private set; }

    public double Value { get; private set; }

    public Func<int, double> Function { get; } = function;

    public int Find(double value)
    {
        Solution = Find(Function, LowerBound, UpperBound, value);
        Value = Function(Solution);
        return Solution;
    }

    public static int Find(
        Func<int, double> function,
        int lowerBound,
        int upperBound,
        double value)
    {
        var num1 = lowerBound;
        var num2 = upperBound;
        var num3 = function(num1) <= function(num2 - 1) ? 1 : -1;
        value = num3 * value;
        var num4 = (int)((num1 + (long)num2) / 2L);
        var num5 = num3 * function(num4);
        while (num2 >= num1)
        {
            if (num5 < value)
            {
                num1 = num4 + 1;
            }
            else
            {
                if (num5 <= value)
                    return num4;
                num2 = num4 - 1;
            }

            num4 = (int)((num1 + (long)num2) / 2L);
            num5 = num3 * function(num4);
        }

        return num5 > value || num4 == upperBound ? num4 : num4 + 1;
    }
}