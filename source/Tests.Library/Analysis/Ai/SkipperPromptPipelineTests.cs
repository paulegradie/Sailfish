using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis.Ai;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.Ai;

public class DefaultSkipperPromptBuilderTests
{
    [Fact]
    public void Preamble_StatesAuthority_Role_AndRepositoryRoot()
    {
        var prompt = new DefaultSkipperPromptBuilder(SkipperPromptTestHelpers.DefaultSections())
            .Build(SkipperPromptTestHelpers.Session(SkipperPromptTestHelpers.EmptyContext(), repoRoot: "/repo"));

        prompt.ShouldContain("You are Skipper");
        prompt.ShouldContain("AUTHORITATIVE");
        prompt.ShouldContain("role for this review is: Explain");
        prompt.ShouldContain("rooted at: /repo");
    }

    [Fact]
    public void Preamble_PrefersGrantedCodeReadCapabilityRoot_OverSessionRoot()
    {
        var session = SkipperPromptTestHelpers.Session(
            SkipperPromptTestHelpers.EmptyContext(),
            repoRoot: "/session-root",
            new CodeReadCapability("/granted-root"));

        var prompt = new DefaultSkipperPromptBuilder(SkipperPromptTestHelpers.DefaultSections()).Build(session);

        prompt.ShouldContain("rooted at: /granted-root");
        prompt.ShouldNotContain("/session-root");
    }

    [Fact]
    public void OutputContract_IsEmittedLast_AfterTheBody()
    {
        var context = new PerformanceNarrativeContext(
            new[] { SkipperPromptTestHelpers.Comparison("Bench.Method") },
            "## the result table",
            Environment: null);

        var prompt = new DefaultSkipperPromptBuilder(SkipperPromptTestHelpers.DefaultSections())
            .Build(SkipperPromptTestHelpers.Session(context));

        // The schema contract is the final instruction — after every body section.
        prompt.IndexOf("## Required output", StringComparison.Ordinal)
            .ShouldBeGreaterThan(prompt.IndexOf("## SailDiff comparisons", StringComparison.Ordinal));
        prompt.IndexOf("## Required output", StringComparison.Ordinal)
            .ShouldBeGreaterThan(prompt.IndexOf("## Sailfish result table", StringComparison.Ordinal));
    }

    [Fact]
    public void Sections_AreEmittedInAscendingOrder_RegardlessOfRegistrationOrder()
    {
        var sections = new ISkipperPromptSection[]
        {
            new MarkerSection(Order: 400, Marker: "ZZZ-LATE"),
            new MarkerSection(Order: 100, Marker: "AAA-EARLY")
        };

        var prompt = new DefaultSkipperPromptBuilder(sections)
            .Build(SkipperPromptTestHelpers.Session(SkipperPromptTestHelpers.EmptyContext()));

        prompt.IndexOf("AAA-EARLY", StringComparison.Ordinal)
            .ShouldBeLessThan(prompt.IndexOf("ZZZ-LATE", StringComparison.Ordinal));
    }

    [Fact]
    public void ConsumerSection_ComposesBetweenTheBookends()
    {
        var sections = new List<ISkipperPromptSection>(SkipperPromptTestHelpers.DefaultSections())
        {
            new MarkerSection(Order: SkipperPromptOrder.Comparisons - 1, Marker: "CONSUMER-CONTEXT")
        };

        var prompt = new DefaultSkipperPromptBuilder(sections)
            .Build(SkipperPromptTestHelpers.Session(SkipperPromptTestHelpers.EmptyContext()));

        // After the framework preamble, before the framework output contract.
        prompt.IndexOf("CONSUMER-CONTEXT", StringComparison.Ordinal)
            .ShouldBeGreaterThan(prompt.IndexOf("You are Skipper", StringComparison.Ordinal));
        prompt.IndexOf("CONSUMER-CONTEXT", StringComparison.Ordinal)
            .ShouldBeLessThan(prompt.IndexOf("## Required output", StringComparison.Ordinal));
    }

    [Fact]
    public void Comparison_RendersDisplayNameVerdictAndMeanChange()
    {
        var context = new PerformanceNarrativeContext(
            new[] { SkipperPromptTestHelpers.Comparison("Bench.ParseHeaders", percentChangeMean: 18.0) },
            SailDiffMarkdown: string.Empty,
            Environment: null);

        var prompt = new DefaultSkipperPromptBuilder(SkipperPromptTestHelpers.DefaultSections())
            .Build(SkipperPromptTestHelpers.Session(context));

        prompt.ShouldContain("## SailDiff comparisons (authoritative)");
        prompt.ShouldContain("Bench.ParseHeaders");
        prompt.ShouldContain("+18% mean change");
    }

