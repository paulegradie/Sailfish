using System.Text;
using Sailfish.Attributes;

namespace Tests.Sailfish.TestAdapter.TestResources;

[Sailfish(NumIterations = 6)]
public class ExampleComponentTest
{
    public const string A = "Wow";
    public const string B = "SoFast";

    [SailfishVariable(1, 10, 100)]
    public int N { get; set; }

    [SailfishMethod]
    public void Interpolate()
    {
        for (var i = 0; i < N; i++)
        {
            ExampleOctopusComponent.InterpolateStrings(A, B);
        }
    }

    [SailfishMethod]
    public void Concatenate()
    {
        for (var i = 0; i < N; i++)
        {
            ExampleOctopusComponent.ConcatenateStrings(A, B);
        }
    }

    [SailfishMethod]
    public void StringBuilder()
    {
        for (var i = 0; i < N; i++)
        {
            ExampleOctopusComponent.StringBuilder(A, B);
        }
    }
}

public class ExampleOctopusComponent
{
    public static string InterpolateStrings(string a, string b)
    {
        return $"{a}-{b}";
    }

    public static string ConcatenateStrings(string a, string b)
    {
        return a + "-" + b;
    }

    public static string StringBuilder(string a, string b)
    {
        var builder = new StringBuilder();
        return builder.Append(a).Append('-').Append(b).ToString();
    }
}