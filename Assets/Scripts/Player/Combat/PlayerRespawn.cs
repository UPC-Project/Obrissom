using System.Collections;
using System.Collections.Generic;
using Obrissom.Environment;
using Obrissom.UI;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    /// Death flow: at 0 health the player goes down and a teammate can revive them for a while.
    /// If nobody does (or the whole party is down), they respawn at their respawn point with full health and resource.
    /// The respawn point starts at the spawn position; bonfires replace it.
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerTeleportService))]
    public class PlayerRespawn : NetworkBehaviour
    {
        public enum RespawnNotice : byte { Downed, Died, Revived, ReviveInterrupted, Rested, EnemiesNearby }

        [Header("Downed")]
        [Tooltip("Seconds a downed player waits for a revive before respawning. Paused while a teammate is reviving them.")]
        [SerializeField] private float _downedDuration = 15f;
        [Tooltip("Delay before respawning when nobody can revive (solo, or the whole party is down).")]
        [SerializeField] private float _noReviveRespawnDelay = 3f;
        [Tooltip("Time after the teleport before the player can be targeted again, so enemies near the death spot can't hit the arrival.")]
        [SerializeField] private float _arrivalGrace = 0.5f;

        [Header("Revive")]
        [Tooltip("Seconds the teammate has to hold the interact key.")]
        [SerializeField] private float _reviveHoldTime = 3f;
        [SerializeField] private float _reviveRange = 2.5f;
        [Tooltip("Health and resource the revived player gets back, as a fraction of max.")]
        [SerializeField, Range(0.1f, 1f)] private float _reviveFraction = 0.5f;

        [Header("Messages")]
        [SerializeField] private string _downedMessage = "You are down. A teammate can revive you";
        [SerializeField] private string _deathMessage = "You died";
        [SerializeField] private string _revivingMessage = "Reviving teammate...";
        [SerializeField] private string _reviveInterruptedMessage = "Revive interrupted";
        [SerializeField] private string _revivedMessage = "You were revived";
        [SerializeField] private string _restedMessage = "You rested. Respawn point set";
        [SerializeField] private string _enemiesNearbyMessage = "You can't rest with enemies nearby";

        // Every player on this peer: used to find downed teammates and to detect a party wipe
        private static readonly List<PlayerRespawn> s_players = new List<PlayerRespawn>();

        private PlayerCombat _playerCombat;
        private PlayerTeleportService _teleportService;
        private PlayerSkills _playerSkills;
        private PlayerLocomotionInput _locomotionInput;
        private PlayerCombatInput _combatInput;
        private PlayerInteraction _interaction;

        // Server only
        private Vector3 _respawnPosition;
        private Quaternion _respawnRotation;
        private Coroutine _downedRoutine;
        private bool _isRespawning;
        private bool _isBeingRevived;
        private Coroutine _reviveRoutine; // this player reviving a teammate
        private PlayerRespawn _reviveTarget;

        // Owner only
        private bool _isHoldingRevive;

        public bool IsDowned => _playerCombat.IsDead;
        private bool CanBeRevived => IsDowned && !_isRespawning && !_isBeingRevived;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_players.Clear();

        private void Awake()
        {
            _playerCombat = GetComponent<PlayerCombat>();
            _teleportService = GetComponent<PlayerTeleportService>();
            _playerSkills = GetComponent<PlayerSkills>();
            _locomotionInput = GetComponent<PlayerLocomotionInput>();
            _combatInput = GetComponent<PlayerCombatInput>();
            _interaction = GetComponent<PlayerInteraction>();
        }

        public override void OnNetworkSpawn()
        {
            s_players.Add(this);

            if (IsServer) SetRespawnPoint(transform.position, transform.rotation);
            if (IsOwner) _playerCombat._isDead.OnValueChanged += OnDeadChanged;
        }

        public override void OnNetworkDespawn()
        {
            s_players.Remove(this);

            if (IsServer) StopReviving(false);
            if (IsOwner) _playerCombat._isDead.OnValueChanged -= OnDeadChanged;
        }

        /// Server only.
        public void SetRespawnPoint(Vector3 position, Quaternion rotation)
        {
            if (!IsServer) return;

            _respawnPosition = position;
            _respawnRotation = rotation;
        }

        // BONFIRES

        /// Owner. Rest at a bonfire in range (interact key).
        public void RequestRest(Bonfire bonfire)
        {
            // Non-owner copies also receive local input
            if (!IsOwner || bonfire == null) return;

            RestRpc(bonfire.BonfireId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RestRpc(string bonfireId)
        {
            Bonfire bonfire = Bonfire.FindById(bonfireId);
            if (bonfire == null || IsDowned || !bonfire.IsInRestRange(transform.position)) return;

            if (bonfire.AreEnemiesNearby())
            {
                NotifyOwnerRpc(RespawnNotice.EnemiesNearby);
                return;
            }

            _playerCombat.RestoreHealthAndResource();
            SetRespawnPoint(bonfire.RespawnPosition, bonfire.RespawnRotation);
            NotifyOwnerRpc(RespawnNotice.Rested);
        }

        // DOWNED (server)

        /// Server only. Called by PlayerCombat when health reaches 0.
        public void OnDowned()
        {
            if (!IsServer) return;

            // A downed player can't keep reviving someone else
            StopReviving(false);

            if (AnyPlayerAlive())
            {
                _downedRoutine = StartCoroutine(DownedRoutine());
                NotifyOwnerRpc(RespawnNotice.Downed);
                return;
            }

            // Party wipe: nobody left to revive, so every downed player respawns
            foreach (PlayerRespawn player in s_players)
            {
                if (player.IsDowned) player.RespawnAfter(_noReviveRespawnDelay);
            }
        }

        private IEnumerator DownedRoutine()
        {
            float remaining = _downedDuration;
            while (remaining > 0f)
            {
                if (!_isBeingRevived) remaining -= Time.deltaTime;
                yield return null;
            }

            // Nobody revived in time
            yield return RespawnRoutine(0f);
        }

        private void RespawnAfter(float delay)
        {
            if (_isRespawning) return;

            if (_downedRoutine != null) StopCoroutine(_downedRoutine);
            _downedRoutine = StartCoroutine(RespawnRoutine(delay));
        }

        private IEnumerator RespawnRoutine(float delay)
        {
            _isRespawning = true;
            NotifyOwnerRpc(RespawnNotice.Died);

            if (delay > 0f) yield return new WaitForSeconds(delay);

            _teleportService.TeleportTo(_respawnPosition, _respawnRotation);

            // Still dead (untargetable) while the owner applies the teleport
            yield return new WaitForSeconds(_arrivalGrace);

            _playerCombat.Revive(1f);
            _isRespawning = false;
            _downedRoutine = null;
        }

        private bool AnyPlayerAlive()
        {
            foreach (PlayerRespawn player in s_players)
            {
                if (!player.IsDowned) return true;
            }
            return false;
        }

        // Called on the downed player by the teammate that finished reviving them
        private void ReviveByTeammate()
        {
            if (_downedRoutine != null) StopCoroutine(_downedRoutine);
            _downedRoutine = null;
            _isBeingRevived = false;

            _playerCombat.Revive(_reviveFraction);
            NotifyOwnerRpc(RespawnNotice.Revived);
        }

        // REVIVE A TEAMMATE (owner input -> server)

        /// Owner. Starts reviving the closest downed teammate in range. False if there is none.
        public bool TryStartRevive()
        {
            // Non-owner copies also receive local input
            if (!IsOwner) return false;

            PlayerRespawn target = FindDownedTeammateInRange();
            if (target == null) return false;

            _isHoldingRevive = true;
            NotificationPopup.Instance?.ShowMessage(_revivingMessage);
            StartReviveRpc(target.NetworkObject);
            return true;
        }

        /// Owner. Interact key released.
        public void CancelRevive()
        {
            if (!IsOwner || !_isHoldingRevive) return;

            _isHoldingRevive = false;
            CancelReviveRpc();
        }

        private PlayerRespawn FindDownedTeammateInRange()
        {
            PlayerRespawn closest = null;
            float closestSqrDistance = _reviveRange * _reviveRange;

            foreach (PlayerRespawn player in s_players)
            {
                if (player == this || !player.IsDowned) continue;

                float sqrDistance = (player.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance <= closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = player;
                }
            }
            return closest;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void StartReviveRpc(NetworkObjectReference targetRef)
        {
            if (IsDowned || _reviveRoutine != null) return;

            PlayerRespawn target = targetRef.TryGet(out NetworkObject targetObj) ? targetObj.GetComponent<PlayerRespawn>() : null;
            if (target == null || target == this || !target.CanBeRevived || !IsInReviveRange(target))
            {
                NotifyOwnerRpc(RespawnNotice.ReviveInterrupted);
                return;
            }

            _reviveRoutine = StartCoroutine(ReviveRoutine(target));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void CancelReviveRpc() => StopReviving(false);

        private IEnumerator ReviveRoutine(PlayerRespawn target)
        {
            _reviveTarget = target;
            target._isBeingRevived = true;

            float elapsed = 0f;
            while (elapsed < _reviveHoldTime)
            {
                yield return null;
                elapsed += Time.deltaTime;

                bool interrupted = IsDowned || target == null || !target.IsSpawned || !target.IsDowned || !IsInReviveRange(target);
                if (interrupted)
                {
                    ClearRevive();
                    NotifyOwnerRpc(RespawnNotice.ReviveInterrupted);
                    yield break;
                }
            }

            ClearRevive();
            target.ReviveByTeammate();
        }

        // Small margin over the start range, so the server's interpolated positions don't cut it off
        private bool IsInReviveRange(PlayerRespawn target)
        {
            float range = _reviveRange + 0.5f;
            return (target.transform.position - transform.position).sqrMagnitude <= range * range;
        }

        // Server
        private void StopReviving(bool notifyOwner)
        {
            if (_reviveRoutine == null) return;

            StopCoroutine(_reviveRoutine);
            ClearRevive();
            if (notifyOwner) NotifyOwnerRpc(RespawnNotice.ReviveInterrupted);
        }

        private void ClearRevive()
        {
            if (_reviveTarget != null) _reviveTarget._isBeingRevived = false;
            _reviveTarget = null;
            _reviveRoutine = null;
        }

        // OWNER FEEDBACK

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void NotifyOwnerRpc(RespawnNotice notice)
        {
            string message = notice switch
            {
                RespawnNotice.Downed => _downedMessage,
                RespawnNotice.Died => _deathMessage,
                RespawnNotice.Revived => _revivedMessage,
                RespawnNotice.Rested => _restedMessage,
                RespawnNotice.EnemiesNearby => _enemiesNearbyMessage,
                _ => _reviveInterruptedMessage,
            };

            if (notice == RespawnNotice.ReviveInterrupted) _isHoldingRevive = false;

            NotificationPopup.Instance?.ShowMessage(message);
        }

        // Owner: no moving, attacking or interacting while downed
        private void OnDeadChanged(bool previous, bool isDead)
        {
            if (isDead) _playerSkills.CancelActiveSkill();

            _locomotionInput.enabled = !isDead;
            _combatInput.enabled = !isDead;
            _interaction.enabled = !isDead;
        }
    }
}
