[System.Serializable]
public class InventorySlot
{
    public Item item;
    public int quantity;

    public InventorySlot(Item newItem, int amount)
    {
        item = newItem;
        quantity = amount;
    }

    public void AddQuantity(int amount) => quantity += amount;
    public void RemoveQuantity(int amount) => quantity -= amount; 

    public bool IsEmpty => item == null || quantity <= 0;
}