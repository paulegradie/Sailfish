namespace Sailfish.Analysis.ScaleFish.CurveFitting;

public class FitnessResult
{
    public FitnessResult(
        double rSquared,
        double rmse,
        double mae,
        double sad,
        double ssd,
        double mse)
    {
        RSquared = rSquared;
        Rmse = rmse;
        Mae = mae;
        Sad = sad;
        Ssd = ssd;
        Mse = mse;

        if (double.IsNaN(rSquared) || double.IsNaN(rmse) || double.IsNaN(mae) || double.IsNaN(sad) || double.IsNaN(ssd) || double.IsNaN(mse))
            IsValid = false;
        else
            IsValid = true;
    }

    public double RSquared { get; }
    public double Rmse { get; }
    public double Mae { get; }
    public double Sad { get; }
    public double Ssd { get; }
    public double Mse { get; }

    public bool IsValid { get; set; }
}