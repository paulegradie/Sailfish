namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public delegate double SmoothingRule(double[] observations, double[] weights = null, int[] repeats = null);