// BaitBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem {
    [System.Serializable]
    public class BaitBehaviour : IItemBehaviour {
        [SerializeField] private ItemBaitType baitType = ItemBaitType.hook;

        public ItemBaitType BaitType => baitType;

        // Bait durability handled via optional DurabilityBehaviour
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }

        // Clone method removed as it should not be needed for immutable behaviours
    }
} 