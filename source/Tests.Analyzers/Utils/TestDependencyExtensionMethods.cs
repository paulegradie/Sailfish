namespace Tests.Analyzers.Utils;

public static class TestDependencyExtensionMethods
{
    /// <summary>
    /// Normalizes line endings to the system's default (CRLF on Windows, LF on Unix).
    /// This is necessary for cross-platform Roslyn code fix tests.
    /// </summary>
    public static string NormalizeLineEndings(this string source)
    {
        // Replace all line endings with the system default
        return source.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
    }

    public static string AddSailfishAttributeDependencies(this string testSource)
    {
        return @"
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Sailfish.AnalyzerTests;

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
    private const int DefaultSampleSize = 3;
    private const int DefaultNumWarmupIterations = 3;

    internal SailfishAttribute()
    {
    }

    public SailfishAttribute(
        [Range(2, int.MaxValue)] int sampleSize = DefaultSampleSize,
        [Range(0, int.MaxValue)] int numWarmupIterations = DefaultNumWarmupIterations)
    {
        SampleSize = sampleSize;
        NumWarmupIterations = numWarmupIterations;
    }

    [Range(2, int.MaxValue)]
    public int SampleSize { get; set; }

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

" + testSource;
    }
}