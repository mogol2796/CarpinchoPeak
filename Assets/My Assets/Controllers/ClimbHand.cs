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
    [Header("Hand Visual (to tint)")]
    public Renderer handRenderer;
    public string colorProperty = "_BaseColor";

    [Header("Stamina Colors")]
    public Color highColor = new Color(0.2f, 1f, 0.2f, 1f);
    public Color midColor  = new Color(1f, 0.85f, 0.2f, 1f);
    public Color lowColor  = new Color(1f, 0.25f, 0.25f, 1f);
    public float lowThreshold = 0.25f;
    public float midThreshold = 0.6f;

    // [Header("Visual Debug")]
    // public MeshRenderer handRenderer;
    // public Material freeMat;
    // public Material touchMat;
    // public Material grabMat;

    public bool IsGrabbing { get; private set; }
    public bool HasClimbContact => climbContacts > 0;
    public Vector3 WallNormal { get; private set; } = Vector3.zero;
    public Vector3 HandWorldPos => transform.position;

    private int climbContacts = 0;

    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        grabAction.action?.Enable();
        climbManager?.RegisterHand(this);
        // SetFree();
    }

    private void OnDisable()
    {
        climbManager?.UnregisterHand(this);
        grabAction.action?.Disable();
    }

    private void Update()
    {
        UpdateHandColor();

        float v = grabAction.action.ReadValue<float>();
        bool pressed = v >= pressThreshold;

        if (pressed && climbContacts > 0)
        {
            if (climbManager != null && climbManager.outOfStamina)
            {
                // no puede agarrar, solo muestra touch/free según contacto
                IsGrabbing = false;
                // if (climbContacts > 0) SetTouch();
                // else SetFree();
                return;
            }

            if (!IsGrabbing)
            {
                IsGrabbing = true;
                // SetGrab();
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

            // if (climbContacts > 0) SetTouch();
            // else SetFree();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) != 0)
        {
            climbContacts++;
            // if (!IsGrabbing) SetTouch();
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
                // if (!IsGrabbing) SetFree();
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

    public void ForceRelease()
    {
        IsGrabbing = false;
        // visual
        // if (HasClimbContact) SetTouch();
        // else SetFree();
    }

    private void UpdateHandColor()
    {
        if (!handRenderer || climbManager == null) return;

        float s = Mathf.Clamp01(climbManager.Stamina01);

        Color c;
        if (s <= lowThreshold) c = lowColor;
        else if (s <= midThreshold)
        {
            // interpolate low->mid across [lowThreshold, midThreshold]
            float t = Mathf.InverseLerp(lowThreshold, midThreshold, s);
            c = Color.Lerp(lowColor, midColor, t);
        }
        else
        {
            // interpolate mid->high across [midThreshold, 1]
            float t = Mathf.InverseLerp(midThreshold, 1f, s);
            c = Color.Lerp(midColor, highColor, t);
        }

        handRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(colorProperty, c);
        handRenderer.SetPropertyBlock(_mpb);
    }


    // private void SetFree()  { if (handRenderer) handRenderer.material = freeMat; }
    // private void SetTouch() { if (handRenderer) handRenderer.material = touchMat; }
    // private void SetGrab()  { if (handRenderer) handRenderer.material = grabMat; }
}
