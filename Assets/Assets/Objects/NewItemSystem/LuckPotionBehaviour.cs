// BaitBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    public enum LuckPotionType
    {
        Rare,
        Epic,
    }
    [System.Serializable]
    public class LuckPotionBehaviour : IItemBehaviour
    {
        [SerializeField] private LuckPotionType luckType;
        [SerializeField] private float effectTime;

        public LuckPotionType LuckType => luckType;
        public float EffectTime => effectTime;

        // No extra initialize code needed
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 