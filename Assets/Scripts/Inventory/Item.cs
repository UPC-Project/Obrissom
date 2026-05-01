using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("General Info")]
    public string itemName;

    [TextArea]
    [SerializeField] private string _description;
    public Sprite icon;

    [Header("Stacking Properties")]
    public bool isStackable;
    public int maxStackSize = 1;

    [Header("Type")]
    public ItemType itemType;

    // [Header("Stat Modifiers")]
    // TODO: Implement modifier system when player statistics system is available

    public enum ItemType
    {
        Rings,
        Consumable,
        QuestItem,
        Misc
    }
}