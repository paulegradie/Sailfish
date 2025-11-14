using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Sailfish.Contracts.Private.CsvMaps.Converters;
using Shouldly;
using Xunit;

namespace Tests.Library.Contracts.CsvMaps;

public class DoubleArrayCsvConverterTests
{
    private readonly DoubleArrayCsvConverter _converter = new();

    [Fact]
    public void ConvertToString_WithDoubleArray_ReturnsCommaSeparatedInvariant()
    {
        var input = new[] { 1.1, 2.2, 3.3 };
        var result = _converter.ConvertToString(input, row: null!, memberMapData: null!);
        result.ShouldBe("1.1,2.2,3.3");
    }

    [Fact]
    public void ConvertToString_WithNull_ReturnsNull()
    {
        var result = _converter.ConvertToString(null, row: null!, memberMapData: null!);
        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertFromString_WithCommaSeparated_ReturnsDoubleArray()
    {
        var value = "1.1,2.2,3.3";
        var result = (double[]?)_converter.ConvertFromString(value, row: null!, memberMapData: null!);
        result.ShouldNotBeNull();
        result!.ShouldBe([1.1, 2.2, 3.3]);
    }

    [Fact]
    public void ConvertFromString_WithNull_ReturnsNull()
    {
        var result = _converter.ConvertFromString(null, row: null!, memberMapData: null!);
        result.ShouldBeNull();
    }
}
