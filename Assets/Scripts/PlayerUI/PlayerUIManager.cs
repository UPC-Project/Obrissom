using Obrissom.Player;
using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    [SerializeField] private LevelAndXPUI _levelAndXPUI;
    [SerializeField] private PlayerMenu _playerMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public LevelAndXPUI GetLevelAndXPUI() => _levelAndXPUI;
    public PlayerMenu GetPlayerMenu() => _playerMenu;
}