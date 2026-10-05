using System;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    public enum ShoebillAttackPhase : byte
    {
        Windup,
        Telegraph,
        Impact
    }

    /// <summary>Area attack state synced to clients.</summary>
    public struct ShoebillAttackSnapshot : INetworkSerializable, IEquatable<ShoebillAttackSnapshot>
    {
        public byte Sequence; // +1 every attack
        public ShoebillAttackKind Kind;
        public ShoebillAttackPhase Phase;
        public Vector3 Center;
        public double PhaseStartTime; // Server time

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Kind);
            serializer.SerializeValue(ref Phase);
            serializer.SerializeValue(ref Center);
            serializer.SerializeValue(ref PhaseStartTime);
        }

        public bool Equals(ShoebillAttackSnapshot other) =>
            Sequence == other.Sequence
            && Kind == other.Kind
            && Phase == other.Phase
            && Center == other.Center
            && PhaseStartTime.Equals(other.PhaseStartTime);

        public override bool Equals(object obj) => obj is ShoebillAttackSnapshot other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Sequence, Kind, Phase, Center, PhaseStartTime);
    }
}
