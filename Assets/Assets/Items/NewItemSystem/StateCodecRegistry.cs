// StateCodecRegistry.cs
using System;
using System.Collections.Generic;

namespace NewItemSystem {
    /// <summary>
    /// Global registry that maps a runtime state type to its codec and a compact ushort id.
    /// A codec must register itself *once* in its static constructor.
    /// </summary>
    public static class StateCodecRegistry {
        private static readonly Dictionary<Type, ushort> typeToId = new();
        private static readonly Dictionary<ushort, IStateCodec> idToCodec = new();
        private static ushort nextId = 1; // 0 reserved

        static StateCodecRegistry() {
            // Force static constructors of core codecs so they self-register.
            _ = new StackCodec();
            _ = new DurabilityCodec();
        }

        public static void Register(IStateCodec codec) {
            var type = codec.StateType;
            if (typeToId.ContainsKey(type)) {
                return; // already registered
            }
            ushort id = nextId++;
            typeToId[type] = id;
            idToCodec[id] = codec;
        }

        public static ushort GetId(Type t) {
            if (!typeToId.TryGetValue(t, out ushort id)) {
                throw new InvalidOperationException($"State type {t} has not been registered with a codec.");
            }
            return id;
        }

        public static IStateCodec GetCodec(Type t) {
            return idToCodec[GetId(t)];
        }

        public static IStateCodec GetCodec(ushort id) {
            if (!idToCodec.TryGetValue(id, out var codec)) {
                throw new InvalidOperationException($"No codec registered for state id {id}");
            }
            return codec;
        }
    }
} 