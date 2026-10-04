using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>
    /// An enemy that can roam inside an EnemyHerd area. Implemented by enemy types that support group behaviour.
    /// </summary>
    public interface IHerdMember
    {
        Transform transform { get; }

        /// <summary>Minimum distance between this member's destination and the other members.</summary>
        float HerdSpacing { get; }

        /// <summary>Called by EnemyHerd.AddMember.</summary>
        void SetHerd(EnemyHerd herd);
    }
}
