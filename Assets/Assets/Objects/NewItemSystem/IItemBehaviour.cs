// IItemBehaviour.cs
using System.Collections.Generic;

namespace ItemSystem {
    public interface IItemBehaviour {
        /// <summary>
        /// Gives the behaviour a chance to inject its runtime state when an item instance is created.
        /// </summary>
        void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag);
    }
} 