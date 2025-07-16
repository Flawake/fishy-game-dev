// RodBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    [System.Serializable]
    public class RodBehaviour : IItemBehaviour {
        [SerializeField] int strength = 1;

        public int Strength => strength;

        // Rod itself doesn't create durability state. Attach a DurabilityBehaviour
        // to the item definition if the rod should wear out.
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 