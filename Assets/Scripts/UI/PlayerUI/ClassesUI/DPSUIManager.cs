using UnityEngine;

namespace Obrissom.UI
{
    public class DPSUIManager : MonoBehaviour
    {
        public static DPSUIManager Instance { get; private set; }

        [SerializeField] private GameObject _crosshair;
        [SerializeField] private GameObject _teleportCircle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public GameObject GetCrosshair() => _crosshair;
        public GameObject GetTeleportCircle() => _teleportCircle;
    }
}