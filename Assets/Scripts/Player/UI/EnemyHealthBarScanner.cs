using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    /// Could be used if player needs to scan something else
    public class EnemyHealthBarScanner : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private float _scanInterval = 0.2f;
        [SerializeField] private float _activateRadius = 20f;
        [SerializeField] private float _deactivateRadius = 22f;

        private HashSet<EnemyUI> _activeHealthBars = new HashSet<EnemyUI>();

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                StartCoroutine(ScanForEnemiesRoutine());
            }
        }

        private IEnumerator ScanForEnemiesRoutine()
        {
            var wait = new WaitForSeconds(_scanInterval);

            while (true)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, _activateRadius, _enemyLayer);
                foreach (var hit in hits)
                {
                    EnemyUI enemyUi = hit.transform.root.GetComponentInChildren<EnemyUI>();
                    if (enemyUi == null || _activeHealthBars.Contains(enemyUi)) continue;

                    enemyUi.SetHealthBarActive(true);
                    _activeHealthBars.Add(enemyUi);
                }

                List<EnemyUI> toRemove = new List<EnemyUI>();
                foreach (var activeBar in _activeHealthBars)
                {
                    if (activeBar == null)
                    {
                        toRemove.Add(activeBar);
                        continue;
                    }

                    float distance = Vector3.Distance(transform.position, activeBar.transform.position);
                    if (distance > _deactivateRadius)
                    {
                        activeBar.SetHealthBarActive(false);
                        toRemove.Add(activeBar);
                    }
                }

                foreach (var bar in toRemove) _activeHealthBars.Remove(bar);

                yield return wait;
            }
        }
    }
}