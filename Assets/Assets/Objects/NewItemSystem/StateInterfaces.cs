// StateInterfaces.cs
// Interfaces representing runtime state and codecs for the new item system
using Mirror;
using System;

namespace NewItemSystem {
    /// <summary>
    /// Marker interface – any struct/class implementing this is considered part of the
    /// mutable runtime state of an item instance and will be packed into the state blob.
    /// </summary>
    public interface IRuntimeBehaviourState { }

    /// <summary>
    /// Codec capable of serialising/deserialising a specific IRuntimeBehaviourState implementation.
    /// Concrete codecs MUST register themselves in <see cref="StateCodecRegistry"/> so that
    /// the packer can discover them at runtime.
    /// </summary>
    public interface IStateCodec {
        /// <summary>Type of the state this codec can handle.</summary>
        Type StateType { get; }
        void Write(NetworkWriter writer, IRuntimeBehaviourState state);
        IRuntimeBehaviourState Read(NetworkReader reader);
    }
} 