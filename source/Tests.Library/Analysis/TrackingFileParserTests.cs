using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Contracts.Public.Serialization;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tests.Library.Utils;
using Tests.Library.Utils.Builders;
using Xunit;

namespace Tests.Library.Analysis;

public class TrackingFileParserTests
{
    private readonly TrackingFileParser parser;

    public TrackingFileParserTests()
    {
        var mockLogger = Substitute.For<ILogger>();
        parser = new TrackingFileParser(new TrackingFileSerialization(mockLogger), mockLogger);
    }

    [Fact]
    public async Task FilesAreParsedSuccessfully()
    {
        var summaries = new List<ClassExecutionSummaryTrackingFormat>()
        {
            ClassExecutionSummaryTrackingFormatBuilder.Create().Build()
        };
        var serialized = SailfishSerializer.Serialize(summaries);
        var file = TempFileHelper.WriteStringToTempFile(serialized);

        var datalist = new TrackingFileDataList();
        var result = await parser.TryParse(file, datalist, CancellationToken.None);

        result.ShouldBeTrue();
        datalist.Count.ShouldBe(1);
        datalist.Single().Count.ShouldBe(1);

        var data = datalist.Single().Single();
        data.TestClass.Name.ShouldBe(nameof(ClassExecutionSummaryTrackingFormatBuilder.TestClass));
        data.ExecutionSettings.AsMarkdown.ShouldBeFalse();
        data.GetSuccessfulTestCases().Count().ShouldBe(1);
    }

    [Fact]
    public async Task SerializationExceptionCausesFailure()
    {
        var file = TempFileHelper.WriteStringToTempFile(Some.RandomString());

        var datalist = new TrackingFileDataList();
        var result = await parser.TryParse(file, datalist, CancellationToken.None);

        result.ShouldBeFalse();
        datalist.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenCorruptFileIsPresent_SerializationOfTheOthersStillSucceeds()
    {
        var corruptFile = TempFileHelper.WriteStringToTempFile(SailfishSerializer.Serialize(new List<ClassExecutionSummaryTrackingFormat>()));
        var summaries = new List<ClassExecutionSummaryTrackingFormat>()
        {
            ClassExecutionSummaryTrackingFormatBuilder.Create().Build()
        };
        var serialized = SailfishSerializer.Serialize(summaries);
        var goodFile = TempFileHelper.WriteStringToTempFile(serialized);

        var datalist = new TrackingFileDataList();

        var result = await parser.TryParseMany(new List<string>()
        {
            corruptFile,
            goodFile
        }, datalist, CancellationToken.None);

        result.ShouldBeTrue();
        datalist.Count.ShouldBe(1);
        datalist.Single().Count.ShouldBe(1);

        var data = datalist.Single().Single();
        data.TestClass.Name.ShouldBe(nameof(ClassExecutionSummaryTrackingFormatBuilder.TestClass));
        data.ExecutionSettings.AsMarkdown.ShouldBeFalse();
        data.GetSuccessfulTestCases().Count().ShouldBe(1);
    }
}