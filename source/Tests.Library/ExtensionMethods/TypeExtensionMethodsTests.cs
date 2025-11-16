using System;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;
using Shouldly;
using Xunit;

namespace Tests.Library.ExtensionMethods;

public class TypeExtensionMethodsTests
{
    #region GetSingleConstructor Tests

    [Fact]
    public void GetSingleConstructor_WithSinglePublicConstructor_ShouldReturnConstructor()
    {
        // Arrange
        var type = typeof(ClassWithSinglePublicConstructor);

        // Act
        var result = type.GetSingleConstructor();

        // Assert
        result.ShouldNotBeNull();
        result.IsPublic.ShouldBeTrue();
        result.GetParameters().Length.ShouldBe(1);
        result.GetParameters()[0].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    public void GetSingleConstructor_WithSinglePrivateConstructor_ShouldReturnConstructor()
    {
        // Arrange
        var type = typeof(ClassWithSinglePrivateConstructor);

        // Act
        var result = type.GetSingleConstructor();

        // Assert
        result.ShouldNotBeNull();
        result.IsPrivate.ShouldBeTrue();
        result.GetParameters().Length.ShouldBe(0);
    }

    [Fact]
    public void GetSingleConstructor_WithSingleInternalConstructor_ShouldReturnConstructor()
    {
        // Arrange
        var type = typeof(ClassWithSingleInternalConstructor);

        // Act
        var result = type.GetSingleConstructor();

        // Assert
        result.ShouldNotBeNull();
        result.IsAssembly.ShouldBeTrue();
        result.GetParameters().Length.ShouldBe(2);
    }

    [Fact]
    public void GetSingleConstructor_WithMultipleConstructors_ShouldThrowSailfishException()
    {
        // Arrange
        var type = typeof(ClassWithMultipleConstructors);

        // Act & Assert
        var exception = Should.Throw<SailfishException>(() => type.GetSingleConstructor());
        exception.Message.ShouldBe("A single ctor must be declared in all test types");
    }

    [Fact]
    public void GetSingleConstructor_WithNoConstructors_ShouldThrowSailfishException()
    {
        // Arrange
        var type = typeof(ClassWithNoConstructors);

        // Act & Assert
        var exception = Should.Throw<SailfishException>(() => type.GetSingleConstructor());
        exception.Message.ShouldBe("A single ctor must be declared in all test types");
    }

    [Fact]
    public void GetSingleConstructor_WithStaticClass_ShouldThrowSailfishException()
    {
        // Arrange
        var type = typeof(StaticClass);

        // Act & Assert
        var exception = Should.Throw<SailfishException>(() => type.GetSingleConstructor());
        exception.Message.ShouldBe("A single ctor must be declared in all test types");
    }

    #endregion

    #region GetCtorParamTypes Tests

    [Fact]
    public void GetCtorParamTypes_WithParameterlessConstructor_ShouldReturnEmptyArray()
    {
        // Arrange
        var type = typeof(ClassWithSinglePrivateConstructor);

        // Act
        var result = type.GetCtorParamTypes();

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(0);
    }

    [Fact]
    public void GetCtorParamTypes_WithSingleParameter_ShouldReturnSingleType()
    {
        // Arrange
        var type = typeof(ClassWithSinglePublicConstructor);

        // Act
        var result = type.GetCtorParamTypes();

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(1);
        result[0].ShouldBe(typeof(string));
    }

    [Fact]
    public void GetCtorParamTypes_WithMultipleParameters_ShouldReturnAllTypes()
    {
        // Arrange
        var type = typeof(ClassWithSingleInternalConstructor);

        // Act
        var result = type.GetCtorParamTypes();

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result[0].ShouldBe(typeof(int));
        result[1].ShouldBe(typeof(bool));
    }

    [Fact]
    public void GetCtorParamTypes_WithComplexParameterTypes_ShouldReturnCorrectTypes()
    {
        // Arrange
        var type = typeof(ClassWithComplexConstructor);

        // Act
        var result = type.GetCtorParamTypes();

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(3);
        result[0].ShouldBe(typeof(DateTime));
        result[1].ShouldBe(typeof(Guid));
        result[2].ShouldBe(typeof(decimal));
    }

    [Fact]
    public void GetCtorParamTypes_WithMultipleConstructors_ShouldThrowSailfishException()
    {
        // Arrange
        var type = typeof(ClassWithMultipleConstructors);

        // Act & Assert
        var exception = Should.Throw<SailfishException>(() => type.GetCtorParamTypes());
        exception.Message.ShouldBe("A single ctor must be declared in all test types");
    }

    #endregion

    #region Test Helper Classes

    public class ClassWithSinglePublicConstructor
    {
        public ClassWithSinglePublicConstructor(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    public class ClassWithSinglePrivateConstructor
    {
        private ClassWithSinglePrivateConstructor()
        {
        }
    }

    public class ClassWithSingleInternalConstructor
    {
        internal ClassWithSingleInternalConstructor(int number, bool flag)
        {
            Number = number;
            Flag = flag;
        }

        public int Number { get; }
        public bool Flag { get; }
    }

    public class ClassWithComplexConstructor
    {
        public ClassWithComplexConstructor(DateTime date, Guid id, decimal amount)
        {
            Date = date;
            Id = id;
            Amount = amount;
        }

        public DateTime Date { get; }
        public Guid Id { get; }
        public decimal Amount { get; }
    }

    public class ClassWithMultipleConstructors
    {
        public ClassWithMultipleConstructors()
        {
        }

        public ClassWithMultipleConstructors(string value)
        {
            Value = value;
        }

        public string? Value { get; }
    }

    public static class ClassWithNoConstructors
    {
        // Static class with no instance constructors
    }

    public static class StaticClass
    {
        public static void DoSomething() { }
    }

    #endregion
}
