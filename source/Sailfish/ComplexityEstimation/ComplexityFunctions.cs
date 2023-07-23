using System;

namespace Sailfish.ComplexityEstimation;

public interface IComplexityFunction
{
    double Compute(double n);
    public string Name { get; set; }
}

public class LinearComplexityFunction : IComplexityFunction
{
    public double Compute(double n)
    {
        return n;
    }

    public string Name { get; set; } = nameof(LinearComplexityFunction);
}

public class NLogNComplexityFunction : IComplexityFunction
{
    public double Compute(double n)
    {
        return n * Math.Log(n);
    }

    public string Name { get; set; } = nameof(NLogNComplexityFunction);
}

public static class ComplexityFunctions
{
    public static IComplexityFunction[] GetComplexityFunctions()
    {
        return new IComplexityFunction[]
        {
            new LinearComplexityFunction(),
            new NLogNComplexityFunction()
            // Quadratic,
            // Cubic,
            // LogLinear,
            // Exponential,
            // Factorial,
            // SqrtN,
            // LogLogN
        };
    }

    public static double NLogN(double n)
    {
    }

    public static double Quadratic(double n)
    {
        return n * n;
    }

    public static double Cubic(double n)
    {
        return n * n * n;
    }

    public static double LogLinear(double n)
    {
        return n * Math.Log(n, 2);
    }

    public static double Exponential(double n)
    {
        return Math.Pow(2, n);
    }

    public static double Factorial(double n)
    {
        if (n <= 1)
            return 1;

        double result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }

        return result;
    }

    public static double SqrtN(double n)
    {
        return Math.Sqrt(n);
    }

    public static double LogLogN(double n)
    {
        return Math.Log(Math.Log(n, 2), 2);
    }

    // Add more complexity functions as needed
}