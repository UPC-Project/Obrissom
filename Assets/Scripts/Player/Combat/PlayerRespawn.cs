using System.Collections;
using System.Collections.Generic;
using Obrissom.Environment;
using Obrissom.UI;
using Unity.Netcode;
using UnityEngine;
using PlayerInventory = Obrissom.Player.Inventory.Inventory;

namespace Obrissom.Player
{
    /// Death flow: at 0 health the player goes down for a while. Meanwhile a teammate can revive them (hold F),
    /// or they can choose to respawn at their bonfire right away or spend a magic stone to get up where they fell.
    /// When the time runs out they respawn at their respawn point with full health and resource.
    /// The respawn point starts at the spawn position; bonfires replace it.
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerTeleportService))]
    public class PlayerRespawn : NetworkBehaviour
    {
        public enum RespawnNotice : byte { Died, Revived, StoneRevived, ReviveInterrupted, Rested, EnemiesNearby }

        [Header("Downed")]
        [Tooltip("Seconds a downed player stays down before respawning at the bonfire. Paused while a teammate is reviving them.")]
        [SerializeField] private float _downedDuration = 15f;
        [Tooltip("Time after the teleport before the player can be targeted again, so enemies near the death spot can't hit the arrival.")]
        [SerializeField] private float _arrivalGrace = 0.5f;

        [Header("Revive")]
        [Tooltip("Seconds the teammate has to hold the interact key.")]
        [SerializeField] private float _reviveHoldTime = 3f;
        [SerializeField] private float _reviveRange = 2.5f;
        [Tooltip("Health and resource the revived player gets back, as a fraction of max.")]
        [SerializeField, Range(0.1f, 1f)] private float _reviveFraction = 0.5f;

        [Header("Magic stone")]
        [Tooltip("Consumable item that revives the player where they fell.")]
        [SerializeField] private Item _reviveStone;
        [Tooltip("Health and resource after using the stone, as a fraction of max.")]
        [SerializeField, Range(0.1f, 1f)] private float _stoneReviveFraction = 1f;

        [Header("Messages")]
        [SerializeField] private string _reviveInterruptedMessage = "Revive interrupted";
        [SerializeField] private string _revivedMessage = "You were revived";
        [SerializeField] private string _stoneRevivedMessage = "The magic stone brought you back";
        [SerializeField] private string _restedMessage = "You rested. Respawn point set";
        [SerializeField] private string _enemiesNearbyMessage = "You can't rest with enemies nearby";

        // Every player on this peer: used to find downed teammates and to know if anyone can still revive
        private static readonly List<PlayerRespawn> s_players = new List<PlayerRespawn>();

        private PlayerCombat _playerCombat;
        private PlayerTeleportService _teleportService;
        private PlayerSkills _playerSkills;
        private PlayerLocomotionInput _locomotionInput;
        private PlayerCombatInput _combatInput;
        private PlayerInteraction _interaction;
        private PlayerInventory _inventory;
        private Animator _animator;

        // Bool in the class Animator Controller. Classes without it simply skip the death animation.
        private static readonly int IsDeadHash = Animator.StringToHash("isDead");

        // Server only
        private Vector3 _respawnPosition;
        private Quaternion _respawnRotation;
        private Coroutine _downedRoutine;
        private float _downedTimeLeft;
        private bool _isRespawning;
        private bool _isBeingRevived;
        private Coroutine _reviveRoutine; // this player reviving a teammate
        private PlayerRespawn _reviveTarget;

        // Owner only
        private bool _isHoldingRevive;
        // Local copy of the downed timer for the UI; the server resyncs it when it pauses or resumes
        private float _ownerTimeLeft;
        private bool _ownerTimerPaused;
        private bool _ownerRespawning;

        public bool IsDowned => _playerCombat.IsDead;
        private bool CanBeRevived => IsDowned && !_isRespawning && !_isBeingRevived;

        /// Magic stones in this player's inventory (meaningful on the owner, where the inventory lives).
        public int ReviveStoneCount => _reviveStone != null && _inventory != null ? _inventory.GetItemCount(_reviveStone) : 0;

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
            _inventory = GetComponent<PlayerInventory>();
            _animator = GetComponent<PlayerController>()._animator;
        }

        public override void OnNetworkSpawn()
        {
            s_players.Add(this);

            if (IsServer) SetRespawnPoint(transform.position, transform.rotation);
            if (IsOwner)
            {
                _playerCombat._isDead.OnValueChanged += OnDeadChanged;
                ResetOwnerTimer();
                DownedUI.Instance?.Bind(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            s_players.Remove(this);

            if (IsServer) StopReviving(false);
            if (IsOwner)
            {
                _playerCombat._isDead.OnValueChanged -= OnDeadChanged;
                DownedUI.Instance?.HideDowned();
                DownedUI.Instance?.HideRevivePrompt();
            }
        }

        // Owner: downed screen (timer + choices), or the "Hold F" prompt near a downed teammate
        private void Update()
        {
            if (!IsOwner || DownedUI.Instance == null) return;

            if (IsDowned)
            {
                if (!_ownerTimerPaused && !_ownerRespawning)
                    _ownerTimeLeft = Mathf.Max(0f, _ownerTimeLeft - Time.deltaTime);

                DownedUI.Instance.ShowDowned(_ownerTimeLeft, _ownerTimerPaused, _ownerRespawning, AnyTeammateAlive(), ReviveStoneCount);
                DownedUI.Instance.HideRevivePrompt();
                return;
            }

            if (FindDownedTeammateInRange() != null) DownedUI.Instance.ShowRevivePrompt(_isHoldingRevive);
            else DownedUI.Instance.HideRevivePrompt();
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

            // Always a window to choose: wait for a teammate, respawn at the bonfire or use a magic stone.
            // With nobody left to revive (solo or whole party down) the time just runs out the same way.
            _downedTimeLeft = _downedDuration;
            _downedRoutine = StartCoroutine(DownedRoutine());
            SyncDownedTimerOwnerRpc(_downedTimeLeft, false);
        }

        private IEnumerator DownedRoutine()
        {
            while (_downedTimeLeft > 0f)
            {
                if (!_isBeingRevived) _downedTimeLeft -= Time.deltaTime;
                yield return null;
            }

            // Time's up
            yield return RespawnRoutine();
        }

        private void RespawnNow()
        {
            if (_isRespawning) return;

            if (_downedRoutine != null) StopCoroutine(_downedRoutine);
            _downedRoutine = StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            _isRespawning = true;
            NotifyOwnerRpc(RespawnNotice.Died);

            _teleportService.TeleportTo(_respawnPosition, _respawnRotation);

            // Still dead (untargetable) while the owner applies the teleport
            yield return new WaitForSeconds(_arrivalGrace);

            _playerCombat.Revive(1f);
            _isRespawning = false;
            _downedRoutine = null;
        }

        // Teammate revive or magic stone: back up where they fell
        private void ReviveInPlace(float fraction, RespawnNotice notice)
        {
            if (_downedRoutine != null) StopCoroutine(_downedRoutine);
            _downedRoutine = null;
            _isBeingRevived = false;

            _playerCombat.Revive(fraction);
            NotifyOwnerRpc(notice);
        }

        // Server. Pauses or resumes the downed timer and tells the owner, so their countdown matches
        private void SetBeingRevived(bool beingRevived)
        {
            _isBeingRevived = beingRevived;
            if (IsSpawned && IsDowned) SyncDownedTimerOwnerRpc(_downedTimeLeft, beingRevived);
        }

        private bool AnyTeammateAlive()
        {
            foreach (PlayerRespawn player in s_players)
            {
                if (player != this && !player.IsDowned) return true;
            }
            return false;
        }

        // DOWNED CHOICES (owner buttons -> server)

        /// Owner. Downed screen button: stop waiting and respawn at the bonfire now.
        public void RequestRespawnAtBonfire()
        {
            if (!IsOwner || !IsDowned || _ownerRespawning) return;

            RespawnNowRpc();
        }

        /// Owner. Downed screen button: spend a magic stone to get up right here.
        public void RequestStoneRevive()
        {
            if (!IsOwner || !IsDowned || _ownerRespawning || ReviveStoneCount <= 0) return;

            StoneReviveRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RespawnNowRpc()
        {
            if (IsDowned) RespawnNow();
        }

        // The inventory isn't networked (it lives on the owner), so the owner checks the stone
        // and only spends it once the server has accepted the revive
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void StoneReviveRpc()
        {
            if (!IsDowned || _isRespawning) return;

            ReviveInPlace(_stoneReviveFraction, RespawnNotice.StoneRevived);
            ConsumeReviveStoneOwnerRpc();
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void ConsumeReviveStoneOwnerRpc()
        {
            if (_reviveStone != null && _inventory != null) _inventory.RemoveItem(_reviveStone, 1);
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
            target.SetBeingRevived(true);

            float elapsed = 0f;
            while (elapsed < _reviveHoldTime)
            {
                yield return null;
                elapsed += Time.deltaTime;

                // Also stops if the target chose to respawn at the bonfire or used a magic stone
                bool interrupted = IsDowned || target == null || !target.IsSpawned || !target.IsDowned
                                   || target._isRespawning || !IsInReviveRange(target);
                if (interrupted)
                {
                    ClearRevive();
                    NotifyOwnerRpc(RespawnNotice.ReviveInterrupted);
                    yield break;
                }
            }

            ClearRevive();
            target.ReviveInPlace(target._reviveFraction, RespawnNotice.Revived);
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
            if (_reviveTarget != null) _reviveTarget.SetBeingRevived(false);
            _reviveTarget = null;
            _reviveRoutine = null;
        }

        // OWNER FEEDBACK

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void NotifyOwnerRpc(RespawnNotice notice)
        {
            // Downed, death and reviving are shown by DownedUI, no popup for them
            if (notice == RespawnNotice.Died)
            {
                _ownerRespawning = true;
                return;
            }

            if (notice == RespawnNotice.Rested && TryGetComponent(out PlayerQuestTracker questTracker))
                questTracker.ReportRest();

            string message = notice switch
            {
                RespawnNotice.Revived => _revivedMessage,
                RespawnNotice.StoneRevived => _stoneRevivedMessage,
                RespawnNotice.Rested => _restedMessage,
                RespawnNotice.EnemiesNearby => _enemiesNearbyMessage,
                _ => _reviveInterruptedMessage,
            };

            if (notice == RespawnNotice.ReviveInterrupted) _isHoldingRevive = false;

            // Empty message in the inspector = no popup
            if (!string.IsNullOrEmpty(message)) NotificationPopup.Instance?.ShowMessage(message);
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void SyncDownedTimerOwnerRpc(float timeLeft, bool paused)
        {
            _ownerTimeLeft = timeLeft;
            _ownerTimerPaused = paused;
        }

        private void ResetOwnerTimer()
        {
            _ownerTimeLeft = _downedDuration;
            _ownerTimerPaused = false;
            _ownerRespawning = false;
        }

        // Owner: no moving, attacking or interacting while downed
        private void OnDeadChanged(bool previous, bool isDead)
        {
            if (isDead) _playerSkills.CancelActiveSkill();

            _locomotionInput.enabled = !isDead;
            _combatInput.enabled = !isDead;
            _interaction.enabled = !isDead;

            SetDeathAnimation(isDead);

            // Back up: ready for the next time and grey screen off
            if (!isDead)
            {
                ResetOwnerTimer();
                DownedUI.Instance?.HideDowned();
            }
        }

        // Owner. The NetworkAnimator is owner-authoritative, so the parameter reaches everyone (late joiners too).
        private void SetDeathAnimation(bool isDead)
        {
            if (_animator == null) return;

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == IsDeadHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    _animator.SetBool(IsDeadHash, isDead);
                    return;
                }
            }
        }
    }
}
