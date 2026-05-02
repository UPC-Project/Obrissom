using Obrissom.Player;
using UnityEngine;

namespace Obrissom.UI
{
    public class PlayerUIManager : MonoBehaviour
    {
        public static PlayerUIManager Instance { get; private set; }

        [SerializeField] private LevelAndXPUI _levelAndXPUI;
        [SerializeField] private PlayerMenu _playerMenu;
        [SerializeField] private SkillCooldownUI _skillCooldownUI;
        [SerializeField] private HealthAndResourceUI _healthAndResourceUI;

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
        public HealthAndResourceUI GetHealthAndResourceUI() => _healthAndResourceUI;

        public void RegisterPlayer(PlayerSkills playerSkills)
        {
            _skillCooldownUI.SetPlayerSkills(playerSkills);
        }
    }
}