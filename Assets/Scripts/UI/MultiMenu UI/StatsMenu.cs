using UnityEngine;

public class StatsMenu : MonoBehaviour
{
    public bool isStatsMenuOpen = false;
    [SerializeField] private GameObject _statsMenu;
    public void SetStatsMenuState(bool state)
    {
        _statsMenu.SetActive(state);
        isStatsMenuOpen = state;
    }
}
