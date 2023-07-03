using Microsoft.CodeAnalysis.Testing;
using Sailfish.Analyzers.DiagnosticAnalyzers.SailfishVariable;
using Sailfish.Analyzers.Utils;
using Tests.Sailfish.Analyzers.Utils;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Sailfish.Analyzers.DiagnosticAnalyzers.SailfishVariable.ShouldBePublicAnalyzer>;

namespace Tests.Sailfish.Analyzers.SailfishVariables;

public class SailfishVariablePropertiesShouldBePublic
{
    [Fact]
    public async Task NoError()
    {
        const string source = @"
[Sailfish]
public class TestClass
{
    [SailfishVariable(1, 2, 3)] public int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await Verify.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task PrivateModifierInNormalClassDoesNotThrowError()
    {
        const string source = @"
public class TestClass
{
    [SailfishVariable(1, 2, 3)] private int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await Verify.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NonSailfishTestWithVariableAttributeShouldNotError()
    {
        const string source = @"
public class TestClass
{
    [SailfishVariable(1, 2, 3)] 
    private int Placeholder { get; set; }
}";

        await Verify.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NonSailfishTestClassesDoNotCauseWarnings()
    {
        const string source = @"
public class NonSailfishTestClassesDoNotCauseWarnings
{
    int Placeholder { get; set; }
    int OtherPlaceholder { get; set; }

    public void MainMethod()
    {
        // do nothing
    }
}";
        await Verify.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task PrivatePropertiesDoNotError()
    {
        const string source = @"
[Sailfish]
public class TestClass
{
    [SailfishVariable(200)] public int TestProperty { get; set; }

    int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        // do nothing
    }
}";
        await Verify.VerifyAnalyzerAsync(source.AddSailfishAttributeDependencies());
    }

    [Fact]
    public async Task NonPublicVariablePropertiesShouldBeDiscoveredOnSailfishTestBaseClasses()
    {
        const string source = @"
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Two;
using Sailfish.AnalyzerTests;
using System.Threading.Tasks;
using System.Threading;

namespace Two
{
    public class BaseAnalyzerClass
    {
        [SailfishVariable(1, 2, 3)] 
        private int {|#1:MyVar|} { get; set; }

        public int BaseValue { get; set; }

        [SailfishGlobalSetup]
        public async Task GlobalSetupBaseAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            BaseValue = 2;
        }
    }
}

namespace Sailfish.AnalyzerTests
{
    [Sailfish]
    public class AnalyzerExample : BaseAnalyzerClass
    {
        [SailfishVariable(1, 2, 3)] int {|#0:Placeholder|} { get; set; }

        [SailfishMethod]
        public void MainMethod()
        {
            Console.WriteLine(Placeholder);
        }
    }

    public class SailfishException : Exception
    {
        public SailfishException(string? message = null) : base(message)
        {
        }

        public SailfishException(Exception ex) : base(ex.Message)
        {
        }
    }

    internal interface IInnerLifecycleAttribute
    {
        public string[] MethodNames { get; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class SailfishAttribute : Attribute
    {
        private const int DefaultNumIterations = 3;
        private const int DefaultNumWarmupIterations = 3;

        internal SailfishAttribute()
        {
        }

        public SailfishAttribute(
            [Range(2, int.MaxValue)] int numIterations = DefaultNumIterations,
            [Range(0, int.MaxValue)] int numWarmupIterations = DefaultNumWarmupIterations)
        {
            NumIterations = numIterations;
            NumWarmupIterations = numWarmupIterations;
        }

        [Range(2, int.MaxValue)]
        public int NumIterations { get; set; }

        [Range(0, int.MaxValue)]
        public int NumWarmupIterations { get; set; }
        public bool Disabled { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class SailfishGlobalSetupAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SailfishGlobalTeardownAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SailfishIterationSetupAttribute : Attribute, IInnerLifecycleAttribute
    {
        public SailfishIterationSetupAttribute(params string[] methodNames)
        {
            MethodNames = methodNames;
        }
        public string[] MethodNames { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SailfishIterationTeardownAttribute : Attribute, IInnerLifecycleAttribute
    {
        public SailfishIterationTeardownAttribute(params string[] methodNames)
        {
            MethodNames = methodNames;
        }

        public string[] MethodNames { get; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class SailfishMethodAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SailfishMethodSetupAttribute : Attribute, IInnerLifecycleAttribute
    {
        public SailfishMethodSetupAttribute(params string[] methodNames)
        {
            MethodNames = methodNames;
        }

        public string[] MethodNames { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SailfishMethodTeardownAttribute : Attribute, IInnerLifecycleAttribute
    {
        public SailfishMethodTeardownAttribute(params string[] methodNames)
        {
            MethodNames = methodNames;
        }

        public string[] MethodNames { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SailfishVariableAttribute : Attribute
    {
        public SailfishVariableAttribute([MinLength(1)] params object[] n)
        {
            if (n.Length == 0)
            {
                throw new SailfishException();
            }

            N.AddRange(n);
        }

        public List<object> N { get; } = new();

        public IEnumerable<object> GetVariables()
        {
            return N.ToArray();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SuppressConsoleAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WriteToCsvAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WriteToMarkdownAttribute : Attribute
    {
    }
}
";
        await Verify.VerifyAnalyzerAsync(
            source,
            new DiagnosticResult(ShouldBePublicAnalyzer.Descriptor).WithLocation(0),
            new DiagnosticResult(ShouldBePublicAnalyzer.Descriptor).WithLocation(1));
    }
}