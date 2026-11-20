using System.Collections.Generic;
using Obrissom.Player;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Enemy
{
    public class BossRootPool : MonoBehaviour
    {
        [SerializeField] private BossRoot _rootPrefab;
        [Tooltip("Roots created when the boss spawns.")]
        [SerializeField, Min(1)] private int _poolSize = 10;
        [Tooltip("The pool grows up to this many when an attack needs more roots.")]
        [SerializeField, Min(1)] private int _maxRoots = 60;
        [Tooltip("After a root hits a player, other roots can't hit that player for this long.")]
        [SerializeField, Min(0f)] private float _hitCooldown = 0.75f;

        private struct PendingRoot
        {
            public BossRoot Root;
            public Vector3 Position;
            public double StartServerTime;
            public float Damage;
            public int GroundMask;
            public float Size;
        }

        private readonly List<BossRoot> _roots = new List<BossRoot>();
        private readonly List<PendingRoot> _pending = new List<PendingRoot>();
        private readonly HashSet<BossRoot> _reserved = new HashSet<BossRoot>();
        private readonly Dictionary<PlayerCombat, float> _lastHit = new Dictionary<PlayerCombat, float>();
        private Transform _container;

        private static double ServerTime =>
            NetworkManager.Singleton != null ? NetworkManager.Singleton.ServerTime.Time : Time.timeAsDouble;

        public void Build()
        {
            if (_roots.Count > 0) return;
            if (_rootPrefab == null)
            {
                Debug.LogError($"[BossRootPool] {name} has no root prefab assigned.", this);
                return;
            }

            _container = new GameObject($"{name} Roots").transform;
            for (int i = 0; i < _poolSize; i++) CreateRoot();
        }

        public void Spawn(Vector3 position, double startServerTime, float damage, int groundMask, float size)
        {
            BossRoot root = TakeFreeRoot();
            if (root == null) return;

            _reserved.Add(root);
            _pending.Add(new PendingRoot
            {
                Root = root,
                Position = position,
                StartServerTime = startServerTime,
                Damage = damage,
                GroundMask = groundMask,
                Size = size
            });
        }

        public void RecallAll()
        {
            _pending.Clear();
            _reserved.Clear();
            foreach (BossRoot root in _roots)
            {
                if (root != null && root.IsActive) root.ReturnToPool();
            }
        }

        public void Clear()
        {
            _pending.Clear();
            _reserved.Clear();
            if (_container != null) Destroy(_container.gameObject);
            _container = null;
            _roots.Clear();
        }

        private void OnDestroy() => Clear();

        private void Update()
        {
            if (_pending.Count == 0) return;

            double now = ServerTime;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingRoot pending = _pending[i];
                if (pending.StartServerTime > now) continue;

                _pending.RemoveAt(i);
                _reserved.Remove(pending.Root);
                if (pending.Root != null)
                {
                    pending.Root.Activate(pending.Position, pending.StartServerTime, pending.Damage, pending.GroundMask, pending.Size);
                }
            }
        }

        private BossRoot CreateRoot()
        {
            if (_container == null) return null;

            BossRoot root = Instantiate(_rootPrefab, _container);
            root.name = $"{_rootPrefab.name} {_roots.Count}";
            root.gameObject.SetActive(false);
            if (root.DamageTrigger != null) root.DamageTrigger.CanHit = CanHitPlayer;
            _roots.Add(root);
            return root;
        }

        private BossRoot TakeFreeRoot()
        {
            BossRoot oldest = null;
            foreach (BossRoot root in _roots)
            {
                if (root == null || _reserved.Contains(root)) continue;
                if (!root.IsActive) return root;
                if (oldest == null || root.ActiveSince < oldest.ActiveSince) oldest = root;
            }

            if (_roots.Count < _maxRoots) return CreateRoot();

            if (oldest != null) oldest.ReturnToPool();
            return oldest;
        }

        private bool CanHitPlayer(PlayerCombat player)
        {
            if (_lastHit.TryGetValue(player, out float lastHit) && Time.time - lastHit < _hitCooldown) return false;

            _lastHit[player] = Time.time;
            return true;
        }
    }
}
