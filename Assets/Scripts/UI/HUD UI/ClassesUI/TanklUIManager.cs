using UnityEngine;

namespace Obrissom.UI
{
    public class TankUIManager : MonoBehaviour
    {
        public static TankUIManager Instance { get; private set; }

        [SerializeField] private GameObject _tauntCircle;
        [SerializeField] private GameObject _dashArea;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public GameObject GetTauntCircle() => _tauntCircle;
        public GameObject GetDashArea() => _dashArea;
    }
}
