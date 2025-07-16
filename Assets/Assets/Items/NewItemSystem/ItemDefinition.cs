// ItemDefinition.cs
using System;
using System.Linq;
using UnityEngine;

namespace NewItemSystem {
    [CreateAssetMenu(menuName = "Items/Definition", fileName = "NewItemDefinition")]
    public class ItemDefinition : ScriptableObject {
        [Header("Core")]
        [SerializeField] int id;
        [SerializeField] string displayName;
        [SerializeField] Sprite icon;
        [SerializeField] int maxStack = 1;

        [Header("Behaviours")]
        [SerializeReference] IItemBehaviour[] behaviours;

        // --- Properties ----------------------------------------------------
        public int Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;
        // Rarity moved to FishBehaviour where applicable.
        public IItemBehaviour[] Behaviours => behaviours ?? Array.Empty<IItemBehaviour>();

        public T GetBehaviour<T>() where T : class, IItemBehaviour {
            return Behaviours.OfType<T>().FirstOrDefault();
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (string.IsNullOrEmpty(displayName)) {
                displayName = name;
            }
        }
#endif
    }

    public enum Rarity { None, Common, Uncommon, Rare, Epic, Legendary }
} 