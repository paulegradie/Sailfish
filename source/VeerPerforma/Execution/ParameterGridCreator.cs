using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VeerPerforma.Attributes;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution
{
    public class ParameterGridCreator : IParameterGridCreator
    {
        private readonly IParameterCombinator parameterCombinator;

        public ParameterGridCreator(IParameterCombinator parameterCombinator)
        {
            this.parameterCombinator = parameterCombinator;
        }

        public (List<string>, int[][]) GenerateParameterGrid(Type test)
        {
            var variableProperties = GetParams(test);

            var propNames = new List<string>();
            var propValues = new List<List<int>>();
            foreach (var (propertyName, values) in variableProperties)
            {
                propNames.Add(propertyName);
                propValues.Add(values.ToList());
            }

            var combos = parameterCombinator.GetAllPossibleCombos(propValues);

            foreach (var testCombos in combos)
            {
                var combs = testCombos.ToList();
            }

            // Propnames = ["A", "B"]
            //           A   B    A  B    A  B    A  B
            // combos = [[1, 2], [1, 4], [2, 2], [2, 4]
            return (propNames, combos);
        }

        private static Dictionary<string, int[]> GetParams(Type type)
        {
            var dict = new Dictionary<string, int[]>();
            var propertiesWithAttribute = type.GetPropertiesWithAttribute<IterationVariableAttribute>();
            foreach (var property in propertiesWithAttribute)
            {
                var variableValues = property
                    .GetCustomAttributes()
                    .OfType<IterationVariableAttribute>()
                    .Single() // multiple prop on the attribute is false, so this shouldn't throw - we validate first to give feedback
                    .N
                    .Distinct() // Duplicate values are currently allowed until we have an analyzer that prevents folks from providing duplicate values
                    .OrderBy(x => x)
                    .ToArray();
                dict.Add(property.Name, variableValues);
            }

            return dict;
        }
    }
}