using System;

namespace Sailfish.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SailfishMethodTeardownAttribute : Attribute
{
}