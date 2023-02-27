using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sailfish.Utils;

internal static class Cloner
{
    public static void ApplyPropertiesAndFieldsTo(this PropertiesAndFields propertiesAndFields, object destination)
    {
        foreach (var property in propertiesAndFields.Properties)
        {
            property.Key.SetValue(destination, property.Value);
        }

        foreach (var field in propertiesAndFields.Fields)
        {
            field.Key.SetValue(destination, field.Value);
        }
    }

    public static PropertiesAndFields RetrievePropertiesAndFields(this object source)
    {
        var properties = new Dictionary<PropertyInfo, dynamic?>();
        var fields = new Dictionary<FieldInfo, dynamic?>();

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source), "Source object is null");
        }

        var typeSrc = source.GetType();

        foreach (var srcProp in typeSrc.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!srcProp.CanRead) continue;

            var property = typeSrc.GetProperty(srcProp.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null || !property.CanWrite || property.GetSetMethod(true)?.IsPrivate == true
                || (property.GetSetMethod()?.Attributes & MethodAttributes.Static) != 0
                || !property.PropertyType.IsAssignableFrom(srcProp.PropertyType))
            {
                continue;
            }

            properties.Add(property, srcProp.GetValue(source));
        }

        foreach (var srcField in typeSrc.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var field = typeSrc.GetField(srcField.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null || field.IsInitOnly || !field.FieldType.IsAssignableFrom(srcField.FieldType))
            {
                continue;
            }

            fields.Add(field, srcField.GetValue(source));
        }

        return new PropertiesAndFields(properties, fields);
    }
}