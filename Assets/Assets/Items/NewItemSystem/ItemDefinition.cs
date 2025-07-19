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
        [SerializeField] string description;
        [SerializeField] Sprite icon;
        [SerializeField] int maxStack = 1;
        [SerializeField] bool isStatic = false;

        [Header("Behaviours")]
        [SerializeReference] IItemBehaviour[] behaviours;

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

        // Runtime deep clone (not an asset, just a memory copy)
        public ItemDefinition Clone()
        {
            ItemDefinition copy = ScriptableObject.CreateInstance<ItemDefinition>();
            copy.id = this.id;
            copy.displayName = this.displayName;
            copy.icon = this.icon;
            copy.maxStack = this.maxStack;
            copy.isStatic = this.isStatic;
            if (this.behaviours != null)
            {
                copy.behaviours = new IItemBehaviour[this.behaviours.Length];
                for (int i = 0; i < this.behaviours.Length; i++)
                {
                    if (this.behaviours[i] is ICloneable cloneable)
                    {
                        copy.behaviours[i] = (IItemBehaviour)cloneable.Clone();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Behaviour {this.behaviours[i].GetType().Name} does not implement ICloneable. All behaviours must support deep cloning.");
                    }
                }
            }
            return copy;
        }
    }

    public enum Rarity { None, Common, Uncommon, Rare, Epic, Legendary }
} 