using System;
using System.Globalization;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

[Serializable]
public abstract class BaseTwoSamplePowerAnalysis :
    ITwoSamplePowerAnalysis
{
    protected BaseTwoSamplePowerAnalysis(DistributionTailSailfish tail)
    {
        Tail = tail;
        Size = 0.05;
        Power = 0.8;
        Samples1 = 2.0;
        Samples2 = 2.0;
    }

    public double TotalSamples => Samples1 + Samples2;

    public DistributionTailSailfish Tail { get; set; }

    public double Power { get; set; }

    public double Size { get; set; }

    public double Samples1 { get; set; }

    public double Samples2 { get; set; }

    public double Samples => TotalSamples;

    public double Effect { get; set; }

    public object Clone() => MemberwiseClone();

    public string ToString(string? format, IFormatProvider? formatProvider) => Power.ToString(format, formatProvider);

    public abstract void ComputePower();

    public virtual void ComputeEffect()
    {
        var requiredPower = Power;
        Effect = BrentSearch.FindRoot(eff =>
        {
            Effect = eff;
            ComputePower();
            return requiredPower - Power;
        }, 1E-05, 100000.0);
        ComputePower();
        var power = Power;
        Power = requiredPower;
        if (Math.Abs(requiredPower - power) <= 1E-05)
            return;
        Effect = double.NaN;
    }

    public virtual void ComputeSize()
    {
        var requiredPower = Power;
        Size = BrentSearch.FindRoot(alpha =>
        {
            Size = alpha;
            ComputePower();
            return requiredPower - Power;
        }, 0.0, 1.0);
        ComputePower();
        var power = Power;
        Power = requiredPower;
        if (Math.Abs(requiredPower - power) <= 1E-05)
            return;
        Effect = double.NaN;
    }

    public virtual void ComputeSamples(double proportion = 1.0)
    {
        var requiredPower = Power;
        var root = BrentSearch.FindRoot(n =>
        {
            Samples1 = n;
            Samples2 = n * proportion;
            ComputePower();
            return requiredPower - Power;
        }, 2.0, 10000.0);
        Samples1 = root;
        Samples2 = root * proportion;
        ComputePower();
        var power = Power;
        Power = requiredPower;
        if (Math.Abs(requiredPower - power) <= 1E-05)
            return;
        Samples1 = Samples2 = double.NaN;
    }

    public override string ToString()
    {
        return Power.ToString(CultureInfo.CurrentCulture);
    }
}