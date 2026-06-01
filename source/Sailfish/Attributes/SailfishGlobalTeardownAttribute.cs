using System;

namespace Sailfish.Attributes;

/// <summary>
///     Specifies that the attributed method runs <b>once per test class</b>, after all test cases have executed.
/// </summary>
/// <remarks>
///     <para>
///         This attribute should be placed on a single method. Only one method is allowed per Sailfish test class.
///     </para>
///     <para>
///         Because this hook runs only once, a <see cref="SailfishVariableAttribute" /> read here resolves to a single
///         value rather than varying per test case. Per-case teardown belongs in
///         <see cref="SailfishMethodTeardownAttribute" />. The <c>SF1016</c> analyzer flags variable reads in this hook.
///     </para>
/// </remarks>
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">
///     Sailfish Lifecycle Method
///     Attributes
/// </seealso>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SailfishGlobalTeardownAttribute : Attribute;