    private sealed class MarkerSection : ISkipperPromptSection
    {
        private readonly string marker;

        public MarkerSection(int Order, string Marker)
        {
            this.Order = Order;
            marker = Marker;
        }

        public int Order { get; }

        public void Contribute(StringBuilder prompt, SkipperSession session) => prompt.AppendLine(marker);
    }
}

public class DefaultSkipperResponseParserTests
{
    private readonly DefaultSkipperResponseParser parser = new();

    [Fact]
    public void Parse_FullSchema_PopulatesEveryField()
    {
        var review = parser.Parse(SkipperPromptTestHelpers.CanonicalReviewJson);

        review.OverallVerdict.ShouldBe(SkipperVerdict.Regressed);
        review.ConsoleSummary.ShouldBe("ParseHeaders is 18% slower than baseline.");
        review.MarkdownReport.ShouldContain("Parser.cs:88");

        var finding = review.Findings.ShouldHaveSingleItem();
        finding.TestCaseDisplayName.ShouldBe("Bench.ParseHeaders");
        finding.Verdict.ShouldBe(SkipperVerdict.Regressed);
        finding.Summary.ShouldBe("regex compiled inside the per-row loop");
        finding.CitedSourceLocations.ShouldBe(new[] { "Parser.cs:88" });
        finding.Confidence.ShouldBe(0.91, 1e-9);
    }

    [Fact]
    public void Parse_ExtractsJsonObject_WrappedInProseAndFences()
    {
        var wrapped = "Sure, here is the analysis:\n```json\n" + SkipperPromptTestHelpers.CanonicalReviewJson + "\n```\nHope that helps!";

        var review = parser.Parse(wrapped);

        review.OverallVerdict.ShouldBe(SkipperVerdict.Regressed);
        review.Findings.ShouldHaveSingleItem();
    }

    [Fact]
    public void Parse_NonJson_DegradesToRawText_RatherThanThrowing()
    {
        var review = parser.Parse("the model just rambled without any json");

        review.OverallVerdict.ShouldBe(SkipperVerdict.Inconclusive);
        review.ConsoleSummary.ShouldBe("the model just rambled without any json");
        review.HasContent.ShouldBeTrue();
    }

    [Fact]
    public void Parse_EmptyOrWhitespace_IsEmptyReview()
    {
        parser.Parse(string.Empty).HasContent.ShouldBeFalse();
        parser.Parse("   ").HasContent.ShouldBeFalse();
    }
}

/// <summary>
///     Locks the prompt's output-schema contract to the parser that reads it — the two halves of one serialization
///     contract. If a field name drifts on either side, one of these fails.
/// </summary>
public class SkipperOutputContractTests
{
    private static readonly string[] ContractFieldNames =
    {
        "overallVerdict", "consoleSummary", "markdownReport", "findings",
        "testCaseDisplayName", "verdict", "summary", "citedSourceLocations", "confidence"
    };

    [Fact]
    public void Prompt_AdvertisesEveryFieldTheParserReads()
    {
        var prompt = new DefaultSkipperPromptBuilder(SkipperPromptTestHelpers.DefaultSections())
            .Build(SkipperPromptTestHelpers.Session(SkipperPromptTestHelpers.EmptyContext()));

        foreach (var field in ContractFieldNames)
        {
            // Every field the parser deserializes must be advertised by the prompt's output contract.
            prompt.ShouldContain(field);
        }
    }

    [Fact]
    public void Parser_UnderstandsAFullInstanceOfTheAdvertisedSchema()
    {
        // The canonical JSON uses exactly the field names the contract advertises; the parser must fully populate.
        var review = new DefaultSkipperResponseParser().Parse(SkipperPromptTestHelpers.CanonicalReviewJson);

        review.HasContent.ShouldBeTrue();
        review.Findings.ShouldHaveSingleItem();
    }
}

public class PromptDrivenSailfishAgentTests
{
    private readonly ILogger logger = Substitute.For<ILogger>();

