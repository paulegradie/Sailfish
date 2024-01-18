namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

public enum BrentSearchStatus : byte
{
    None,

    Success,

    RootNotBracketed,

    FunctionNotFinite,

    MaxIterationsReached
}