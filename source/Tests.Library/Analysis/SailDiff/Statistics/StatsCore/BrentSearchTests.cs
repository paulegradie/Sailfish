using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore;

public class BrentSearchTests
{
    #region FindRoot Tests

    [Fact]
    public void FindRoot_WithSimpleLinearFunction_ShouldFindRoot()
    {
        // Arrange: f(x) = x - 5, root at x = 5
        Func<double, double> function = x => x - 5;

        // Act
        var result = BrentSearch.FindRoot(function, 0, 10);

        // Assert
        result.ShouldBe(5.0, 1e-6);
    }

    [Fact]
    public void FindRoot_WithQuadraticFunction_ShouldFindRoot()
    {
        // Arrange: f(x) = x^2 - 4, root at x = 2 (in range [0, 10])
        Func<double, double> function = x => x * x - 4;

        // Act
        var result = BrentSearch.FindRoot(function, 0, 10);

        // Assert
        result.ShouldBe(2.0, 1e-6);
    }

    [Fact]
    public void FindRoot_WithCubicFunction_ShouldFindRoot()
    {
        // Arrange: f(x) = x^3 - 8, root at x = 2
        Func<double, double> function = x => x * x * x - 8;

        // Act
        var result = BrentSearch.FindRoot(function, 0, 10);

        // Assert
        result.ShouldBe(2.0, 1e-6);
    }

    [Fact]
    public void FindRoot_WithTrigonometricFunction_ShouldFindRoot()
    {
        // Arrange: f(x) = sin(x), root at x = π (in range [3, 4])
        Func<double, double> function = Math.Sin;

        // Act
        var result = BrentSearch.FindRoot(function, 3, 4);

        // Assert
        result.ShouldBe(Math.PI, 1e-6);
    }

    [Fact]
    public void FindRoot_WithExponentialFunction_ShouldFindRoot()
    {
        // Arrange: f(x) = e^x - 10, root at x = ln(10)
        Func<double, double> function = x => Math.Exp(x) - 10;

        // Act
        var result = BrentSearch.FindRoot(function, 0, 5);

        // Assert
        result.ShouldBe(Math.Log(10), 1e-6);
    }

    [Fact]
    public void FindRoot_WithNegativeRoot_ShouldFindRoot()
    {
        // Arrange: f(x) = x + 3, root at x = -3
        Func<double, double> function = x => x + 3;

        // Act
        var result = BrentSearch.FindRoot(function, -10, 0);

        // Assert
        result.ShouldBe(-3.0, 1e-6);
    }

    [Fact]
    public void FindRoot_WithRootAtZero_ShouldFindRoot()
    {
        // Arrange: f(x) = x, root at x = 0
        Func<double, double> function = x => x;

        // Act
        var result = BrentSearch.FindRoot(function, -1, 1);

        // Assert
        result.ShouldBe(0.0, 1e-6);
    }

    [Fact]
    public void FindRoot_WithCustomTolerance_ShouldRespectTolerance()
    {
        // Arrange: f(x) = x - 5
        Func<double, double> function = x => x - 5;
        var tolerance = 1e-10;

        // Act
        var result = BrentSearch.FindRoot(function, 0, 10, tolerance);

        // Assert
        result.ShouldBe(5.0, tolerance * 10); // Allow some margin
    }

