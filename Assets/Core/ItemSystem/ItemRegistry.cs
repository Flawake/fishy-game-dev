// ItemRegistry.cs
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem {
    public static class ItemRegistry {
        private static readonly Dictionary<int, ItemDefinition> byId = new();
        private static ItemDefinition[] defs;
        private static bool loaded = false;
        

        public static ItemDefinition Get(int id) {
            EnsureLoaded();
            if (!byId.TryGetValue(id, out var def)) {
                Debug.LogError($"ItemDefinition with id {id} not found in registry");
            }
            return def;
        }

        public static ItemDefinition[] GetFullItemsList()
        {
            EnsureLoaded();
            return defs;
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            defs = Resources.LoadAll<ItemDefinition>(""); // search all resources
            foreach (var d in defs)
            {
                if (byId.ContainsKey(d.Id))
                {
                    Debug.LogWarning($"Duplicate ItemDefinition id {d.Id} between {byId[d.Id].name} and {d.name}");
                    continue;
                }
                byId[d.Id] = d;
            }
        }
    }
} 