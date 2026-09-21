using UnityEngine;

public class QuestsMenu : MonoBehaviour
{
    public bool isQuestsMenuOpen = false;
    [SerializeField] private GameObject _questsMenu;
    public void SetQuestsMenuState(bool state)
    {
        _questsMenu.SetActive(state);
        isQuestsMenuOpen = state;
    }
}
