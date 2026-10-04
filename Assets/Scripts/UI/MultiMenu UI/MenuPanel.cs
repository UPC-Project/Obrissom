using UnityEngine;

/// <summary>
/// Base class for all menus managed by MultiMenuController.
/// </summary>
public abstract class MenuPanel : MonoBehaviour
{
    [SerializeField] protected GameObject _menuPanel;

    public bool IsOpen { get; private set; }

    public virtual void SetMenuState(bool state)
    {
        _menuPanel.SetActive(state);
        IsOpen = state;
    }
}
