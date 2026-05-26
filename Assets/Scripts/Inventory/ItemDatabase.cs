using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.Database
{
    /// <summary>
    /// Central database that stores all game items.
    /// It uses a Singleton pattern to be accessible from any script.
    /// </summary>
    public class ItemDatabase : MonoBehaviour
    {
        private static ItemDatabase _instance;

        /// <summary>
        /// Global access point. If no instance exists in the scene, 
        /// it creates one automatically (Lazy Initialization).
        /// </summary>
        public static ItemDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ItemDatabase>();


                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ItemDatabase_AutoSpawned");
                        _instance = go.AddComponent<ItemDatabase>();
                    }
                }
                return _instance;
            }
        }

        private Dictionary<int, Item> _itemDictionary = new Dictionary<int, Item>();
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDatabase();
        }

        /// <summary>
        /// Loads all Item ScriptableObjects from the 'Resources/Items' folder.
        /// </summary>
        private void InitializeDatabase()
        {
            if (_isInitialized) return;

            Item[] allItems = Resources.LoadAll<Item>("Items");

            foreach (Item item in allItems)
            {
                // Verify that the ID is unique before adding it to the dictionary
                if (!_itemDictionary.ContainsKey(item.itemID))
                {
                    _itemDictionary.Add(item.itemID, item);
                }
                else
                {
                    Debug.LogWarning($"Duplicate Item ID detected: {item.itemID}");
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Returns an Item object based on its numerical ID.
        /// </summary>
        public Item GetItemByID(int id)
        {
            if (_itemDictionary.TryGetValue(id, out Item item))
            {
                return item;
            }

            return null;
        }
    }
}