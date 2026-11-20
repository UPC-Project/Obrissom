using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    /// <summary>
    /// Stun from enemies: the player can't move, use skills or interact for a while.
    /// The server applies it, the owner blocks its own input and everyone sees the effect.
    /// </summary>
    [RequireComponent(typeof(PlayerCombat))]
    public class PlayerStun : NetworkBehaviour
    {
        [Tooltip("Effect shown over the head while stunned.")]
        [SerializeField] private ParticleSystem _stunEffectPrefab;
        [Tooltip("Height of the effect over the player.")]
        [SerializeField] private float _effectHeight = 2.2f;

        private readonly NetworkVariable<bool> _isStunned = new NetworkVariable<bool>(false);

        private PlayerCombat _playerCombat;
        private PlayerSkills _playerSkills;
        private PlayerLocomotionInput _locomotionInput;
        private PlayerCombatInput _combatInput;
        private PlayerInteraction _interaction;
        private ParticleSystem _stunEffect;

        // Server only
        private float _stunEndTime;
        private Coroutine _stunRoutine;

        public bool IsStunned => _isStunned.Value;

        private void Awake()
        {
            _playerCombat = GetComponent<PlayerCombat>();
            _playerSkills = GetComponent<PlayerSkills>();
            _locomotionInput = GetComponent<PlayerLocomotionInput>();
            _combatInput = GetComponent<PlayerCombatInput>();
            _interaction = GetComponent<PlayerInteraction>();
        }

        public override void OnNetworkSpawn()
        {
            _isStunned.OnValueChanged += OnStunnedChanged;
            if (_isStunned.Value) ShowEffect(true);

            if (IsServer) _playerCombat._isDead.OnValueChanged += OnDeadChanged;
        }

        public override void OnNetworkDespawn()
        {
            _isStunned.OnValueChanged -= OnStunnedChanged;
            if (IsServer) _playerCombat._isDead.OnValueChanged -= OnDeadChanged;
        }

        /// Server only. A longer stun replaces a shorter one.
        public void ApplyStun(float duration)
        {
            if (!IsServer || duration <= 0f || _playerCombat.IsDead) return;

            _stunEndTime = Mathf.Max(_stunEndTime, Time.time + duration);
            _isStunned.Value = true;
            if (_stunRoutine == null) _stunRoutine = StartCoroutine(StunRoutine());
        }

        private IEnumerator StunRoutine()
        {
            while (Time.time < _stunEndTime) yield return null;

            _stunRoutine = null;
            EndStun();
        }

        // Server
        private void EndStun()
        {
            if (_stunRoutine != null) StopCoroutine(_stunRoutine);
            _stunRoutine = null;
            _stunEndTime = 0f;
            _isStunned.Value = false;
        }

        // Server. Going down ends the stun
        private void OnDeadChanged(bool previous, bool isDead)
        {
            if (isDead && _isStunned.Value) EndStun();
        }

        private void OnStunnedChanged(bool previous, bool stunned)
        {
            ShowEffect(stunned);
            if (IsOwner) SetInputBlocked(stunned);
        }

        // Owner. A skill that gives the input back early can't break the stun
        private void Update()
        {
            if (!IsOwner || !_isStunned.Value) return;
            if (_locomotionInput.enabled || _combatInput.enabled || _interaction.enabled) SetInputBlocked(true);
        }

        // Owner. Disabling the input also stops the movement
        private void SetInputBlocked(bool blocked)
        {
            if (blocked) _playerSkills.CancelActiveSkill();
            // Downed players keep their input off
            else if (_playerCombat.IsDead) return;

            _locomotionInput.enabled = !blocked;
            _combatInput.enabled = !blocked;
            _interaction.enabled = !blocked;
        }

        private void ShowEffect(bool show)
        {
            if (_stunEffectPrefab == null) return;

            if (_stunEffect == null)
            {
                if (!show) return;
                _stunEffect = Instantiate(_stunEffectPrefab, transform);
                _stunEffect.transform.localPosition = Vector3.up * _effectHeight;
            }

            if (show) _stunEffect.Play(true);
            else _stunEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
