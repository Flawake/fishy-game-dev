using System;
using System.Collections.Generic;

#nullable enable

namespace ItemSystem
{
public class DeltaItem
{
    ItemDefinition itemDefinition;
    Guid itemUUID;
    Dictionary<Type, IRuntimeBehaviourState> states = new();

    public ItemDefinition ItemDefinition => itemDefinition;
    public Guid ItemUUID => itemUUID;

    public DeltaItem(ItemDefinition itemDefinition, Guid itemUUID, List<(Type, IRuntimeBehaviourState)> states)
    {
        this.itemDefinition = itemDefinition;
        this.itemUUID = itemUUID;

        foreach ((Type t, IRuntimeBehaviourState i) in states)
        {
            this.states[t] = i;
        }
    }

    public TState? GetState<TState>()
    where TState : class, IRuntimeBehaviourState
    {
        return states.TryGetValue(typeof(TState), out var state)
            ? (TState)state
            : null;
    }
}
   
}
