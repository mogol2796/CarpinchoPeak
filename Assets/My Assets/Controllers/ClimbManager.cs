using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using OvrCharacterController = Oculus.Interaction.Locomotion.CharacterController;

public class ClimbManager : MonoBehaviour
{
    [Header("Refs (Building Blocks)")]
    public Transform playerRoot;
    public OvrCharacterController ovrCharacterController;
    public PlayerManager playerManager;

    [Header("Disable while climbing/mantling (BB safety)")]
    public MonoBehaviour[] disableWhileClimbing;

    [Header("Climb Tuning")]
    public float climbStrength = 2.5f;
    public float maxClimbSpeed = 6f;

    [Header("Climb Jitter Filter")]
    public float handDeadzone = 0.006f;   // 3–8 mm
    public float handFilter = 22f;        // 15–35 (más alto = más estable)

    [Header("Corners / Ledges")]
    public float normalLerp = 16f;        // suaviza cambios bruscos de normal
    public float normalCompliance = 0.25f;// deja algo de componente normal al coronar
    public float minSlideSpeed = 0.02f;   // si tras proyectar queda casi 0, desliza en tangente

    [Header("Ovr CharacterController (stability)")]
    public bool overrideOvrControllerDefaults = false;
    public float locomotionOvrMaxStep = 0.18f;
    [Range(1, 8)] public int locomotionOvrMaxReboundSteps = 2;
    // Set < 0 to keep the current (default) SkinWidth.
    public float locomotionOvrSkinWidth = -1f;

    public bool tuneOvrControllerDuringClimb = true;
    public float climbOvrMaxStep = 0f;
    [Range(1, 8)] public int climbOvrMaxReboundSteps = 1;
    // Set < 0 to keep the current (default) SkinWidth.
    public float climbOvrSkinWidth = -1f;

    [Header("Collider While Climbing")]
    public bool resizeColliderWhileClimbing = true;
    public CapsuleCollider characterCapsule;
    public Transform head; // asigna CenterEye / MainCamera (lo que represente la cabeza)
    public float climbCapsuleRadius = 0.18f;
    public float climbCapsuleHeight = 0.36f; // set ~= 2*radius to behave like a sphere
    public float capsuleShrinkSpeed = 25f;
    public float capsuleExpandSpeed = 8f;
    public float capsuleExpandDelay = 0.05f;
    public float capsuleExpandCheckInflation = 0.002f;

    [Header("Release Assist")]
    public float releaseWallPushDistance = 0.05f;

    [Header("Unstuck / Penetration")]
    public bool resolvePenetration = true;
    [Range(1, 8)] public int penetrationIterations = 4;
    public float penetrationExtraDistance = 0.001f;
    public float penetrationMinDistance = 0.002f;
    public float penetrationMaxDistancePerFrame = 0.08f;
    public bool penetrationPreferHorizontal = true;

    [Header("Smoothing (used for non-climb moves)")]
    public float moveSmoothing = 20f;

    [Header("Release / Gravity")]
    public float releaseGravity = 9.81f;
    public float releaseSeconds = 0.25f;

    [Header("Stamina")]
    // public float maxStamina = 100f;
    // public float stamina = 100f;
    public float drainPerSecond = 18f;
    public float regenPerSecond = 25f;
    public float minStaminaToGrab = 15f;
    // public bool outOfStamina = false;
    // public float Stamina01 => maxStamina <= 0 ? 0 : stamina / maxStamina;

    [Header("Mantle (by Zone)")]
    public InputActionProperty mantleAction;
    public float mantleDuration = 0.25f;
    public float mantleFallbackUp = 0.55f;
    public float mantleFallbackForward = 0.35f;

    private readonly List<ClimbHand> hands = new();
    private ClimbHand activeHand;
    private bool isClimbing;

