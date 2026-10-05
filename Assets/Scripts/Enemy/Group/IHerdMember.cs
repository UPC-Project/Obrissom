using UnityEngine;

namespace Obrissom.Enemy
{
    /// <summary>An enemy that can be part of an EnemyHerd.</summary>
    public interface IHerdMember
    {
        Transform transform { get; }

        float HerdSpacing { get; }

        // True while fighting
        bool IsEngaged { get; }

        void SetHerd(EnemyHerd herd);

        // React to the target after the delay
        void OnHerdAlert(Transform target, float delay);
    }
}
