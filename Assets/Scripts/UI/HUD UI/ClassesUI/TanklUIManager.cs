using UnityEngine;

namespace Obrissom.UI
{
    public class TankUIManager : MonoBehaviour
    {
        [SerializeField] private GameObject _tauntCircle;
        [SerializeField] private GameObject _dashArea;

        // Each player prefab has its own manager: get the one of this player
        public static TankUIManager Of(GameObject player) => player.GetComponentInChildren<TankUIManager>(true);

        public GameObject GetTauntCircle() => _tauntCircle;
        public GameObject GetDashArea() => _dashArea;
    }
}