    private Vector3 lastHandPos;
    private Vector3 smoothedApplied;
    private Vector3 smoothedNormal = Vector3.zero;
    private float defaultOvrMaxStep;
    private int defaultOvrMaxReboundSteps;
    private float defaultOvrSkinWidth;
    private bool hasCachedOvrDefaults;
    private bool ovrTunedForClimb;
    private float defaultCapsuleRadius;
    private float defaultCapsuleHeight;
    private Vector3 defaultCapsuleLocalPos;
    private float capsuleBlend01;
    private float capsuleExpandDelayTimer;
    private bool hasCachedCapsuleDefaults;
    private readonly Collider[] capsuleOverlapCache = new Collider[16];
    private bool pendingReenableDisabledComponents;

    // filtro de jitter
    private Vector3 filteredHandDelta = Vector3.zero;

    private float releaseTimer = 0f;
    private float fallVelocity = 0f;

    private bool isMantling;
    private float mantleT;
    private Vector3 mantleStart;
    private Vector3 mantleTarget;

    // private MantleZone currentZone;

    public bool IsClimbing => isClimbing;
    public bool IsMantling => isMantling;
    public bool IsClimbColliderResizing => resizeColliderWhileClimbing && capsuleBlend01 > 0.001f;

    private void Awake()
    {
        if (!ovrCharacterController)
            ovrCharacterController = GetComponentInChildren<OvrCharacterController>();

        if (!characterCapsule && ovrCharacterController)
            characterCapsule = ovrCharacterController.GetComponent<CapsuleCollider>();

        if (characterCapsule && characterCapsule.center != Vector3.zero)
            Debug.LogWarning("ClimbManager: characterCapsule.center should be (0,0,0) with OVR CharacterController (it ignores CapsuleCollider.center).");

        if (!head && Camera.main) head = Camera.main.transform;

        CacheOvrControllerDefaults(force: true);
        ApplyOvrLocomotionTuningIfEnabled();
        CacheCapsuleDefaults(force: true);
    }

    private void OnEnable() => mantleAction.action?.Enable();

    private void OnDisable()
    {
        mantleAction.action?.Disable();
        RestoreOvrControllerDefaultsIfNeeded();
    }

    // public void SetMantleZone(MantleZone z) => currentZone = z;

    public void RegisterHand(ClimbHand hand)
    {
        if (hand && !hands.Contains(hand)) hands.Add(hand);
    }

    public void UnregisterHand(ClimbHand hand)
    {
        hands.Remove(hand);
        if (hand == activeHand) StopClimb();
    }

    public void TryBeginClimb(ClimbHand hand)
    {
        if (playerManager.outOfStamina) return;
        if (isMantling) return;

        if (hand == null || !hand.IsGrabbing || !hand.HasClimbContact) return;
        if (!ovrCharacterController || !playerRoot) return;

        activeHand = hand;
        pendingReenableDisabledComponents = false;

        ApplyOvrClimbTuning();

        if (resizeColliderWhileClimbing)
        {
            CacheCapsuleDefaults(force: true);
            capsuleExpandDelayTimer = 0f;
        }

        if (!isClimbing)
        {
            isClimbing = true;
            if (disableWhileClimbing != null)
                foreach (var b in disableWhileClimbing)
                    if (b) b.enabled = false;
        }

        lastHandPos = hand.transform.position;
        smoothedApplied = Vector3.zero;
        smoothedNormal = hand.LockedNormal;

        // reset filtro (importantísimo para que no “salte”)
        filteredHandDelta = Vector3.zero;

        releaseTimer = 0f;
        fallVelocity = 0f;
    }

    public void TryEndClimb(ClimbHand hand)
    {
        if (hand == null || hand != activeHand) return;

        var fb = FindFallbackHand(hand);
        if (fb != null)
        {
            activeHand = fb;
            lastHandPos = fb.transform.position;
            smoothedApplied = Vector3.zero;
            smoothedNormal = fb.LockedNormal;

            // reset filtro al cambiar de mano
            filteredHandDelta = Vector3.zero;
            return;
        }

        StopClimb();
    }

