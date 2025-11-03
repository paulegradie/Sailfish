using Sailfish.Analysis.ScaleFish;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ScaleFishObservationTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var methodName = "TestMethod";
        var propertyName = "TestProperty";
        var measurements = new ComplexityMeasurement[]
        {
            new(1, 100),
            new(2, 200),
            new(3, 300)
        };

        // Act
        var observation = new ScaleFishObservation(methodName, propertyName, measurements);

        // Assert
        observation.MethodName.ShouldBe(methodName);
        observation.PropertyName.ShouldBe(propertyName);
        observation.ComplexityMeasurements.ShouldBe(measurements);
        observation.ComplexityMeasurements.Length.ShouldBe(3);
    }

    [Fact]
    public void Deconstruction_WorksCorrectly()
    {
        // Arrange
        var methodName = "TestMethod";
        var propertyName = "TestProperty";
        var measurements = new ComplexityMeasurement[]
        {
            new(1, 100),
            new(2, 200)
        };

        var observation = new ScaleFishObservation(methodName, propertyName, measurements);

        // Act
        var (method, property, complexityMeasurements) = observation;

        // Assert
        method.ShouldBe(methodName);
        property.ShouldBe(propertyName);
        complexityMeasurements.ShouldBe(measurements);
    }

    [Fact]
    public void ToString_ReturnsMethodAndPropertyJoinedByDot()
    {
        // Arrange
        var methodName = "MyMethod";
        var propertyName = "MyProperty";
        var measurements = new ComplexityMeasurement[] { new(1, 100) };

        var observation = new ScaleFishObservation(methodName, propertyName, measurements);

        // Act
        var result = observation.ToString();

        // Assert
        result.ShouldBe("MyMethod.MyProperty");
    }

    [Fact]
    public void EmptyMeasurements_IsAllowed()
    {
        // Arrange
        var methodName = "TestMethod";
        var propertyName = "TestProperty";
        var measurements = new ComplexityMeasurement[] { };

        // Act
        var observation = new ScaleFishObservation(methodName, propertyName, measurements);

        // Assert
        observation.ComplexityMeasurements.ShouldBeEmpty();
    }

    [Fact]
    public void SingleMeasurement_IsAllowed()
    {
        // Arrange
        var methodName = "TestMethod";
        var propertyName = "TestProperty";
        var measurements = new ComplexityMeasurement[] { new(5, 500) };

        // Act
        var observation = new ScaleFishObservation(methodName, propertyName, measurements);

        // Assert
        observation.ComplexityMeasurements.Length.ShouldBe(1);
        observation.ComplexityMeasurements[0].X.ShouldBe(5);
        observation.ComplexityMeasurements[0].Y.ShouldBe(500);
    }

    [Fact]
    public void PropertiesAreInitOnly()
    {
        // Arrange
        var observation = new ScaleFishObservation("Method", "Property", new ComplexityMeasurement[] { new(1, 100) });

        // Act & Assert - This test verifies that properties can be set during initialization
        var newObservation = new ScaleFishObservation("Method", "Property", new ComplexityMeasurement[] { new(1, 100) })
        {
            MethodName = "NewMethod",
            PropertyName = "NewProperty",
            ComplexityMeasurements = new ComplexityMeasurement[] { new(2, 200) }
        };

        newObservation.MethodName.ShouldBe("NewMethod");
        newObservation.PropertyName.ShouldBe("NewProperty");
        newObservation.ComplexityMeasurements.Length.ShouldBe(1);
    }

    [Fact]
    public void LargeMeasurementArray_IsHandledCorrectly()
    {
        // Arrange
        var methodName = "TestMethod";
        var propertyName = "TestProperty";
        var measurements = new ComplexityMeasurement[100];
        for (int i = 0; i < 100; i++)
        {
            measurements[i] = new ComplexityMeasurement(i, i * 10);
        }

        // Act
        var observation = new ScaleFishObservation(methodName, propertyName, measurements);

        // Assert
        observation.ComplexityMeasurements.Length.ShouldBe(100);
        observation.ComplexityMeasurements[50].X.ShouldBe(50);
        observation.ComplexityMeasurements[50].Y.ShouldBe(500);
    }
}

