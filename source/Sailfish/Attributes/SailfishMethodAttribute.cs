using System;

namespace Sailfish.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class SailfishMethodAttribute : Attribute
    {
    }
}