    private void StopClimb()
    {
        // Keep a copy before clearing, so we can nudge away from the wall on release.
        Vector3 releaseNormal = smoothedNormal;

        isClimbing = false;
        activeHand = null;

        // reset filtro al soltar
        filteredHandDelta = Vector3.zero;
        smoothedNormal = Vector3.zero;

        if (isMantling) return;

        releaseTimer = releaseSeconds;
        fallVelocity = 0f;
        smoothedApplied = Vector3.zero;

        if (resizeColliderWhileClimbing)
            capsuleExpandDelayTimer = capsuleExpandDelay;

        // Small separation push to avoid getting stuck inside rough/stacked rocks when the capsule expands.
        // Prefer a horizontal push so we don't "pop" upward on noisy mesh normals.
        if (releaseWallPushDistance > 0f && releaseNormal != Vector3.zero && ovrCharacterController && playerRoot)
        {
            Vector3 pushDir = Vector3.ProjectOnPlane(releaseNormal, Vector3.up);
            if (pushDir.sqrMagnitude < 0.0001f) pushDir = releaseNormal;
            ApplyMoveWithCollision_NoSmooth(pushDir.normalized * releaseWallPushDistance);
        }

        // Re-enable movement/locomotion only after recovery (see TryReenableDisabledComponents).
        pendingReenableDisabledComponents = true;
    }

    // ✅ CAMBIO: Update() en vez de LateUpdate()
    private void Update()
    {
        // Mantle por zona
        // if (!isMantling && currentZone && mantleAction.action != null && mantleAction.action.WasPressedThisFrame())
        // {
        //     StartMantle(currentZone);
        //     return;
        // }

        // if (isMantling)
        // {
        //     UpdateMantle();
        //     return;
        // }

        UpdateClimbCapsule();
        TryReenableDisabledComponents();

        UpdateStamina();

        if (!isClimbing && releaseTimer > 0f)
        {
            HandleReleaseFall();
            TryReenableDisabledComponents();
            return;
        }

        if (!isClimbing || activeHand == null || !ovrCharacterController || !playerRoot)
            return;

        Vector3 current = activeHand.transform.position;

        // --- jitter filter (deadzone + low-pass) ---
        Vector3 rawDelta = current - lastHandPos;

        if (rawDelta.magnitude < handDeadzone)
            rawDelta = Vector3.zero;

        filteredHandDelta = Vector3.Lerp(
            filteredHandDelta,
            rawDelta,
            1f - Mathf.Exp(-handFilter * Time.deltaTime)
        );

        Vector3 move = -filteredHandDelta * climbStrength;

        // Manejo de esquinas: suaviza la normal y evita quedarse "clavado" al cambiar de cara
        Vector3 n = activeHand.LockedNormal;
        if (n != Vector3.zero)
        {
            float lerp = 1f - Mathf.Exp(-normalLerp * Time.deltaTime);
            smoothedNormal = (smoothedNormal == Vector3.zero) ? n : Vector3.Slerp(smoothedNormal, n, lerp);

            Vector3 rawMove = move;

            // Proyección principal (tangente a la superficie)
            move = Vector3.ProjectOnPlane(rawMove, smoothedNormal);

            // Permite un poco de componente normal cuando la superficie es casi horizontal (coronar cantos)
            float upDot = Mathf.Clamp01(Vector3.Dot(smoothedNormal, Vector3.up));
            float compliance = normalCompliance * upDot;
            if (compliance > 0f)
            {
                float alongNormal = Vector3.Dot(rawMove, smoothedNormal);
                move += smoothedNormal * alongNormal * compliance;
            }

            // Si tras proyectar queda casi 0 (típico en vértices), desliza en una tangente estable
            if (move.magnitude < minSlideSpeed)
            {
                Vector3 tangent = Vector3.Cross(smoothedNormal, Vector3.up);
                if (tangent.sqrMagnitude < 0.0001f)
                    tangent = Vector3.Cross(smoothedNormal, Vector3.right);

                tangent.Normalize();
                float alongTangent = Vector3.Dot(rawMove, tangent);
                move += tangent * alongTangent;
            }
        }
        else
        {
            smoothedNormal = Vector3.zero;
        }

        float maxStep = maxClimbSpeed * Time.deltaTime;
        if (move.magnitude > maxStep) move = move.normalized * maxStep;

        // ✅ CLAVE: durante escalada, SIN smoothing
        ApplyMoveWithCollision_NoSmooth(move);

        lastHandPos = current;
    }

