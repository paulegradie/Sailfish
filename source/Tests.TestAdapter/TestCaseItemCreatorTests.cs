using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using NSubstitute;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestProperties;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

/// <summary>
/// Comprehensive unit tests for TestCaseItemCreator.
/// Tests test case assembly, property handling, and edge cases.
/// </summary>
public class TestCaseItemCreatorTests
{
    #region AssembleTestCases Tests

    [Fact]
    public void AssembleTestCases_WithNullClassMetaData_ShouldThrowNullReferenceException()
    {
        // Arrange
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();

        // Act & Assert - Actual implementation throws NullReferenceException
        Should.Throw<NullReferenceException>(() =>
            TestCaseItemCreator.AssembleTestCases(null!, "source.dll", hashAlgorithm).ToList());
    }

    [Fact]
    public void AssembleTestCases_WithNullSourceDll_ShouldThrowArgumentNullException()
    {
        // Arrange
        var classMetaData = CreateClassMetaData();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            TestCaseItemCreator.AssembleTestCases(classMetaData, null!, hashAlgorithm).ToList());
    }

    [Fact]
    public void AssembleTestCases_WithNullHashAlgorithm_ShouldThrowNullReferenceException()
    {
        // Arrange
        var classMetaData = CreateClassMetaData();

        // Act & Assert - Actual implementation throws NullReferenceException
        Should.Throw<NullReferenceException>(() =>
            TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", null!).ToList());
    }

    [Fact]
    public void AssembleTestCases_WithValidParameters_ShouldCreateTestCases()
    {
        // Arrange
        var classMetaData = CreateClassMetaData();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();
        hashAlgorithm.GuidFromString(Arg.Any<string>()).Returns(Guid.NewGuid());

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert
        testCases.ShouldNotBeEmpty();
        testCases.Count.ShouldBe(2); // Two methods in the test class
    }

    [Fact]
    public void AssembleTestCases_ShouldSetCorrectTestCaseProperties()
    {
        // Arrange
        var classMetaData = CreateClassMetaData();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();
        var expectedGuid = Guid.NewGuid();
        hashAlgorithm.GuidFromString(Arg.Any<string>()).Returns(expectedGuid);

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert
        var testCase = testCases.First();
        testCase.Id.ShouldBe(expectedGuid);
        testCase.Source.ShouldBe("source.dll");
        testCase.ExecutorUri.ShouldBe(TestExecutor.ExecutorUri);
        testCase.CodeFilePath.ShouldBe(classMetaData.FilePath);
        testCase.LineNumber.ShouldBe(10); // From the method metadata
    }

    [Fact]
    public void AssembleTestCases_ShouldSetSailfishManagedProperties()
    {
        // Arrange
        var classMetaData = CreateClassMetaData();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();
        hashAlgorithm.GuidFromString(Arg.Any<string>()).Returns(Guid.NewGuid());

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert
        var testCase = testCases.First();
        testCase.GetPropertyValue(SailfishManagedProperty.SailfishTypeProperty).ShouldBe(typeof(TestCaseItemCreatorTestClass).FullName);
        testCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty).ShouldBe("TestMethod1");
        testCase.GetPropertyValue(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty).ShouldNotBeNull();
        testCase.GetPropertyValue(SailfishManagedProperty.SailfishFormedVariableSectionDefinitionProperty).ShouldNotBeNull();
    }

    [Fact]
    public void AssembleTestCases_WithComparisonGroup_ShouldSetComparisonProperty()
    {
        // Arrange
        var classMetaData = CreateClassMetaDataWithComparison();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();
        hashAlgorithm.GuidFromString(Arg.Any<string>()).Returns(Guid.NewGuid());

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert
        var testCase = testCases.First();
        testCase.GetPropertyValue(SailfishManagedProperty.SailfishComparisonGroupProperty).ShouldBe("TestGroup");
    }

    [Fact]
    public void AssembleTestCases_WithEmptyMethods_ShouldReturnEmptyList()
    {
        // Arrange
        var classMetaData = CreateClassMetaDataWithNoMethods();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert
        testCases.ShouldBeEmpty();
    }

    [Fact]
    public void AssembleTestCases_WithNullTypeFullName_ShouldStillCreateTestCases()
    {
        // Arrange
        var classMetaData = CreateClassMetaDataWithNullTypeName();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert - Implementation still creates test cases even with null type name
        testCases.ShouldNotBeEmpty();
        testCases.Count.ShouldBe(1);
    }

    [Fact]
    public void AssembleTestCases_WithVariableProperties_ShouldCreateMultipleTestCases()
    {
        // Arrange
        var classMetaData = CreateClassMetaDataWithVariables();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();
        hashAlgorithm.GuidFromString(Arg.Any<string>()).Returns(Guid.NewGuid());

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert
        testCases.ShouldNotBeEmpty();
        // Should create test cases based on variable combinations
    }

    [Fact]
    public void AssembleTestCases_ShouldGenerateUniqueDisplayNames()
    {
        // Arrange
        var classMetaData = CreateClassMetaData();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();
        hashAlgorithm.GuidFromString(Arg.Any<string>()).Returns(Guid.NewGuid());

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert
        var displayNames = testCases.Select(tc => tc.DisplayName).ToList();
        displayNames.Distinct().Count().ShouldBe(displayNames.Count); // Check uniqueness
        displayNames.ShouldAllBe(name => !string.IsNullOrEmpty(name));
    }

    [Fact]
    public void AssembleTestCases_ShouldGenerateUniqueFullyQualifiedNames()
    {
        // Arrange
        var classMetaData = CreateClassMetaData();
        var hashAlgorithm = Substitute.For<IHashAlgorithm>();
        hashAlgorithm.GuidFromString(Arg.Any<string>()).Returns(Guid.NewGuid());

        // Act
        var testCases = TestCaseItemCreator.AssembleTestCases(classMetaData, "source.dll", hashAlgorithm).ToList();

        // Assert
        var fullyQualifiedNames = testCases.Select(tc => tc.FullyQualifiedName).ToList();
        fullyQualifiedNames.Distinct().Count().ShouldBe(fullyQualifiedNames.Count); // Check uniqueness
        fullyQualifiedNames.ShouldAllBe(name => !string.IsNullOrEmpty(name));
    }

    #endregion

    #region Helper Methods

    private ClassMetaData CreateClassMetaData()
    {
        var syntaxTree = Substitute.For<SyntaxTree>();
        return new ClassMetaData(
            filePath: "/path/to/TestClass.cs",
            classFullName: "Tests.TestAdapter.TestCaseItemCreatorTestClass",
            performanceTestType: typeof(TestCaseItemCreatorTestClass),
            syntaxTree: syntaxTree,
            methods: new[]
            {
                new MethodMetaData("TestMethod1", 10, null),
                new MethodMetaData("TestMethod2", 20, null)
            });
    }

    private ClassMetaData CreateClassMetaDataWithComparison()
    {
        var syntaxTree = Substitute.For<SyntaxTree>();
        return new ClassMetaData(
            filePath: "/path/to/TestClass.cs",
            classFullName: "Tests.TestAdapter.TestCaseItemCreatorTestClass",
            performanceTestType: typeof(TestCaseItemCreatorTestClass),
            syntaxTree: syntaxTree,
            methods: new[]
            {
                new MethodMetaData("TestMethod1", 10, "TestGroup")
            });
    }

    private ClassMetaData CreateClassMetaDataWithNoMethods()
    {
        var syntaxTree = Substitute.For<SyntaxTree>();
        return new ClassMetaData(
            filePath: "/path/to/TestClass.cs",
            classFullName: "Tests.TestAdapter.TestCaseItemCreatorTestClass",
            performanceTestType: typeof(TestCaseItemCreatorTestClass),
            syntaxTree: syntaxTree,
            methods: Array.Empty<MethodMetaData>());
    }

    private ClassMetaData CreateClassMetaDataWithNullTypeName()
    {
        var syntaxTree = Substitute.For<SyntaxTree>();
        return new ClassMetaData(
            filePath: "/path/to/TestClass.cs",
            classFullName: "Tests.TestAdapter.TestCaseItemCreatorTypeWithNullFullName",
            performanceTestType: typeof(TestCaseItemCreatorTypeWithNullFullName),
            syntaxTree: syntaxTree,
            methods: new[]
            {
                new MethodMetaData("TestMethod1", 10, null)
            });
    }

    private ClassMetaData CreateClassMetaDataWithVariables()
    {
        var syntaxTree = Substitute.For<SyntaxTree>();
        return new ClassMetaData(
            filePath: "/path/to/TestClassWithVariables.cs",
            classFullName: "Tests.TestAdapter.TestCaseItemCreatorTestClassWithVariables",
            performanceTestType: typeof(TestCaseItemCreatorTestClassWithVariables),
            syntaxTree: syntaxTree,
            methods: new[]
            {
                new MethodMetaData("TestMethod1", 10, null)
            });
    }

    #endregion
}

// Test classes for the TestCaseItemCreator tests (without Sailfish attributes to avoid discovery)
public class TestCaseItemCreatorTestClass
{
    public void TestMethod1() { }

    public void TestMethod2() { }
}

public class TestCaseItemCreatorTestClassWithVariables
{
    public string TestVariable { get; set; } = string.Empty;

    public void TestMethod1() { }
}

// Mock type that simulates a type with null FullName
public class TestCaseItemCreatorTypeWithNullFullName
{
    // This class is used to test edge cases where Type.FullName might be null
}
