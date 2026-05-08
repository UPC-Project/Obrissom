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
                    // Look for an existing instance in the scene
                    _instance = FindFirstObjectByType<ItemDatabase>();

                    // If not found, create a new GameObject to host the database
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ItemDatabase_AutoSpawned");
                        _instance = go.AddComponent<ItemDatabase>();
                    }
                }
                return _instance;
            }
        }

        // Dictionary for high-speed item searching using the ID as key
        private Dictionary<int, Item> _itemDictionary = new Dictionary<int, Item>();
        private bool _isInitialized = false;

        private void Awake()
        {
            // Ensure there is only one instance of the database (Singleton)
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject); // Keep the database alive during scene changes

            InitializeDatabase();
        }

        /// <summary>
        /// Loads all Item ScriptableObjects from the 'Resources/Items' folder.
        /// </summary>
        private void InitializeDatabase()
        {
            if (_isInitialized) return;

            // Load all assets of type Item from the specific folder
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
                    // Important warning: two items cannot have the same ID
                    Debug.LogWarning($"Duplicate Item ID detected: {item.itemID}. Please check your Item assets.");
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Returns an Item object based on its numerical ID.
        /// This is O(1) complexity (instant search).
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