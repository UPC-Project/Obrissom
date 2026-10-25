using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    public class PlayerCombat : NetworkBehaviour
    {
        #region
        [Header("Actual health and resource")]
        [Min(0)] public NetworkVariable<float> _health = 
            new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        [Min(0)] public NetworkVariable<float> _resource = 
            new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // Mana / Stamina / Fury, etc

        [Header("Targeting and Invulnerability")]
        public NetworkVariable<bool> _isInvulnerable = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> _isTargetable = new NetworkVariable<bool>(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Components
        private PlayerStats _playerStats;
        private UI.HealthAndResourceUI _healthAndResourceUI;

        #endregion

        // TODO: save information:
        // for example current health, level, bufs, skills available, etc
        // Should be saved in a file. --> Make save system

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
        }

        public override void OnNetworkSpawn()
        {
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

        public void SetInvulnerable(bool value) => _isInvulnerable.Value = value;
        public void SetTargetable(bool value) => _isTargetable.Value = value;
        public bool IsTargetable => _isTargetable.Value && _health.Value > 0f;

        public bool TryConsumeResource(int cost, bool consume = true)
        {
            if (_resource.Value < cost) return false;

            if (IsServer && consume)
            {
                _resource.Value -= cost;
            }

            return true;
        }

        public void TakeDamage(float damageAmount, EffectType damageType)
        {
            if (_isInvulnerable.Value) return;

            if (!IsServer) return;

            float reduction = (damageType == EffectType.PhysicDamage) ? _playerStats.physicalDefense : _playerStats.magicDefense;
            reduction = Mathf.Clamp(reduction, 0f, 0.99f);

            float finalDamage = damageAmount * (1 - reduction);


            _health.Value = Mathf.Max(Mathf.Round(_health.Value - finalDamage), 0f);


            if (_health.Value <= 0)
            {
                Die();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void HealRpc(float healAmount)
        {
            if (!IsServer) return;
            _health.Value = Mathf.Min(_health.Value + healAmount, _playerStats.maxHealth.Value);
        }

        public (int, bool) CalculatePhysicalDamage(int minAttackDamage, int maxAttackDamage)
        {
            int attackDamage = Random.Range(minAttackDamage, maxAttackDamage + 1);
            float damage = (attackDamage + _playerStats.bonusPhysicalAttack) * _playerStats.physicalAttackMultiplier;
            bool isCritic = Random.value < _playerStats.criticalChance;
            if (isCritic)
            {
                damage *= _playerStats.criticalDamage;
            }
            return (Mathf.RoundToInt(damage), isCritic);
        }

        public (int, bool) CalculateMagicDamage(int minAttackDamage, int maxAttackDamage)
        {
            int attackDamage = Random.Range(minAttackDamage, maxAttackDamage + 1);
            float damage = (attackDamage + _playerStats.bonusMagicAttack) * _playerStats.magicAttackMultiplier;
            bool isCritic = Random.value < _playerStats.criticalChance;
            if (isCritic)
            {
                damage *= _playerStats.criticalDamage;
            }
            return (Mathf.RoundToInt(damage), isCritic);
        }

        public (int, bool) CalculateHeal(int minHeal, int maxHeal)
        {
            int baseHeal = Random.Range(minHeal, maxHeal + 1);
            float heal = (baseHeal + _playerStats.bonusHeal) * _playerStats.magicAttackMultiplier;
            bool isCritic = Random.value < _playerStats.criticalChance;
            if (isCritic)
            {
                heal *= _playerStats.criticalDamage;
            }
            return (Mathf.RoundToInt(heal), isCritic);
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

