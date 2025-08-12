using System;
using System.Collections.Generic;

namespace GlobalCompetitionSystem
{
    public static class CompetitionStateRegistry
    {
        private static Dictionary<ICompetitionState, CurrentCompetition> stateToCompetition;

        static CompetitionStateRegistry()
        {
            Register(new MostFishCompetitonState(), new MostFishCompetition());
            Register(new MostItemsCompetitonState(), new MostItemsCompetition());
        }

        public static void Register(ICompetitionState competitionState, CurrentCompetition competitionImplementation)
        {
            if (stateToCompetition.ContainsKey(competitionState))
            {
                if (stateToCompetition[competitionState] != competitionImplementation)
                {
                    throw new InvalidOperationException($"State {competitionState} already registered with a different implementation ({competitionImplementation})");
                }
                stateToCompetition.Add(competitionState, competitionImplementation);
            }
        }

        public static CurrentCompetition GetImplementation(ICompetitionState competitionState)
        {
            if (!stateToCompetition.TryGetValue(competitionState, out CurrentCompetition competitionImplementation))
            {
                throw new InvalidOperationException($"Competition state {competitionState} has not been registered with an implementation.");
            }
            return competitionImplementation;
        }
    }
    public static class CompetitionCodecRegistry
    {
        private static Dictionary<Type, ushort> typeToId = new();
        private static Dictionary<ushort, ICompetitionCodec> idToCodec = new();

        static CompetitionCodecRegistry()
        {
            Register(new MostFishCompetitionCodec(), 1);
            Register(new MostItemsCompetitionCodec(), 2);
            Register(new LargestFishCompetitionCodec(), 2);
        }
        
        // Register a codec with a hardcoded ID
        public static void Register(ICompetitionCodec codec, ushort id)
        {
            Type type = codec.StateType;
            if (typeToId.ContainsKey(type))
            {
                if (typeToId[type] != id)
                {
                    throw new InvalidOperationException($"Type {type} already registered with a different ID ({typeToId[type]} != {id})");
                }
                return;
            }

            if (idToCodec.TryGetValue(id, out var value))
            {
                throw new InvalidOperationException($"ID {id} already registered for type {value.StateType}");
            }
            typeToId[type] = id;
            idToCodec[id] = codec;
        }

        public static ushort GetId(Type t)
        {
            if (!typeToId.TryGetValue(t, out ushort id)) {
                throw new InvalidOperationException($"Competition type {t} has not been registered with a codec.");
            }
            return id;
        }

        public static ICompetitionCodec GetCodec(ushort id)
        {
            if (!idToCodec.TryGetValue(id, out var codec)) {
                throw new InvalidOperationException($"No codec registered for state id {id}");
            }
            return codec;
        }
    }
}