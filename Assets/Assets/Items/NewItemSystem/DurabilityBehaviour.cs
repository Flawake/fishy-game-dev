// DurabilityBehaviour.cs
using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace NewItemSystem {
    [Serializable]
    public class DurabilityState : IRuntimeBehaviourState {
        public int remaining;
    }

    [Serializable]
    public class DurabilityBehaviour : IItemBehaviour, ICloneable {
        [SerializeField] int maxDurability = 1;
        [SerializeField] bool infinite;

        public int MaxDurability => maxDurability;

        public static int DurabilityLeft(ItemInstance inst) {
            DurabilityState s = inst?.GetState<DurabilityState>();
            return s?.remaining ?? -1;
        }

        // Backwards-compat alias
        public static int GetRemaining(ItemInstance inst) => DurabilityLeft(inst);

        public void InitialiseState(Dictionary<Type, IRuntimeBehaviourState> bag) {
            if (infinite) return;
            bag[typeof(DurabilityState)] = new DurabilityState { remaining = maxDurability };
        }

        public object Clone() {
            return this.MemberwiseClone();
        }
    }

    public sealed class DurabilityCodec : IStateCodec {
        public Type StateType => typeof(DurabilityState);

        static DurabilityCodec() {
            StateCodecRegistry.Register(new DurabilityCodec(), 2);
        }

        public void Write(NetworkWriter writer, IRuntimeBehaviourState genericState) {
            var state = (DurabilityState)genericState;
            writer.WriteInt(state.remaining);
        }

        public IRuntimeBehaviourState Read(NetworkReader reader) {
            return new DurabilityState { remaining = reader.ReadInt() };
        }
    }
} 