// FishBehaviour.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace ItemSystem {
    [Serializable]
    public class FishBehaviour : IItemBehaviour {
        [Header("Fish Data")]
        [SerializeField] private int minimumLength;
        [SerializeField] private int maximumLength;
        [SerializeField] private int avarageLength;
        [SerializeField] private int minMartketPrice;
        [SerializeField] private int maxMarketPrice;
        [SerializeField] private FishBaitType bitesOn;
        [SerializeField] private FishRarity rarity;
        [SerializeField] private Locations locations;
        [SerializeField] private float rarityFactor = 1f;
        [SerializeField] private List<TimeRange> timeRanges = new();
        [SerializeField] private List<DateRange> dateRanges = new();

        public FishRarity Rarity => rarity;
        public FishBaitType BitesOn => bitesOn;
        public float RarityFactor => rarityFactor;
        public IReadOnlyList<TimeRange> TimeRanges => timeRanges;
        public IReadOnlyList<DateRange> DateRanges => dateRanges;
        public int MinimumLength => minimumLength;
        public int MaximumLength => maximumLength;
        public int AvarageLength => avarageLength;
        public int MinMartketPrice => minMartketPrice;
        public int MaxMarketPrice => maxMarketPrice;
        public Locations Locations => locations;

        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) {
            if (!bag.ContainsKey(typeof(FishCatchState)))
                bag[typeof(FishCatchState)] = new FishCatchState { maxCaughtLength = 0 };
        }
    }
} 