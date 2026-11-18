using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Obrissom.Player
{
    public class InteractableScanner : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask _interactableLayer = ~0;
        [SerializeField] private float _scanInterval = 0.2f;
        [SerializeField] private float _activateRadius = 3f;
        [SerializeField] private float _deactivateRadius = 4f;

        private readonly HashSet<IInteractable> _activePrompts = new HashSet<IInteractable>();
        private readonly List<IInteractable> _toRemove = new List<IInteractable>();

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                StartCoroutine(ScanForInteractablesRoutine());
        }

        private IEnumerator ScanForInteractablesRoutine()
        {
            var wait = new WaitForSeconds(_scanInterval);

            while (true)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, _activateRadius, _interactableLayer);
                foreach (var hit in hits)
                {
                    IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                    if (interactable == null) continue;

                    interactable.SetInteractPromptActive(true);
                    _activePrompts.Add(interactable);
                }

                _toRemove.Clear();
                foreach (var activePrompt in _activePrompts)
                {
                    if ((activePrompt as Object) == null)
                    {
                        _toRemove.Add(activePrompt);
                        continue;
                    }

                    if (Vector3.Distance(transform.position, activePrompt.Position) > _deactivateRadius)
                    {
                        activePrompt.SetInteractPromptActive(false);
                        _toRemove.Add(activePrompt);
                    }
                }

                foreach (var prompt in _toRemove)
                    _activePrompts.Remove(prompt);

                yield return wait;
            }
        }
    }
}

