using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution
{
    internal class ParameterGridCreator : IParameterGridCreator
    {
        private readonly IParameterCombinator parameterCombinator;
        private readonly IIterationVariableRetriever iterationVariableRetriever;

        public ParameterGridCreator(IParameterCombinator parameterCombinator, IIterationVariableRetriever iterationVariableRetriever)
        {
            this.parameterCombinator = parameterCombinator;
            this.iterationVariableRetriever = iterationVariableRetriever;
        }

        // TODO: We could probably use a better data structure here
        /// <summary>
        /// Returns an tuple of (property name (as a string), variable groups of the structure
        /// Propnames = ["A", "B"]
        ///           A   B    A  B    A  B    A  B
        /// combos = [[1, 2], [1, 4], [2, 2], [2, 4]
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public (List<string>, int[][]) GenerateParameterGrid(Type test)
        {
            var variableProperties = iterationVariableRetriever.RetrieveIterationVariables(test);

            var propNames = new List<string>();
            var propValues = new List<List<int>>();
            foreach (var (propertyName, values) in variableProperties)
            {
                propNames.Add(propertyName);
                propValues.Add(values.ToList());
            }

            var combos = parameterCombinator.GetAllPossibleCombos(propValues);

            return (propNames, combos);
        }
    }
}