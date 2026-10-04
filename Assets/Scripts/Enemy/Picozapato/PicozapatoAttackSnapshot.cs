using System;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    public enum PicozapatoAttackPhase : byte
    {
        Windup,     // Animation started
        Telegraph,  // Area only: circle on the ground, target position locked
        Impact      // Damage resolved on the server
    }

 
    public struct PicozapatoAttackSnapshot : INetworkSerializable, IEquatable<PicozapatoAttackSnapshot>
    {
        /// <summary>Increments every attack, so two identical consecutive attacks are still distinct.</summary>
        public byte Sequence;
        public PicozapatoAttackKind Kind;
        public PicozapatoAttackPhase Phase;

        /// <summary>Area center on the ground. Only meaningful for Telegraph / Impact.</summary>
        public Vector3 Center;

        /// <summary>Server time (NetworkManager.ServerTime) when the phase started. Clients derive progress from it.</summary>
        public double PhaseStartTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Kind);
            serializer.SerializeValue(ref Phase);
            serializer.SerializeValue(ref Center);
            serializer.SerializeValue(ref PhaseStartTime);
        }

        public bool Equals(PicozapatoAttackSnapshot other) =>
            Sequence == other.Sequence
            && Kind == other.Kind
            && Phase == other.Phase
            && Center == other.Center
            && PhaseStartTime.Equals(other.PhaseStartTime);

        public override bool Equals(object obj) => obj is PicozapatoAttackSnapshot other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Sequence, Kind, Phase, Center, PhaseStartTime);
    }
}
