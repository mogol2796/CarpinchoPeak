using UnityEngine;

public class InventorySlots : MonoBehaviour
{
    public ItemData[] slots = new ItemData[3];

    public bool TryUseSlot(int index, PlayerManager player)
    {
        if (index < 0 || index >= slots.Length) return false;
        ItemData item = slots[index];
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

        return true;
    }
}
