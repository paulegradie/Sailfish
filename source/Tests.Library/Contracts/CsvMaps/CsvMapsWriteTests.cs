using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Sailfish.Contracts.Private.CsvMaps;
using Sailfish.Contracts.Public.Models;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Shouldly;
using Xunit;

namespace Tests.Library.Contracts.CsvMaps;

public class CsvMapsWriteTests
{
    [Fact]
    public void WriteAsCsvMap_WritesHeadersAndValues_InExpectedOrder()
    {
        var perf = new PerformanceRunResult(
            displayName: "MyDisplay",
            mean: 2.0,
            stdDev: 1.0,
            variance: 1.5,
            median: 1.7,
            rawExecutionResults: [1.1, 2.2],
            sampleSize: 2,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: [1.1, 2.2],
            upperOutliers: [],
            lowerOutliers: [],
            totalNumOutliers: 0,
            standardError: 0.0,
            confidenceLevel: 0.95,
            confidenceIntervalLower: 1.0,
            confidenceIntervalUpper: 3.0,
            marginOfError: 1.0);

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<WriteAsCsvMap>();
        csv.WriteRecords(new List<PerformanceRunResult> { perf });
        var text = writer.ToString();

        text.ShouldContain("DisplayName");
        text.ShouldContain("CI95_MOE");
        text.ShouldContain("CI99_MOE");
        text.ShouldContain("MyDisplay");

        // Ensure arrays are quoted as a single field
        text.ShouldContain("\"1.1,2.2\"");
    }

    [Fact]
    public void SailDiffWriteAsCsvMap_WritesFlattenedProperties()
    {
        var testCaseId = new TestCaseId("MyClass.MyMethod()");
        var stat = new StatisticalTestResult(
            meanBefore: 10.0,
            meanAfter: 5.0,
            medianBefore: 9.5,
            medianAfter: 4.5,
            testStatistic: 5.0,
            pValue: 0.001,
            changeDescription: "Improved",
            sampleSizeBefore: 100,
            sampleSizeAfter: 100,
            rawDataBefore: [1.0, 2.0, 3.0],
            rawDataAfter: [1.5, 2.5, 3.5],
            additionalResults: new Dictionary<string, object>());
        var result = new TestResultWithOutlierAnalysis(stat, null, null);
        var diff = new SailDiffResult(testCaseId, result);

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<SailDiffWriteAsCsvMap>();
        csv.WriteRecords(new List<SailDiffResult> { diff });
        var text = writer.ToString();

        // Should include display name and key numeric values
        text.ShouldContain(testCaseId.DisplayName);
        text.ShouldContain("10");
        text.ShouldContain("5");
        text.ShouldContain("Improved");
        text.ShouldContain("0.001");
        text.ShouldContain("\"1,2,3\"");
        text.ShouldContain("\"1.5,2.5,3.5\"");
    }
}

