using System.Collections;
using System.Collections.Generic;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    // Church boss. Starts as an altar, wakes up when a player walks in and never walks.
    // The server runs the AI; clients only play animations and warnings.
    [RequireComponent(typeof(MantisBossAnimation))]
    [RequireComponent(typeof(BossRootPool))]
    public class MantisBoss : EnemyBase
    {
        private const float ArenaCheckInterval = 0.25f;
        private const float GroundProbeHeight = 1f;
        private const float GroundProbeDistance = 6f;
        private const float RandomPointProbeDistance = 15f;
        private const int RandomPointAttempts = 8;
        private const float SightHeight = 2f;

        private static readonly Color WarningColor = new Color(1f, 0.55f, 0.1f, 0.8f);
        private static readonly Color ImpactColor = new Color(1f, 0.1f, 0.05f, 1f);

        [Header("Boss")]
        [Tooltip("Turn towards the target. The boss never walks.")]
        [SerializeField] private bool _faceTarget = true;

        [Tooltip("Off: timers turn the hand hitboxes on and raise the roots (placeholder). " +
                 "On: the attack animations do it through MantisBossAnimationEvents.")]
        [SerializeField] private bool _animationEvents;

        [Tooltip("Seconds of transformation before the first attack.")]
        [SerializeField] private float _wakeUpTime = 2f;

        [Header("Melee")]
        [Tooltip("Hand colliders. Damage comes from the stats (min / max attack damage).")]
        [SerializeField] private BossDamageTrigger[] _meleeHitboxes;
        [Tooltip("From the start of the attack until the hands hit.")]
        [SerializeField] private float _meleeWindup = 0.7f;
        [SerializeField] private float _meleeActiveTime = 0.25f;
        [SerializeField] private float _meleeRecovery = 0.6f;
        [Tooltip("Placeholder warning on the ground in front of the boss (off with Animation Events).")]
        [SerializeField] private float _meleeWarningDistance = 2f;
        [SerializeField] private float _meleeWarningRadius = 1.6f;
        [Tooltip("The swing only starts when the target is inside this angle in front of the boss (degrees, total).")]
        [SerializeField, Range(10f, 360f)] private float _meleeFrontAngle = 100f;

        [Header("Roots")]
        [SerializeField] private float _rootDamage = 8f;
        [SerializeField] private float _rootCooldown = 1.5f;
        [Tooltip("From the start of the cast until the first roots start their warning.")]
        [SerializeField] private float _rootCastTime = 0.5f;
        [SerializeField] private float _rootRecovery = 0.7f;
        [Tooltip("Layers the roots stand on. Player and enemy layers are always skipped.")]
        [SerializeField] private LayerMask _groundMask = Physics.DefaultRaycastLayers;
        [Tooltip("No root comes out closer than this to the boss (bigger roots keep further away).")]
        [SerializeField] private float _rootClearRadius = 1.5f;
        [Tooltip("Seconds a player can stay next to the boss before it answers with an anti-melee attack.")]
        [SerializeField] private float _meleePressureTime = 4f;
        [Tooltip("Root attacks. The boss picks one at random (never the same twice in a row).")]
        [SerializeField] private RootPattern[] _rootPatterns = DefaultPatterns();

        [Header("Threat")]
        [Tooltip("To pull the boss, a player needs this many times the threat of its current target (1.1 = 10% more).")]
        [SerializeField, Min(1f)] private float _threatSwitchRatio = 1.1f;
        [Tooltip("While taunted, the tank keeps this much threat above everyone, so the boss stays on it after the taunt ends.")]
        [SerializeField, Min(0f)] private float _tauntThreatLead = 20f;

        [Header("Arena")]
        [Tooltip("Seconds with nobody in the church (standing or down) before the boss heals and turns back into the altar.")]
        [SerializeField] private float _resetDelay = 0f;

        [Header("Victory")]
        [Tooltip("{0} = boss name")]
        [SerializeField] private string _victoryFormat = "{0} defeated";
        [SerializeField] private float _victoryBannerTime = 5f;

        // Synced so late joiners see the boss and not the altar
        private readonly NetworkVariable<bool> _isAwake = new NetworkVariable<bool>(false);

        // Health from 0 to 1, for the boss bar on every client
        private readonly NetworkVariable<float> _healthSynced = new NetworkVariable<float>(1f);

        private readonly HashSet<PlayerCombat> _meleeHits = new HashSet<PlayerCombat>();
        private readonly List<Vector3> _rootPositions = new List<Vector3>();
        private readonly List<float> _rootDelays = new List<float>();
        private readonly List<float> _rootSizes = new List<float>();

        private MantisBossAnimation _bossAnimation;
        private BossRootPool _rootPool;
        private BossAwakenTrigger _arena;
        private ShoebillAreaIndicator _meleeIndicator;
        private Coroutine _meleeWarningRoutine;
        private MantisBossHud _hud;

        private Coroutine _attackRoutine;
        private bool _facingLocked;
        private float _attacksAllowedTime;
        private float _rootReadyTime;
        private float _arenaCheckTimer;
        private float _emptyArenaTime;
        private float _meleePressure;
        private int _lastPattern = -1;
        private int _castingPattern = -1;
        private float[] _nextTimedCast;
        // Damage done by each player (key = player object id)
        private readonly Dictionary<ulong, float> _threat = new Dictionary<ulong, float>();

        public bool IsAwake => _isAwake.Value;

        // Awake and still alive. Clients can read it too
        public bool IsFighting => _isAwake.Value && !IsDeadSynced;

        // The altar can't be hurt
        public override bool IsInvulnerable => !_isAwake.Value;

        // The tank dash can't drag it
        public override bool IsImmovable => true;

        public override bool CanFaceTarget => _faceTarget && _isAwake.Value && !_facingLocked;

        protected override bool IsStaggerImmune => true;

        private string BossName => _stats != null && !string.IsNullOrEmpty(_stats.enemyName) ? _stats.enemyName : "Mantis";

        // Ground for the roots: everything except players and the boss itself
        private int GroundMask => _groundMask.value & ~_playerLayer.value & ~(1 << gameObject.layer);

        protected override void Awake()
        {
            base.Awake();
            _bossAnimation = GetComponent<MantisBossAnimation>();
            _rootPool = GetComponent<BossRootPool>();

            // Both hands share one hit list: one hit per swing
            foreach (BossDamageTrigger hitbox in _meleeHitboxes)
            {
                if (hitbox != null) hitbox.CanHit = player => _meleeHits.Add(player);
            }
        }

        public void SetArena(BossAwakenTrigger arena) => _arena = arena;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isAwake.OnValueChanged += OnAwakeChanged;
            _healthSynced.OnValueChanged += OnHealthChanged;

            // Every peer builds its own roots; only the server's roots deal damage
            _rootPool.Build();

            if (IsClient)
            {
                _meleeIndicator = ShoebillAreaIndicator.CreateFallback(null);

                _hud = FindAnyObjectByType<MantisBossHud>(FindObjectsInactive.Include);
                if (_hud != null)
                {
                    _hud.SetBossName(BossName);
                    _hud.SetHealth(_healthSynced.Value, true);
                }
                else
                {
                    Debug.LogWarning("[MantisBoss] No MantisBossHud in the scene: the boss health bar won't show.", this);
                }
            }

            // Late joiners skip the transformation
            _bossAnimation.SetAwake(_isAwake.Value, true);
        }

        public override void OnNetworkDespawn()
        {
            _isAwake.OnValueChanged -= OnAwakeChanged;
            _healthSynced.OnValueChanged -= OnHealthChanged;
            CleanUpLocalObjects();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            CleanUpLocalObjects();
            base.OnDestroy();
        }

        private void CleanUpLocalObjects()
        {
            if (_rootPool != null) _rootPool.Clear();
            if (_meleeIndicator != null) Destroy(_meleeIndicator.gameObject);
            _meleeIndicator = null;

            if (_hud != null) _hud.SetBarVisible(false);
            _hud = null;
        }

        private void OnAwakeChanged(bool previous, bool current)
        {
            _bossAnimation.SetAwake(current, false);
            if (!current) _bossAnimation.ResetAttacks();
        }

        private void OnHealthChanged(float previous, float current)
        {
            if (_hud != null) _hud.SetHealth(current, current > previous);
        }

        protected override void Update()
        {
            base.Update();

            if (_hud != null) _hud.SetBarVisible(_isAwake.Value && _healthSynced.Value > 0f && IsLocalPlayerInArena());
            if (!IsServer) return;

            CheckForEmptyArena();
            UpdateMeleePressure();
        }

        // Counts how long someone stays close to the boss, to trigger the anti-melee attack
        private void UpdateMeleePressure()
        {
            if (_isDead || !_isAwake.Value)
            {
                _meleePressure = 0f;
                return;
            }

            float range = _stats.attackRange + 1f;
            bool anyoneClose = false;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                PlayerCombat player = GetFighter(client);
                if (player != null && (player.transform.position - transform.position).sqrMagnitude <= range * range)
                {
                    anyoneClose = true;
                    break;
                }
            }

            _meleePressure = anyoneClose ? _meleePressure + Time.deltaTime : Mathf.Max(0f, _meleePressure - Time.deltaTime);
        }

        // Sent by BossAwakenTrigger when a player walks into the church
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestAwakenRpc()
        {
            if (_isAwake.Value || _isDead) return;

            _isAwake.Value = true;
            _emptyArenaTime = 0f;
            _attacksAllowedTime = Time.time + _wakeUpTime;
            _threat.Clear();
            _rootReadyTime = _attacksAllowedTime;
            StartTimedPatterns();
        }

        // Target = player with the most threat. A taunt always wins.
        // While nobody has done damage, the nearest player is the target.
        public override void DetectPlayer()
        {
            if (!_isAwake.Value)
            {
                _target = null;
                return;
            }

            if (ForcedTarget != null)
            {
                PlayerCombat taunter = ForcedTarget.GetComponent<PlayerCombat>();
                if (taunter != null) KeepThreatLead(taunter);
                _target = ForcedTarget;
                return;
            }

            PlayerCombat best = null;
            float bestThreat = -1f;
            float bestSqr = float.MaxValue;
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                PlayerCombat player = GetFighter(client);
                if (player == null) continue;

                float threat = ThreatOf(player);
                float sqr = (player.transform.position - transform.position).sqrMagnitude;
                if (threat > bestThreat || (threat == bestThreat && sqr < bestSqr))
                {
                    best = player;
                    bestThreat = threat;
                    bestSqr = sqr;
                }
            }

            // Keep the current target unless someone beats it by the switch ratio
            PlayerCombat current = _target != null ? _target.GetComponent<PlayerCombat>() : null;
            if (best != null && current != null && current != best && IsFighter(current) &&
                bestThreat <= ThreatOf(current) * _threatSwitchRatio)
            {
                best = current;
            }

            _target = best != null ? best.transform : null;
        }

        // Every point of damage adds one point of threat
        protected override void OnDamagedBy(NetworkObjectReference attackerRef, float damage)
        {
            if (damage <= 0f || !attackerRef.TryGet(out NetworkObject attacker)) return;

            _threat.TryGetValue(attacker.NetworkObjectId, out float threat);
            _threat[attacker.NetworkObjectId] = threat + damage;
        }

        private float ThreatOf(PlayerCombat player) =>
            _threat.TryGetValue(player.NetworkObjectId, out float threat) ? threat : 0f;

        // The taunt puts the tank on top of the threat list
        private void KeepThreatLead(PlayerCombat taunter)
        {
            float top = 0f;
            foreach (KeyValuePair<ulong, float> entry in _threat)
            {
                if (entry.Key != taunter.NetworkObjectId && entry.Value > top) top = entry.Value;
            }

            if (ThreatOf(taunter) < top + _tauntThreatLead) _threat[taunter.NetworkObjectId] = top + _tauntThreatLead;
        }

        public override bool IsPlayerInChaseRange() =>
            _target != null && IsValidTarget(_target) && IsInArena(_target.position);

        private PlayerCombat GetFighter(NetworkClient client)
        {
            if (client.PlayerObject == null) return null;

            PlayerCombat player = client.PlayerObject.GetComponent<PlayerCombat>();
            return IsFighter(player) ? player : null;
        }

        // Alive, not down, and inside the church
        private bool IsFighter(PlayerCombat player) =>
            player != null && player.IsTargetable && IsInArena(player.transform.position);

        // The arena is the BossAwakenTrigger volume
        private bool IsInArena(Vector3 position) => _arena != null && _arena.Contains(position);

        private bool IsLocalPlayerInArena()
        {
            NetworkObject localPlayer = NetworkManager != null && NetworkManager.LocalClient != null
                ? NetworkManager.LocalClient.PlayerObject
                : null;
            return localPlayer != null && IsInArena(localPlayer.transform.position);
        }

        protected override void ApplyDebuffEffect(DebuffType debuff)
        {
            if (!_isAwake.Value) return;
            base.ApplyDebuffEffect(debuff);
        }

        public override void OnMoveStateEnter() { }

        public override void OnMoveStateTick() { }

        public override float GetMoveSpeed(EnemyState state) => 0f;

        public override void PerformAttackRpc() => TryStartAttack(true);

        public override void OnChaseStateTick() => TryStartAttack(false);

        private void TryStartAttack(bool targetInMeleeRange)
        {
            if (!IsServer || _isDead || !_isAwake.Value || _attackRoutine != null) return;
            if (Time.time < _attacksAllowedTime) return;

            // 1. Timed attacks (the ring) go first
            int timed = DueTimedPattern();
            if (timed >= 0)
            {
                _nextTimedCast[timed] = Time.time + _rootPatterns[timed].interval;
                _attackRoutine = StartCoroutine(RootRoutine(timed));
                return;
            }

            bool rootsReady = Time.time >= _rootReadyTime;

            // 2. Anti-melee when someone stayed close too long
            if (rootsReady && _meleePressure >= _meleePressureTime)
            {
                int antiMelee = PickPattern(true);
                if (antiMelee >= 0)
                {
                    _meleePressure = 0f;
                    _attackRoutine = StartCoroutine(RootRoutine(antiMelee));
                    return;
                }
            }

            // 3. Melee if the target is close and in front, otherwise a random root attack
            if (targetInMeleeRange && _attackCooldownTimer <= 0f && IsTargetInFront())
            {
                _attackRoutine = StartCoroutine(MeleeRoutine());
            }
            else if (rootsReady)
            {
                int pattern = PickPattern(false);
                if (pattern >= 0) _attackRoutine = StartCoroutine(RootRoutine(pattern));
            }
        }

        // The boss doesn't turn, so the hands only reach players in front of it
        private bool IsTargetInFront()
        {
            if (_target == null) return false;

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return true;

            return Vector3.Angle(Flat(transform.forward), toTarget) <= _meleeFrontAngle * 0.5f;
        }

        // Weighted random root attack, never the same one twice in a row
        private int PickPattern(bool antiMeleeOnly)
        {
            if (_rootPatterns == null) return -1;

            bool skipLast = TotalWeight(antiMeleeOnly, true) > 0f;
            float total = TotalWeight(antiMeleeOnly, skipLast);
            if (total <= 0f) return -1;

            float roll = Random.Range(0f, total);
            int picked = -1;
            for (int i = 0; i < _rootPatterns.Length; i++)
            {
                if (!IsPickable(i, antiMeleeOnly, skipLast)) continue;
                picked = i;
                roll -= _rootPatterns[i].weight;
                if (roll <= 0f) break;
            }
            return picked;
        }

        private float TotalWeight(bool antiMeleeOnly, bool skipLast)
        {
            float total = 0f;
            for (int i = 0; i < _rootPatterns.Length; i++)
            {
                if (IsPickable(i, antiMeleeOnly, skipLast)) total += _rootPatterns[i].weight;
            }
            return total;
        }

        private bool IsPickable(int index, bool antiMeleeOnly, bool skipLast)
        {
            RootPattern pattern = _rootPatterns[index];
            if (!HasWaves(pattern) || pattern.weight <= 0f || pattern.interval > 0f) return false;
            if (antiMeleeOnly && !pattern.antiMelee) return false;
            return !(skipLast && index == _lastPattern);
        }

        private static bool HasWaves(RootPattern pattern) => pattern != null && pattern.waves != null && pattern.waves.Length > 0;

        // Attacks with an interval run on their own timer, outside the random pick
        private void StartTimedPatterns()
        {
            if (_rootPatterns == null) return;

            _nextTimedCast = new float[_rootPatterns.Length];
            for (int i = 0; i < _rootPatterns.Length; i++)
            {
                RootPattern pattern = _rootPatterns[i];
                _nextTimedCast[i] = HasWaves(pattern) && pattern.interval > 0f ? _attacksAllowedTime + pattern.interval : float.MaxValue;
            }
        }

        private int DueTimedPattern()
        {
            if (_rootPatterns == null || _nextTimedCast == null || _nextTimedCast.Length != _rootPatterns.Length) return -1;

            for (int i = 0; i < _rootPatterns.Length; i++)
            {
                if (Time.time >= _nextTimedCast[i] && HasWaves(_rootPatterns[i]) && _rootPatterns[i].interval > 0f) return i;
            }
            return -1;
        }

        private IEnumerator MeleeRoutine()
        {
            _attackCooldownTimer = _meleeWindup + _meleeActiveTime + _meleeRecovery + _stats.attackCooldown;
            _meleeHits.Clear();

            float damage = RollAttackDamage();
            foreach (BossDamageTrigger hitbox in _meleeHitboxes)
            {
                if (hitbox != null) hitbox.Damage = damage;
            }

            // Stop turning during the windup so the swing can be dodged
            _facingLocked = true;
            Vector3 warningCenter = transform.position + transform.forward * _meleeWarningDistance;
            PlayMeleeRpc(warningCenter, NetworkManager.ServerTime.Time, !_animationEvents);

            if (_animationEvents)
            {
                // The animation turns the hands on and off
                yield return new WaitForSeconds(_meleeWindup + _meleeActiveTime);
                _facingLocked = false;
                yield return new WaitForSeconds(_meleeRecovery);
                SetMeleeHitboxes(false);
            }
            else
            {
                // Placeholder: timers turn the hands on and off
                yield return new WaitForSeconds(_meleeWindup);
                SetMeleeHitboxes(true);
                yield return new WaitForSeconds(_meleeActiveTime);
                SetMeleeHitboxes(false);
                _facingLocked = false;
                yield return new WaitForSeconds(_meleeRecovery);
            }

            _attackRoutine = null;
        }

        // Plays the cast animation, then raises the roots (or waits for the animation event)
        private IEnumerator RootRoutine(int patternIndex)
        {
            if (_rootPatterns[patternIndex].interval <= 0f) _lastPattern = patternIndex;
            _castingPattern = patternIndex;
            _rootReadyTime = Time.time + _rootCastTime + PatternDuration(_rootPatterns[patternIndex]) + _rootCooldown;
            PlayRootsAnimationRpc(patternIndex);

            yield return new WaitForSeconds(_rootCastTime);
            if (!_animationEvents) CastRoots();

            yield return new WaitForSeconds(_rootRecovery);
            _attackRoutine = null;
        }

        private static float PatternDuration(RootPattern pattern)
        {
            float duration = 0f;
            foreach (RootWave wave in pattern.waves)
            {
                if (wave == null) continue;
                bool isLine = wave.shape == RootShape.LinesAcross || wave.shape == RootShape.Fan || wave.shape == RootShape.ParallelLines;
                float spread = isLine ? wave.stagger * (wave.length / wave.rootSpacing)
                    : wave.shape == RootShape.Rings ? wave.stagger * (wave.count - 1)
                    : wave.stagger;
                duration = Mathf.Max(duration, wave.delay + spread);
            }
            return duration;
        }

        public void SetMeleeHitboxes(bool active)
        {
            foreach (BossDamageTrigger hitbox in _meleeHitboxes)
            {
                if (hitbox != null) hitbox.SetActive(active);
            }
        }

        // Server: works out every root of the attack and sends them all in one message
        public void CastRoots()
        {
            if (!IsServer || _isDead || !_isAwake.Value) return;

            int patternIndex = _castingPattern >= 0 ? _castingPattern : PickPattern(false);
            _castingPattern = -1;
            if (patternIndex < 0) return;

            _rootPositions.Clear();
            _rootDelays.Clear();
            _rootSizes.Clear();
            foreach (RootWave wave in _rootPatterns[patternIndex].waves) AddWave(wave);

            if (_rootPositions.Count > 0)
            {
                SpawnRootsRpc(_rootPositions.ToArray(), _rootDelays.ToArray(), _rootSizes.ToArray(), NetworkManager.ServerTime.Time);
            }
        }

        // Each shape adds its roots, each one with a position, a delay and a size
        private void AddWave(RootWave wave)
        {
            if (wave == null) return;

            switch (wave.shape)
            {
                case RootShape.UnderPlayers:
                    foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
                    {
                        PlayerCombat player = GetFighter(client);
                        if (player != null && TryPlaceCircle(player.transform.position, wave.size, out Vector3 ground))
                        {
                            AddRoot(ground, wave.delay + Random.Range(0f, wave.stagger), wave.size);
                        }
                    }
                    break;

                case RootShape.RandomCircles:
                    for (int i = 0; i < wave.count; i++)
                    {
                        if (TryGetRandomArenaPoint(out Vector3 point) && TryPlaceCircle(point, wave.size, out Vector3 ground))
                        {
                            AddRoot(ground, wave.delay + Random.Range(0f, wave.stagger), wave.size);
                        }
                    }
                    break;

                case RootShape.LinesAcross:
                {
                    Vector3 forward = Flat(transform.forward);
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    float firstLine = _rootClearRadius + 1f;
                    for (int i = 0; i < wave.count; i++)
                    {
                        Vector3 center = transform.position + forward * (firstLine + wave.spread * i);
                        AddLine(center - right * (wave.length * 0.5f), right, wave);
                    }
                    break;
                }

                case RootShape.Fan:
                {
                    Vector3 aim = AimDirection();
                    for (int i = 0; i < wave.count; i++)
                    {
                        float angle = wave.count == 1 ? 0f : Mathf.Lerp(-wave.spread * 0.5f, wave.spread * 0.5f, i / (float)(wave.count - 1));
                        AddLine(transform.position, Quaternion.Euler(0f, angle, 0f) * aim, wave);
                    }
                    break;
                }

                case RootShape.ParallelLines:
                {
                    Vector3 aim = AimDirection();
                    Vector3 right = Vector3.Cross(Vector3.up, aim);
                    for (int i = 0; i < wave.count; i++)
                    {
                        float offset = (i - (wave.count - 1) * 0.5f) * wave.spread;
                        AddLine(transform.position + right * offset, aim, wave);
                    }
                    break;
                }

                case RootShape.Rings:
                    for (int ring = 0; ring < wave.count; ring++)
                    {
                        AddRing(wave.radius + wave.spread * ring, wave, wave.delay + wave.stagger * ring);
                    }
                    break;
            }
        }

        private void AddRing(float radius, RootWave wave, float delay)
        {
            int roots = Mathf.Max(3, Mathf.RoundToInt(2f * Mathf.PI * radius / wave.rootSpacing));
            float startAngle = Random.Range(0f, 360f);
            for (int i = 0; i < roots; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, startAngle + 360f * i / roots, 0f) * Vector3.forward;
                Vector3 point = transform.position + direction * radius;
                if (IsTooCloseToBoss(point, wave.size)) continue;
                if (!TryGetGround(point, GroundProbeDistance, out Vector3 ground) || !IsValidRootSpot(ground)) continue;
                AddRoot(ground, delay, wave.size);
            }
        }

        // Roots along a line. Stagger makes them come out one after another
        private void AddLine(Vector3 start, Vector3 direction, RootWave wave)
        {
            int steps = Mathf.FloorToInt(wave.length / wave.rootSpacing) + 1;
            for (int i = 0; i < steps; i++)
            {
                Vector3 point = start + direction * (i * wave.rootSpacing);
                if (IsTooCloseToBoss(point, wave.size)) continue;
                if (!TryGetGround(point, GroundProbeDistance, out Vector3 ground) || !IsValidRootSpot(ground)) continue;
                AddRoot(ground, wave.delay + wave.stagger * i, wave.size);
            }
        }

        // Inside the church and no wall between the boss and the spot
        private bool IsValidRootSpot(Vector3 ground)
        {
            if (!IsInArena(ground)) return false;

            Vector3 from = transform.position + Vector3.up * SightHeight;
            Vector3 to = ground + Vector3.up * SightHeight;
            return !Physics.Linecast(from, to, GroundMask, QueryTriggerInteraction.Ignore);
        }

        private float ClearRadius(float size) => _rootClearRadius * Mathf.Max(1f, size);

        private bool IsTooCloseToBoss(Vector3 point, float size)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude < ClearRadius(size) * ClearRadius(size);
        }

        // Circles too close to the boss get pushed out, so no root comes out under it
        private bool TryPlaceCircle(Vector3 position, float size, out Vector3 ground)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            float clear = ClearRadius(size);
            if (offset.sqrMagnitude < clear * clear)
            {
                Vector3 direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : Flat(transform.forward);
                Vector3 pushed = transform.position + direction * clear;
                position = new Vector3(pushed.x, position.y, pushed.z);
            }

            return TryGetGround(position, GroundProbeDistance, out ground) && IsValidRootSpot(ground);
        }

        private void AddRoot(Vector3 position, float delay, float size)
        {
            _rootPositions.Add(position);
            _rootDelays.Add(delay);
            _rootSizes.Add(size);
        }

        private bool TryGetRandomArenaPoint(out Vector3 point)
        {
            for (int attempt = 0; attempt < RandomPointAttempts; attempt++)
            {
                if (_arena == null) break;
                if (TryGetGround(_arena.RandomPoint(), RandomPointProbeDistance, out point) && IsValidRootSpot(point)) return true;
            }
            point = default;
            return false;
        }

        private bool TryGetGround(Vector3 position, float distance, out Vector3 ground)
        {
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, GroundMask, QueryTriggerInteraction.Ignore))
            {
                ground = hit.point;
                return true;
            }
            ground = position;
            return false;
        }

        private Vector3 AimDirection()
        {
            if (_target != null)
            {
                Vector3 toTarget = Flat(_target.position - transform.position);
                if (toTarget != Vector3.zero) return toTarget;
            }
            return Flat(transform.forward);
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        // Used only when the prefab has no patterns saved. The prefab values win.
        private static RootPattern[] DefaultPatterns() => new[]
        {
            new RootPattern("Big circle", false,
                new RootWave(RootShape.UnderPlayers, 0f, 2.2f, 1, 0f, 2f, 0f, 0f)),
            new RootPattern("Little circles", false,
                new RootWave(RootShape.RandomCircles, 0f, 1f, 35, 0f, 2f, 0f, 2f),
                new RootWave(RootShape.UnderPlayers, 0.6f, 1f, 1, 0f, 2f, 0f, 0f)),
            new RootPattern("Circles then lines", true,
                new RootWave(RootShape.RandomCircles, 0f, 1f, 6, 0f, 2f, 0f, 0.5f),
                new RootWave(RootShape.LinesAcross, 1.5f, 1f, 3, 6f, 2f, 18f, 0f)),
            new RootPattern("Fan", false,
                new RootWave(RootShape.Fan, 0f, 1f, 5, 90f, 2f, 22f, 0.08f)),
            new RootPattern("Columns", false,
                new RootWave(RootShape.ParallelLines, 0f, 1f, 3, 4f, 2f, 26f, 0.08f)),
            new RootPattern("Ring", 12f,
                new RootWave(RootShape.Rings, 0f, 1f, 2, 2f, 2f, 0f, 0.4f, 2.5f)),
        };

        public override void CancelAttack()
        {
            base.CancelAttack();
            StopAttack();
        }

        private void StopAttack()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            _facingLocked = false;
            SetMeleeHitboxes(false);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlayMeleeRpc(Vector3 warningCenter, double startServerTime, bool showWarning)
        {
            _bossAnimation.PlayMeleeAnimation();

            if (!showWarning || _meleeIndicator == null) return;

            StopMeleeWarning();
            _meleeWarningRoutine = StartCoroutine(MeleeWarningRoutine(warningCenter, startServerTime));
        }

        private void StopMeleeWarning()
        {
            if (_meleeWarningRoutine != null)
            {
                StopCoroutine(_meleeWarningRoutine);
                _meleeWarningRoutine = null;
            }
            if (_meleeIndicator != null) _meleeIndicator.Hide();
        }

        private IEnumerator MeleeWarningRoutine(Vector3 center, double startServerTime)
        {
            _meleeIndicator.ShowTelegraph(center, _meleeWarningRadius, startServerTime, _meleeWindup,
                                          WarningColor, ImpactColor, GroundMask);

            double impactTime = startServerTime + _meleeWindup;
            while (_meleeIndicator != null && NetworkManager != null && NetworkManager.ServerTime.Time < impactTime)
                yield return null;

            if (_meleeIndicator != null)
                _meleeIndicator.PlayImpact(center, _meleeWarningRadius, _meleeActiveTime + 0.15f, ImpactColor, GroundMask);
            _meleeWarningRoutine = null;
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void PlayRootsAnimationRpc(int patternIndex)
        {
            RootPattern pattern = _rootPatterns != null && patternIndex >= 0 && patternIndex < _rootPatterns.Length ? _rootPatterns[patternIndex] : null;
            _bossAnimation.PlayRootsAnimation(pattern != null ? pattern.animatorTrigger : null);
        }

        // Every peer schedules the same roots at the same server time
        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void SpawnRootsRpc(Vector3[] positions, float[] delays, float[] sizes, double startServerTime)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                _rootPool.Spawn(positions[i], startServerTime + delays[i], _rootDamage, GroundMask, sizes[i]);
            }
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void RecallAttacksRpc()
        {
            _rootPool.RecallAll();
            StopMeleeWarning();
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void ShowVictoryRpc()
        {
            if (_hud != null) _hud.ShowVictory(string.Format(_victoryFormat, BossName), _victoryBannerTime);
        }

        protected override void OnTakeDamage(float rawAmount)
        {
            _healthSynced.Value = _currentHealth / _stats.maxHealth;
        }

        // Nobody left in the church (standing or down): heal and go back to the altar
        private void CheckForEmptyArena()
        {
            if (_isDead || !_isAwake.Value)
            {
                _emptyArenaTime = 0f;
                return;
            }

            _arenaCheckTimer -= Time.deltaTime;
            if (_arenaCheckTimer > 0f) return;
            _arenaCheckTimer = ArenaCheckInterval;

            if (AnyPlayerInArena())
            {
                _emptyArenaTime = 0f;
                return;
            }

            _emptyArenaTime += ArenaCheckInterval;
            if (_emptyArenaTime >= _resetDelay) ResetToAltar();
        }

        private bool AnyPlayerInArena()
        {
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject != null && IsInArena(client.PlayerObject.transform.position)) return true;
            }
            return false;
        }

        private void ResetToAltar()
        {
            StopAttack();
            RecallAttacksRpc();

            _emptyArenaTime = 0f;
            _attackCooldownTimer = 0f;
            _meleePressure = 0f;
            _castingPattern = -1;
            _lastPattern = -1;
            _target = null;
            _threat.Clear();

            _currentHealth = _stats.maxHealth;
            _healthSynced.Value = 1f;

            _isAwake.Value = false;
            _stateMachine.ChangeState(EnemyState.Idle);
        }

        // Clears the roots and shows the victory banner to everyone
        protected override void Die(NetworkObjectReference attackerRef)
        {
            StopAttack();
            base.Die(attackerRef);

            RecallAttacksRpc();
            ShowVictoryRpc();
        }
    }
}
