// FishBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    [System.Serializable]
    public class FishBehaviour : IItemBehaviour {
        [SerializeField] Rarity rarity = Rarity.Common;
        [SerializeField] FishBaitType bitesOn = FishBaitType.hook | FishBaitType.dough | FishBaitType.meat;

        [Header("Spawn / Meta")]
        [Tooltip("Value between 0‒1 controlling selection probability; lower means rarer.")]
        [SerializeField] float rarityFactor = 1f;

        [SerializeField] List<TimeRange> timeRanges = new();
        [SerializeField] List<DateRange> dateRanges = new();

        // Public accessors --------------------------------------------------
        public Rarity Rarity => rarity;
        public FishBaitType BitesOn => bitesOn;
        public float RarityFactor => rarityFactor;
        public IReadOnlyList<TimeRange> TimeRanges => timeRanges;
        public IReadOnlyList<DateRange> DateRanges => dateRanges;

        // Fish behaviour carries no per-instance mutable state.
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 