using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [Serializable]
    public struct ClassPrefab
    {
        public PlayerClass ClassType;
        public GameObject Prefab;
    }

    [SerializeField] private List<ClassPrefab> _classPrefabs;

    private Dictionary<PlayerClass, GameObject> _prefabsMap;

    private void Awake()
    {
        _prefabsMap = new Dictionary<PlayerClass, GameObject>();
        foreach (var entry in _classPrefabs)
            _prefabsMap[entry.ClassType] = entry.Prefab;
    }

    public override void OnNetworkSpawn()
    {
        SpawnPlayerServerRpc(LobbyNetworking.Instance.SelectedClass, LobbyNetworking.Instance.PlayerName);
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerServerRpc(PlayerClass playerClass, string playerName, RpcParams rpcParams = default)
    {
        GameObject prefab = _prefabsMap[playerClass];
        GameObject instance = Instantiate(prefab);
        if (instance.TryGetComponent(out Obrissom.Player.PlayerCombat combat))
            combat.SetPlayerIdentity(playerName, playerClass);
        instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(rpcParams.Receive.SenderClientId);
    }
}

