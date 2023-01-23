using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis;
using Sailfish.Exceptions;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class ParameterCombinator : IParameterCombinator
{
    public IEnumerable<PropertySet> GetAllPossibleCombos(IEnumerable<string> orderedPropertyNames, IEnumerable<IEnumerable<int>> orderedPropertyValues)
    {
        var propNames = orderedPropertyNames.ToArray();
        if (propNames.Length == 0)
        {
            return new List<PropertySet>().AsEnumerable();
        }

        var ints = orderedPropertyValues.ToArray();
        if (ints.ToArray().Length != propNames.Length)
        {
            throw new SailfishException(
                $"The number of property {propNames.Length} names did not match the number of property value sets {ints.Length}");
        }

        var strings = ints.Select(x => x.Select(y => y.ToString()));
        IEnumerable<IEnumerable<string>> combos = new[] { Array.Empty<string>() };

        combos = strings
            .Aggregate(
                combos,
                (current, inner) =>
                    from c in current
                    from i
                        in inner
                    select ParameterCombinatorExtensionMethods.Append(c, i));

        var parsedCombinations = combos.Select(x => x.Select(int.Parse).ToArray()).ToArray();

        var propertySets = new List<PropertySet>();
        foreach (var paredCombination in parsedCombinations)
        {
            var variableSets = new List<TestCaseVariable>();
            for (var j = 0; j < propNames.Length; j++)
            {
                var propertyName = propNames[j];
                var propertyValue = paredCombination[j];
                var variableSet = new TestCaseVariable(propertyName, propertyValue);
                variableSets.Add(variableSet);
            }

            propertySets.Add(new PropertySet(variableSets));
        }


        return propertySets;
    }
}