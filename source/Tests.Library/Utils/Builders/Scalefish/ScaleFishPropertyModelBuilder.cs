using System;
using Sailfish.Analysis.ScaleFish;

namespace Tests.Library.Utils.Builders.Scalefish;

public class ScaleFishPropertyModelBuilder : ScaleFishPropertyModelBuilder.IHavePropertyName
{
    public interface IHavePropertyName
    {
        ScaleFishPropertyModelBuilder WithPropertyName(string name);
        ScaleFishPropertyModel Build();
    }

    private string? propertyName;
    private ScalefishModel? scalefishModel;

    public static IHavePropertyName Create()
    {
        return new ScaleFishPropertyModelBuilder();
    }

    public ScaleFishPropertyModelBuilder WithPropertyName(string name)
    {
        propertyName = name;
        return this;
    }

    public ScaleFishPropertyModelBuilder WithScaleFishModel(ScalefishModel model)
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