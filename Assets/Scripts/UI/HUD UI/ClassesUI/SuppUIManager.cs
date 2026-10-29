using UnityEngine;

namespace Obrissom.UI
{
    public class SuppUIManager : MonoBehaviour
    {
        public static SuppUIManager Instance { get; private set; }

        [SerializeField] private GameObject _healCircle;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public GameObject GetHealCircle() => _healCircle;
    }
}
