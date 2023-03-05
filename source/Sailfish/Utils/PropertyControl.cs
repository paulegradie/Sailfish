using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using System.Text.RegularExpressions;

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
    

    public static bool MatchString(string input, string match)
    {
        // Define the regular expression pattern to match "<MATCH_ME>_backing"
        const string pattern = @"<(.*?)>*";

        // Use Regex.Match to extract the word between < and > in the input string
        var m = Regex.Match(input, pattern);

        // If a match is found, check if it equals the input string
        if (m.Success && m.Groups[1].Value == match)
        {
            return true;
        }

        return false;
    }
}