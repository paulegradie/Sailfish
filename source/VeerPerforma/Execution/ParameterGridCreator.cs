using System.Reflection;
using VeerPerforma.Attributes;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution;

public class ParameterGridCreator : IParameterGridCreator
{
    private readonly IParameterCombinator parameterCombinator;

    public ParameterGridCreator(IParameterCombinator parameterCombinator)
    {
        this.parameterCombinator = parameterCombinator;
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
                .Single() // multiple is false, so this shouldn't throw - we validate first to give feedback
                .N
                .ToArray();
            dict.Add(property.Name, variableValues);
        }

        return dict;
    }

    public (List<string>, IEnumerable<IEnumerable<int>>) GenerateParameterGrid(Type test)
    {
        logger.Verbose("Getting Params from test type: {0}", test.Name);
        var variableProperties = GetParams(test);
        
        var propNames = new List<string>();
        var propValues = new List<List<int>>();
        logger.Verbose("Variable props gotten. Logging them here:");
        foreach (var (propertyName, values) in variableProperties)
        {
            logger.Verbose("Property Name: {0}", propertyName);
            logger.Verbose("Property Values = : {0}", string.Join(", ", values.Select(x => x.ToString())));
            propNames.Add(propertyName);
            propValues.Add(values.ToList());
        }

        var combos = parameterCombinator.GetAllPossibleCombos(propValues);
        var testA = combos.ToList();
        logger.Verbose("Num Combos: {0}", testA.Count.ToString());

        foreach (var testCombos in testA)
        {
            var combs = testCombos.ToList();
            logger.Verbose("Combos: {0}", string.Join(", ", combs.Select(x => x.ToString())));
        }

        // Propnames = ["PropA", "PropB"]
        // combos = [[1, 2], [1, 4], [2, 2], [2, 4]
        return (propNames, combos);
    }
}