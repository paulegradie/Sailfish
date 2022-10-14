using System;

namespace Sailfish.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SailfishGlobalTeardownAttribute : Attribute
{
}