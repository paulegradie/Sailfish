using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = Constants.Disabled)]
public class NonSystemTypeVariablesFromMethodTest
{
    [SailfishVariable(typeof(NonSystemTypeVariableProvider))]
    public MyNonSystemType MyNonSystemType { get; set; }

    public static AsyncLocal<List<MyNonSystemType>> CaptureStringVariablesForTestingThisTest = new();

    [SailfishMethod]
    public void Run()
    {
        if (CaptureStringVariablesForTestingThisTest.Value is not null)
        {
            CaptureStringVariablesForTestingThisTest.Value.Add(MyNonSystemType);
        }
    }
}

public class MyNonSystemType(string myString, int myInt) : IComparable
{
    public string MyString { get; } = myString;
    public int MyInt { get; } = myInt;

    /// <summary>
    /// Sailfish uses this to define the order the variables are tested in.
    /// By setting this to 0, the order defined in the method that supplies the variables
    /// will define the order in which the tests are run.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public int CompareTo(object? obj) => 0;

    /// <summary>
    /// The ToString value is what is displayed in the IDE UI and other places in Sailfish.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"MyString: {MyString}, MyInt: {MyInt}";
    }

    /// <summary>
    /// Overriding Equals is required so sailfish can ensure we have distinct test runs.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((MyNonSystemType)obj);
    }
    
    protected bool Equals(MyNonSystemType other) => MyString == other.MyString && MyInt == other.MyInt;

    public override int GetHashCode()
    {
        return HashCode.Combine(MyString, MyInt);
    }
}

public class NonSystemTypeVariableProvider : ISailfishVariablesProvider<MyNonSystemType>
{
    public IEnumerable<MyNonSystemType> Variables()
    {
        // Sailfish will execute tests in the order below since the CompareTo method above always
        // returns 0
        return
        [
            new MyNonSystemType("Foo", 1337),
            new MyNonSystemType("Bar", 42)
        ];
    }
}