using System;
using Mirror;
using UnityEngine;

namespace GlobalCompetitionSystem
{
    public static class CompetitionPacker
    {
        public static byte[] Pack(Type eventType, ICompetitionState state)
        {
            NetworkWriterPooled writer = NetworkWriterPool.Get();
            try
            {
                ushort id = CompetitionCodecRegistry.GetId(eventType);
                writer.WriteUShort(id);
                
                // Also write the custom state
                NetworkWriterPooled sub = NetworkWriterPool.Get();
                try
                {
                    CompetitionCodecRegistry.GetCodec(id).Write(sub, state);
                    writer.WriteArraySegmentAndSize(sub.ToArraySegment());
                }
                finally
                {
                    NetworkWriterPool.Return(sub);
                }
                return writer.ToArraySegment().ToArray();
            }
            finally
            {
                NetworkWriterPool.Return(writer);
            }
        }

        public static ICompetitionState UnpackInto(byte[] data)
        {
            ICompetitionState state;
            NetworkReaderPooled reader = NetworkReaderPool.Get(data);
            try
            {
                ushort id = reader.ReadUShort();
                byte[] blob = reader.ReadBytesAndSize();
                ICompetitionCodec codec = CompetitionCodecRegistry.GetCodec(id);
                NetworkReaderPooled sub = NetworkReaderPool.Get(blob);
                try
                {
                    state = codec.Read(sub);
                }
                finally
                {
                    NetworkReaderPool.Return(sub);
                }
            }
            finally
            {
                NetworkReaderPool.Return(reader);
            }
            return state;
        }
    }
}
