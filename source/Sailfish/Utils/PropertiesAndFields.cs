using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Utils;

internal class PropertiesAndFields(Dictionary<PropertyInfo, object?> properties, Dictionary<FieldInfo, object?> fields)
{
    public Dictionary<PropertyInfo, object?> Properties { get; set; } = properties;
    public Dictionary<FieldInfo, object?> Fields { get; set; } = fields;
}