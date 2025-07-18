// BaitBehaviour.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    [System.Serializable]
    public class BaitBehaviour : IItemBehaviour, ICloneable {
        [SerializeField] ItemBaitType baitType = ItemBaitType.hook;

        public ItemBaitType BaitType => baitType;

        // Bait durability handled via optional DurabilityBehaviour
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }

        public object Clone() {
            return this.MemberwiseClone();
        }
    }
} 