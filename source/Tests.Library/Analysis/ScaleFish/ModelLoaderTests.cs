using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.ScaleFish;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ModelLoaderTests
{
    private readonly string _testFilePath;
    private readonly string _testJsonContent;

    public ModelLoaderTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_model_{Guid.NewGuid()}.json");
        _testJsonContent = CreateTestJsonContent();
    }

    [Fact]
    public void LoadModelFile_WithValidFile_ReturnsScalefishClassModels()
    {
        // Arrange
        File.WriteAllText(_testFilePath, _testJsonContent);

        try
        {
            // Act
            var result = ModelLoader.LoadModelFile(_testFilePath).ToList();

            // Assert
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(1);
            result[0].TestClassName.ShouldBe("TestClass");
        }
        finally
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }
    }

    [Fact]
    public void LoadModelFile_WithNonExistentFile_ThrowsException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => ModelLoader.LoadModelFile(nonExistentPath));
    }

    [Fact]
    public void LoadModelFile_WithInvalidJson_ThrowsException()
    {
        // Arrange
        var invalidJsonPath = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid()}.json");
        File.WriteAllText(invalidJsonPath, "{ invalid json content }");

        try
        {
            // Act & Assert
            Should.Throw<Exception>(() => ModelLoader.LoadModelFile(invalidJsonPath));
        }
        finally
        {
            if (File.Exists(invalidJsonPath))
                File.Delete(invalidJsonPath);
        }
    }

    [Fact]
    public void GetModelsForTestClass_WithStringName_ReturnsMatchingModel()
    {
        // Arrange
        var models = CreateTestModels();

        // Act
        var result = models.GetModelsForTestClass("TestClass1");

        // Assert
        result.ShouldNotBeNull();
        result.TestClassName.ShouldBe("TestClass1");
    }

    [Fact]
    public void GetModelsForTestClass_WithStringName_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var models = CreateTestModels();

        // Act
        var result = models.GetModelsForTestClass("NonExistentClass");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModelsForTestClass_WithType_ReturnsMatchingModel()
    {
        // Arrange
        var models = CreateTestModels();

        // Act
        var result = models.GetModelsForTestClass(typeof(TestClass1));

        // Assert
        result.ShouldNotBeNull();
        result.TestClassName.ShouldBe("TestClass1");
    }

    [Fact]
    public void GetModelsForMethod_WithStringName_ReturnsMatchingModel()
    {
        // Arrange
        var methodModels = CreateTestMethodModels();

        // Act
        var result = methodModels.GetModelsForMethod("Method1");

        // Assert
        result.ShouldNotBeNull();
        result.TestMethodName.ShouldBe("Method1");
    }

    [Fact]
    public void GetModelsForMethod_WithStringName_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var methodModels = CreateTestMethodModels();

        // Act
        var result = methodModels.GetModelsForMethod("NonExistentMethod");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModelsForMethod_WithMemberInfo_ReturnsMatchingModel()
    {
        // Arrange
        var methodModels = CreateTestMethodModels();
        var memberInfo = typeof(TestClass1).GetMethod(nameof(TestClass1.Method1))!;

        // Act
        var result = methodModels.GetModelsForMethod(memberInfo);

        // Assert
        result.ShouldNotBeNull();
        result.TestMethodName.ShouldBe("Method1");
    }

    [Fact]
    public void GetModelsForProperty_WithStringName_ReturnsMatchingModel()
    {
        // Arrange
        var propertyModels = CreateTestPropertyModels();

        // Act
        var result = propertyModels.GetModelsForProperty("Property1");

        // Assert
        result.ShouldNotBeNull();
        result.PropertyName.ShouldEndWith(".Property1");
    }

    [Fact]
    public void GetModelsForProperty_WithStringName_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var propertyModels = CreateTestPropertyModels();

        // Act
        var result = propertyModels.GetModelsForProperty("NonExistentProperty");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetModelsForProperty_WithMemberInfo_ReturnsMatchingModel()
    {
        // Arrange
        var propertyModels = CreateTestPropertyModels();
        var memberInfo = typeof(TestClass1).GetProperty(nameof(TestClass1.Property1))!;

        // Act
        var result = propertyModels.GetModelsForProperty(memberInfo);

        // Assert
        result.ShouldNotBeNull();
        result.PropertyName.ShouldEndWith(".Property1");
    }

    [Fact]
    public void GetScaleFishModel_WithStrings_ReturnsCorrectModel()
    {
        // Arrange
        var classModels = CreateTestModels();

        // Act
        var result = classModels.GetScaleFishModel("TestClass1", "Method1", "Property1");

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetScaleFishModel_WithStrings_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var classModels = CreateTestModels();

        // Act
        var result = classModels.GetScaleFishModel("NonExistent", "Method1", "Property1");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetScaleFishModel_WithTypes_ReturnsCorrectModel()
    {
        // Arrange
        var classModels = CreateTestModels();
        var methodInfo = typeof(TestClass1).GetMethod(nameof(TestClass1.Method1))!;
        var propertyInfo = typeof(TestClass1).GetProperty(nameof(TestClass1.Property1))!;

        // Act
        var result = classModels.GetScaleFishModel(typeof(TestClass1), methodInfo, propertyInfo);

        // Assert
        result.ShouldNotBeNull();
    }

    private static IEnumerable<ScalefishClassModel> CreateTestModels()
    {
        var mockFunction = Substitute.For<ScaleFishModelFunction>();
        mockFunction.Name.Returns("Linear");

        var mockNextClosestFunction = Substitute.For<ScaleFishModelFunction>();
        mockNextClosestFunction.Name.Returns("Quadratic");

        var scaleFishModel = new ScaleFishModel(mockFunction, 0.95, mockNextClosestFunction, 0.85);
        var propertyModel = new ScaleFishPropertyModel("TestClass1.Method1.Property1", scaleFishModel);
        var methodModel = new ScaleFishMethodModel("Method1", new List<ScaleFishPropertyModel> { propertyModel });
        var classModel = new ScalefishClassModel("TestNamespace", "TestClass1", new List<ScaleFishMethodModel> { methodModel });

        return new List<ScalefishClassModel> { classModel };
    }

    private static IEnumerable<ScaleFishMethodModel> CreateTestMethodModels()
    {
        var mockFunction = Substitute.For<ScaleFishModelFunction>();
        var mockNextClosestFunction = Substitute.For<ScaleFishModelFunction>();
        var scaleFishModel = new ScaleFishModel(mockFunction, 0.95, mockNextClosestFunction, 0.85);
        var propertyModel = new ScaleFishPropertyModel("Property1", scaleFishModel);

        return new List<ScaleFishMethodModel>
        {
            new("Method1", new List<ScaleFishPropertyModel> { propertyModel }),
            new("Method2", new List<ScaleFishPropertyModel> { propertyModel })
        };
    }

    private static IEnumerable<ScaleFishPropertyModel> CreateTestPropertyModels()
    {
        var mockFunction = Substitute.For<ScaleFishModelFunction>();
        var mockNextClosestFunction = Substitute.For<ScaleFishModelFunction>();
        var scaleFishModel = new ScaleFishModel(mockFunction, 0.95, mockNextClosestFunction, 0.85);

        return new List<ScaleFishPropertyModel>
        {
            new("TestClass.Method.Property1", scaleFishModel),
            new("TestClass.Method.Property2", scaleFishModel)
        };
    }

    private static string CreateTestJsonContent()
    {
        return @"[
            {
                ""NameSpace"": ""TestNamespace"",
                ""TestClassName"": ""TestClass"",
                ""ScaleFishMethodModels"": [
                    {
                        ""TestMethodName"": ""TestMethod"",
                        ""ScaleFishPropertyModels"": [
                            {
                                ""PropertyName"": ""TestProperty"",
                                ""ScaleFishModel"": {
                                    ""ScaleFishModelFunction"": {
                                        ""Name"": ""Linear"",
                                        ""OName"": ""O(n)"",
                                        ""Quality"": ""Good"",
                                        ""FunctionDef"": ""f(x) = {0}x + {1}"",
                                        ""FunctionParameters"": {
                                            ""Scale"": 1.0,
                                            ""Bias"": 0.0
                                        }
                                    },
                                    ""GoodnessOfFit"": 0.95,
                                    ""NextClosestScaleFishModelFunction"": {
                                        ""Name"": ""Quadratic"",
                                        ""OName"": ""O(n^2)"",
                                        ""Quality"": ""Bad"",
                                        ""FunctionDef"": ""f(x) = {0}x^2 + {1}"",
                                        ""FunctionParameters"": {
                                            ""Scale"": 1.0,
                                            ""Bias"": 0.0
                                        }
                                    },
                                    ""NextClosestGoodnessOfFit"": 0.85
                                }
                            }
                        ]
                    }
                ]
            }
        ]";
    }

    private class TestClass1
    {
        public int Property1 { get; set; }
        public void Method1() { }
    }
}

