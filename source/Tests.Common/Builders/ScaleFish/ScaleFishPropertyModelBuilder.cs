using Sailfish.Analysis.ScaleFish;
using System;
using Tests.Common.Utils;

namespace Tests.Common.Builders.ScaleFish;

public class ScaleFishPropertyModelBuilder : ScaleFishPropertyModelBuilder.IHavePropertyName
{
    private string? propertyName;
    private ScaleFishModel? scaleFishModel;

    public ScaleFishPropertyModelBuilder WithPropertyName(string name)
    {
        propertyName = name;
        return this;
    }

    public ScaleFishPropertyModel Build()
    {
        return new ScaleFishPropertyModel(propertyName ?? Some.RandomString(), scaleFishModel ?? ScaleFishModelBuilder.Create().Build());
    }

    public static IHavePropertyName Create()
    {
        return new ScaleFishPropertyModelBuilder();
    }

    public ScaleFishPropertyModelBuilder WithScaleFishModel(ScaleFishModel model)
    {
        scaleFishModel = model;
        return this;
    }


    public ScaleFishPropertyModelBuilder WithScaleFishModel(Action<ScaleFishModelBuilder.IHavePrimaryFunction> configure)
    {
        var builder = ScaleFishModelBuilder.Create();
        configure(builder);
        scaleFishModel = builder.Build();
        return this;
    }

    public interface IHavePropertyName
    {
        ScaleFishPropertyModelBuilder WithPropertyName(string name);
        ScaleFishPropertyModel Build();
    }
}