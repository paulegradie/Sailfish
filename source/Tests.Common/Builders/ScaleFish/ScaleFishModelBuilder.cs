using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;

namespace Tests.Common.Builders.ScaleFish;

public class ScaleFishModelBuilder :
    ScaleFishModelBuilder.IHavePrimaryFunction,
    ScaleFishModelBuilder.IHavePrimaryGoodnessOfFit,
    ScaleFishModelBuilder.IHaveSecondaryFunction,
    ScaleFishModelBuilder.IHaveSecondaryGoodnessOfFit
{
    private ScaleFishModelFunction? _primaryFunction;
    private double? _primaryGoodnessOfFit;
    private ScaleFishModelFunction? _secondaryFunction;
    private double? _secondaryGoodnessOfFit;


    public IHavePrimaryGoodnessOfFit AddPrimaryFunction(ScaleFishModelFunction function)
    {
        _primaryFunction = function;
        return this;
    }

    public ScaleFishModel Build()
    {
        return new ScaleFishModel(
            _primaryFunction ?? new Linear(),
            _primaryGoodnessOfFit ?? 0.98,
            _secondaryFunction ?? new Quadratic(),
            _secondaryGoodnessOfFit ?? 0.95);
    }

    public IHaveSecondaryFunction SetPrimaryGoodnessOfFit(double goodnessOfFit)
    {
        _primaryGoodnessOfFit = goodnessOfFit;
        return this;
    }

    public IHaveSecondaryGoodnessOfFit AddSecondaryFunction(ScaleFishModelFunction function)
    {
        _secondaryFunction = function;
        return this;
    }

    public IHaveSecondaryGoodnessOfFit SetSecondaryGoodnessOfFit(double goodnessOfFit)
    {
        _secondaryGoodnessOfFit = goodnessOfFit;
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