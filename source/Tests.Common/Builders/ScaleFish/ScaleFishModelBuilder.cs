using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;

namespace Tests.Common.Builders.ScaleFish;

public class ScaleFishModelBuilder :
    ScaleFishModelBuilder.IHavePrimaryFunction,
    ScaleFishModelBuilder.IHavePrimaryGoodnessOfFit,
    ScaleFishModelBuilder.IHaveSecondaryFunction,
    ScaleFishModelBuilder.IHaveSecondaryGoodnessOfFit
{
    private ScaleFishModelFunction? primaryFunction;
    private double? primaryGoodnessOfFit;
    private ScaleFishModelFunction? secondaryFunction;
    private double? secondaryGoodnessOfFit;


    public IHavePrimaryGoodnessOfFit AddPrimaryFunction(ScaleFishModelFunction function)
    {
        primaryFunction = function;
        return this;
    }

    public ScaleFishModel Build()
    {
        return new ScaleFishModel(
            primaryFunction ?? new Linear(),
            primaryGoodnessOfFit ?? 0.98,
            secondaryFunction ?? new Quadratic(),
            secondaryGoodnessOfFit ?? 0.95);
    }

    public IHaveSecondaryFunction SetPrimaryGoodnessOfFit(double goodnessOfFit)
    {
        primaryGoodnessOfFit = goodnessOfFit;
        return this;
    }

    public IHaveSecondaryGoodnessOfFit AddSecondaryFunction(ScaleFishModelFunction function)
    {
        secondaryFunction = function;
        return this;
    }

    public IHaveSecondaryGoodnessOfFit SetSecondaryGoodnessOfFit(double goodnessOfFit)
    {
        secondaryGoodnessOfFit = goodnessOfFit;
        return this;
    }

    public static IHavePrimaryFunction Create()
    {
        return new ScaleFishModelBuilder();
    }

    public interface IHavePrimaryFunction
    {
        IHavePrimaryGoodnessOfFit AddPrimaryFunction(ScaleFishModelFunction function);
        ScaleFishModel Build();
    }

    public interface IHavePrimaryGoodnessOfFit
    {
        IHaveSecondaryFunction SetPrimaryGoodnessOfFit(double goodnessOfFit);
    }

    public interface IHaveSecondaryFunction
    {
        IHaveSecondaryGoodnessOfFit AddSecondaryFunction(ScaleFishModelFunction function);
    }

    public interface IHaveSecondaryGoodnessOfFit
    {
        ScaleFishModel Build();
        IHaveSecondaryGoodnessOfFit SetSecondaryGoodnessOfFit(double goodnessOfFit);
    }
}