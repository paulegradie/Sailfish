using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Flags]
public enum MatrixType
{
    Symmetric = 0,

    LowerTriangular = 1,

    UpperTriangular = 2,

    Diagonal = UpperTriangular | LowerTriangular,

    Rectangular = 4,

    Square = Rectangular | LowerTriangular
}