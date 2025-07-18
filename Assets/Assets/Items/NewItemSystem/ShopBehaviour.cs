// BaitBehaviour.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewItemSystem {
    [System.Serializable]
    public class ShopBehaviour : IItemBehaviour, ICloneable {
        [SerializeField] int priceCoins = -1;
        [SerializeField] int priceBucks = -1;

        public int PriceCoins => priceCoins;
        public int PriceBucks => priceBucks;

        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }

        public object Clone() {
            return this.MemberwiseClone();
        }
    }
} 