    [Fact]
    public async Task NoOpTransport_ShortCircuitsToEmpty_WithoutBuildingThePrompt()
    {
        var promptBuilder = Substitute.For<ISkipperPromptBuilder>();
        var parser = Substitute.For<ISkipperResponseParser>();
        var agent = new PromptDrivenSailfishAgent(promptBuilder, new NoOpSkipperTransport(), parser, logger);

        var review = await agent.RunAsync(SkipperPromptTestHelpers.Session(SkipperPromptTestHelpers.EmptyContext()), CancellationToken.None);

        review.HasContent.ShouldBeFalse();
        promptBuilder.DidNotReceive().Build(Arg.Any<SkipperSession>());
    }

    [Fact]
    public async Task RealPipeline_BuildsFrameworkPrompt_AndParsesTransportReply()
    {
        var transport = new StubTransport(SkipperPromptTestHelpers.CanonicalReviewJson);
        var agent = new PromptDrivenSailfishAgent(
            new DefaultSkipperPromptBuilder(SkipperPromptTestHelpers.DefaultSections()),
            transport,
            new DefaultSkipperResponseParser(),
            logger);

        var review = await agent.RunAsync(SkipperPromptTestHelpers.Session(SkipperPromptTestHelpers.EmptyContext()), CancellationToken.None);

        review.OverallVerdict.ShouldBe(SkipperVerdict.Regressed);
        review.Findings.ShouldHaveSingleItem();
        // The agent built the framework's grounded prompt (preamble + output contract) and handed it to transport.
        transport.LastPrompt.ShouldContain("You are Skipper");
        transport.LastPrompt.ShouldContain("## Required output");
    }

    [Fact]
    public async Task TransportThrows_DegradesToEmpty_AndNeverThrows()
    {
        var agent = new PromptDrivenSailfishAgent(
            new DefaultSkipperPromptBuilder(SkipperPromptTestHelpers.DefaultSections()),
            new ThrowingTransport(),
            new DefaultSkipperResponseParser(),
            logger);

        var review = await agent.RunAsync(SkipperPromptTestHelpers.Session(SkipperPromptTestHelpers.EmptyContext()), CancellationToken.None);

        review.HasContent.ShouldBeFalse();
    }

    private sealed class StubTransport : ISkipperTransport
    {
        private readonly string response;

        public StubTransport(string response) => this.response = response;

        public string? LastPrompt { get; private set; }

        public Task<string> CompleteAsync(string prompt, SkipperSession session, CancellationToken cancellationToken)
        {
            LastPrompt = prompt;
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingTransport : ISkipperTransport
    {
        public Task<string> CompleteAsync(string prompt, SkipperSession session, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("claude CLI offline");
    }
}

internal static class SkipperPromptTestHelpers
{
    /// <summary>A reply that conforms to the output-schema contract, using exactly the advertised field names.</summary>
    public const string CanonicalReviewJson =
        """
        {
          "overallVerdict": "Regressed",
          "consoleSummary": "ParseHeaders is 18% slower than baseline.",
          "markdownReport": "## Why\nThe regex is compiled inside the per-row loop. See Parser.cs:88",
          "findings": [
            {
              "testCaseDisplayName": "Bench.ParseHeaders",
              "verdict": "Regressed",
              "summary": "regex compiled inside the per-row loop",
              "citedSourceLocations": ["Parser.cs:88"],
              "confidence": 0.91
            }
          ]
        }
        """;

    public static IEnumerable<ISkipperPromptSection> DefaultSections() => new ISkipperPromptSection[]
    {
        new ComparisonsPromptSection(),
        new ScalingPromptSection(),
        new EnvironmentPromptSection(),
        new ResultTablePromptSection()
    };

    public static PerformanceNarrativeContext EmptyContext() =>
        new(Array.Empty<SailDiffCaseContext>(), string.Empty, Environment: null);

    public static SkipperSession Session(
        PerformanceNarrativeContext context,
        string repoRoot = "/repo",
        params ISkipperCapability[] capabilities) =>
        new(SkipperRole.Explain, context, new CapabilityRegistry(capabilities), repoRoot);

    public static SailDiffCaseContext Comparison(string displayName, double percentChangeMean = 0.0) =>
        new(
            displayName,
            SkipperVerdict.Regressed,
            MeanBefore: 100,
            MeanAfter: 118,
            MedianBefore: 100,
            MedianAfter: 118,
            PercentChangeMean: percentChangeMean,
            PValue: 0.001,
            AdjustedPValue: null,
            ChangeDescription: "slower",
            SampleSizeBefore: 10,
            SampleSizeAfter: 10,
            Failed: false);
}
