using UnityEngine;

public class InventorySlotsUI : MonoBehaviour
{
    [Header("Refs")]
    public InventorySlots inventory;
    public InventorySlotUI[] slotUIs;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!inventory || slotUIs == null) return;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (i >= inventory.slots.Length || inventory.slots[i] == null)
            {
                slotUIs[i].SetEmpty();
            }
            else
            {
                slotUIs[i].SetItem(inventory.slots[i]);
            }
        }
    }
}
