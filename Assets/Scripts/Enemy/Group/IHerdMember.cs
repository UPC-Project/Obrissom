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

        /// <summary>True while fighting. Engaged members ignore herd alerts.</summary>
        bool IsEngaged { get; }

        /// <summary>Called by EnemyHerd.AddMember.</summary>
        void SetHerd(EnemyHerd herd);

        /// <summary>Another member spotted a target. React after the given delay so the herd doesn't aggro in sync.</summary>
        void OnHerdAlert(Transform target, float delay);
    }
}
