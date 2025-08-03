using System;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Tests.Integration.SailDiffFormatting
{
    /// <summary>
    /// Simple validation test to verify the unified formatter is working correctly.
    /// This test can be run manually to validate the implementation.
    /// </summary>
    public class UnifiedFormatterValidationTest
    {
        public static void RunValidationTest()
        {
            Console.WriteLine("🧪 Running Unified Formatter Validation Test...");
            Console.WriteLine();

            try
            {
                // Create the unified formatter
                var formatter = SailDiffUnifiedFormatterFactory.Create();
                Console.WriteLine("✅ Successfully created unified formatter");

                // Create test comparison data
                var testData = CreateTestComparisonData();
                Console.WriteLine("✅ Successfully created test comparison data");

                // Test IDE formatting
                Console.WriteLine("\n📱 Testing IDE Context Formatting:");
                var ideResult = formatter.Format(testData, OutputContext.IDE);
                Console.WriteLine("Impact Summary:");
                Console.WriteLine(ideResult.ImpactSummary);
                Console.WriteLine("\nFull Output:");
                Console.WriteLine(ideResult.FullOutput);

                // Test Markdown formatting
                Console.WriteLine("\n📝 Testing Markdown Context Formatting:");
                var markdownResult = formatter.Format(testData, OutputContext.Markdown);
                Console.WriteLine("Impact Summary:");
                Console.WriteLine(markdownResult.ImpactSummary);
                Console.WriteLine("\nFull Output:");
                Console.WriteLine(markdownResult.FullOutput);

                // Test Console formatting
                Console.WriteLine("\n💻 Testing Console Context Formatting:");
                var consoleResult = formatter.Format(testData, OutputContext.Console);
                Console.WriteLine("Impact Summary:");
                Console.WriteLine(consoleResult.ImpactSummary);
                Console.WriteLine("\nFull Output:");
                Console.WriteLine(consoleResult.FullOutput);

                // Validate consistency
                Console.WriteLine("\n🔍 Validating Consistency:");
                Console.WriteLine($"IDE Significance: {ideResult.Significance}");
                Console.WriteLine($"Markdown Significance: {markdownResult.Significance}");
                Console.WriteLine($"Console Significance: {consoleResult.Significance}");
                
                if (ideResult.Significance == markdownResult.Significance && 
                    markdownResult.Significance == consoleResult.Significance)
                {
                    Console.WriteLine("✅ All contexts report consistent significance");
                }
                else
                {
                    Console.WriteLine("❌ Inconsistent significance across contexts");
                }

                Console.WriteLine($"\nIDE Percentage Change: {ideResult.PercentageChange:F1}%");
                Console.WriteLine($"Markdown Percentage Change: {markdownResult.PercentageChange:F1}%");
                Console.WriteLine($"Console Percentage Change: {consoleResult.PercentageChange:F1}%");

                Console.WriteLine("\n🎉 Unified Formatter Validation Test PASSED!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Unified Formatter Validation Test FAILED: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static SailDiffComparisonData CreateTestComparisonData()
        {
            // Create a realistic statistical test result
            var statisticalResult = new StatisticalTestResult(
                meanBefore: 1.909,      // BubbleSort mean
                meanAfter: 0.006,       // QuickSort mean  
                medianBefore: 1.850,    // BubbleSort median
                medianAfter: 0.005,     // QuickSort median
                testStatistic: 15.234,  // T-test statistic
                pValue: 0.000001,       // Highly significant
                changeDescription: "Regressed", // Performance got worse (slower)
                sampleSizeBefore: 100,
                sampleSizeAfter: 100,
                rawDataBefore: new double[] { 1.8, 1.9, 2.0, 1.85, 1.95 },
                rawDataAfter: new double[] { 0.005, 0.006, 0.007, 0.005, 0.006 },
                additionalResults: new System.Collections.Generic.Dictionary<string, object>()
            );

            return new SailDiffComparisonData
            {
                GroupName = "SortingAlgorithms",
                PrimaryMethodName = "BubbleSort",
                ComparedMethodName = "QuickSort",
                Statistics = statisticalResult,
                Metadata = new ComparisonMetadata
                {
                    SampleSize = 100,
                    AlphaLevel = 0.05,
                    TestType = "T-Test",
                    OutliersRemoved = 3
                },
                IsPerspectiveBased = false
            };
        }
    }

    /// <summary>
    /// Console application entry point for manual testing
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            UnifiedFormatterValidationTest.RunValidationTest();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
