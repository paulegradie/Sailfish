namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Decompositions;

internal interface ISolverArrayDecomposition<T> where T : struct
{
    T[][] Solve(T[][] value);

    T[] Solve(T[] value);

    T[][] SolveForDiagonal(T[] diagonal);

    T[][] Inverse();

    T[][] GetInformationMatrix();

    T[][] Reverse();
}