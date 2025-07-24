﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Sailfish.Contracts.Public.Models;

public class TestCaseVariables
{
    private const string OpenBracket = "(";
    private const string CloseBracket = ")";
    private const char Colon = ':';

#pragma warning disable CS8618

    public TestCaseVariables()
#pragma warning restore CS8618
    {
    }

    public TestCaseVariables(string displayName)
    {
        Variables = GetElements(displayName);
    }

    [JsonConstructor]
    public TestCaseVariables(IEnumerable<TestCaseVariable> variables)
    {
        Variables = variables.OrderBy(x => x.Name);
    }

    public IEnumerable<TestCaseVariable> Variables { get; }

    public TestCaseVariable? GetVariableIndex(int index)
    {
        try
        {
            return Variables.ToArray()[index];
        }
        catch
        {
            return null;
        }
    }

    public TestCaseVariable GetVariableByName(string name)
    {
        return Variables.Single(x => x.Name.Equals(name));
    }

    public string FormVariableSection()
    {
        var parts = Variables.Select(variable =>
        {
            var valueString = variable.Value.ToString()?.Trim() ?? string.Empty;

            // Clean up complex object representations
            var cleanValue = CleanComplexObjectString(variable.Value, valueString);

            // Escape commas in the value to prevent parsing issues while preserving data integrity
            cleanValue = cleanValue.Replace(",", "\\,");

            return $"{variable.Name.Trim()}: {cleanValue}";
        });
        return OpenBracket + string.Join(", ", parts).Trim() + CloseBracket;
    }

    private static string CleanComplexObjectString(object value, string valueString)
    {
        // If it's a primitive type or string, return as-is
        if (IsPrimitiveType(value.GetType()))
        {
            return valueString;
        }

        // If it contains braces, it's likely a complex object representation
        if (valueString.Contains('{') && valueString.Contains('}'))
        {
            return RemoveRedundantTypeNames(valueString, value.GetType());
        }

        return valueString;
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(TimeSpan) ||
               type.IsEnum;
    }

    private static string RemoveRedundantTypeNames(string objectString, Type objectType)
    {
        var typeName = objectType.Name;
        if (objectString.StartsWith(typeName + " {"))
        {
            if (typeName.Length <= objectString.Length)
            {
                objectString = objectString[typeName.Length..].TrimStart();
            }
        }

        // Remove nested type names recursively
        // Look for patterns like "PropertyName = TypeName { ... }"
        var properties = objectType.GetProperties();
        foreach (var prop in properties)
        {
            if (IsPrimitiveType(prop.PropertyType))
            {
                continue;
            }

            var propTypeName = prop.PropertyType.Name;
            // Use regex with word boundaries to ensure we only match property assignments
            var pattern = $@"\b{Regex.Escape(prop.Name)}\s*=\s*{Regex.Escape(propTypeName)}\s*\{{";
            var replacement = $"{prop.Name} = {{";
            objectString = Regex.Replace(objectString, pattern, replacement);
        }

        return objectString;
    }

    private static TestCaseVariable ParseVariable(string variable)
    {
        // like varName:int
        var parts = variable.Split(Colon);
        return int.TryParse(parts[1], out var value)
            ? new TestCaseVariable(parts[0], value)
            : new TestCaseVariable(parts[0], parts[1]);
    }

    private static IEnumerable<TestCaseVariable> GetElements(string s)
    {
        var rawElements = s
            .Split(OpenBracket)
            .Last()
            .Replace(CloseBracket, string.Empty)
            .Split(",")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        if (rawElements.Count != 0)
        {
            return rawElements
                .Select(ParseVariable)
                .OrderByDescending(x => x.Name)
                .ThenBy(x => x.Value)
                .ToList();
        }

        return [];
    }
}