using UnityEngine;

namespace Obrissom.UI
{
    public class DPSUIManager : MonoBehaviour
    {
        [SerializeField] private GameObject _crosshair;
        [SerializeField] private GameObject _teleportCircle;
        [SerializeField] private GameObject _magicDamageRing;

        // Each player prefab has its own manager: get the one of this player
        public static DPSUIManager Of(GameObject player) => player.GetComponentInChildren<DPSUIManager>(true);

        public GameObject GetCrosshair() => _crosshair;
        public GameObject GetTeleportCircle() => _teleportCircle;
        public GameObject GetMagicDamageRing() => _magicDamageRing;
    }
}