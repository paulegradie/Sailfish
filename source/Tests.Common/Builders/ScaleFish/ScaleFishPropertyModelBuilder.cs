using Sailfish.Analysis.ScaleFish;
using Tests.Common.Utils;

namespace Tests.Common.Builders.ScaleFish;

public class ScaleFishPropertyModelBuilder : ScaleFishPropertyModelBuilder.IHavePropertyName
{
    private string? _propertyName;
    private ScaleFishModel? _scaleFishModel;

    public ScaleFishPropertyModelBuilder WithPropertyName(string name)
    {
        _propertyName = name;
        return this;
    }

    public ScaleFishPropertyModel Build()
    {
        return new ScaleFishPropertyModel(_propertyName ?? Some.RandomString(), _scaleFishModel ?? ScaleFishModelBuilder.Create().Build());
    }

    public static IHavePropertyName Create()
    {
        return new ScaleFishPropertyModelBuilder();
    }

    public ScaleFishPropertyModelBuilder WithScaleFishModel(ScaleFishModel model)
    {
        _scaleFishModel = model;
        return this;
    }


    public ScaleFishPropertyModelBuilder WithScaleFishModel(Action<ScaleFishModelBuilder.IHavePrimaryFunction> configure)
    {
        var builder = ScaleFishModelBuilder.Create();
        configure(builder);
        _scaleFishModel = builder.Build();
        return this;
    }

    public interface IHavePropertyName
    {
        ScaleFishPropertyModelBuilder WithPropertyName(string name);
        ScaleFishPropertyModel Build();
    }
}