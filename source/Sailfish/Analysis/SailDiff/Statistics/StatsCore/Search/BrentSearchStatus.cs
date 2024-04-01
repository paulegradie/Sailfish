namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

internal enum BrentSearchStatus : byte
{
    Success,

    RootNotBracketed,

    FunctionNotFinite,

    MaxIterationsReached
}