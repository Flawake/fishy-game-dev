using System;
using ItemSystem;
using Mirror;
using UnityEngine;

namespace GlobalCompetitionSystem
{
    public class largestFishCompetitonState : ICompetitionState
    {
        public bool specificFish;
        public int fishIDToCatch;
        
        [Client]
        public string AsString()
        {
            if (!specificFish)
            {
                return "Catch the biggest fish";
            }
            return $"Catch the biggest {ItemRegistry.Get(fishIDToCatch).DisplayName}";
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
    
    public class LargestFishCompetition : CurrentCompetition<CurrentFish>
    {
        public ICompetitionState State { get; set; }

        public override void SetState(ICompetitionState state)
        {
            if (state is not largestFishCompetitonState competitionState)
            {
                throw new InvalidOperationException($"State must be a {typeof(largestFishCompetitonState)} at this point");
            }
            State = competitionState;
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
            int newScore = currentFish.length;
            if (result != null && result.Result > newScore)
            {
                return false;
            }
            
            CompetitionData.AddOrUpdateResult(playerId, playerName, newScore);
            return true;
        }
    }

    public class LargestFishCompetitionCodec : ICompetitionCodec
    {
        public Type StateType => typeof(MostFishCompetitonState);

        static LargestFishCompetitionCodec() {
            CompetitionCodecRegistry.Register(new LargestFishCompetitionCodec(), 3);
        }

        public void Write(NetworkWriter writer, ICompetitionState genericState) {
            largestFishCompetitonState state = (largestFishCompetitonState)genericState;
            writer.WriteBool(state.specificFish);
            writer.WriteInt(state.fishIDToCatch);
        }

        public ICompetitionState Read(NetworkReader reader) {
            return new largestFishCompetitonState { specificFish = reader.ReadBool(), fishIDToCatch = reader.ReadInt() };
        }
    }
}