using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Flags]
public enum MatrixType
{
    LowerTriangular = 1,

    UpperTriangular = 2,

    Diagonal = UpperTriangular | LowerTriangular
}