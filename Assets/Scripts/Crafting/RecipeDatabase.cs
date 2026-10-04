using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.Database
{
    /// <summary>
    /// Central database that stores all game recipes.
    /// </summary>
    public class RecipeDatabase : MonoBehaviour
    {
        private static RecipeDatabase _instance;

        /// <summary>
        /// Global access point. If no instance exists in the scene,
        /// it creates one automatically (Lazy Initialization).
        /// </summary>
        public static RecipeDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RecipeDatabase>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RecipeDatabase_AutoSpawned");
                        _instance = go.AddComponent<RecipeDatabase>();
                    }
                }
                return _instance;
            }
        }

        private Dictionary<int, Recipe> _recipeDictionary = new Dictionary<int, Recipe>();
        private List<Recipe> _allRecipes = new List<Recipe>();
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
        /// Loads all Recipe ScriptableObjects from the 'Resources/Recipes' folder.
        /// </summary>
        private void InitializeDatabase()
        {
            if (_isInitialized) return;

            Recipe[] recipes = Resources.LoadAll<Recipe>("Recipes");

            foreach (Recipe recipe in recipes)
            {
                if (!_recipeDictionary.ContainsKey(recipe.recipeID))
                {
                    _recipeDictionary.Add(recipe.recipeID, recipe);
                    _allRecipes.Add(recipe);
                }
                else
                {
                    Debug.LogWarning($"Duplicate Recipe ID detected: {recipe.recipeID} ({recipe.result.itemName})");
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Returns a Recipe based on its numerical ID.
        /// </summary>
        public Recipe GetRecipeByID(int id)
        {
            if (_recipeDictionary.TryGetValue(id, out Recipe recipe))
            {
                return recipe;
            }

            return null;
        }

        /// <summary>
        /// Returns a read-only list of all available recipes.
        /// Used by CraftingMenu to populate the UI.
        /// </summary>
        public IReadOnlyList<Recipe> GetAllRecipes() => _allRecipes;
    }
}

