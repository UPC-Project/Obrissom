using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.Enemy
{
    public class BossRootPool : MonoBehaviour
    {
        [SerializeField] private BossRoot _rootPrefab;
        [SerializeField, Min(1)] private int _poolSize = 10;

        private readonly List<BossRoot> _roots = new List<BossRoot>();
        private Transform _container;

        public void Build()
        {
            if (_roots.Count > 0) return;
            if (_rootPrefab == null)
            {
                Debug.LogError($"[BossRootPool] {name} has no root prefab assigned.", this);
                return;
            }

            _container = new GameObject($"{name} Roots").transform;
            for (int i = 0; i < _poolSize; i++)
            {
                BossRoot root = Instantiate(_rootPrefab, _container);
                root.name = $"{_rootPrefab.name} {i}";
                root.gameObject.SetActive(false);
                _roots.Add(root);
            }
        }

        public void Spawn(Vector3 position, double startServerTime, float damage, int groundMask)
        {
            BossRoot root = TakeFreeRoot();
            if (root != null) root.Activate(position, startServerTime, damage, groundMask);
        }

        public void RecallAll()
        {
            foreach (BossRoot root in _roots)
            {
                if (root != null && root.IsActive) root.ReturnToPool();
            }
        }

        public void Clear()
        {
            if (_container != null) Destroy(_container.gameObject);
            _container = null;
            _roots.Clear();
        }

        private void OnDestroy() => Clear();

        private BossRoot TakeFreeRoot()
        {
            BossRoot oldest = null;
            foreach (BossRoot root in _roots)
            {
                if (root == null) continue;
                if (!root.IsActive) return root;
                if (oldest == null || root.ActiveSince < oldest.ActiveSince) oldest = root;
            }

            if (oldest != null) oldest.ReturnToPool();
            return oldest;
        }
    }
}
