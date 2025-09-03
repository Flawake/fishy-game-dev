// ItemDefinitionConstraints.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ItemSystem.Editor {
    /// <summary>
    /// Central place to declare behaviour constraints.  Add entries to 'ForbiddenPairs'
    /// to prevent an ItemDefinition from containing both behaviours at the same time.
    /// </summary>
    public static class ItemDefinitionConstraints {
        // List of forbidden behaviour type pairs (order independent)
        static readonly HashSet<(Type, Type)> ForbiddenPairs = new() {
            (typeof(RodBehaviour),  typeof(BaitBehaviour)),
            (typeof(RodBehaviour),  typeof(FishBehaviour)),
            (typeof(BaitBehaviour), typeof(FishBehaviour)),
        };

        // Validate a single item definition.  Returns error string or null if valid.
        public static string Validate(ItemDefinition def) {
            var list = def.Behaviours.Select(b => b.GetType()).ToList();
            foreach (var (a,b) in ForbiddenPairs) {
                if (list.Contains(a) && list.Contains(b)) {
                    return $"{def.name} contains incompatible behaviours {a.Name} and {b.Name}";
                }
            }
            return null;
        }

        // Scan all assets (e.g. on compilation).
        [InitializeOnLoadMethod]
        static void ValidateAllDefinitions() {
            var defs = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            foreach (var d in defs) {
                string err = Validate(d);
                if (err != null) Debug.LogError(err, d);
            }
        }
    }
}
#endif 