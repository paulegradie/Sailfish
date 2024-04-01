using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;

internal class ConvergenceException(string? message) : Exception(message);