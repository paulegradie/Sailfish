namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

public interface ITwoSamplePowerAnalysis : IPowerAnalysis
{
    double Samples1 { get; }

    double Samples2 { get; }
}