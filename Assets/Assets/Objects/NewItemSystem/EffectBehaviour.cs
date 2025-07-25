// BaitBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    public enum EffectType
    {
        LuckPotion,
        Watch,
    }
    [System.Serializable]
    public class EffectBehaviour : IItemBehaviour
    {
        [SerializeField] private EffectType effect;
        [SerializeField] private float effectTime;
        [SerializeField] private float effectStrength;

        public EffectType EffectType => effect;
        public float EffectTime => effectTime;
        public float EffectStrength => effectStrength;

        // No extra initialize code needed
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 