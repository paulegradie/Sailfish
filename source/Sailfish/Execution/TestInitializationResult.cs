using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal class TestInitializationResult
{
    private TestInitializationResult(bool isValid, Type[] tests, Dictionary<string, List<string>> errors)
    {
        IsValid = isValid;
        Errors = errors;
        Tests = tests;
    }

    public Type[] Tests { get; set; }
    public bool IsValid { get; set; }
    public Dictionary<string, List<string>> Errors { get; }

    public static TestInitializationResult CreateSuccess(Type[] tests)
    {
        return new TestInitializationResult(true, tests, new Dictionary<string, List<string>>());
    }

    public static TestInitializationResult CreateFailure(Type[] tests, Dictionary<string, List<string>> errors)
    {
        return new TestInitializationResult(false, tests, errors);
    }
}