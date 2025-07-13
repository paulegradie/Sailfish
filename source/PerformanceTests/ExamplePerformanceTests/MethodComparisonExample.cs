using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish]
public class MethodComparisonExample
{
    [SailfishVariable(100, 1000, 10000)]
    public int DataSize { get; set; }

    private List<int> data = new();

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random(42); // Fixed seed for consistent results
        data = Enumerable.Range(0, DataSize).Select(_ => random.Next(1000)).ToList();
    }

    [SailfishMethodComparison("SortingAlgorithms", BaselineMethod = "QuickSort")]
    [SailfishMethod]
    public void BubbleSort()
    {
        var array = data.ToArray();
        BubbleSortImplementation(array);
    }

    [SailfishMethodComparison("SortingAlgorithms")]
    [SailfishMethod]
    public void QuickSort()
    {
        var array = data.ToArray();
        QuickSortImplementation(array, 0, array.Length - 1);
    }

    [SailfishMethodComparison("SortingAlgorithms")]
    [SailfishMethod]
    public void MergeSort()
    {
        var array = data.ToArray();
        MergeSortImplementation(array, 0, array.Length - 1);
    }

    [SailfishMethodComparison("SearchAlgorithms", BaselineMethod = "LinearSearch")]
    [SailfishMethod]
    public void LinearSearch()
    {
        var target = data[data.Count / 2];
        LinearSearchImplementation(data.ToArray(), target);
    }

    [SailfishMethodComparison("SearchAlgorithms")]
    [SailfishMethod]
    public void BinarySearch()
    {
        var sortedArray = data.OrderBy(x => x).ToArray();
        var target = sortedArray[sortedArray.Length / 2];
        BinarySearchImplementation(sortedArray, target);
    }

    private static void BubbleSortImplementation(int[] array)
    {
        int n = array.Length;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                if (array[j] > array[j + 1])
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                }
            }
        }
    }

    private static void QuickSortImplementation(int[] array, int low, int high)
    {
        if (low < high)
        {
            int pi = Partition(array, low, high);
            QuickSortImplementation(array, low, pi - 1);
            QuickSortImplementation(array, pi + 1, high);
        }
    }

    private static int Partition(int[] array, int low, int high)
    {
        int pivot = array[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (array[j] < pivot)
            {
                i++;
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
        (array[i + 1], array[high]) = (array[high], array[i + 1]);
        return i + 1;
    }

    private static void MergeSortImplementation(int[] array, int left, int right)
    {
        if (left < right)
        {
            int middle = left + (right - left) / 2;
            MergeSortImplementation(array, left, middle);
            MergeSortImplementation(array, middle + 1, right);
            Merge(array, left, middle, right);
        }
    }

    private static void Merge(int[] array, int left, int middle, int right)
    {
        int n1 = middle - left + 1;
        int n2 = right - middle;

        int[] leftArray = new int[n1];
        int[] rightArray = new int[n2];

        Array.Copy(array, left, leftArray, 0, n1);
        Array.Copy(array, middle + 1, rightArray, 0, n2);

        int i = 0, j = 0, k = left;

        while (i < n1 && j < n2)
        {
            if (leftArray[i] <= rightArray[j])
            {
                array[k] = leftArray[i];
                i++;
            }
            else
            {
                array[k] = rightArray[j];
                j++;
            }
            k++;
        }

        while (i < n1)
        {
            array[k] = leftArray[i];
            i++;
            k++;
        }

        while (j < n2)
        {
            array[k] = rightArray[j];
            j++;
            k++;
        }
    }

    private static int LinearSearchImplementation(int[] array, int target)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == target)
                return i;
        }
        return -1;
    }

    private static int BinarySearchImplementation(int[] array, int target)
    {
        int left = 0;
        int right = array.Length - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;

            if (array[mid] == target)
                return mid;

            if (array[mid] < target)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return -1;
    }
}
