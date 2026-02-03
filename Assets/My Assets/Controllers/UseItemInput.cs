using UnityEngine;
using UnityEngine.InputSystem;

public class UseItemInput : MonoBehaviour
{
    [Header("Refs")]
    public PlayerManager player;
    public InventorySlots inventory;
    public HUDMenuToggle hudMenu;

    public InputActionProperty useSlot1; // slot 0
    public InputActionProperty useSlot2; // slot 1
    public InputActionProperty useSlot3; // slot 2

    void OnEnable()
    {
        useSlot1.action?.Enable();
        useSlot2.action?.Enable();
        useSlot3.action?.Enable();
    }

    void OnDisable()
    {
        useSlot1.action?.Disable();
        useSlot2.action?.Disable();
        useSlot3.action?.Disable();
    }

    void Update()
    {
        if (!player || !inventory) return;

        // Sólo consumir si el inventario está abierto
        if (hudMenu == null || !hudMenu.IsOpen()) return;

        if (useSlot1.action != null && useSlot1.action.WasPressedThisFrame())
            inventory.TryUseSlot(0, player);

        if (useSlot2.action != null && useSlot2.action.WasPressedThisFrame())
            inventory.TryUseSlot(1, player);

        if (useSlot3.action != null && useSlot3.action.WasPressedThisFrame())
            inventory.TryUseSlot(2, player);
    }
}
