using System.Text;
using Sailfish.Attributes;

namespace Tests.Sailfish.TestAdapter.TestResources;

[Sailfish(Disabled = true)]
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
            ExampleComponent.InterpolateStrings(A, B);
        }
    }

    [SailfishMethod]
    public void Concatenate()
    {
        for (var i = 0; i < N; i++)
        {
            ExampleComponent.ConcatenateStrings(A, B);
        }
    }

    [SailfishMethod]
    public void StringBuilder()
    {
        for (var i = 0; i < N; i++)
        {
            ExampleComponent.StringBuilder(A, B);
        }
    }
}

public class ExampleComponent
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