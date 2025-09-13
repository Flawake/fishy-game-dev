// ItemDefinitionEditor.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ItemSystem.Editor {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ItemDefinition), true)]
    public class ItemDefinitionEditor : UnityEditor.Editor {
        SerializedProperty behavioursProp;
        List<Type> cachedBehaviourTypes;

        void OnEnable() {
            behavioursProp = serializedObject.FindProperty("behaviours");
            cachedBehaviourTypes = GetAllConcreteBehaviourTypes();
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            // Draw default fields except behaviours list
            DrawPropertiesExcluding(serializedObject, "behaviours");

            // Behaviours foldout
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Behaviours", EditorStyles.boldLabel);

            for (int i = 0; i < behavioursProp.arraySize; i++) {
                SerializedProperty element = behavioursProp.GetArrayElementAtIndex(i);
                Type elementType = GetManagedReferenceType(element);
                string label = elementType != null ? elementType.Name : element.displayName;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(element, new GUIContent(label), true);
                if (GUILayout.Button("Remove")) {
                    behavioursProp.DeleteArrayElementAtIndex(i);
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Behaviour")) {
                GenericMenu menu = new GenericMenu();
                foreach (Type t in cachedBehaviourTypes) {
                    bool already = BehaviourExists(t);
                    if (already) {
                        menu.AddDisabledItem(new GUIContent(t.Name));
                    } else {
                        menu.AddItem(new GUIContent(t.Name), false, () => AddBehaviour(t));
                    }
                }
                menu.ShowAsContext();
            }

            // Constraint validation
            string error = ItemDefinitionConstraints.Validate((ItemDefinition)target);
            if (!string.IsNullOrEmpty(error)) {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();

            // Ensure no duplicate behaviour types (can happen via copy/paste etc.)
            RemoveDuplicateBehaviours();
        }

        void AddBehaviour(Type behaviourType) {
            behavioursProp.arraySize++;
            SerializedProperty element = behavioursProp.GetArrayElementAtIndex(behavioursProp.arraySize - 1);
            element.managedReferenceValue = Activator.CreateInstance(behaviourType);
            serializedObject.ApplyModifiedProperties();
        }

        static Type GetManagedReferenceType(SerializedProperty property) {
            string full = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return null;

            // Most Unity versions return "AssemblyName FullTypeName"
            if (full.Contains(" ")) {
                var split = full.Split(' ');
                if (split.Length == 2) {
                    string assembly = split[0];
                    string qualified = split[1];
                    return Type.GetType($"{qualified}, {assembly}");
                }
            }

            // Some versions use "AssemblyName::FullTypeName"
            if (full.Contains("::")) {
                var split = full.Split(new[] { "::" }, StringSplitOptions.None);
                if (split.Length == 2) {
                    string assembly = split[0];
                    string qualified = split[1];
                    return Type.GetType($"{qualified}, {assembly}");
                }
            }

            // Fallback - maybe already in the classic Type.GetType() format
            return Type.GetType(full);
        }

        static List<Type> GetAllConcreteBehaviourTypes() {
            var baseType = typeof(IItemBehaviour);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => GetSafeTypes(a))
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();
        }

        static IEnumerable<Type> GetSafeTypes(Assembly assembly) {
            try { return assembly.GetTypes(); }
            catch { return Array.Empty<Type>(); }
        }

        bool BehaviourExists(Type type) {
            for (int i = 0; i < behavioursProp.arraySize; i++) {
                SerializedProperty element = behavioursProp.GetArrayElementAtIndex(i);
                Type elementType = GetManagedReferenceType(element);
                if (elementType == type) return true;
            }
            return false;
        }

        void RemoveDuplicateBehaviours() {
            HashSet<Type> seen = new HashSet<Type>();
            for (int i = behavioursProp.arraySize - 1; i >= 0; i--) {
                SerializedProperty element = behavioursProp.GetArrayElementAtIndex(i);
                Type t = GetManagedReferenceType(element);
                if (t == null) continue;
                if (seen.Contains(t)) {
                    behavioursProp.DeleteArrayElementAtIndex(i);
                } else {
                    seen.Add(t);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif 