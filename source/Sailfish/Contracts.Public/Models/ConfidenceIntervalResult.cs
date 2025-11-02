using System;

namespace Sailfish.Contracts.Public.Models;

public sealed class ConfidenceIntervalResult
{
    public ConfidenceIntervalResult(double confidenceLevel, double marginOfError, double lower, double upper)
    {
        ConfidenceLevel = confidenceLevel;
        MarginOfError = marginOfError;
        Lower = lower;
        Upper = upper;
    }

    public double ConfidenceLevel { get; }
    public double MarginOfError { get; }
    public double Lower { get; }
    public double Upper { get; }
}

