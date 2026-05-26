namespace Sailfish.Attributes;

internal interface IInnerLifecycleAttribute
{
    public string[] MethodNames { get; }

    /// <summary>
    ///     When true, the lifecycle method is invoked at most once per executor run for the
    ///     declaring test class, even if it applies to multiple SailfishMethods.
    /// </summary>
    public bool RunOnce { get; }
}