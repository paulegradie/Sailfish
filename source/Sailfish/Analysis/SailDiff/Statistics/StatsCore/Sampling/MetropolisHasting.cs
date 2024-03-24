using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Sampling;

public class MetropolisHasting<TObservation, TProposalDistribution> :
    MetropolisHasting<TObservation>
    where TProposalDistribution : ISampleableDistribution<TObservation[]>
{
    private TProposalDistribution proposal;

    public MetropolisHasting(
        int dimensions,
        Func<TObservation[], double> logDensity,
        TProposalDistribution proposal)
    {
        Initialize(dimensions, logDensity, proposal);
    }

    protected void Initialize(
        int dimensions,
        Func<TObservation[], double> logDensity,
        TProposalDistribution proposal)
    {
        this.proposal = proposal;
        Initialize(dimensions, logDensity, generate);
    }

    private TObservation[] generate(TObservation[] current, TObservation[] next)
    {
        return proposal.Generate(next);
    }
}

public class MetropolisHasting<T> : Distributions.IRandomNumberGenerator<T[]>
{
    private long accepts;
    private bool initialized;
    private T[] next;

    protected MetropolisHasting()
    {
    }

    public T[] Current { get; private set; }

    public Random RandomSource { get; set; }

    public double CurrentValue { get; private set; }

    public Func<T[], double> LogProbabilityDensityFunction { get; private set; }

    public Func<T[], T[], T[]> Proposal { get; private set; }

    public int NumberOfInputs { get; private set; }

    public int Discard { get; set; }

    public T[][] Generate(int samples)
    {
        return Generate(samples, new T[samples][]);
    }

    public T[][] Generate(int samples, T[][] result)
    {
        if (!initialized)
            WarmUp();
        for (var index1 = 0; index1 < samples; ++index1)
        {
            do
            {
                ;
            } while (!TryGenerate());

            for (var index2 = 0; index2 < Current.Length; ++index2)
                result[index1][index2] = Current[index2];
        }

        return result;
    }

    public T[] Generate()
    {
        if (!initialized)
            WarmUp();
        do
        {
            ;
        } while (!TryGenerate());

        return Current;
    }

    protected void Initialize(
        int dimensions,
        Func<T[], double> logDensity,
        Func<T[], T[], T[]> proposal)
    {
        NumberOfInputs = dimensions;
        Current = new T[dimensions];
        next = new T[dimensions];
        LogProbabilityDensityFunction = logDensity;
        Proposal = proposal;
        RandomSource = Generator.Random;
    }

    public bool TryGenerate()
    {
        var randomSource = RandomSource;
        next = Proposal(Current, next);
        var num1 = LogProbabilityDensityFunction(next);
        var num2 = num1 - CurrentValue;
        if (Math.Log(randomSource.NextDouble()) >= num2)
            return false;
        var current = Current;
        Current = next;
        next = current;
        CurrentValue = num1;
        ++accepts;
        return true;
    }

    public void WarmUp()
    {
        for (var index = 0; index < Discard; ++index)
            TryGenerate();
        initialized = true;
    }
}

public class MetropolisHasting : MetropolisHasting<double, Independent<NormalDistribution>>
{
    public MetropolisHasting(int dimensions, Func<double[], double> logDensity)
        : base(dimensions, logDensity, new Independent<NormalDistribution>(dimensions, () => new NormalDistribution()))
    {
    }
}