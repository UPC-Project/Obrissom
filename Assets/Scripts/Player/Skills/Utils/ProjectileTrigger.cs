using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(MeshRenderer))]
public class ProjectileTrigger : NetworkBehaviour
{
    public event Action<Collider> OnHit;

    [SerializeField] private Renderer _renderer;
    [SerializeField] private Collider _collider;
    [SerializeField] private ParticleSystem _fire;
    [SerializeField] private ParticleSystem _smoke;
    [SerializeField] private ParticleSystem _sparks;
    public override void OnNetworkSpawn()
    {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();

        // clients joins after projectiles where spawn by the pool
        if (!IsServer)
        {
            _renderer.enabled = false;
            _collider.enabled = false;
        }
    }

    public void ClearSubscriptions() => OnHit = null;

    private void OnTriggerEnter(Collider col)
    {
        if (!IsServer) return;
        OnHit?.Invoke(col);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void SetActiveClientRpc(bool visible)
    {
        if (_renderer != null) _renderer.enabled = visible;
        if (_collider != null) _collider.enabled = visible;


        if (_fire != null)
        {
            var emission = _fire.emission;
            emission.enabled = visible; 
        }

        if (_smoke != null)
        {
            var emission = _smoke.emission;
            emission.enabled = visible;
        }

        if (_sparks != null)
        {
            var emission = _sparks.emission;
            emission.enabled = visible;
        }
    }

}
