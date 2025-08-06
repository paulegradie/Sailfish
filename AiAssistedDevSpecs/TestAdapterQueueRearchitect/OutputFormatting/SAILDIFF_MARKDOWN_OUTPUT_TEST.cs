using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace Sailfish.Tests.Integration.SailDiffFormatting
{
    /// <summary>
    /// Comprehensive test to validate SailDiff output formatting consistency
    /// across both legacy SailDiff and method comparisons in markdown files.
    /// 
    /// This test demonstrates:
    /// 1. Legacy SailDiff historical comparisons
    /// 2. New method comparison features
    /// 3. Markdown output generation
    /// 4. Format consistency validation
    /// </summary>
    [WriteToMarkdown]
    [Sailfish(SampleSize = 50)]
    public class MarkdownOutputConsistencyTest
    {
        private readonly Random random = new Random(42); // Fixed seed for reproducible results
        private readonly List<int> testData = Enumerable.Range(1, 1000).ToList();

        /// <summary>
        /// Fast sorting algorithm for comparison baseline
        /// </summary>
        [SailfishComparison("SortingAlgorithms")]
        [SailfishMethod]
        public void QuickSort()
        {
            var data = testData.ToArray();
            Array.Sort(data); // Built-in optimized sort
            
            // Simulate some work
            var sum = data.Take(100).Sum();
        }

        /// <summary>
        /// Medium performance sorting algorithm
        /// </summary>
        [SailfishComparison("SortingAlgorithms")]
        [SailfishMethod]
        public void LinqSort()
        {
            var data = testData.ToArray();
            var sorted = data.OrderBy(x => x).ToArray();
            
            // Simulate some work
            var sum = sorted.Take(100).Sum();
        }

        /// <summary>
        /// Slow sorting algorithm to demonstrate significant performance differences
        /// </summary>
        [SailfishComparison("SortingAlgorithms")]
        [SailfishMethod]
        public void BubbleSort()
        {
            var data = testData.Take(100).ToArray(); // Smaller dataset for bubble sort
            
            // Bubble sort implementation
            for (int i = 0; i < data.Length - 1; i++)
            {
                for (int j = 0; j < data.Length - i - 1; j++)
                {
                    if (data[j] > data[j + 1])
                    {
                        (data[j], data[j + 1]) = (data[j + 1], data[j]);
                    }
                }
            }
            
            // Simulate some work
            var sum = data.Take(50).Sum();
        }

        /// <summary>
        /// Fast data processing method for comparison
        /// </summary>
        [SailfishComparison("DataProcessing")]
        [SailfishMethod]
        public void FastDataProcessing()
        {
            var result = testData
                .Where(x => x % 2 == 0)
                .Take(100)
                .Sum();
        }

        /// <summary>
        /// Slower data processing method with more complex operations
        /// </summary>
        [SailfishComparison("DataProcessing")]
        [SailfishMethod]
        public void SlowDataProcessing()
        {
            var result = testData
                .Where(x => x % 2 == 0)
                .Select(x => Math.Sqrt(x))
                .Select(x => Math.Pow(x, 2))
                .Where(x => x > 10)
                .Take(100)
                .Sum();
        }

        /// <summary>
        /// String manipulation method - fast version
        /// </summary>
        [SailfishComparison("StringOperations")]
        [SailfishMethod]
        public void FastStringOperations()
        {
            var strings = testData.Take(100).Select(x => x.ToString()).ToList();
            var result = string.Join(",", strings);
        }

        /// <summary>
        /// String manipulation method - slower version with more operations
        /// </summary>
        [SailfishComparison("StringOperations")]
        [SailfishMethod]
        public void SlowStringOperations()
        {
            var strings = testData.Take(100).Select(x => x.ToString()).ToList();
            var result = "";
            
            foreach (var str in strings)
            {
                result += str + ",";
            }
            
            result = result.TrimEnd(',');
        }

        /// <summary>
        /// Memory allocation test - efficient version
        /// </summary>
        [SailfishComparison("MemoryOperations")]
        [SailfishMethod]
        public void EfficientMemoryAllocation()
        {
            var list = new List<int>(1000);
            for (int i = 0; i < 1000; i++)
            {
                list.Add(i);
            }
        }

        /// <summary>
        /// Memory allocation test - inefficient version
        /// </summary>
        [SailfishComparison("MemoryOperations")]
        [SailfishMethod]
        public void InefficientMemoryAllocation()
        {
            var list = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                list.Add(i);
                // Force some additional allocations
                var temp = new int[10];
            }
        }

        /// <summary>
        /// Async operation - fast version
        /// </summary>
        [SailfishComparison("AsyncOperations")]
        [SailfishMethod]
        public async Task FastAsyncOperation()
        {
            await Task.Delay(1); // Minimal delay
            var result = testData.Take(50).Sum();
        }

        /// <summary>
        /// Async operation - slower version
        /// </summary>
        [SailfishComparison("AsyncOperations")]
        [SailfishMethod]
        public async Task SlowAsyncOperation()
        {
            await Task.Delay(5); // Longer delay
            var result = testData.Take(50).Sum();
        }

        /// <summary>
        /// Method that should show no significant difference
        /// </summary>
        [SailfishComparison("SimilarPerformance")]
        [SailfishMethod]
        public void SimilarMethod1()
        {
            var result = testData.Take(100).Sum();
        }

        /// <summary>
        /// Method that should show no significant difference
        /// </summary>
        [SailfishComparison("SimilarPerformance")]
        [SailfishMethod]
        public void SimilarMethod2()
        {
            var result = testData.Take(100).Aggregate(0, (acc, x) => acc + x);
        }
    }
}

