// BaitBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    public enum WatchType
    {
        Rare,
    }
    [System.Serializable]
    public class MagicWatchBehaviour : IItemBehaviour
    {
        [SerializeField] private WatchType watchype;
        [SerializeField] private float effectTime;

        public WatchType WatchType => watchype;
        public float EffectTime => effectTime;

        // No extra initialize code needed
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 