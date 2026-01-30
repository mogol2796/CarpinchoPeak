using UnityEngine;

public class WristUILookToggle : MonoBehaviour
{
    [Header("Refs")]
    public Transform centerEye;     // CenterEyeAnchor
    public Transform wrist;         // WristUI (o LeftHandAnchor)
    public GameObject panelRoot;    // Panel (lo que se activa/desactiva)

    [Header("Look detection")]
    [Range(5f, 60f)] public float showAngleDeg = 22f;   // más bajo = más estricto
    public float maxDistance = 0.6f;                    // opcional (0 = sin límite)
    public float showDelay = 0.05f;                     // anti-parpadeo
    public float hideDelay = 0.12f;

    [Header("Which direction is the panel facing?")]
    public bool useWristForward = true;                 // si tu panel mira hacia el forward del wrist
    public bool invertFacing = false;                   // si está al revés, actívalo

    float _timer = 0f;
    bool _targetVisible = false;

    void Reset()
    {
        panelRoot = transform.gameObject;
        wrist = transform;
    }

    void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!centerEye || !wrist || !panelRoot) return;

        Vector3 eyePos = centerEye.position;
        Vector3 toWrist = (wrist.position - eyePos);
        float dist = toWrist.magnitude;

        if (maxDistance > 0f && dist > maxDistance)
        {
            SetTarget(false);
            TickVisibility();
            return;
        }

        Vector3 lookDir = centerEye.forward.normalized;
        Vector3 toWristDir = toWrist.normalized;

        // "Estoy mirando hacia la muñeca"
        float angle = Vector3.Angle(lookDir, toWristDir);
        bool lookingAtWrist = angle <= showAngleDeg;

        // "La pantalla me está mirando a mí" (evita que se muestre si miras la parte de atrás)
        Vector3 faceDir = useWristForward ? wrist.forward : wrist.up;
        if (invertFacing) faceDir = -faceDir;

        Vector3 wristToEyeDir = (eyePos - wrist.position).normalized;
        bool facingEye = Vector3.Dot(faceDir.normalized, wristToEyeDir) > 0.2f;

        SetTarget(lookingAtWrist && facingEye);
        TickVisibility();
    }

    void SetTarget(bool visible)
    {
        _targetVisible = visible;
    }

    void TickVisibility()
    {
        bool current = panelRoot.activeSelf;

        float delay = _targetVisible ? showDelay : hideDelay;
        if (_targetVisible == current)
        {
            _timer = 0f;
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= delay)
        {
            panelRoot.SetActive(_targetVisible);
            _timer = 0f;
        }
    }
}
