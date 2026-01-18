using UnityEngine;
using UnityEngine.InputSystem;

public class ClimbHand : MonoBehaviour
{
    [Header("References")]
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

    private int climbContacts = 0;
    private bool isGrabbing = false;

    private void OnEnable()
    {
        grabAction.action?.Enable();
        SetFree();
    }

    private void OnDisable()
    {
        grabAction.action?.Disable();
    }

    private void Update()
    {
        float v = grabAction.action.ReadValue<float>();
        bool pressed = v >= pressThreshold;

        if (pressed && climbContacts > 0)
        {
            isGrabbing = true;
            SetGrab();
            climbManager.TryBeginClimb(this);
        }
        else
        {
            if (isGrabbing)
            {
                climbManager.TryEndClimb(this);
            }

            isGrabbing = false;

            if (climbContacts > 0)
                SetTouch();
            else
                SetFree();
        }
    }

    public Vector3 HandWorldPos => transform.position;

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) != 0)
        {
            climbContacts++;
            if (!isGrabbing)
                SetTouch();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) != 0)
        {
            climbContacts = Mathf.Max(0, climbContacts - 1);
            if (climbContacts == 0 && !isGrabbing)
                SetFree();
        }
    }

    private void SetFree()
    {
        if (handRenderer) handRenderer.material = freeMat;
    }

    private void SetTouch()
    {
        if (handRenderer) handRenderer.material = touchMat;
    }

    private void SetGrab()
    {
        if (handRenderer) handRenderer.material = grabMat;
    }
}
