using Sailfish.Exceptions;
using Sailfish.Utils;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface IExecutionState
{
    PropertiesAndFields GetState(string key);
    void SetState(string key, PropertiesAndFields state);
    void RemoveState(string key);
    bool Contains(string key);
}

internal class ExecutionState : IExecutionState
{
    private readonly Dictionary<string, PropertiesAndFields> hiddenState = new();

    public PropertiesAndFields GetState(string key)
    {
        if (!hiddenState.TryGetValue(key, out var state))
        {
            throw new SailfishException("State properties were expected but not found");
        };
        return state;
    }

    public void SetState(string key, PropertiesAndFields state)
    {
        hiddenState[key] = state;
    }

    public void RemoveState(string key)
    {
        hiddenState.Remove(key);
    }

    public bool Contains(string key)
    {
        return hiddenState.ContainsKey(key);
    }
}