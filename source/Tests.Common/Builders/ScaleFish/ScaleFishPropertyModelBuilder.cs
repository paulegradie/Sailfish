using Sailfish.Analysis.ScaleFish;
using Tests.Common.Builders.Scalefish;
using Tests.Common.Utils;

namespace Tests.Common.Builders.ScaleFish;

public class ScaleFishPropertyModelBuilder : ScaleFishPropertyModelBuilder.IHavePropertyName
{
    public interface IHavePropertyName
    {
        ScaleFishPropertyModelBuilder WithPropertyName(string name);
        ScaleFishPropertyModel Build();
    }

    private string? propertyName;
    private ScaleFishModel? scalefishModel;

    public static IHavePropertyName Create()
    {
        return new ScaleFishPropertyModelBuilder();
    }

    public ScaleFishPropertyModelBuilder WithPropertyName(string name)
    {
        propertyName = name;
        return this;
    }

    public ScaleFishPropertyModelBuilder WithScaleFishModel(ScaleFishModel model)
    {
        scalefishModel = model;
        return this;
    }


    public ScaleFishPropertyModelBuilder WithScaleFishModel(Action<ScalefishModelBuilder.IHavePrimaryFunction> configure)
    {
        var builder = ScalefishModelBuilder.Create();
        configure(builder);
        scalefishModel = builder.Build();
        return this;
    }

    public ScaleFishPropertyModel Build()
    {
        return new ScaleFishPropertyModel(propertyName ?? Some.RandomString(), scalefishModel ?? ScalefishModelBuilder.Create().Build());
    }
}