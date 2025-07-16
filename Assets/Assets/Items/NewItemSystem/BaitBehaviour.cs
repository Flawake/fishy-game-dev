// BaitBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    [System.Serializable]
    public class BaitBehaviour : IItemBehaviour {
        [SerializeField] ItemBaitType baitType = ItemBaitType.hook;

        public ItemBaitType BaitType => baitType;

        // Bait durability handled via optional DurabilityBehaviour
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 