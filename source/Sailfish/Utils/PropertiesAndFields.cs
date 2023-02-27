using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Utils;

internal class PropertiesAndFields
{
    public PropertiesAndFields(Dictionary<PropertyInfo, dynamic?> properties, Dictionary<FieldInfo, dynamic?> fields)
    {
        Properties = properties;
        Fields = fields;
    }

    public Dictionary<PropertyInfo, dynamic?> Properties { get; set; }
    public Dictionary<FieldInfo, dynamic?> Fields { get; set; }
}