    private void LateUpdate()
    {
        if (!resolvePenetration) return;
        if (!characterCapsule || !ovrCharacterController || !playerRoot) return;

        ResolveCapsulePenetration();
    }

    private void CacheCapsuleDefaults(bool force = false)
    {
        if (!resizeColliderWhileClimbing) return;
        if (!characterCapsule) return;
        if (hasCachedCapsuleDefaults && !force) return;

        defaultCapsuleRadius = characterCapsule.radius;
        defaultCapsuleHeight = characterCapsule.height;
        defaultCapsuleLocalPos = characterCapsule.transform.localPosition;
        hasCachedCapsuleDefaults = true;
    }

    private void ResolveCapsulePenetration()
    {
        // Meta's CharacterController does not try to depenetrate if we end up inside colliders
        // (eg. stacked rocks, teleport-ish moves, or large frame deltas). This pass pushes the whole rig out.
        float remainingBudget = Mathf.Max(0f, penetrationMaxDistancePerFrame);
        int iters = Mathf.Clamp(penetrationIterations, 1, 8);

        Transform rigRoot = playerRoot ? playerRoot : transform.root;
        int mask = ovrCharacterController ? ovrCharacterController.LayerMask.value : ~0;

        for (int iter = 0; iter < iters; iter++)
        {
            if (remainingBudget <= 0f) break;

            Vector3 center = characterCapsule.transform.position;
            float radius = characterCapsule.radius;
            float height = characterCapsule.height;
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 capsuleBase = center - Vector3.up * halfSegment;
            Vector3 capsuleTop = center + Vector3.up * halfSegment;

            int count = Physics.OverlapCapsuleNonAlloc(
                capsuleBase,
                capsuleTop,
                radius,
                capsuleOverlapCache,
                mask,
                QueryTriggerInteraction.Ignore);

            if (count <= 0) break;

            Vector3 totalPush = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider other = capsuleOverlapCache[i];
                if (!other) continue;
                if (other == characterCapsule) continue;
                if (rigRoot && other.transform.IsChildOf(rigRoot)) continue;

                if (Physics.ComputePenetration(
                        characterCapsule, characterCapsule.transform.position, characterCapsule.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 direction, out float distance))
                {
                    if (distance < penetrationMinDistance) continue;

                    Vector3 push = direction * (distance + penetrationExtraDistance);

                    if (penetrationPreferHorizontal)
                    {
                        Vector3 flat = Vector3.ProjectOnPlane(push, Vector3.up);
                        if (flat.sqrMagnitude > 0.000001f)
                            push = flat;
                    }

                    totalPush += push;
                }
            }

            if (totalPush.sqrMagnitude < 0.0000001f) break;

            float mag = totalPush.magnitude;
            if (mag > remainingBudget)
                totalPush = totalPush * (remainingBudget / mag);

            playerRoot.position += totalPush;
            remainingBudget -= totalPush.magnitude;
        }
    }

    private void UpdateClimbCapsule()
    {
        if (!resizeColliderWhileClimbing) return;
        if (!ovrCharacterController) return;

        if (!characterCapsule)
            characterCapsule = ovrCharacterController.GetComponent<CapsuleCollider>();
        if (!characterCapsule) return;

        CacheCapsuleDefaults();
        if (!hasCachedCapsuleDefaults) return;

        float prevBlend = capsuleBlend01;

        // Keep the capsule small while climbing, then expand back when safe.
        bool keepSmall = isClimbing || isMantling;
        float target = keepSmall ? 1f : 0f;

        if (!keepSmall && capsuleExpandDelayTimer > 0f)
        {
            capsuleExpandDelayTimer -= Time.deltaTime;
            // durante el delay, mantenemos collider pequeño pero seguimos actualizando posición
            if (capsuleExpandDelayTimer > 0f)
                target = 1f;
        }

        float dt = Time.deltaTime;
        float speed = (target > capsuleBlend01) ? capsuleShrinkSpeed : capsuleExpandSpeed;
        float nextBlend = Mathf.MoveTowards(capsuleBlend01, target, speed * dt);

        // Primero intentamos aplicar el cambio de tamaño (puede fallar si está bloqueado al expandir)
        if (!Mathf.Approximately(nextBlend, capsuleBlend01))
        {
            float nextRadius = Mathf.Lerp(defaultCapsuleRadius, climbCapsuleRadius, nextBlend);
            float nextHeight = Mathf.Lerp(defaultCapsuleHeight, climbCapsuleHeight, nextBlend);
            nextHeight = Mathf.Max(nextHeight, nextRadius * 2f + 0.001f);

            Vector3 nextLocalPos = ComputeCapsuleLocalPosForBlend(nextBlend, nextHeight, nextRadius);
            Transform capsuleParent = characterCapsule.transform.parent;
            Vector3 nextCenterWorld = capsuleParent ? capsuleParent.TransformPoint(nextLocalPos) : nextLocalPos;

            bool isExpanding = nextBlend < capsuleBlend01;
            if (!isExpanding || CanResizeCapsule(nextCenterWorld, nextRadius, nextHeight))
            {
                characterCapsule.radius = nextRadius;
                characterCapsule.height = nextHeight;
                capsuleBlend01 = nextBlend;
            }
        }

        // Sólo controlamos la posición del capsule mientras estamos en "modo escalada".
        // Si no, dejamos que el locomotor / rig gestione la posición normal (para no pelearse con otros scripts).
        if (capsuleBlend01 > 0f)
        {
            float curRadius = Mathf.Lerp(defaultCapsuleRadius, climbCapsuleRadius, capsuleBlend01);
            float curHeight = Mathf.Lerp(defaultCapsuleHeight, climbCapsuleHeight, capsuleBlend01);
            curHeight = Mathf.Max(curHeight, curRadius * 2f + 0.001f);
            characterCapsule.transform.localPosition = ComputeCapsuleLocalPosForBlend(capsuleBlend01, curHeight, curRadius);
        }
        else if (prevBlend > 0f)
        {
            // If we just finished resizing (blend reached 0 this frame), make sure we fully restore the
            // pre-climb capsule local position. Otherwise (large dt) we can get stuck at the head-anchored
            // position and the locomotor may "snap" the player origin upward.
            characterCapsule.transform.localPosition = defaultCapsuleLocalPos;
        }
    }

    private void TryReenableDisabledComponents()
    {
        if (!pendingReenableDisabledComponents) return;
        if (releaseTimer > 0f) return;
        if (resizeColliderWhileClimbing && capsuleBlend01 > 0.001f) return;

        pendingReenableDisabledComponents = false;

        RestoreOvrControllerDefaultsIfNeeded();

        if (disableWhileClimbing != null)
            foreach (var b in disableWhileClimbing)
                if (b) b.enabled = true;
    }

    private void CacheOvrControllerDefaults(bool force = false)
    {
        if (!ovrCharacterController) return;
        if (hasCachedOvrDefaults && !force) return;

        defaultOvrMaxStep = ovrCharacterController.MaxStep;
        defaultOvrMaxReboundSteps = ovrCharacterController.MaxReboundSteps;
        defaultOvrSkinWidth = ovrCharacterController.SkinWidth;
        hasCachedOvrDefaults = true;
    }

    private void ApplyOvrLocomotionTuningIfEnabled()
    {
        if (!overrideOvrControllerDefaults) return;
        if (!ovrCharacterController) return;

        CacheOvrControllerDefaults();

        ovrCharacterController.MaxStep = Mathf.Max(0f, locomotionOvrMaxStep);
        ovrCharacterController.MaxReboundSteps = Mathf.Clamp(locomotionOvrMaxReboundSteps, 1, 8);
        if (locomotionOvrSkinWidth >= 0f) ovrCharacterController.SkinWidth = locomotionOvrSkinWidth;
    }

    private void ApplyOvrClimbTuning()
    {
        if (!tuneOvrControllerDuringClimb) return;
        if (!ovrCharacterController) return;

        CacheOvrControllerDefaults();

        ovrCharacterController.MaxStep = Mathf.Max(0f, climbOvrMaxStep);
        ovrCharacterController.MaxReboundSteps = Mathf.Clamp(climbOvrMaxReboundSteps, 1, 8);
        if (climbOvrSkinWidth >= 0f) ovrCharacterController.SkinWidth = climbOvrSkinWidth;

        ovrTunedForClimb = true;
    }

    private void RestoreOvrControllerDefaultsIfNeeded()
    {
        if (!ovrTunedForClimb) return;
        if (!ovrCharacterController) return;
        ovrTunedForClimb = false;

        // Restore to the "locomotion" settings (either overridden, or the cached defaults).
        if (overrideOvrControllerDefaults)
        {
            ApplyOvrLocomotionTuningIfEnabled();
            return;
        }

        if (!hasCachedOvrDefaults) return;
        ovrCharacterController.MaxStep = defaultOvrMaxStep;
        ovrCharacterController.MaxReboundSteps = defaultOvrMaxReboundSteps;
        ovrCharacterController.SkinWidth = defaultOvrSkinWidth;
    }

    private Vector3 ComputeCapsuleLocalPosForBlend(float blend01, float height, float radius)
    {
        // Queremos que el collider pequeño quede a la altura de la cabeza.
        // Para que el "stand up" parezca natural, anclamos el "top" del capsule al nivel de la cámara,
        // de forma que al expandir crece hacia abajo.
        Vector3 localPos = defaultCapsuleLocalPos;
        if (!head) return localPos;

        Transform parent = characterCapsule.transform.parent;
        Vector3 headLocal = parent ? parent.InverseTransformPoint(head.position) : head.position;
        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
        float headAnchoredCenterY = headLocal.y - halfSegment;
        localPos.y = Mathf.Lerp(defaultCapsuleLocalPos.y, headAnchoredCenterY, blend01);
        return localPos;
    }

    private bool CanResizeCapsule(Vector3 desiredCenterWorld, float radius, float height)
    {
        int mask = ovrCharacterController ? ovrCharacterController.LayerMask.value : ~0;

        float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 capsuleBase = desiredCenterWorld - Vector3.up * halfSegment;
        Vector3 capsuleTop = desiredCenterWorld + Vector3.up * halfSegment;

        float probeRadius = radius + capsuleExpandCheckInflation;
        int count = Physics.OverlapCapsuleNonAlloc(
            capsuleBase,
            capsuleTop,
            probeRadius,
            capsuleOverlapCache,
            mask,
            QueryTriggerInteraction.Ignore);

        // If we fill the cache, assume blocked (safer than clipping through).
        if (count >= capsuleOverlapCache.Length) return false;

        Transform rigRoot = playerRoot ? playerRoot : transform.root;

        for (int i = 0; i < count; i++)
        {
            Collider c = capsuleOverlapCache[i];
            if (!c) continue;
            if (c == characterCapsule) continue;
            if (rigRoot && c.transform.IsChildOf(rigRoot)) continue;
            return false;
        }

        return true;
    }

    private void ApplyMoveWithCollision(Vector3 move)
    {
        Transform ccT = ovrCharacterController.transform;

        Vector3 localBefore = ccT.localPosition;
        Vector3 worldBefore = ccT.position;

        ovrCharacterController.Move(move);

        Vector3 worldAfter = ccT.position;
        Vector3 appliedDelta = worldAfter - worldBefore;

        smoothedApplied = Vector3.Lerp(
            smoothedApplied,
            appliedDelta,
            1f - Mathf.Exp(-moveSmoothing * Time.deltaTime)
        );

        playerRoot.position += smoothedApplied;

        ccT.localPosition = localBefore;
    }

    private void ApplyMoveWithCollision_NoSmooth(Vector3 move)
    {
        Transform ccT = ovrCharacterController.transform;

        Vector3 localBefore = ccT.localPosition;
        Vector3 worldBefore = ccT.position;

        ovrCharacterController.Move(move);

        Vector3 worldAfter = ccT.position;
        Vector3 appliedDelta = worldAfter - worldBefore;

        playerRoot.position += appliedDelta;

        ccT.localPosition = localBefore;
    }

    private void HandleReleaseFall()
    {
        releaseTimer -= Time.deltaTime;

        fallVelocity -= releaseGravity * Time.deltaTime;
        Vector3 fallMove = Vector3.up * fallVelocity * Time.deltaTime;

        // aquí sí puedes dejar smoothing si te gusta
        ApplyMoveWithCollision(fallMove);

        if (releaseTimer <= 0f)
            smoothedApplied = Vector3.zero;
    }

    private void UpdateStamina()
    {
        bool anyGrabbing = false;
        for (int i = 0; i < hands.Count; i++)
        {
            var h = hands[i];
            if (h && h.IsGrabbing && h.HasClimbContact)
            {
                anyGrabbing = true;
                break;
            }
        }

        if (anyGrabbing)
        {
            playerManager.energy = Mathf.Max(0f, playerManager.energy - drainPerSecond * Time.deltaTime);
            if (playerManager.energy <= 0f) playerManager.outOfStamina = true;
        }
        else
        {
            // Regen even while airborne so the player can recover after letting go.
            playerManager.energy = Mathf.Min(playerManager.maxEnergy, playerManager.energy + regenPerSecond * Time.deltaTime);

            if (playerManager.outOfStamina && playerManager.energy >= minStaminaToGrab)
                playerManager.outOfStamina = false;
        }

        if (playerManager.outOfStamina && isClimbing)
            ForceReleaseAllHands();
    }

    private void ForceReleaseAllHands()
    {
        for (int i = 0; i < hands.Count; i++)
        {
            var h = hands[i];
            if (h) h.ForceRelease();
        }
        StopClimb();
    }

    // private void StartMantle(MantleZone zone)
    // {
    //     if (zone == null || !playerRoot || !ovrCharacterController) return;

    //     isMantling = true;
    //     mantleT = 0f;

    //     isClimbing = false;
    //     activeHand = null;
    //     releaseTimer = 0f;
    //     fallVelocity = 0f;
    //     smoothedApplied = Vector3.zero;

    //     // reset filtro al empezar mantle
    //     filteredHandDelta = Vector3.zero;

    //     mantleStart = playerRoot.position;

    //     if (zone.standPoint != null)
    //         mantleTarget = zone.standPoint.position;
    //     else
    //         mantleTarget = mantleStart + Vector3.up * mantleFallbackUp + playerRoot.forward * mantleFallbackForward;

    //     if (disableWhileClimbing != null)
    //         foreach (var b in disableWhileClimbing)
    //             if (b) b.enabled = false;
    // }

    private void UpdateMantle()
    {
        mantleT += Time.deltaTime / Mathf.Max(0.01f, mantleDuration);
        float t = Mathf.Clamp01(mantleT);
        t = t * t * (3f - 2f * t);

        Vector3 desired = Vector3.Lerp(mantleStart, mantleTarget, t);
        Vector3 delta = desired - playerRoot.position;

        ApplyMoveWithCollision_NoSmooth(delta);

        if (mantleT >= 1f)
        {
            isMantling = false;
            smoothedApplied = Vector3.zero;

            RestoreOvrControllerDefaultsIfNeeded();

            if (disableWhileClimbing != null)
                foreach (var b in disableWhileClimbing)
                    if (b) b.enabled = true;
        }
    }

    private ClimbHand FindFallbackHand(ClimbHand ignored)
    {
        for (int i = 0; i < hands.Count; i++)
        {
            var h = hands[i];
            if (h && h != ignored && h.IsGrabbing && h.HasClimbContact)
                return h;
        }
        return null;
    }
}
