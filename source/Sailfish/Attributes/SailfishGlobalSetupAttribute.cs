using System;

namespace Sailfish.Attributes;

/// <summary>
///     Specifies that the attributed method runs <b>once per test class</b>, before any test case executes. Use it for
///     expensive setup that is shared, unchanged, across every variable set (opening a connection, loading a fixture).
/// </summary>
/// <remarks>
///     <para>
///         This attribute should be placed on a single method. Only one method is allowed per Sailfish test class.
///     </para>
///     <para>
///         <b>Do not build <see cref="SailfishVariableAttribute" />- or <see cref="SailfishRangeVariableAttribute" />-dependent
///         state here.</b> GlobalSetup runs only for the first variable set, and the field/property state it produces is
///         captured and replayed onto every subsequent test-case instance — while the variable property itself is
///         re-injected per case. Any field derived from a variable inside GlobalSetup is therefore silently frozen at its
///         first value, so the benchmark measures a single input size for all cases (and ScaleFish reports ~O(1)). Build
///         variable-dependent state in <see cref="SailfishMethodSetupAttribute" />, which runs once per variable set after
///         the replay. The <c>SF1016</c> analyzer flags this mistake.
///     </para>
/// </remarks>
/// <seealso href="https://paulgradie.com/Sailfish/docs/2/sailfish-lifecycle-method-attributes">
///     Sailfish Lifecycle Method
///     Attributes
/// </seealso>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class SailfishGlobalSetupAttribute : Attribute;