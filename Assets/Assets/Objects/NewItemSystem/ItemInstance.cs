// ItemInstance.cs
using System;
using System.Collections.Generic;

namespace NewItemSystem {
    [Serializable]
    public class ItemInstance
    {
        public Guid uuid;
        public ItemDefinition def;
        public readonly Dictionary<Type, IRuntimeBehaviourState> state = new();

        // Needed for Mirror deserialisation
        public ItemInstance() { }

        public ItemInstance(ItemDefinition definition, int initialStack = 1)
        {
            uuid = Guid.NewGuid();
            def = definition;

            // Core stack state
            state[typeof(StackState)] = new StackState { currentAmount = initialStack };

            // Behaviours may create their own state
            foreach (var behaviour in definition.Behaviours)
            {
                behaviour.InitialiseState(state);
            }
        }

        // Generic helpers ---------------------------------------------------
        public TState GetState<TState>() where TState : class, IRuntimeBehaviourState
        {
            state.TryGetValue(typeof(TState), out var result);
            return result as TState;
        }

        public void SetState<TState>(TState newState) where TState : class, IRuntimeBehaviourState
        {
            state[typeof(TState)] = newState;
        }

    }

    // ------------------------------------------------------------------
    // Basic stackable state (present on *all* items)
    // ------------------------------------------------------------------
    [Serializable]
    public class StackState : IRuntimeBehaviourState {
        public int currentAmount;
    }
} 