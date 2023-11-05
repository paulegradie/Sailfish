using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Sailfish.Contracts.Private.CsvMaps;

internal sealed class DoubleArrayCsvConverter : ITypeConverter
{
    private const string Sep = ",";

    public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) => text?.Split(Sep).Select(double.Parse).ToArray();

    public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is null) return null;
        return value is not double[] doubleArray ? null : string.Join(Sep, doubleArray.Select(x => x.ToString(CultureInfo.InvariantCulture)));
    }
}