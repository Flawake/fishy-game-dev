using Mirror;
using System;

namespace ItemSystem {
    [Serializable]
    public class FishCatchState : IRuntimeBehaviourState
    {
        public int maxCaughtLength;
    }

    public sealed class FishCatchStateCodec : IStateCodec
    {
        public Type StateType => typeof(FishCatchState);

        public void Write(NetworkWriter writer, IRuntimeBehaviourState genericState)
        {
            var state = (FishCatchState)genericState;
            writer.WriteInt(state.maxCaughtLength);
        }

        public IRuntimeBehaviourState Read(NetworkReader reader)
        {
            return new FishCatchState
            {
                maxCaughtLength = reader.ReadInt()
            };
        }
    }
} 