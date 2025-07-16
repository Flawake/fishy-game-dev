// StatePacker.cs
using System;
using System.Collections.Generic;
using Mirror;

namespace NewItemSystem {
    public static class StatePacker {
        public static byte[] Pack(Dictionary<Type, IRuntimeBehaviourState> bag) {
            var writer = NetworkWriterPool.Get();
            try {
                writer.WriteUShort((ushort)bag.Count);
                foreach (var kvp in bag) {
                    Type t = kvp.Key;
                    IRuntimeBehaviourState state = kvp.Value;
                    ushort id = StateCodecRegistry.GetId(t);
                    writer.WriteUShort(id);

                    // Write state into sub writer for length framing
                    var sub = NetworkWriterPool.Get();
                    try {
                        StateCodecRegistry.GetCodec(id).Write(sub, state);
                        writer.WriteArraySegmentAndSize(sub.ToArraySegment());
                    }
                    finally { NetworkWriterPool.Return(sub); }
                }
                return writer.ToArraySegment().ToArray();
            }
            finally {
                NetworkWriterPool.Return(writer);
            }
        }

        public static void UnpackInto(byte[] data, Dictionary<Type, IRuntimeBehaviourState> bag) {
            bag.Clear();
            var reader = NetworkReaderPool.Get(data);
            try {
                ushort entries = reader.ReadUShort();
                for (int i = 0; i < entries; i++) {
                    ushort id = reader.ReadUShort();
                    byte[] blob = reader.ReadBytesAndSize();
                    var codec = StateCodecRegistry.GetCodec(id);
                    var subReader = NetworkReaderPool.Get(blob);
                    try {
                        IRuntimeBehaviourState state = codec.Read(subReader);
                        bag[codec.StateType] = state;
                    }
                    finally { NetworkReaderPool.Return(subReader); }
                }
            }
            finally {
                NetworkReaderPool.Return(reader);
            }
        }
    }
} 