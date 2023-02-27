using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis;
using Sailfish.Exceptions;

namespace Sailfish.Execution;

internal class ParameterCombinator : IParameterCombinator
{
    public IEnumerable<PropertySet> GetAllPossibleCombos(IEnumerable<string> orderedPropertyNames, IEnumerable<IEnumerable<object>> orderedPropertyValues)
    {
        var propNames = orderedPropertyNames.ToArray();
        if (propNames.Length == 0)
        {
            return new List<PropertySet>().AsEnumerable();
        }

        var ints = orderedPropertyValues.Select(x => x.ToArray()).ToArray();
        if (ints.ToArray().Length != propNames.Length)
        {
            throw new SailfishException(
                $"The number of property {propNames.Length} names did not match the number of property value sets {ints.Length}");
        }
        
        var combos = GetAllCombinations(ints).ToArray();

        var propertySets = new List<PropertySet>();
        foreach (var pairedCombo in combos)
        {
            var variableSets = new List<TestCaseVariable>();
            for (var j = 0; j < propNames.Length; j++)
            {
                var propertyName = propNames[j];
                var propertyValue = pairedCombo[j];
                var variableSet = new TestCaseVariable(propertyName, propertyValue);
                variableSets.Add(variableSet);
            }

            propertySets.Add(new PropertySet(variableSets));
        }


        return propertySets;
    }

    public static IEnumerable<object[]> GetAllCombinations(object[][] arrays)
    {
        var indices = new int[arrays.Length];
        var total = arrays.Aggregate(1, (current, t) => current * t.Length);

        for (var count = 0; count < total; count++)
        {
            var combination = new object[arrays.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                combination[i] = arrays[i].GetValue(indices[i]);
            }

            yield return combination;
            for (var i = indices.Length - 1; i >= 0; i--)
            {
                indices[i]++;
                if (indices[i] == arrays[i].Length)
                {
                    indices[i] = 0;
                }
                else
                {
                    break;
                }
            }
        }
    }

}