using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;

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
    public Dictionary<Type, IRuntimeBehaviourState> States => states;

    public DeltaItem(ItemDefinition itemDefinition, Guid itemUUID, List<(Type, IRuntimeBehaviourState)> states)
    {
        this.itemDefinition = itemDefinition;
        this.itemUUID = itemUUID;

        foreach ((Type t, IRuntimeBehaviourState i) in states)
        {
            this.states[t] = i;
        }
    }

    // New DeltaItem, e.g. used for buying items from the store
    public DeltaItem(ItemDefinition itemDefinition, int deltaStack)
    {
        this.itemDefinition = itemDefinition;
        itemUUID = Guid.NewGuid();
        states[typeof(StackState)] = new StackState { currentAmount = deltaStack };

        // Behaviours may create their own state
        foreach (var behaviour in itemDefinition.Behaviours)
        {
            behaviour.InitialiseState(states);
        }
    }

    public TState? GetState<TState>()
    where TState : class, IRuntimeBehaviourState
    {
        return states.TryGetValue(typeof(TState), out var state)
            ? (TState)state
            : null;
    }

    public void SetState<TState>(TState newState)
    where TState : class, IRuntimeBehaviourState
    {
        states[typeof(TState)] = newState;
    }

    public ItemInstance IntoItemInstance(bool regenerateUUID)
    {
        Guid uuid = regenerateUUID ? Guid.NewGuid() : itemUUID;
        ItemInstance item = new()
        {
            uuid = uuid,
            def = ItemDefinition,
        };

        foreach ((Type type, IRuntimeBehaviourState state) in states)
        {
            // Keyed by the concrete state type. SetState cannot be used here: it would infer
            // IRuntimeBehaviourState as the key and every state would land under that one entry.
            item.state[type] = state;
        }

        // The stack amount is the one piece of state both sides keep changing, so the instance gets
        // its own copy instead of sharing this delta's.
        if (GetState<StackState>() is { } stack)
        {
            item.state[typeof(StackState)] = new StackState { currentAmount = stack.currentAmount };
        }

        return item;
    }

    public static DeltaItem FromItemInstance(ItemInstance item)
    {
        List<(Type, IRuntimeBehaviourState)> states = item.state.Select(state => (state.Key, state.Value)).ToList();
        return new DeltaItem(item.def, item.uuid, states);
    }
}

public static class DeltaItemReaderWriter
{
    // Mirror uses the method name pattern WriteX to auto-register.
    public static void WriteDeltaItem(this NetworkWriter writer, DeltaItem item)
    {
        writer.WriteInt(item.ItemDefinition.Id);
        writer.WriteGuid(item.ItemUUID);
        byte[] blob = StatePacker.Pack(item.States);
        writer.WriteBytesAndSize(blob);
    }

    // Mirror uses the method name pattern ReadX to auto-register.
    public static DeltaItem ReadDeltaItem(this NetworkReader reader)
    {
        int defId = reader.ReadInt();
        Guid uuid = reader.ReadGuid();
        byte[] blob = reader.ReadBytesAndSize();

        ItemDefinition def = ItemRegistry.Get(defId);
        Dictionary<Type, IRuntimeBehaviourState> states = new();
        StatePacker.UnpackInto(blob, states);

        return new DeltaItem(def, uuid, states.Select(state => (state.Key, state.Value)).ToList());
    }
}

}
