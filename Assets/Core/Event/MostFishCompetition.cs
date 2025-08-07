using System;
using Mirror;

namespace GlobalCompetitionSystem
{
    public class MostFishCompetitonState : ICompetitionState
    {
        public bool specificFish;
        public int fishIDToCatch;
    }
    public class MostFishCompetition : CurrentCompetition<CurrentFish>
    {
        public ICompetitionState State { get; set; }

        public override void SetState(ICompetitionState state)
        {
            if (state is not MostFishCompetitonState)
            {
                throw new InvalidOperationException($"State must be a {typeof(MostFishCompetitonState)} at this point");
            }
            State = (MostFishCompetitonState)state;
        }

        [Server]
        public override bool AddToCompetition(CurrentFish competition)
        {
            return false;
        }
    }

    public class MostFishCompetitionCodec : ICompetitionCodec
    {
        public Type StateType => typeof(MostFishCompetitonState);

        static MostFishCompetitionCodec() {
            CompetitionCodecRegistry.Register(new MostFishCompetitionCodec(), 2);
        }

        public void Write(NetworkWriter writer, ICompetitionState genericState) {
            MostFishCompetitonState state = (MostFishCompetitonState)genericState;
            writer.WriteBool(state.specificFish);
            writer.WriteInt(state.fishIDToCatch);
        }

        public ICompetitionState Read(NetworkReader reader) {
            return new MostFishCompetitonState { specificFish = reader.ReadBool(), fishIDToCatch = reader.ReadInt() };
        }
    }
}