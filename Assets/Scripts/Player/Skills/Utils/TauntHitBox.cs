using Obrissom.Enemy;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class TauntHitBox : MonoBehaviour
{
    private NetworkObject _netObj;
    private SphereCollider _collider;

    private float _tauntDuration;
    private HashSet<EnemyBase> _tauntedEnemies = new HashSet<EnemyBase>();

    private void Awake()
    {
        _netObj = GetComponentInParent<NetworkObject>();
        _collider = GetComponent<SphereCollider>();
    }

    public void SetupCollider(float radius, float tauntDuration)
    {
        _collider.radius = radius;
        _tauntDuration = tauntDuration;
        _tauntedEnemies.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_netObj.IsOwner || !other.transform.root.CompareTag("Enemy")) return;

        EnemyBase enemy = other.transform.root.GetComponent<EnemyBase>();
        if (enemy == null || _tauntedEnemies.Contains(enemy)) return;

        _tauntedEnemies.Add(enemy);
        enemy.ApplyTauntRpc(_netObj, _tauntDuration);
    }
}

