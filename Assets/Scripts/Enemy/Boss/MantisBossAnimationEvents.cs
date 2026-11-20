using UnityEngine;

namespace Obrissom.Enemy
{
    public class MantisBossAnimationEvents : MonoBehaviour
    {
        private MantisBoss _boss;

        private void Awake()
        {
            _boss = GetComponentInParent<MantisBoss>();
        }

        public void MeleeHitboxesOn()
        {
            if (_boss != null) _boss.SetMeleeHitboxes(true);
        }

        public void MeleeHitboxesOff()
        {
            if (_boss != null) _boss.SetMeleeHitboxes(false);
        }

        public void CastRoots()
        {
            if (_boss != null) _boss.CastRoots();
        }
    }
}
