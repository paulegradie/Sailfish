using System;

namespace Sailfish.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class SailfishGlobalSetupAttribute : Attribute
    {
    }
}