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

        static StateCodecRegistry() {
            // Register core codecs with hardcoded IDs, changing IDs already registered will break existing saves in the db
            Register(new StackCodec(), 1);
            Register(new DurabilityCodec(), 2);
            Register(new FishCatchStateCodec(), 3);
        }

        // Register a codec with a hardcoded ID
        public static void Register(IStateCodec codec, ushort id) {
            var type = codec.StateType;
            if (typeToId.ContainsKey(type)) {
                if (typeToId[type] != id)
                    throw new InvalidOperationException($"Type {type} already registered with a different ID ({typeToId[type]} != {id})");
                return; // already registered with correct ID
            }
            if (idToCodec.ContainsKey(id))
                throw new InvalidOperationException($"ID {id} already registered for type {idToCodec[id].StateType}");
            typeToId[type] = id;
            idToCodec[id] = codec;
        }

        // Deprecated: do not use auto-incrementing registration anymore
        [Obsolete("Use Register(codec, id) with a hardcoded ID instead.")]
        public static void Register(IStateCodec codec) {
            throw new NotSupportedException("Use Register(codec, id) with a hardcoded ID instead.");
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