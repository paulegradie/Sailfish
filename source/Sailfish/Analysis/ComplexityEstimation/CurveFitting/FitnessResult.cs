namespace Sailfish.Analysis.ComplexityEstimation.CurveFitting;

public class FitnessResult
{
    public double RSquared { get; }
    public double RMSE { get; }

    public FitnessResult(double rSquared, double rmse)
    {
        RSquared = rSquared;
        RMSE = rmse;
    }
}