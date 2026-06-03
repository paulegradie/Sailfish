using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Contracts.Public.Serialization;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.Analysis;

public class TrackingFileParserTests
{
    private readonly TrackingFileParser _parser;

    public TrackingFileParserTests()
    {
        var mockLogger = Substitute.For<ILogger>();
        _parser = new TrackingFileParser(new TrackingFileSerialization(mockLogger), mockLogger);
    }

    [Fact]
    public async Task FilesAreParsedSuccessfully()
    {
        var summaries = new List<ClassExecutionSummaryTrackingFormat>
            { ClassExecutionSummaryTrackingFormatBuilder.Create().Build() };
        var serialized = SailfishSerializer.Serialize(summaries);
        var file = TempFileHelper.WriteStringToTempFile(serialized);

        var datalist = new TrackingFileDataList();
        var result = await _parser.TryParse(file, datalist, CancellationToken.None);

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
        var result = await _parser.TryParse(file, datalist, CancellationToken.None);

        result.ShouldBeFalse();
        datalist.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenCorruptFileIsPresent_SerializationOfTheOthersStillSucceeds()
    {
        var corruptFile = TempFileHelper.WriteStringToTempFile(SailfishSerializer.Serialize(new List<ClassExecutionSummaryTrackingFormat>()));
        var summaries = new List<ClassExecutionSummaryTrackingFormat>
            { ClassExecutionSummaryTrackingFormatBuilder.Create().Build() };
        var serialized = SailfishSerializer.Serialize(summaries);
        var goodFile = TempFileHelper.WriteStringToTempFile(serialized);

        var datalist = new TrackingFileDataList();

        var result = await _parser.TryParseMany(new List<string>
            { corruptFile, goodFile }, datalist, CancellationToken.None);

        result.ShouldBeTrue();
        datalist.Count.ShouldBe(1);
        datalist.Single().Count.ShouldBe(1);

        var data = datalist.Single().Single();
        data.TestClass.Name.ShouldBe(nameof(ClassExecutionSummaryTrackingFormatBuilder.TestClass));
        data.ExecutionSettings.AsMarkdown.ShouldBeFalse();
        data.GetSuccessfulTestCases().Count().ShouldBe(1);
    }

    [Fact]
    public async Task OneValidPlusOneGenuinelyCorruptFile_ReturnsValid_AndWarnsOnceNamingTheBadFile()
    {
        // #294: a single corrupt file must not abort the batch. The valid run is still returned and a single
        // skip warning names the offending file.
        var parserLogger = Substitute.For<ILogger>();
        var parser = new TrackingFileParser(new TrackingFileSerialization(Substitute.For<ILogger>()), parserLogger);

        var corruptFile = TempFileHelper.WriteStringToTempFile(Some.RandomString()); // not valid JSON
        var goodFile = TempFileHelper.WriteStringToTempFile(SailfishSerializer.Serialize(
            new List<ClassExecutionSummaryTrackingFormat> { ClassExecutionSummaryTrackingFormatBuilder.Create().Build() }));

        var datalist = new TrackingFileDataList();
        var result = await parser.TryParseMany(new List<string> { corruptFile, goodFile }, datalist, CancellationToken.None);

        result.ShouldBeTrue();
        datalist.Count.ShouldBe(1);
        datalist.Single().Single().TestClass.Name.ShouldBe(nameof(ClassExecutionSummaryTrackingFormatBuilder.TestClass));

        parserLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Skipping tracking file")),
            Arg.Is<object[]>(args => args.Length == 1 && Equals(args[0], corruptFile)));
    }

    [Fact]
    public async Task OnlyCorruptFiles_ReturnsFalse_WithEmptyResult_AndDoesNotThrow()
    {
        // #294: when every file is corrupt, retrieval degrades gracefully — empty result, no throw/abort.
        var corruptA = TempFileHelper.WriteStringToTempFile(Some.RandomString());
        var corruptB = TempFileHelper.WriteStringToTempFile(Some.RandomString());

        var datalist = new TrackingFileDataList();
        var result = await _parser.TryParseMany(new List<string> { corruptA, corruptB }, datalist, CancellationToken.None);

        result.ShouldBeFalse();
        datalist.ShouldBeEmpty();
    }

    [Fact]
    public async Task NonSerializationExceptionFromSerializer_IsReportedAsFailure_NotThrown()
    {
        // #292: the parser must be genuinely non-throwing. Previously it only caught SerializationException,
        // so a serializer that threw anything else (e.g. InvalidOperationException — exactly what
        // System.Text.Json throws when reflection serialization is disabled) propagated and crashed the run.
        var throwingSerialization = Substitute.For<ITrackingFileSerialization>();
        throwingSerialization
            .Deserialize(Arg.Any<string>())
            .Returns(_ => throw new System.InvalidOperationException("reflection serialization disabled"));
        var parser = new TrackingFileParser(throwingSerialization, Substitute.For<ILogger>());

        var file = TempFileHelper.WriteStringToTempFile(Some.RandomString());
        var datalist = new TrackingFileDataList();

        var result = await parser.TryParse(file, datalist, CancellationToken.None);

        result.ShouldBeFalse();
        datalist.ShouldBeEmpty();
    }
}