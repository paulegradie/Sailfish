using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

public class ConvergenceException : Exception
{
    public ConvergenceException(string? message) : base(message)
    {
    }
}