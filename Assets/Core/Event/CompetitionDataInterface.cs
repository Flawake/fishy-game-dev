using System;
using Mirror;
using UnityEngine;

namespace GlobalCompetitionSystem
{
    /// <summary>
    /// Marker interface - Anything that implements ICompetitionState is considered part of the mutable data needed to make a competition.
    /// </summary>
    public interface ICompetitionState
    {
        [Client]
        public string AsString();
        [Client]
        public Sprite Icon();
    }
    
    public interface ICompetitionCodec
    {
        Type StateType { get; }
        
        void Write(NetworkWriter writer, ICompetitionState state);
        ICompetitionState Read(NetworkReader reader);
    }
}
