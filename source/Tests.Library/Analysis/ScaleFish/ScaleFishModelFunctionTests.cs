using System;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Exceptions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

public class ScaleFishModelFunctionTests
{
    [Fact]
    public void PredictThrowsWhenFunctionParametersNotSet()
    {
        Should.Throw<SailfishModelException>(() => new TestFunction().Predict(10));
    }

    private class TestFunction : ScaleFishModelFunction
    {
        public override string Name { get; set; } = "TestFunction";
        public override string OName { get; set; } = "O(Test)";
        public override string Quality { get; set; } = "Great";
        public override string FunctionDef { get; set; } = "TestFunctionDef";

        public override double Compute(double bias, double scale, double x)
        {
            throw new NotImplementedException();
        }
    }
}