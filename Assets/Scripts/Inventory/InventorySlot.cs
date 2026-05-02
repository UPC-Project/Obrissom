namespace Obrissom.Player.Inventory
{
    public enum SlotType { All, Equipment }


    [System.Serializable]
    public class InventorySlot
    {
        public Item item;
        public int quantity;
        public SlotType slotType;
        public Item.EquipmentSlotType acceptedEquipmentType;

        public InventorySlot(Item newItem, int amount, SlotType slotType = SlotType.All, Item.EquipmentSlotType acceptedType = Item.EquipmentSlotType.Ring)
        {
            item = newItem;
            quantity = amount;
            this.slotType = slotType;
            this.acceptedEquipmentType = acceptedType;
        }

        public bool CanAccept(Item incomingItem)
        {
            if (slotType == SlotType.All) return true;
            return incomingItem.isEquippable && incomingItem.equipmentSlotType == acceptedEquipmentType;
        }

        public void AddQuantity(int amount) => quantity += amount;
        public void RemoveQuantity(int amount) => quantity -= amount;
        public bool IsEmpty => item == null || quantity <= 0;
    }
}