using UnityEngine;
using UnityEngine.InputSystem;

public class HUDMenuToggle : MonoBehaviour
{
    [Header("Input")]
    public InputActionProperty toggleAction; // botón Y (o el que uses)

    [Header("Refs")]
    public Transform centerEye;      // CenterEyeAnchor
    public GameObject panelRoot;     // tu Panel (o Canvas)

    [Header("Placement (relative to the view)")]
    public Vector3 localOffset = new Vector3(0f, -0.05f, 0.5f);
    public bool faceCamera = true;

    [Header("Smoothing")]
    public float followLerp = 18f;   // 10-30
    public float rotLerp = 18f;

    private bool _visible;

    void OnEnable()
    {
        toggleAction.action?.Enable();
    }

    void OnDisable()
    {
        toggleAction.action?.Disable();
    }

    void Start()
    {
        if (panelRoot) panelRoot.SetActive(false);
        _visible = false;
    }

    void Update()
    {
        if (toggleAction.action != null && toggleAction.action.WasPressedThisFrame())
        {
            _visible = !_visible;
            if (panelRoot) panelRoot.SetActive(_visible);
        }
    }

    void LateUpdate()
    {
        if (!_visible) return;
        if (!centerEye) return;

        // Posición objetivo delante de la cámara
        Vector3 targetPos = centerEye.TransformPoint(localOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

        if (faceCamera)
        {
            Quaternion targetRot = Quaternion.LookRotation(transform.position - centerEye.position, Vector3.up);
            // lo queremos mirando hacia la cámara, así que invertimos forward
            targetRot = Quaternion.LookRotation(centerEye.position - transform.position, Vector3.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotLerp * Time.deltaTime));
        }
    }
}
