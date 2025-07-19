// ItemDefinition.cs
using System;
using System.Linq;
using UnityEngine;

namespace NewItemSystem {
    [CreateAssetMenu(menuName = "Items/Definition", fileName = "NewItemDefinition")]
    public class ItemDefinition : ScriptableObject {
        [Header("Core")]
        [SerializeField] private int id;
        [SerializeField] private string displayName;
        [TextArea(3, 10)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private int maxStack = 1;
        [SerializeField] private bool isStatic = false;

        [Header("Behaviours")]
        [SerializeReference] private IItemBehaviour[] behaviours;

        // --- Properties ----------------------------------------------------
        public int Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;
        public IItemBehaviour[] Behaviours => behaviours ?? Array.Empty<IItemBehaviour>();
        // isStatic tells if the object CAN be removed from the inventory
        public bool IsStatic => isStatic;

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
} 