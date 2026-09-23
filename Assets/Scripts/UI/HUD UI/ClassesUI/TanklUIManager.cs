using UnityEngine;

namespace Obrissom.UI
{
    public class TankUIManager : MonoBehaviour
    {
        public static TankUIManager Instance { get; private set; }

        [SerializeField] private GameObject _tauntCircle;

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
    }
}
