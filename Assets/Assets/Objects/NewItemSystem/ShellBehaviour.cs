// ShellBehaviour.cs
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem {
    
    [System.Serializable]
    public class ShellBehaviour : IItemBehaviour
    {
        // ShellBehaviour is used for marking the item, no implementation needed
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 