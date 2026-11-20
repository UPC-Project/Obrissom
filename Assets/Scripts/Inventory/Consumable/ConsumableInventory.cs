using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Obrissom.Player.Inventory
{
    /// <summary>
    /// Manages the 3 fixed consumable quick-use slots (F1, F2, F3).
    /// </summary>
    public class ConsumableInventory : MonoBehaviour
    {
        [Header("Connections")]
        [SerializeField] private Inventory _inventory;

        [Header("Input Actions (ConsumableUI)")]
        [SerializeField] private InputActionReference _useSlot1Action;
        [SerializeField] private InputActionReference _useSlot2Action;
        [SerializeField] private InputActionReference _useSlot3Action;

        public event Action OnConsumablesChanged;

        private PlayerCombat _playerCombat;
        private InventorySlot _consumableSlot1;
        private InventorySlot _consumableSlot2;
        private InventorySlot _consumableSlot3;

        public InventorySlot ConsumableSlot1 => _consumableSlot1;
        public InventorySlot ConsumableSlot2 => _consumableSlot2;
        public InventorySlot ConsumableSlot3 => _consumableSlot3;

        private void Awake()
        {
            _consumableSlot1 = new InventorySlot(null, 0, SlotType.Consumable);
            _consumableSlot2 = new InventorySlot(null, 0, SlotType.Consumable);
            _consumableSlot3 = new InventorySlot(null, 0, SlotType.Consumable);
        }

        private void OnEnable()
        {
            BindInputAction(_useSlot1Action, OnUseSlot1);
            BindInputAction(_useSlot2Action, OnUseSlot2);
            BindInputAction(_useSlot3Action, OnUseSlot3);
        }

        private void OnDisable()
        {
            UnbindInputAction(_useSlot1Action, OnUseSlot1);
            UnbindInputAction(_useSlot2Action, OnUseSlot2);
            UnbindInputAction(_useSlot3Action, OnUseSlot3);
        }

        public void SetInventory(Inventory inventory)
        {
            _inventory = inventory;
            _playerCombat = _inventory != null ? _inventory.GetComponent<PlayerCombat>() : null;
        }

        /// <summary>
        /// Moves or swaps a consumable item from the main inventory into one of the 3 consumable slots.
        /// </summary>
        public bool AssignFromInventory(int inventorySlotIndex, int consumableSlotIndex)
        {
            if (_inventory == null) return false;

            InventorySlot source = _inventory.Slots[inventorySlotIndex];
            if (source.IsEmpty) return false;

            InventorySlot targetSlot = GetSlotByIndex(consumableSlotIndex);
            if (targetSlot == null || !targetSlot.CanAccept(source.item)) return false;

            // Stack if both hold the same stackable item
            if (!targetSlot.IsEmpty && targetSlot.item == source.item && source.item.isStackable && targetSlot.quantity < source.item.maxStackSize)
            {
                int spaceLeft = source.item.maxStackSize - targetSlot.quantity;
                int toMove = Mathf.Min(source.quantity, spaceLeft);
                targetSlot.AddQuantity(toMove);
                source.RemoveQuantity(toMove);
                if (source.quantity <= 0)
                {
                    source.item = null;
                    source.quantity = 0;
                }
            }
            else
            {
                Item tempItem = targetSlot.item;
                int tempQuantity = targetSlot.quantity;

                targetSlot.item = source.item;
                targetSlot.quantity = source.quantity;

                source.item = tempItem;
                source.quantity = tempQuantity;
            }

            _inventory.TriggerInventoryChanged();
            OnConsumablesChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Returns a consumable item from a consumable slot back to the main inventory
        /// (at destinationIndex if provided, or the first available slot otherwise).
        /// </summary>
        public bool ReturnToInventory(int consumableSlotIndex, int destinationIndex = -1)
        {
            if (_inventory == null) return false;

            InventorySlot slot = GetSlotByIndex(consumableSlotIndex);
            if (slot == null || slot.IsEmpty) return false;

            if (destinationIndex >= 0 && destinationIndex < _inventory.Slots.Count)
            {
                InventorySlot destination = _inventory.Slots[destinationIndex];
                if (!destination.CanAccept(slot.item)) return false;

                if (destination.IsEmpty)
                {
                    destination.item = slot.item;
                    destination.quantity = slot.quantity;
                    slot.item = null;
                    slot.quantity = 0;
                }
                else if (destination.item == slot.item && slot.item.isStackable && destination.quantity < slot.item.maxStackSize)
                {
                    int spaceLeft = slot.item.maxStackSize - destination.quantity;
                    int toMove = Mathf.Min(slot.quantity, spaceLeft);
                    destination.AddQuantity(toMove);
                    slot.RemoveQuantity(toMove);
                    if (slot.quantity <= 0)
                    {
                        slot.item = null;
                        slot.quantity = 0;
                    }
                }
                else
                {
                    if (!slot.CanAccept(destination.item)) return false;

                    Item tempItem = destination.item;
                    int tempQuantity = destination.quantity;
                    destination.item = slot.item;
                    destination.quantity = slot.quantity;
                    slot.item = tempItem;
                    slot.quantity = tempQuantity;
                }

                _inventory.TriggerInventoryChanged();
                OnConsumablesChanged?.Invoke();
                return true;
            }

            bool returned = _inventory.AddItem(slot.item, slot.quantity);
            if (!returned) return false;

            slot.item = null;
            slot.quantity = 0;

            OnConsumablesChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Moves or swaps items between two consumable slots.
        /// </summary>
        public bool MoveConsumable(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex) return false;

            InventorySlot from = GetSlotByIndex(fromIndex);
            InventorySlot to = GetSlotByIndex(toIndex);

            if (from == null || to == null || from.IsEmpty) return false;

            if (!to.IsEmpty && to.item == from.item && from.item.isStackable && to.quantity < from.item.maxStackSize)
            {
                int spaceLeft = from.item.maxStackSize - to.quantity;
                int toMove = Mathf.Min(from.quantity, spaceLeft);
                to.AddQuantity(toMove);
                from.RemoveQuantity(toMove);
                if (from.quantity <= 0)
                {
                    from.item = null;
                    from.quantity = 0;
                }
            }
            else
            {
                Item tempItem = to.item;
                int tempQuantity = to.quantity;

                to.item = from.item;
                to.quantity = from.quantity;

                from.item = tempItem;
                from.quantity = tempQuantity;
            }

            OnConsumablesChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Consumes 1 unit of the item in the given slot and applies its effects to the player.
        /// </summary>
        public bool UseConsumable(int consumableSlotIndex)
        {
            if (_playerCombat == null || !_playerCombat.IsOwner) return false;

            InventorySlot slot = GetSlotByIndex(consumableSlotIndex);
            if (slot == null || slot.IsEmpty) return false;

            ApplyConsumableEffects(slot.item);

            slot.RemoveQuantity(1);
            if (slot.quantity <= 0)
            {
                slot.item = null;
                slot.quantity = 0;
            }

            OnConsumablesChanged?.Invoke();
            return true;
        }

        public InventorySlot GetSlotByIndex(int index)
        {
            return index switch
            {
                0 => _consumableSlot1,
                1 => _consumableSlot2,
                2 => _consumableSlot3,
                _ => null
            };
        }

        private void ApplyConsumableEffects(Item item)
        {
            if (item.consumableEffects == null) return;

            foreach (Item.ConsumableEffect effect in item.consumableEffects)
            {
                switch (effect.EffectType)
                {
                    case Item.ConsumableEffectType.RestoreHealth:
                        _playerCombat.HealRpc(effect.Value, false);
                        break;
                    case Item.ConsumableEffectType.RestoreResource:
                        _playerCombat.RestoreResourceRpc(effect.Value);
                        break;
                }
            }
        }

        private void OnUseSlot1(InputAction.CallbackContext ctx) => UseConsumable(0);
        private void OnUseSlot2(InputAction.CallbackContext ctx) => UseConsumable(1);
        private void OnUseSlot3(InputAction.CallbackContext ctx) => UseConsumable(2);

        private void BindInputAction(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
        {
            if (actionRef == null || actionRef.action == null) return;
            actionRef.action.Enable();
            actionRef.action.performed += callback;
        }

        private void UnbindInputAction(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
        {
            if (actionRef == null || actionRef.action == null) return;
            actionRef.action.performed -= callback;
        }
    }
}

