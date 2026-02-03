using UnityEngine;

public class InventorySlots : MonoBehaviour
{
    public PickupItem[] slots = new PickupItem[3];

    // 🔔 UI opcional (si existe, se refresca sola)
    public InventorySlotsUI slotsUI;

    public bool TryAdd(PickupItem item)
    {
        if (!item) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                slotsUI?.Refresh();
                return true;
            }
        }

        // inventario lleno
        return false;
    }

    public bool TryUseSlot(int index, PlayerManager player)
    {
        if (index < 0 || index >= slots.Length) return false;

        PickupItem item = slots[index];
        if (!item || !player) return false;

        switch (item.effectType)
        {
            case ItemEffectType.HealHealth:
                player.Heal(item.amount);
                break;

            case ItemEffectType.RestoreHunger:
                player.Eat(item.amount);
                break;
        }

        if (item.consumeOnUse)
            slots[index] = null;

        slotsUI?.Refresh();
        return true;
    }
}
