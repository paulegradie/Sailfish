using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;

namespace Sailfish.Utils;

internal static class TestCasePropertyCloner
{
    public static void ApplyPropertiesAndFieldsTo(this PropertiesAndFields propertiesAndFields, object destination)
    {
        foreach (var (key, value) in propertiesAndFields.Properties)
        {
            key.SetValue(destination, value);
        }

        foreach (var (key, value) in propertiesAndFields.Fields)
        {
            key.SetValue(destination, value);
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

        var customProperties = typeSrc
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(x => !x.GetCustomAttributes<SailfishVariableAttribute>().Any())
            .ToList();

        foreach (var property in customProperties)
        {
            if ((property.GetSetMethod()?.Attributes & MethodAttributes.Static) != 0 || !property.PropertyType.IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            properties.Add(property, property.GetValue(source));
        }

        var customFields = typeSrc
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field => !field.Name.EndsWith("BackingField"));
        foreach (var field in customFields)
        {
            if (!field.FieldType.IsAssignableFrom(field.FieldType))
            {
                continue;
            }

            var fieldValue = field.GetValue(source);
            fields.Add(field, fieldValue);
        }

        return new PropertiesAndFields(properties, fields);
    }
}