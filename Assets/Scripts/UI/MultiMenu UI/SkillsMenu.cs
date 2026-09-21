using UnityEngine;

public class SkillsMenu : MonoBehaviour
{
    public bool isSkillMenuOpen = false;
    [SerializeField] private GameObject _skillMenu;
    public void SetSkillMenuState(bool state)
    {
        _skillMenu.SetActive(state);
        isSkillMenuOpen = state;
    }
}
