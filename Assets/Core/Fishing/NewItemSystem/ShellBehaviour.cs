using System.Collections.Generic;

namespace ItemSystem {
    
    [System.Serializable]
    public class ShellBehaviour : IItemBehaviour
    {
        // ShellBehaviour is used for marking the item, no implementation needed
        public void InitialiseState(Dictionary<System.Type, IRuntimeBehaviourState> bag) { }
    }
} 