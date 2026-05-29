using System.Collections.Generic;

namespace Sailfish.Analysis.ScaleFish;

/// <summary>
/// Returns the catalog of complexity families considered by the estimator. As of phase-2 hardening this
/// delegates to <see cref="ComplexityFunctionRegistry"/>, which built-in families auto-populate and user
/// code can extend via <see cref="ComplexityFunctionRegistry.Register{T}"/>.
/// </summary>
public static class ComplexityReferences
{
    /// <summary>
    /// Fresh instances of every registered family. Each call returns a new set so the per-family
    /// <see cref="ScaleFishModelFunction.FunctionParameters"/> can be mutated by the fit without sharing
    /// state across runs or threads.
    /// </summary>
    public static IEnumerable<ScaleFishModelFunction> GetComplexityFunctions()
    {
        return ComplexityFunctionRegistry.CreateFitInstances();
    }
}
