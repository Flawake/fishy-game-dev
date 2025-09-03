using System;
using ItemSystem;
using Mirror;
using UnityEngine;

namespace GlobalCompetitionSystem
{
    public class MostFishCompetitonState : ICompetitionState
    {
        public bool specificFish;
        public int fishIDToCatch;
        
        [Client]
        public string AsString()
        {
            if (!specificFish)
            {
                return "Catch the most fishes";
            }
            return $"Catch the most {ItemRegistry.Get(fishIDToCatch).DisplayName}";
        }

        [Client]
        public Sprite Icon()
        {
            if (!specificFish)
            {
                throw new NotImplementedException();
            }
            return ItemRegistry.Get(fishIDToCatch).Icon;
        }
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
        public override bool AddToCompetition(CurrentFish currentFish, PlayerData playerData)
        {
            MostFishCompetitonState state = (MostFishCompetitonState)State;
            if (state.specificFish && state.fishIDToCatch != currentFish.id)
            {
                return false;
            }
            Guid playerId = playerData.GetUuid();
            string playerName = playerData.GetUsername();
            (int _, PlayerResult result) = CompetitionData.GetPlayerResult(playerId);
            int newScore = 0;
            if (result.PlayerID != playerId)
            {
                newScore = result.Result;
            }

            newScore += 1;
            CompetitionData.AddOrUpdateResult(playerId, playerName, newScore);
            return true;
        }
    }

    public class MostFishCompetitionCodec : ICompetitionCodec
    {
        public Type StateType => typeof(MostFishCompetitonState);

        static MostFishCompetitionCodec() {
            CompetitionCodecRegistry.Register(new MostFishCompetitionCodec(), 1);
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