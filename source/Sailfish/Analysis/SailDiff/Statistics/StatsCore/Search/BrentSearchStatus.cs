namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

public enum BrentSearchStatus : byte
{
    Success,

    RootNotBracketed,

    FunctionNotFinite,

    MaxIterationsReached
}