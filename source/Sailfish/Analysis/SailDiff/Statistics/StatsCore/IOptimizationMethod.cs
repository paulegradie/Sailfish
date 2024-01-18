namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public interface IOptimizationMethod<TInput, TOutput>
{
    int NumberOfVariables { get; set; }

    TInput Solution { get; set; }

    TOutput Value { get; }

    bool Minimize();

    bool Maximize();
}