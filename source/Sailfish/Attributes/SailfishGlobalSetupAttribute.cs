using System;

namespace Sailfish.Attributes;


/// <summary>
/// Attribute to be placed on a single method responsible for Global Setup
/// See: <a href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">Sailfish Lifecycle Method Attributes</a>
/// </summary>
/// <param name="numIterations">Number of times each SailfishMethod will be iterated</param>
/// <param name="numWarmupIterations">Number of times each SailfishMethod will be iterated without being timed before executing numIterations with tracking</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class SailfishGlobalSetupAttribute : Attribute
{
}