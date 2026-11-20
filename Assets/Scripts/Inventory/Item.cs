using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("General Info")]
    public string itemName;
    [TextArea] public string description;
    public Sprite image;

    [Tooltip("Must be unique")]
    public int itemID;

    [Header("Stacking Properties")]
    public bool isStackable;
    public int maxStackSize = 1;

    [Header("Type")]
    public ItemType itemType;


    [Header("Stat Modifiers. Leave empty for non equippable items.")]
    public List<Modifier> modifiers;

    public enum ItemType
    {
        Equippable,
        Consumable,
        QuestItem,
        Misc
    }

    [Serializable]
    public class Modifier
    {
        public float Value;
        public Stats Stat;
    }
}
