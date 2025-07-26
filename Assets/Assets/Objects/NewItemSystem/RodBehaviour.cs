// RodBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem {
    [System.Serializable]
    public class RodBehaviour : IItemBehaviour {
        [SerializeField] private int strength = 1;
        [SerializeField] private RodThrowDistance throwDistance = RodThrowDistance.Close;

        public int Strength => strength;
        public RodThrowDistance ThrowDistance => throwDistance;

        // Rod itself doesn't create durability state. Attach a DurabilityBehaviour
        // to the item definition if the rod should wear out.
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }

    }
} 