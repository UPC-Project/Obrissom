using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProjectileTrigger : NetworkBehaviour
{
    public event Action<Collider> OnHit;

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private ParticleSystem[] _particleSystems;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    public override void OnNetworkSpawn()
    {
        // clients joins after projectiles were spawn by the pool
        if (!IsServer)
        {
            SetComponentsEnabled(false);
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
        SetComponentsEnabled(visible);

        if (_particleSystems == null || _particleSystems.Length == 0)
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in _particleSystems)
        {
            if (ps == null) continue;

            var emission = ps.emission;
            emission.enabled = visible;

            if (visible)
            {
                ps.Play(true);
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void SetComponentsEnabled(bool isEnabled)
    {
        if (_renderers != null)
        {
            foreach (var r in _renderers)
                if (r != null) r.enabled = isEnabled;
        }

        if (_colliders != null)
        {
            foreach (var c in _colliders)
                if (c != null) c.enabled = isEnabled;
        }
    }
}
