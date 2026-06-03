namespace Sailfish.Attributes;

/// <summary>
///     Controls how the test-class instance is managed across the cases of a single class
///     (a "case" = one <see cref="SailfishMethodAttribute" /> × one variable combination).
/// </summary>
public enum SailfishLifetime
{
    /// <summary>
    ///     One instance per class (default). The constructor and <see cref="SailfishGlobalSetupAttribute" /> run
    ///     <b>once</b>; every case runs on that same instance (variables re-injected and
    ///     <see cref="SailfishMethodSetupAttribute" /> re-run per case); <see cref="SailfishGlobalTeardownAttribute" />
    ///     runs once at the end. Use this for expensive shared setup (seed a database, prewarm a cache) that should
    ///     happen once for the whole class. Cases are not isolated from each other — reset per-case scratch state in
    ///     a method-level setup hook.
    /// </summary>
    /// <remarks>
    ///     <b>Migration note:</b> this is a behavioral change from the previous model, which created a fresh instance
    ///     per case. Mutable instance fields now accumulate across cases; reset them in
    ///     <see cref="SailfishMethodSetupAttribute" /> (or choose <see cref="PerCase" />) if you need a clean slate
    ///     per case. Also, if a lifecycle hook throws, the class aborts and
    ///     <see cref="SailfishGlobalTeardownAttribute" /> does not run — though the instance is still disposed if it
    ///     implements <see cref="System.IDisposable" />/<see cref="System.IAsyncDisposable" />.
    /// </remarks>
    SharedInstance = 0,

    /// <summary>
    ///     A fresh instance per case, resolved from its own DI scope. <see cref="SailfishGlobalSetupAttribute" /> and
    ///     <see cref="SailfishGlobalTeardownAttribute" /> run per case. Singletons (and
    ///     <c>ISailfishFixture&lt;T&gt;</c>) are still shared via the container; scoped/transient dependencies are
    ///     fresh per case. Use this for strict isolation between cases. Put genuinely-once expensive setup in a
    ///     fixture/singleton rather than in GlobalSetup.
    /// </summary>
    PerCase = 1
}
