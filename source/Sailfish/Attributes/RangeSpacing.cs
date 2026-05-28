namespace Sailfish.Attributes;

/// <summary>
/// Specifies how values are distributed across the [start, end] range of a <see cref="SailfishRangeVariableAttribute"/>.
/// </summary>
public enum RangeSpacing
{
    /// <summary>Equally spaced values in the linear domain (start, start+step, start+2·step, ...).</summary>
    Linear = 0,

    /// <summary>
    /// Geometrically spaced values: each successive point is multiplied by a fixed ratio so the values
    /// are equally spaced in the log domain. Recommended for ScaleFish complexity probes — gives far more
    /// discrimination between complexity classes per data point.
    /// </summary>
    Geometric = 1
}
