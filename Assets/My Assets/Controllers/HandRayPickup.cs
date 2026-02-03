using UnityEngine;
using UnityEngine.InputSystem;

public class HandRayPickup : MonoBehaviour
{
    [Header("Refs")]
    public InventorySlots inventory;

    [Header("Raycast")]
    public Transform rayOrigin;
    public float rayDistance = 2.0f;
    public LayerMask pickupMask;
    public bool showDebugRay = true;

    [Header("Input")]
    public InputActionProperty triggerAction;

    [Header("Prompt")]
    public float fullInventoryMsgSeconds = 0.8f;

    private PickupItem _currentTarget;
    private float _fullMsgTimer;

    void OnEnable() => triggerAction.action?.Enable();
    void OnDisable() => triggerAction.action?.Disable();

    void Update()
    {
        if (!inventory) return;

        UpdateRayTarget();

        if (triggerAction.action != null && triggerAction.action.WasPressedThisFrame())
            TryPickup();

        if (_fullMsgTimer > 0f)
        {
            _fullMsgTimer -= Time.deltaTime;
            if (_fullMsgTimer <= 0f)
            {
                // cuando se acabe el timer, vuelve a enseñar el prompt del target actual (si existe)
                if (_currentTarget != null)
                    _currentTarget.ShowPrompt(true);
            }
        }
    }

    private void UpdateRayTarget()
    {
        Transform o = rayOrigin ? rayOrigin : transform;

        Ray ray = new Ray(o.position, o.forward);
        if (showDebugRay) Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.cyan);

        PickupItem hitPickup = null;

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, pickupMask, QueryTriggerInteraction.Ignore))
            hitPickup = hit.collider.GetComponentInParent<PickupItem>();
        
        if (hitPickup == _currentTarget) return;

        // ✅ apaga prompt del target anterior
        if (_currentTarget != null)
            _currentTarget.ShowPrompt(false);

        _currentTarget = hitPickup;

        // si veníamos de "inventario lleno", no muestres prompt hasta que acabe el timer
        if (_fullMsgTimer > 0f) return;

        // ✅ enciende prompt del nuevo target
        if (_currentTarget != null)
            _currentTarget.ShowPrompt(true);
    }

    private void TryPickup()
    {
        if (_currentTarget == null) return;
        if (_fullMsgTimer > 0f) return;

        bool added = inventory.TryAdd(_currentTarget);

        if (added)
        {
            // ✅ apaga prompt antes de desactivar el objeto
            _currentTarget.ShowPrompt(false);
            _currentTarget.OnPickedUp();
            _currentTarget = null;
        }
        else
        {
            // inventario lleno => oculta prompt un rato
            _currentTarget.ShowPrompt(false);
            _fullMsgTimer = fullInventoryMsgSeconds;
        }
    }
}
