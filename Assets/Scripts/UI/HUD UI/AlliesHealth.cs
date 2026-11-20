using System;
using System.Collections.Generic;
using Obrissom.Player;
using Unity.Collections;
using UnityEngine;

namespace Obrissom.UI
{
    public class AlliesHealth : MonoBehaviour
    {
        public static AlliesHealth Instance { get; private set; }

        [Serializable]
        public struct ClassIconEntry
        {
            public PlayerClass classType;

            public Sprite icon;
            public Sprite damagedIcon;
        }

        [Header("UI References")]
        [SerializeField] private GameObject _allyHealthPrefab;
        [SerializeField] private Transform _container;

        [Header("Class Icons")]
        [SerializeField] private List<ClassIconEntry> _classIcons;

        private Dictionary<PlayerClass, ClassIconEntry> _iconsMap;
        private readonly Dictionary<PlayerCombat, AllyBinding> _allies = new();

        private class AllyBinding
        {
            public AllyHealthEntry Entry;
            public Unity.Netcode.NetworkVariable<float>.OnValueChangedDelegate OnHealthChanged;
            public Unity.Netcode.NetworkVariable<float>.OnValueChangedDelegate OnMaxHealthChanged;
            public Unity.Netcode.NetworkVariable<FixedString64Bytes>.OnValueChangedDelegate OnNameChanged;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_container == null)
                _container = transform;

            _iconsMap = new Dictionary<PlayerClass, ClassIconEntry>();
            foreach (var entry in _classIcons)
                _iconsMap[entry.classType] = entry;
        }

        private void Start()
        {
            // Catch any allies that spawned before this UI initialized
            foreach (PlayerCombat player in FindObjectsByType<PlayerCombat>(FindObjectsSortMode.None))
            {
                if (player.IsSpawned && !player.IsOwner)
                    RegisterAlly(player);
            }
        }

        public void RegisterAlly(PlayerCombat ally)
        {
            if (ally.IsOwner || _allies.ContainsKey(ally))
                return;

            GameObject go = Instantiate(_allyHealthPrefab, _container);
            AllyHealthEntry entry = go.GetComponent<AllyHealthEntry>();

            _iconsMap.TryGetValue(ally._playerClass.Value, out ClassIconEntry icons);
            entry.Setup(
                icons.icon,
                icons.damagedIcon,
                ally._playerName.Value.ToString(),
                ally._health.Value,
                ally.Stats.maxHealth.Value
            );

            var binding = new AllyBinding
            {
                Entry = entry,
                OnHealthChanged = (_, newHealth) => entry.UpdateHealth(newHealth, ally.Stats.maxHealth.Value),
                OnMaxHealthChanged = (_, newMax) => entry.UpdateHealth(ally._health.Value, newMax),
                OnNameChanged = (_, newName) => entry.UpdateName(newName.ToString())
            };

            ally._health.OnValueChanged += binding.OnHealthChanged;
            ally.Stats.maxHealth.OnValueChanged += binding.OnMaxHealthChanged;
            ally._playerName.OnValueChanged += binding.OnNameChanged;

            _allies[ally] = binding;
        }

        public void UnregisterAlly(PlayerCombat ally)
        {
            if (ally == null || !_allies.TryGetValue(ally, out AllyBinding binding))
                return;

            ally._health.OnValueChanged -= binding.OnHealthChanged;
            ally.Stats.maxHealth.OnValueChanged -= binding.OnMaxHealthChanged;
            ally._playerName.OnValueChanged -= binding.OnNameChanged;

            if (binding.Entry != null)
                Destroy(binding.Entry.gameObject);

            _allies.Remove(ally);
        }

        private void OnDestroy()
        {
            foreach (var kvp in _allies)
            {
                if (kvp.Key == null) continue;
                kvp.Key._health.OnValueChanged -= kvp.Value.OnHealthChanged;
                kvp.Key.Stats.maxHealth.OnValueChanged -= kvp.Value.OnMaxHealthChanged;
                kvp.Key._playerName.OnValueChanged -= kvp.Value.OnNameChanged;
            }
            _allies.Clear();

            if (Instance == this)
                Instance = null;
        }
    }
}
