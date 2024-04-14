using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

public class ConvergenceException(string? message) : Exception(message);