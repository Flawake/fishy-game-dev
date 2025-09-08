// FishPriceBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem {
    [System.Serializable]
    public class FishPriceBehaviour : IItemBehaviour {
        [SerializeField, Range(0.8f, 1.2f)] private float valueMultiplier = 1;
        
        public float ValueMultiplier => valueMultiplier;

        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 