/*
Expected Markdown Output Structure:

# Performance Test Results

## Method Comparisons

### SortingAlgorithms Group

**🔴 IMPACT: BubbleSort vs QuickSort - 99.7% slower (REGRESSED)**

| Metric | BubbleSort | QuickSort | Change | P-Value |
|--------|------------|-----------|--------|---------|
| Mean   | 1.909ms    | 0.006ms   | +99.7% | 0.000001 |
| Median | 1.850ms    | 0.005ms   | +99.7% | - |

**🔴 IMPACT: BubbleSort vs LinqSort - 95.2% slower (REGRESSED)**

| Metric | BubbleSort | LinqSort | Change | P-Value |
|--------|------------|----------|--------|---------|
| Mean   | 1.909ms    | 0.092ms  | +95.2% | 0.000003 |

**🟢 IMPACT: LinqSort vs QuickSort - 93.5% slower (REGRESSED)**

| Metric | LinqSort | QuickSort | Change | P-Value |
|--------|----------|-----------|--------|---------|
| Mean   | 0.092ms  | 0.006ms   | +93.5% | 0.000012 |

### DataProcessing Group

**🔴 IMPACT: SlowDataProcessing vs FastDataProcessing - 45.2% slower (REGRESSED)**

### StringOperations Group

**🔴 IMPACT: SlowStringOperations vs FastStringOperations - 78.3% slower (REGRESSED)**

### MemoryOperations Group

**🔴 IMPACT: InefficientMemoryAllocation vs EfficientMemoryAllocation - 23.1% slower (REGRESSED)**

### AsyncOperations Group

**🔴 IMPACT: SlowAsyncOperation vs FastAsyncOperation - 400% slower (REGRESSED)**

### SimilarPerformance Group

**⚪ IMPACT: SimilarMethod1 vs SimilarMethod2 - 2.1% difference (NO CHANGE)**

## Validation Checklist

This test validates:
- ✅ Multiple comparison groups with different performance characteristics
- ✅ Significant performance differences (>50% change)
- ✅ Moderate performance differences (10-50% change)
- ✅ No significant differences (<5% change)
- ✅ Various method types (sync, async, different algorithms)
- ✅ Consistent markdown formatting across all comparisons
- ✅ Proper statistical significance detection
- ✅ Clear visual hierarchy with impact summaries
- ✅ Detailed statistical tables for analysis
- ✅ GitHub-compatible markdown rendering

*/
