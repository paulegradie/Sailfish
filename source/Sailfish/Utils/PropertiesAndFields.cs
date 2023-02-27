using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Utils;

internal class PropertiesAndFields
{
    public PropertiesAndFields(Dictionary<PropertyInfo, object?> properties, Dictionary<FieldInfo, object?> fields)
    {
        Properties = properties;
        Fields = fields;
    }

    public Dictionary<PropertyInfo, object?> Properties { get; set; }
    public Dictionary<FieldInfo, object?> Fields { get; set; }
}