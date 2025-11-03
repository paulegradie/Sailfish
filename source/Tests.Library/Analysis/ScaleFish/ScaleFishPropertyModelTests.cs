using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.ScaleFish;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ScaleFishPropertyModelTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var propertyName = "TestProperty";
        var scaleFishModel = CreateMockScaleFishModel();

        // Act
        var propertyModel = new ScaleFishPropertyModel(propertyName, scaleFishModel);

        // Assert
        propertyModel.PropertyName.ShouldBe(propertyName);
        propertyModel.ScaleFishModel.ShouldBe(scaleFishModel);
    }

    [Fact]
    public void PropertyName_CanBeModified()
    {
        // Arrange
        var propertyModel = new ScaleFishPropertyModel("Original", CreateMockScaleFishModel());

        // Act
        propertyModel.PropertyName = "Modified";

        // Assert
        propertyModel.PropertyName.ShouldBe("Modified");
    }

    [Fact]
    public void ScaleFishModel_CanBeModified()
    {
        // Arrange
        var originalModel = CreateMockScaleFishModel();
        var newModel = CreateMockScaleFishModel();
        var propertyModel = new ScaleFishPropertyModel("Test", originalModel);

        // Act
        propertyModel.ScaleFishModel = newModel;

        // Assert
        propertyModel.ScaleFishModel.ShouldBe(newModel);
    }

    [Fact]
    public void ParseResult_WithEmptyDictionary_ReturnsEmptyEnumerable()
    {
        // Arrange
        var emptyDict = new Dictionary<string, ScaleFishModel>();

        // Act
        var result = ScaleFishPropertyModel.ParseResult(emptyDict);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ParseResult_WithSingleEntry_ReturnsSinglePropertyModel()
    {
        // Arrange
        var model = CreateMockScaleFishModel();
        var dict = new Dictionary<string, ScaleFishModel>
        {
            { "Property1", model }
        };

        // Act
        var result = ScaleFishPropertyModel.ParseResult(dict).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].PropertyName.ShouldBe("Property1");
        result[0].ScaleFishModel.ShouldBe(model);
    }

    [Fact]
    public void ParseResult_WithMultipleEntries_ReturnsMultiplePropertyModels()
    {
        // Arrange
        var model1 = CreateMockScaleFishModel();
        var model2 = CreateMockScaleFishModel();
        var model3 = CreateMockScaleFishModel();
        var dict = new Dictionary<string, ScaleFishModel>
        {
            { "Property1", model1 },
            { "Property2", model2 },
            { "Property3", model3 }
        };

        // Act
        var result = ScaleFishPropertyModel.ParseResult(dict).ToList();

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain(x => x.PropertyName == "Property1" && x.ScaleFishModel == model1);
        result.ShouldContain(x => x.PropertyName == "Property2" && x.ScaleFishModel == model2);
        result.ShouldContain(x => x.PropertyName == "Property3" && x.ScaleFishModel == model3);
    }

    [Fact]
    public void ParseResult_PreservesPropertyNames()
    {
        // Arrange
        var model = CreateMockScaleFishModel();
        var dict = new Dictionary<string, ScaleFishModel>
        {
            { "ComplexPropertyName.With.Dots", model },
            { "SimpleProperty", model }
        };

        // Act
        var result = ScaleFishPropertyModel.ParseResult(dict).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(x => x.PropertyName == "ComplexPropertyName.With.Dots");
        result.ShouldContain(x => x.PropertyName == "SimpleProperty");
    }

    [Fact]
    public void ParseResult_ReturnsEnumerableThatCanBeEnumeratedMultipleTimes()
    {
        // Arrange
        var model = CreateMockScaleFishModel();
        var dict = new Dictionary<string, ScaleFishModel>
        {
            { "Property1", model },
            { "Property2", model }
        };

        // Act
        var result = ScaleFishPropertyModel.ParseResult(dict);

        // Assert
        var firstEnumeration = result.ToList();
        var secondEnumeration = result.ToList();

        firstEnumeration.Count.ShouldBe(2);
        secondEnumeration.Count.ShouldBe(2);
    }

    [Fact]
    public void ImplementsIScaleFishPropertyModels()
    {
        // Arrange
        var propertyModel = new ScaleFishPropertyModel("Test", CreateMockScaleFishModel());

        // Act & Assert
        propertyModel.ShouldBeAssignableTo<IScaleFishPropertyModels>();
    }

    private static ScaleFishModel CreateMockScaleFishModel()
    {
        var mockFunction = Substitute.For<ScaleFishModelFunction>();
        mockFunction.Name.Returns("Linear");

        var mockNextClosestFunction = Substitute.For<ScaleFishModelFunction>();
        mockNextClosestFunction.Name.Returns("Quadratic");

        return new ScaleFishModel(mockFunction, 0.95, mockNextClosestFunction, 0.85);
    }
}

