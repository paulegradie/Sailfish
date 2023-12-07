using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public interface IRange<T> : IFormattable
{
    T Min { get; set; }

    T Max { get; set; }
}