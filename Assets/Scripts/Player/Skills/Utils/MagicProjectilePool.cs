using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class MagicProjectilePool : NetworkBehaviour
{
    public static MagicProjectilePool Instance { get; private set; }
    [SerializeField] protected GameObject prefab;
    [SerializeField] protected int initialSize = 5;
    private Queue<ProjectileTrigger> _pool = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        for (int i = 0; i < initialSize; i++)
        {
            _pool.Enqueue(CreateNew());
        }
    }

    private ProjectileTrigger CreateNew()
    {
        var obj = Instantiate(prefab, transform);
        obj.GetComponent<NetworkObject>().Spawn();
        ProjectileTrigger trigger = obj.GetComponent<ProjectileTrigger>();
        trigger.SetActiveClientRpc(false);
        return trigger;
    }

    public ProjectileTrigger Get(Vector3 position)
    {
        ProjectileTrigger obj = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        if (obj.TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(position, obj.transform.rotation, obj.transform.localScale);
        }
        else
        {
            obj.transform.position = position;
        }

        obj.SetActiveClientRpc(true);
        return obj;
    }

    public void Return(ProjectileTrigger obj)
    {
        obj.SetActiveClientRpc(false);
        _pool.Enqueue(obj);
    }

}
