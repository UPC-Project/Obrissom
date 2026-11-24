using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Obrissom.UI;

namespace Obrissom.Player
{
    public class PlayerCombat : NetworkBehaviour
    {
        #region
        [Header("Actual health and resource")]
        [Min(0)]
        public NetworkVariable<float> _health =
            new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        [Min(0)]
        public NetworkVariable<float> _resource =
            new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // Mana / Stamina / Fury, etc

        [Header("Targeting and Invulnerability")]
        public NetworkVariable<bool> _isInvulnerable = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> _isTargetable = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [Header("Identity")]
        public NetworkVariable<FixedString64Bytes> _playerName =
            new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<PlayerClass> _playerClass =
            new(PlayerClass.Tank, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Death")]
        public NetworkVariable<bool> _isDead = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Components
        private PlayerRespawn _playerRespawn;
        private PlayerStats _playerStats;
        public PlayerStats Stats => _playerStats;
        private UI.HealthAndResourceUI _healthAndResourceUI;
        protected PlayerHealPopUp _healPopUp;
        private Vignette _vignette;
        private TankUIManager _tankUi;
        private SuppUIManager _suppUi;

        #endregion

        // TODO: save information:
        // for example current health, level, bufs, skills available, etc
        // Should be saved in a file. --> Make save system

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            _healPopUp = GetComponent<PlayerHealPopUp>();
            _playerRespawn = GetComponent<PlayerRespawn>();
            _tankUi = TankUIManager.Of(gameObject);
            _suppUi = SuppUIManager.Of(gameObject);
            Volume camVolume = GetComponentInChildren<Volume>();
            camVolume.profile.TryGet(out _vignette);
        }

        public void SetPlayerIdentity(string playerName, PlayerClass playerClass)
        {
            _playerName.Value = playerName ?? "";
            _playerClass.Value = playerClass;
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
                UpdateLowHealthVignette();

                _health.OnValueChanged += OnHealthChanged;
                _playerStats.maxHealth.OnValueChanged += OnHealthChanged;
                _resource.OnValueChanged += OnResourceChanged;
                _playerStats.maxResource.OnValueChanged += OnResourceChanged;
            }
            else
            {
                UI.AlliesHealth.Instance?.RegisterAlly(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _health.OnValueChanged -= OnHealthChanged;
                _resource.OnValueChanged -= OnResourceChanged;
            }
            else
            {
                UI.AlliesHealth.Instance?.UnregisterAlly(this);
            }
        }
        private void OnHealthChanged(float previousValue, float newValue)
        {
            _healthAndResourceUI.UpdateHealth(newValue, _playerStats.maxHealth.Value);
            UpdateLowHealthVignette();
        }

        private void UpdateLowHealthVignette()
        {
            float maxHealth = _playerStats.maxHealth.Value;
            float healthPercent = maxHealth > 0f ? _health.Value / maxHealth : 0f;

            if (healthPercent > 0.5f)
            {
                _vignette.intensity.value = 0f;
            }
            else if (healthPercent < 0.2f)
            {
                _vignette.intensity.value = 0.7f;
            }
            else
            {
                // normalize
                float t = Mathf.InverseLerp(0.5f, 0.2f, healthPercent);
                // exponential
                float curvedT = Mathf.Pow(t, 2f);
                // range between  0-0.4
                _vignette.intensity.value = curvedT * 0.4f;
            }
        }

        private void OnResourceChanged(float previousValue, float newValue)
        {
            _healthAndResourceUI.UpdateResource(newValue, _playerStats.maxResource.Value);
        }

        public void Update()
        {

            if (!IsServer || _isDead.Value) return;

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
        public bool IsTargetable => _isTargetable.Value && !_isDead.Value && _health.Value > 0f;
        public bool IsDead => _isDead.Value;

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
            if (_isInvulnerable.Value || _isDead.Value) return;

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
        public void HealRpc(float healAmount, bool isCritic)
        {
            if (!IsServer || _isDead.Value) return;
            _health.Value = Mathf.Min(_health.Value + healAmount, _playerStats.maxHealth.Value);

            _healPopUp.ShowPopUpClientRpc(healAmount.ToString(), isCritic);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RestoreResourceRpc(float amount)
        {
            if (!IsServer || _isDead.Value) return;
            _resource.Value = Mathf.Min(_resource.Value + amount, _playerStats.maxResource.Value);
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

        private static readonly RaycastHit[] s_aimHits = new RaycastHit[16];

        public Vector3 GetAimPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            int hitCount = Physics.RaycastNonAlloc(ray, s_aimHits, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            Vector3 closestPoint = ray.origin + ray.direction * 100f;

            for (int i = 0; i < hitCount; i++)
            {
                if (s_aimHits[i].collider.transform.root == transform.root) continue;

                if (s_aimHits[i].distance < closestDistance)
                {
                    closestDistance = s_aimHits[i].distance;
                    closestPoint = s_aimHits[i].point;
                }
            }

            return closestPoint;
        }

        // Server only. Resting at a bonfire
        public void RestoreHealthAndResource()
        {
            if (!IsServer || _isDead.Value) return;

            _health.Value = _playerStats.maxHealth.Value;
            _resource.Value = _playerStats.maxResource.Value;
        }

        public void Revive(float fraction)
        {
            if (!IsServer || !_isDead.Value) return;

            _health.Value = Mathf.Max(1f, Mathf.Round(_playerStats.maxHealth.Value * fraction));
            _resource.Value = _playerStats.maxResource.Value * fraction;
            _isDead.Value = false;
        }

        private void Die()
        {
            _isDead.Value = true;

            if (_playerRespawn != null) _playerRespawn.OnDowned();
            else Debug.LogWarning($"[PlayerCombat] {name} has no PlayerRespawn, the player will stay dead.");
        }

        // SKILL AREAS: the owner shows them and the other players see them too

        // Owner only. A zero scale keeps the current size.
        public void SetSkillArea(SkillArea area, bool visible, Vector3 scale = default)
        {
            ApplySkillArea(area, visible, scale);
            if (IsSpawned && IsOwner) SetSkillAreaServerRpc(area, visible, scale);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetSkillAreaServerRpc(SkillArea area, bool visible, Vector3 scale) => SetSkillAreaForOthersRpc(area, visible, scale);

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
        private void SetSkillAreaForOthersRpc(SkillArea area, bool visible, Vector3 scale) => ApplySkillArea(area, visible, scale);

        private void ApplySkillArea(SkillArea area, bool visible, Vector3 scale)
        {
            GameObject target = GetSkillArea(area);
            if (target == null) return;

            if (scale != Vector3.zero) target.transform.localScale = scale;
            target.SetActive(visible);
        }

        // The area object of this player, from its own class UI manager
        public GameObject GetSkillArea(SkillArea area)
        {
            switch (area)
            {
                case SkillArea.TauntCircle: return _tankUi != null ? _tankUi.GetTauntCircle() : null;
                case SkillArea.DashArea: return _tankUi != null ? _tankUi.GetDashArea() : null;
                case SkillArea.HealCircle: return _suppUi != null ? _suppUi.GetHealCircle() : null;
                default: return null;
            }
        }
    }

    // Skill visuals that other players also see (PlayerCombat.SetSkillArea)
    public enum SkillArea : byte { TauntCircle, DashArea, HealCircle }
}

