// BaitBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem {
    [System.Serializable]
    public class ShopBehaviour : IItemBehaviour {
        [SerializeField] private int priceCoins = -1;
        [SerializeField] private int priceBucks = -1;
        [SerializeField] private int amount = 1;
        [SerializeField] private int unlockLevel = 0;
        
        public int PriceCoins => priceCoins;
        public int PriceBucks => priceBucks;
        public int Amount => amount;
        public int UnlockLevel => unlockLevel;

        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 