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

    [Header("Grab Anchor")]
    public float grabPointSearchRadius = 0.18f; // 0.12–0.25 suele ir bien

    public bool IsGrabbing { get; private set; }
    public bool HasClimbContact => climbContacts > 0;

    public Vector3 WallNormal { get; private set; } = Vector3.zero;
    public Vector3 LockedNormal { get; private set; } = Vector3.zero;

    // ✅ Punto de agarre pegado a la pared
    public Vector3 GrabPointWorld { get; private set; }
    public bool HasGrabPoint { get; private set; }

    // ✅ ESTA es la posición que debe usar ClimbManager
    public Vector3 HandWorldPosForClimb => (IsGrabbing && HasGrabPoint) ? GrabPointWorld : transform.position;

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

        if (pressed && HasClimbContact)
        {
            if (climbManager != null && climbManager.playerManager.outOfStamina)
            {
                // no puede agarrar
                ForceRelease();
                return;
            }

            if (!IsGrabbing)
            {
                IsGrabbing = true;

                // ✅ bloquea normal + grab point
                UpdateGrabPointAndNormal();
                climbManager?.TryBeginClimb(this);
            }
            else
            {
                // ✅ mientras agarra, mantén el grabpoint pegado a superficie
                UpdateGrabPointAndNormal();
            }
        }
        else
        {
            if (IsGrabbing)
            {
                IsGrabbing = false;
                LockedNormal = Vector3.zero;
                HasGrabPoint = false;
                climbManager?.TryEndClimb(this);
            }
        }
    }

    private void UpdateGrabPointAndNormal()
    {
        Vector3 origin = transform.position;

        // buscamos colliders cercanos en climbable
        Collider[] cols = Physics.OverlapSphere(origin, grabPointSearchRadius, climbableMask, QueryTriggerInteraction.Ignore);

        if (cols.Length == 0)
        {
            // si no encontramos nada, no bloquees escalada: usa fallback
            HasGrabPoint = false;
            if (LockedNormal == Vector3.zero)
                LockedNormal = (WallNormal != Vector3.zero) ? WallNormal : (-transform.forward);
            return;
        }

        // elegimos el collider con closestPoint más cercano
        Collider best = null;
        Vector3 bestPoint = Vector3.zero;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < cols.Length; i++)
        {
            Collider c = cols[i];
            if (!c) continue;

            Vector3 p = c.ClosestPoint(origin);
            float d = (origin - p).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = c;
                bestPoint = p;
            }
        }

        if (!best)
        {
            HasGrabPoint = false;
            return;
        }

        GrabPointWorld = bestPoint;
        HasGrabPoint = true;

        // normal estable
        Vector3 n = origin - bestPoint;
        if (n.sqrMagnitude > 0.0001f)
        {
            LockedNormal = n.normalized; // normal "hacia fuera" de la pared
        }
        else
        {
            // si estás exactamente sobre el punto, usa la última normal conocida
            if (LockedNormal == Vector3.zero)
                LockedNormal = (WallNormal != Vector3.zero) ? WallNormal : (-transform.forward);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) != 0)
            climbContacts++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) != 0)
        {
            climbContacts = Mathf.Max(0, climbContacts - 1);
            if (climbContacts == 0)
            {
                WallNormal = Vector3.zero;

                if (!IsGrabbing)
                {
                    LockedNormal = Vector3.zero;
                    HasGrabPoint = false;
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsGrabbing) return;

        if (((1 << other.gameObject.layer) & climbableMask) == 0) return;

        Vector3 p = other.ClosestPoint(transform.position);
        Vector3 n = transform.position - p;
        if (n.sqrMagnitude > 0.0001f)
            WallNormal = n.normalized;
    }

    public void ForceRelease()
    {
        IsGrabbing = false;
        LockedNormal = Vector3.zero;
        HasGrabPoint = false;
    }

    private void UpdateHandColor()
    {
        if (!handRenderer || climbManager == null) return;

        float s = Mathf.Clamp01(climbManager.playerManager.energy / climbManager.playerManager.maxEnergy);

        Color c;
        if (s <= lowThreshold) c = lowColor;
        else if (s <= midThreshold)
        {
            float t = Mathf.InverseLerp(lowThreshold, midThreshold, s);
            c = Color.Lerp(lowColor, midColor, t);
        }
        else
        {
            float t = Mathf.InverseLerp(midThreshold, 1f, s);
            c = Color.Lerp(midColor, highColor, t);
        }

        handRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(colorProperty, c);
        handRenderer.SetPropertyBlock(_mpb);
    }
}
