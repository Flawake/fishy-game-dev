// StackCodec.cs
using System;
using Mirror;

namespace NewItemSystem {
    public sealed class StackCodec : IStateCodec {
        public Type StateType => typeof(StackState);

        static StackCodec() {
            StateCodecRegistry.Register(new StackCodec());
        }

        public void Write(NetworkWriter writer, IRuntimeBehaviourState genericState) {
            var s = (StackState)genericState;
            writer.WriteInt(s.currentAmount);
            writer.WriteInt(s.maxStack);
        }

        public IRuntimeBehaviourState Read(NetworkReader reader) {
            return new StackState {
                currentAmount = reader.ReadInt(),
                maxStack = reader.ReadInt()
            };
        }
    }
} 