    [Fact]
    public void FindRoot_WithInfiniteLowerBound_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        Func<double, double> function = x => x - 5;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            BrentSearch.FindRoot(function, double.NegativeInfinity, 10))
            .ParamName.ShouldBe("lowerBound");
    }

    [Fact]
    public void FindRoot_WithInfiniteUpperBound_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        Func<double, double> function = x => x - 5;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            BrentSearch.FindRoot(function, 0, double.PositiveInfinity))
            .ParamName.ShouldBe("upperBound");
    }

    [Fact]
    public void FindRoot_WithNegativeTolerance_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        Func<double, double> function = x => x - 5;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            BrentSearch.FindRoot(function, 0, 10, -0.001))
            .ParamName.ShouldBe("tol");
    }

    [Fact]
    public void FindRoot_WithRootNotBracketed_ShouldThrowConvergenceException()
    {
        // Arrange: f(x) = x^2 + 1, no real roots (always positive)
        Func<double, double> function = x => x * x + 1;

        // Act & Assert
        var exception = Should.Throw<ConvergenceException>(() =>
            BrentSearch.FindRoot(function, 0, 10));
        exception.Message.ShouldContain("Root must be enclosed between bounds");
    }

    [Fact]
    public void FindRoot_WithFunctionReturningNaN_ShouldThrowConvergenceException()
    {
        // Arrange: Function that returns valid values at bounds but NaN in the middle
        // This ensures BrentSearch's NaN detection (not Math.Sign) is triggered
        // f(0) = -1, f(10) = 1 (valid bracket), but f(x) = NaN for 4 < x < 6
        Func<double, double> function = x =>
        {
            if (x <= 4) return x - 5;  // negative for x < 5
            if (x >= 6) return x - 5;  // positive for x > 5
            return double.NaN;         // NaN in the middle where root would be
        };

        // Act & Assert - BrentSearch should throw ConvergenceException for non-finite function values
        var exception = Should.Throw<ConvergenceException>(() =>
            BrentSearch.FindRoot(function, 0, 10));
        exception.Message.ShouldContain("Function evaluation didn't return a finite number");
    }

    [Fact]
    public void FindRoot_WithFunctionReturningInfinity_ShouldHandleGracefully()
    {
        // Arrange: Function that returns infinity after finding root
        // The algorithm finds the root at x=2.5 before encountering infinity
        Func<double, double> function = x => x < 5 ? x - 2.5 : double.PositiveInfinity;

        // Act
        var result = BrentSearch.FindRoot(function, 0, 10);

        // Assert - Should find the root at 2.5 before hitting infinity region
        result.ShouldBe(2.5, 1e-6);
    }

    [Fact]
    public void FindRoot_WithLimitedIterations_ShouldStillConverge()
    {
        // Arrange: Simple function that converges quickly
        Func<double, double> function = x => x - 0.5;

        // Act - Even with just 5 iterations, this simple function should converge
        var result = BrentSearch.FindRoot(function, 0, 1, 1e-6, 5);

        // Assert
        result.ShouldBe(0.5, 1e-6);
    }

    #endregion

    #region Find Tests

    [Fact]
    public void Find_WithLinearFunction_ShouldFindValue()
    {
        // Arrange: f(x) = 2x, find x where f(x) = 10
        Func<double, double> function = x => 2 * x;
        var targetValue = 10.0;

        // Act
        var result = BrentSearch.Find(function, targetValue, 0, 10);

        // Assert
        result.ShouldBe(5.0, 1e-6);
    }

    [Fact]
    public void Find_WithQuadraticFunction_ShouldFindValue()
    {
        // Arrange: f(x) = x^2, find x where f(x) = 16
        Func<double, double> function = x => x * x;
        var targetValue = 16.0;

        // Act
        var result = BrentSearch.Find(function, targetValue, 0, 10);

        // Assert
        result.ShouldBe(4.0, 1e-6);
    }

    [Fact]
    public void Find_WithExponentialFunction_ShouldFindValue()
    {
        // Arrange: f(x) = e^x, find x where f(x) = 100
        Func<double, double> function = Math.Exp;
        var targetValue = 100.0;

        // Act
        var result = BrentSearch.Find(function, targetValue, 0, 10);

        // Assert
        result.ShouldBe(Math.Log(100), 1e-6);
    }

    [Fact]
    public void Find_WithCustomTolerance_ShouldRespectTolerance()
    {
        // Arrange
        Func<double, double> function = x => 3 * x;
        var targetValue = 15.0;
        var tolerance = 1e-10;

        // Act
        var result = BrentSearch.Find(function, targetValue, 0, 10, tolerance);

        // Assert
        result.ShouldBe(5.0, tolerance * 10);
    }

    [Fact]
    public void Find_WithNegativeTarget_ShouldFindValue()
    {
        // Arrange: f(x) = x - 10, find x where f(x) = -5
        Func<double, double> function = x => x - 10;
        var targetValue = -5.0;

        // Act
        var result = BrentSearch.Find(function, targetValue, 0, 10);

        // Assert
        result.ShouldBe(5.0, 1e-6);
    }

    [Fact]
    public void Find_WithValueNotInRange_ShouldThrowConvergenceException()
    {
        // Arrange: f(x) = x^2 + 10, find x where f(x) = 5 (impossible, minimum is 10)
        Func<double, double> function = x => x * x + 10;
        var targetValue = 5.0;

        // Act & Assert
        Should.Throw<ConvergenceException>(() =>
            BrentSearch.Find(function, targetValue, 0, 10));
    }

    #endregion
}

