using System;
using System.Collections.Generic;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class BossDamageTrigger : MonoBehaviour
    {
        [SerializeField] private float _damage = 3f;
        [SerializeField] private EffectType _damageType = EffectType.PhysicDamage;

        [Tooltip("Layers that take damage.")]
        [SerializeField] private LayerMask _targetLayers = 1 << 6;

        private readonly HashSet<PlayerCombat> _hitPlayers = new HashSet<PlayerCombat>();
        private Collider _collider;

        public Func<PlayerCombat, bool> CanHit { get; set; }

        public float Damage
        {
            get => _damage;
            set => _damage = value;
        }

        public bool IsActive => _collider != null && _collider.enabled;

        private static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void OnDisable() => _hitPlayers.Clear();

        private void FixedUpdate()
        {
            if (!_collider.enabled && _hitPlayers.Count > 0) _hitPlayers.Clear();
        }

        public void SetActive(bool active)
        {
            _collider.enabled = active;
            if (!active) _hitPlayers.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger || (_targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

            ApplyDamage(other.GetComponentInParent<PlayerCombat>());
        }

        public bool ApplyDamage(PlayerCombat player)
        {
            if (!IsServer || player == null || !player.IsTargetable) return false;
            if (_hitPlayers.Contains(player)) return false;
            if (CanHit != null && !CanHit(player)) return false;

            _hitPlayers.Add(player);
            player.TakeDamage(_damage, _damageType);
            return true;
        }
    }
}
