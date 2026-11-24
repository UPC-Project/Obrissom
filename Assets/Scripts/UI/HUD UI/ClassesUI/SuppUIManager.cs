using UnityEngine;

namespace Obrissom.UI
{
    public class SuppUIManager : MonoBehaviour
    {
        [SerializeField] private GameObject _healCircle;
        [SerializeField] private GameObject _crosshair;

        // Each player prefab has its own manager: get the one of this player
        public static SuppUIManager Of(GameObject player) => player.GetComponentInChildren<SuppUIManager>(true);

        public GameObject GetHealCircle() => _healCircle;
        public GameObject GetCrosshair() => _crosshair;
    }
}
