using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

public class DimensionMismatchException : ArgumentException
{
    public DimensionMismatchException(string paramName)
        : base("Array dimensions must match.", paramName)
    {
    }

    public DimensionMismatchException(string paramName, string message) : base(message, paramName)
    {
    }
}