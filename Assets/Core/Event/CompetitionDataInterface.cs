using System;
using Mirror;

namespace GlobalCompetitionSystem
{
    /// <summary>
    /// Marker interface - Anything that implements ICompetitionState is considered part of the mutable data needed to make a competition.
    /// </summary>
    public interface ICompetitionState {}
    
    public interface ICompetitionCodec
    {
        Type StateType { get; }
        void Write(NetworkWriter writer, ICompetitionState state);
        ICompetitionState Read(NetworkReader reader);
    }
}
