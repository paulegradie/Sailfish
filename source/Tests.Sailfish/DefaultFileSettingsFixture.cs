using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Test;

public class DefaultFileSettingsFixture
{
    [Fact]
    public void DataIsExtractedFromAValidFileNameWithTags()
    {
        const string filename =
            "PerformanceTracking_2022-28-9--21-33-11.tags-Item1=some_value__Item2=a_value__Version=0.0.123-local.csv.tracking";

        var values = DefaultFileSettings.ExtractDataFromFileNameWithTagSection(filename);
        values["Version"].ShouldBe("0.0.123-local");
        values["Item1"].ShouldBe("some_value");
        values["Item2"].ShouldBe("a_value");
    }

    [Fact]
    public void DataIsNotExtractedWhenNoTagsPresent()
    {
        const string filename =
            "PerformanceTracking_2022-28-9--21-33-11.csv.tracking";
        var values = DefaultFileSettings.ExtractDataFromFileNameWithTagSection(filename);
        values.Keys.Count.ShouldBe(0);
    }
}