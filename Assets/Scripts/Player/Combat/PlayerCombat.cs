using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerCombat : NetworkBehaviour
    {
        #region
        [Header("Actual health and resource")]
        [Min(0)] public NetworkVariable<float> _health = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        [Min(0)] public NetworkVariable<float> _resource = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // Mana / Stamina / Fury, etc

        [Header("Player Skills")] // TODO: delete
        [SerializeField] private Skill _basicSkill;
        [SerializeField] private Skill _Skill1; // TODO: Will change (not hardcoded) -> gained by leveling up
        [SerializeField] private Skill _Skill2; // TODO: Will change (not hardcoded) -> gained by leveling up

        private PlayerStats _playerStats;
        private PlayerSkills _playerSkills;
        private UI.HealthAndResourceUI _healthAndResourceUI;

        private bool _isInvulnerable = false;

        #endregion

        // TODO: save information:
        // for example current health, level, bufs, skills available, etc
        // Should be saved in a file. --> Make save system


        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            _playerSkills = GetComponent<PlayerSkills>();
        }
        public override void OnNetworkSpawn()
        {
            // Assign basic skill -> will change, what happen when player choose another button?
            _playerSkills.AssignSkill(SkillKey.LB, _basicSkill);
            _playerSkills.AssignSkill(SkillKey.ONE, _Skill1);
            _playerSkills.AssignSkill(SkillKey.TWO, _Skill2);

            if (IsServer)
            {
                _resource.Value = _playerStats.maxResource.Value;
                _health.Value = _playerStats.maxHealth.Value;
            }

            if (IsOwner)
            {
                _healthAndResourceUI = UI.PlayerUIManager.Instance.GetHealthAndResourceUI();
                _healthAndResourceUI.UpdateHealth(_health.Value, _playerStats.maxHealth.Value);
                _healthAndResourceUI.UpdateResource(_resource.Value, _playerStats.maxResource.Value);

                _health.OnValueChanged += OnHealthChanged;
                _playerStats.maxHealth.OnValueChanged += OnHealthChanged;
                _resource.OnValueChanged += OnResourceChanged;
                _playerStats.maxResource.OnValueChanged += OnResourceChanged;
            }

        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _health.OnValueChanged -= OnHealthChanged;
                _resource.OnValueChanged -= OnResourceChanged;
            }
        }
        private void OnHealthChanged(float previousValue, float newValue)
        {
            _healthAndResourceUI.UpdateHealth(newValue, _playerStats.maxHealth.Value);
        }

        private void OnResourceChanged(float previousValue, float newValue)
        {
            _healthAndResourceUI.UpdateResource(newValue, _playerStats.maxResource.Value);
        }

        public void Update()
        {

            if (!IsServer) return;

            if (_resource.Value < _playerStats.maxResource.Value)
            {
                _resource.Value = Mathf.Min(_resource.Value + _playerStats.resourceRegen * Time.deltaTime, _playerStats.maxResource.Value);
            }

            if (_health.Value < _playerStats.maxHealth.Value)
            {
                _health.Value = Mathf.Min(_health.Value + _playerStats.healthRegen * Time.deltaTime, _playerStats.maxHealth.Value);
            }
        }

        public void SetInvulnerable(bool value) => _isInvulnerable = value;

        public bool TryConsumeResource(int cost)
        {
            if (_resource.Value < cost) return false;

            if (IsServer)
            {
                _resource.Value -= cost;
            }

            return true;
        }

        public void TakeDamage(float damageAmount, DamageType damageType)
        {
            if (_isInvulnerable) return;

            if (!IsServer) return;

            float reduction = (damageType == DamageType.PhysicDamage) ? _playerStats.physicalDefense : _playerStats.magicDefense;
            reduction = Mathf.Clamp(reduction, 0f, 0.99f);

            float finalDamage = damageAmount * (1 - reduction);


            _health.Value = Mathf.Max(Mathf.Round(_health.Value - finalDamage), 0f);


            Debug.Log($"Player took {finalDamage} damage. Remaining health: {_health.Value}");
            if (_health.Value <= 0)
            {
                Die();
            }
        }

        public (int, bool) CalculatePhysicalDamage(int minAttackDamage, int maxAttackDamage)
        {
            int attackDamage = Random.Range(minAttackDamage, maxAttackDamage);
            float damage = (attackDamage + _playerStats.bonusPhysicalAttack) * _playerStats.physicalAttackMultiplier;
            bool isCritical = Random.value < _playerStats.criticalChance;
            if (isCritical)
            {
                damage *= _playerStats.criticalDamage;
            }
            return (Mathf.RoundToInt(damage), isCritical);
        }

        public (int, bool) CalculateMagicDamage(int minAttackDamage, int maxAttackDamage)
        {
            int attackDamage = Random.Range(minAttackDamage, maxAttackDamage);
            float damage = (attackDamage + _playerStats.bonusMagicAttack) * _playerStats.magicAttackMultiplier;
            bool isCritical = Random.value < _playerStats.criticalChance;
            if (isCritical)
            {
                damage *= _playerStats.criticalDamage;
            }
            return (Mathf.RoundToInt(damage), isCritical);
        }

        public Vector3 GetAimPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 100f)) return hit.point;

            // fixed distance
            return ray.origin + ray.direction * 100f;
        }

        private void Die()
        {
            Debug.Log($"Player {OwnerClientId} has died.");
            // TODO
        }
    }

}