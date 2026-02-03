using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class HandRayPickup : MonoBehaviour
{
    [Header("Refs")]
    public InventorySlots inventory;

    [Header("Raycast")]
    public Transform rayOrigin;
    public float rayDistance = 2.0f;
    public LayerMask pickupMask;
    public bool showDebugRay = true;

    [Header("Ray Visual")]
    public bool showRayInGame = true;
    public Color rayColor = Color.cyan;
    public float rayWidth = 0.01f;

    [Header("Input")]
    public InputActionProperty triggerAction;

    [Header("Prompt")]
    public float fullInventoryMsgSeconds = 0.8f;

    private PickupItem _currentTarget;
    private float _fullMsgTimer;
    private LineRenderer _line;

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
        if (_line == null) _line = gameObject.AddComponent<LineRenderer>();

        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.material = new Material(Shader.Find("Sprites/Default"));
    }

    void OnEnable() => triggerAction.action?.Enable();
    void OnDisable() => triggerAction.action?.Disable();

    void Update()
    {
        UpdateRayVisual();

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

        // if (_currentTarget != null)
        //     Debug.Log($"[HandRayPickup] Ray tocó pickup '{_currentTarget.displayName}' ({_currentTarget.gameObject.name})", _currentTarget);

        // si veníamos de "inventario lleno", no muestres prompt hasta que acabe el timer
        if (_fullMsgTimer > 0f) return;

        // ✅ enciende prompt del nuevo target
        if (_currentTarget != null)
            _currentTarget.ShowPrompt(true);
    }

    private void UpdateRayVisual()
    {
        if (_line == null) return;

        if (!showRayInGame)
        {
            if (_line.enabled) _line.enabled = false;
            return;
        }

        Transform o = rayOrigin ? rayOrigin : transform;

        _line.enabled = true;
        _line.startColor = rayColor;
        _line.endColor = rayColor;
        _line.startWidth = rayWidth;
        _line.endWidth = rayWidth;

        _line.SetPosition(0, o.position);
        _line.SetPosition(1, o.position + o.forward * rayDistance);
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
