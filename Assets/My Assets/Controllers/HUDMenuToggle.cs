using UnityEngine;
using UnityEngine.InputSystem;

public class HUDMenuToggle : MonoBehaviour
{
    [Header("Input")]
    public InputActionProperty toggleAction;

    [Header("Refs")]
    public Transform centerEye;
    public GameObject panelRoot;

    [Header("Placement (relative to the view)")]
    public Vector3 localOffset = new Vector3(0f, -0.05f, 0.5f);
    public bool faceCamera = true;

    [Header("Smoothing")]
    public float followLerp = 18f;
    public float rotLerp = 18f;

    public bool IsOpen() => _visible;
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

        Vector3 targetPos = centerEye.TransformPoint(localOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

        if (faceCamera)
        {
            Quaternion targetRot = Quaternion.LookRotation(transform.position - centerEye.position, Vector3.up);
            targetRot = Quaternion.LookRotation(centerEye.position - transform.position, Vector3.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotLerp * Time.deltaTime));
        }
    }
}
