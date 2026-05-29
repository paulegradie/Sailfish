using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies that the new ScaleFish fields (AICc, Akaike weight, PowerLog, Bootstrap, SuggestedNextN)
/// survive a JSON serialize → deserialize cycle through <see cref="ComplexityFunctionConverter"/>.
/// Also confirms that older model files (without these fields) still load with sensible defaults.
/// </summary>
public class ScaleFishJsonRoundTripTests
{
    private static JsonSerializerOptions BuildOptions()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new ComplexityFunctionConverter());
        return opts;
    }

    [Fact]
    public void NewFields_RoundTripWithFidelity()
    {
        var best = new Linear();
        best.FunctionParameters = new FittedCurve(scale: 3.14, bias: 1.59);

        var next = new NLogN();
        next.FunctionParameters = new FittedCurve(scale: 0.5, bias: 2.0);

        var model = new ScaleFishModel(
            scaleFishModelFunction: best,
            goodnessOfFit: 0.997,
            nextClosestScaleFishModelFunction: next,
            nextClosestGoodnessOfFit: 0.953,
            bestAicc: -42.5,
            nextBestAicc: -18.2,
            akaikeWeight: 0.999,
            isDistinguishable: true,
            sampleSize: 6,
            powerLog: new PowerLogResult(2.1, 1.03, 0.02, 0.5, 0.99))
        {
            Bootstrap = new BootstrapDiagnostic(
                iterations: 200,
                selectionAgreement: 0.95,
                scaleCiLower: 2.8,
                scaleCiUpper: 3.5,
                biasCiLower: 1.2,
                biasCiUpper: 1.9),
            SuggestedNextN = 1024
        };

        var classes = new List<ScalefishClassModel>
        {
            new("TestNs", "TestClass", new List<ScaleFishMethodModel>
            {
                new("TestMethod", new List<ScaleFishPropertyModel>
                {
                    new("TestNs.TestMethod.N", model)
                })
            })
        };

        var json = JsonSerializer.Serialize(classes, BuildOptions());
        var roundTrip = JsonSerializer.Deserialize<List<ScalefishClassModel>>(json, BuildOptions());

        roundTrip.ShouldNotBeNull();
        var loaded = roundTrip[0].ScaleFishMethodModels.First().ScaleFishPropertyModels.First().ScaleFishModel;

        loaded.ScaleFishModelFunction.Name.ShouldBe(nameof(Linear));
        loaded.NextClosestScaleFishModelFunction.Name.ShouldBe(nameof(NLogN));
        loaded.GoodnessOfFit.ShouldBe(0.997, tolerance: 1e-9);
        loaded.NextClosestGoodnessOfFit.ShouldBe(0.953, tolerance: 1e-9);

        loaded.BestAicc.ShouldBe(-42.5, tolerance: 1e-9);
        loaded.NextBestAicc.ShouldBe(-18.2, tolerance: 1e-9);
        loaded.AkaikeWeight.ShouldBe(0.999, tolerance: 1e-9);
        loaded.IsDistinguishable.ShouldBeTrue();
        loaded.SampleSize.ShouldBe(6);

        loaded.PowerLog.ShouldNotBeNull();
        loaded.PowerLog.A.ShouldBe(2.1, tolerance: 1e-9);
        loaded.PowerLog.B.ShouldBe(1.03, tolerance: 1e-9);
        loaded.PowerLog.C.ShouldBe(0.02, tolerance: 1e-9);

        loaded.Bootstrap.ShouldNotBeNull();
        loaded.Bootstrap.Iterations.ShouldBe(200);
        loaded.Bootstrap.SelectionAgreement.ShouldBe(0.95, tolerance: 1e-9);
        loaded.Bootstrap.ScaleCiLower.ShouldBe(2.8, tolerance: 1e-9);
        loaded.Bootstrap.ScaleCiUpper.ShouldBe(3.5, tolerance: 1e-9);

        loaded.SuggestedNextN.ShouldBe(1024);
    }

    [Fact]
    public void OlderModelFile_LoadsWithDefaultsForNewFields()
    {
        // Pre-phase-2 JSON: only the original four fields are present.
        const string olderJson = @"[{
            ""NameSpace"": ""LegacyNs"",
            ""TestClassName"": ""LegacyClass"",
            ""ScaleFishMethodModels"": [{
                ""TestMethodName"": ""LegacyMethod"",
                ""ScaleFishPropertyModels"": [{
                    ""PropertyName"": ""LegacyNs.LegacyMethod.N"",
                    ""ScaleFishModel"": {
                        ""ScaleFishModelFunction"": {
                            ""Name"": ""Linear"",
                            ""OName"": ""O(n)"",
                            ""Quality"": ""Good"",
                            ""FunctionDef"": ""f(x) = {0}x + {1}"",
                            ""FunctionParameters"": { ""Scale"": 1.0, ""Bias"": 0.0 }
                        },
                        ""GoodnessOfFit"": 0.99,
                        ""NextClosestScaleFishModelFunction"": {
                            ""Name"": ""NLogN"",
                            ""OName"": ""O(nLog(n))"",
                            ""Quality"": ""Good"",
                            ""FunctionDef"": ""f(x) = {0}xLog_e(x) + {1}"",
                            ""FunctionParameters"": { ""Scale"": 0.7, ""Bias"": 0.5 }
                        },
                        ""NextClosestGoodnessOfFit"": 0.95
                    }
                }]
            }]
        }]";

        var roundTrip = JsonSerializer.Deserialize<List<ScalefishClassModel>>(olderJson, BuildOptions());
        roundTrip.ShouldNotBeNull();
        var loaded = roundTrip[0].ScaleFishMethodModels.First().ScaleFishPropertyModels.First().ScaleFishModel;

        // Original fields populated as expected.
        loaded.ScaleFishModelFunction.Name.ShouldBe(nameof(Linear));
        loaded.GoodnessOfFit.ShouldBe(0.99, tolerance: 1e-9);

        // New fields default — NaN for the AICc-family numerics, false/0/null for the rest.
        double.IsNaN(loaded.BestAicc).ShouldBeTrue();
        double.IsNaN(loaded.NextBestAicc).ShouldBeTrue();
        double.IsNaN(loaded.AkaikeWeight).ShouldBeTrue();
        loaded.IsDistinguishable.ShouldBeFalse();
        loaded.SampleSize.ShouldBe(0);
        loaded.PowerLog.ShouldBeNull();
        loaded.Bootstrap.ShouldBeNull();
        loaded.SuggestedNextN.ShouldBeNull();
    }
}
