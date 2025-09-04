// StackCodec.cs
using System;
using Mirror;

namespace ItemSystem {
    public sealed class StackCodec : IStateCodec {
        public Type StateType => typeof(StackState);

        static StackCodec() {
            StateCodecRegistry.Register(new StackCodec(), 1);
        }

        public void Write(NetworkWriter writer, IRuntimeBehaviourState genericState) {
            var s = (StackState)genericState;
            writer.WriteInt(s.currentAmount);
        }

        public IRuntimeBehaviourState Read(NetworkReader reader) {
            return new StackState {
                currentAmount = reader.ReadInt(),
            };
        }
    }
} 