// BaitBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    [System.Serializable]
    public class ShopBehaviour : IItemBehaviour {
        [SerializeField] private int priceCoins = -1;
        [SerializeField] private int priceBucks = -1;
        [SerializeField] private int amount = 1;
        
        public int PriceCoins => priceCoins;
        public int PriceBucks => priceBucks;
        public int Amount => amount;

        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 