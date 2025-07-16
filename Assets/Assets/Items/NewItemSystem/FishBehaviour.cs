// FishBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    [System.Serializable]
    public class FishBehaviour : IItemBehaviour {
        [SerializeField] Rarity rarity = Rarity.Common;
        [SerializeField] FishBaitType bitesOn = FishBaitType.hook | FishBaitType.dough | FishBaitType.meat;

        public Rarity Rarity => rarity;
        public FishBaitType BitesOn => bitesOn;

        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) {
            // no runtime state necessary for fish behaviour.
        }
    }
} 