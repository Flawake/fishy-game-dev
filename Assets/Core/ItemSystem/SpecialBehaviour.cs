// SpecialBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem {
    public enum SpecialEffectType
    {
        LuckBoost,
        WaitTimeReduction,
        // Add more special effects as needed
    }
    
    [System.Serializable]
    public class SpecialBehaviour : IItemBehaviour
    {
        [SerializeField] private SpecialEffectType effectType;
        [SerializeField] private float effectValue = 1.0f; // Multiplier, seconds, etc.
        [SerializeField] private float durationSeconds = 60f;

        public SpecialEffectType EffectType => effectType;
        public float EffectValue => effectValue;
        public float DurationSeconds => durationSeconds;

        // No extra initialize code needed
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 