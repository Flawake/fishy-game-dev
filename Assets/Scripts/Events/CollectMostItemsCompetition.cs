using System;
using ItemSystem;
using Mirror;
using UnityEngine;

namespace GlobalCompetitionSystem
{
    public class MostItemsCompetitonState : ICompetitionState
    {
        public int ItemId;

        [Client]
        public string AsString()
        {
            return $"Collect the most {ItemRegistry.Get(ItemId).DisplayName}";
        }

        [Client]
        public Sprite Icon()
        {
            return ItemRegistry.Get(ItemId).Icon;
        }
    }
    
    public class MostItemsCompetition : CurrentCompetition<ItemInstance>
    {
        public ICompetitionState State { get; set; }

        public override void SetState(ICompetitionState state)
        {
            if (state is not MostItemsCompetitonState)
            {
                throw new InvalidOperationException($"State must be a {typeof(MostItemsCompetitonState)} at this point");
            }
            State = (MostItemsCompetitonState)state;
        }

        [Server]
        public override bool AddToCompetition(ItemInstance collectedItem, PlayerData playerData)
        {
            MostItemsCompetitonState state = (MostItemsCompetitonState)State;
            if (collectedItem.def.Id != state.ItemId)
            {
                return false;
            }
            Guid playerId = playerData.GetUuid();
            string playerName = playerData.GetUsername();
            (int _, PlayerResult result) = CompetitionData.GetPlayerResult(playerId);
            int newScore = 0;
            if (result != null)
            {
                newScore = result.Result;
            }

            newScore += 1;
            CompetitionData.AddOrUpdateResult(playerId, playerName, newScore);
            return true;
        }
    }

    public class MostItemsCompetitionCodec : ICompetitionCodec
    {
        public Type StateType => typeof(MostItemsCompetitonState);

        static MostItemsCompetitionCodec() {
            CompetitionCodecRegistry.Register(new MostItemsCompetitionCodec(), 2);
        }

        public void Write(NetworkWriter writer, ICompetitionState genericState) {
            MostItemsCompetitonState state = (MostItemsCompetitonState)genericState;
            writer.WriteInt(state.ItemId);
        }

        public ICompetitionState Read(NetworkReader reader) {
            return new MostItemsCompetitonState { ItemId = reader.ReadInt() };
        }
    }
}