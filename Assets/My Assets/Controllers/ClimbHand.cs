using UnityEngine;
using UnityEngine.InputSystem;

public class ClimbHand : MonoBehaviour
{
    [Header("Refs")]
    public ClimbManager climbManager;
    public InputActionProperty grabAction;
    public float pressThreshold = 0.6f;

    [Header("Climbable")]
    public LayerMask climbableMask;

    [Header("Visual Debug")]
    public MeshRenderer handRenderer;
    public Material freeMat;
    public Material touchMat;
    public Material grabMat;

    public bool IsGrabbing { get; private set; }
    public bool HasClimbContact => climbContacts > 0;
    public Vector3 WallNormal { get; private set; } = Vector3.zero;
    public Vector3 HandWorldPos => transform.position;

    private int climbContacts = 0;

    private void OnEnable()
    {
        grabAction.action?.Enable();
        climbManager?.RegisterHand(this);
        SetFree();
    }

    private void OnDisable()
    {
        climbManager?.UnregisterHand(this);
        grabAction.action?.Disable();
    }

    private void Update()
    {
        float v = grabAction.action.ReadValue<float>();
        bool pressed = v >= pressThreshold;

        if (pressed && climbContacts > 0)
        {
            if (!IsGrabbing)
            {
                IsGrabbing = true;
                SetGrab();
                climbManager?.TryBeginClimb(this);
            }
        }
        else
        {
            if (IsGrabbing)
            {
                IsGrabbing = false;
                climbManager?.TryEndClimb(this);
            }

            if (climbContacts > 0) SetTouch();
            else SetFree();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) != 0)
        {
            climbContacts++;
            if (!IsGrabbing) SetTouch();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) != 0)
        {
            climbContacts = Mathf.Max(0, climbContacts - 1);
            if (climbContacts == 0)
            {
                WallNormal = Vector3.zero;
                if (!IsGrabbing) SetFree();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) == 0) return;

        Vector3 p = other.ClosestPoint(transform.position);
        Vector3 n = transform.position - p;
        if (n.sqrMagnitude > 0.0001f) WallNormal = n.normalized;
    }

    private void SetFree()  { if (handRenderer) handRenderer.material = freeMat; }
    private void SetTouch() { if (handRenderer) handRenderer.material = touchMat; }
    private void SetGrab()  { if (handRenderer) handRenderer.material = grabMat; }
}
