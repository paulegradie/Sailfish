using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Math;

namespace Sailfish.ComplexityEstimation;

public class ComplexityEstimate
{
    public string ComplexityName { get; set; }
}



public class ComplexityEstimator
{
    public void EstimateComplexity(double[] data)
    {
        var normalizedData = NormalizeData(data);
        var bestFitIndex = FindBestFit(normalizedData, ComplexityFunctions.GetComplexityFunctions());
        var bestFitComplexity = GetComplexityName(bestFitIndex);

        Console.WriteLine("Estimated complexity: " + bestFitComplexity);
    }

    public string Name { get; set; }

    class FitResult
    {
        public FitResult(IComplexityFunction complexityFunction, double error)
        {
            
        }
    }



    static int FindBestFit(double[] dataPoints, IReadOnlyList<IComplexityFunction> complexityFunctions)
    {
        var bestFitIndex = -1;
        var minError = double.MaxValue;

        var results = new ConcurrentBag<FitResult>();

        Parallel.ForEach(Enumerable.Range(0, complexityFunctions.Count), (i) =>
        {
            var function = complexityFunctions[i];
            var fittedData = GenerateFittedData(dataPoints.Length, function);
            var error = CalculateError(dataPoints, fittedData);
            results.Add(new FitResult(function));
        });

        for (var i = 0; i < complexityFunctions.Count; i++)
        {
            var function = complexityFunctions[i];
            var fittedData = GenerateFittedData(dataPoints.Length, function);
            var error = CalculateError(dataPoints, fittedData);

            if (error < minError)
            {
                minError = error;
                bestFitIndex = i;
            }
        }

        return bestFitIndex;
    }

    private static double[] NormalizeData(double[] data)
    {
        var min = data.Min();
        var max = data.Max();
        var range = max - min;

        var normalizedData = new double[data.Length];

        for (var i = 0; i < data.Length; i++)
        {
            normalizedData[i] = (data[i] - min) / range;
        }

        return normalizedData;
    }

    static Tuple<double, double>[] GenerateFittedData(int data, Func<double, double> complexityFunction)
    {
        var fittedData = new Tuple<double, double>[numDataPoints];

        for (var i = 0; i < numDataPoints; i++)
        {
            double x = i + 1; // Assuming x starts from 1
            var y = complexityFunction(x);
            fittedData[i] = Tuple.Create(x, y);
        }

        return fittedData;
    }

    static double CalculateError(double[] actualData, Tuple<double, double>[] fittedData)
    {
        var error = 0.0;

        for (var i = 0; i < actualData.Length; i++)
        {
            var actual = actualData[i];
            var fitted = fittedData[i].Item2;
            var squaredError = Math.Pow(actual - fitted, 2);
            error += squaredError;
        }

        return error;
    }

    static string GetComplexityName(int index)
    {
        // Assuming you have predefined names for the complexity functions
        var complexityNames = new string[]
        {
            "O(n)",
            "O(nlog(n))",
            "O(n^2)",
            // Add more names as needed
        };

        if (index >= 0 && index < complexityNames.Length)
        {
            return complexityNames[index];
        }

        return "Unknown";
    }
}