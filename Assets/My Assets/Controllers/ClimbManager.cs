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

    private void OnEnable() => mantleAction.action?.Enable();
    private void OnDisable() => mantleAction.action?.Disable();

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
        isClimbing = false;
        activeHand = null;

        // reset filtro al soltar
        filteredHandDelta = Vector3.zero;
        smoothedNormal = Vector3.zero;

        if (isMantling) return;

        releaseTimer = releaseSeconds;
        fallVelocity = 0f;
        smoothedApplied = Vector3.zero;
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

        UpdateStamina();

        if (!isClimbing && releaseTimer > 0f)
        {
            HandleReleaseFall();
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
        {
            smoothedApplied = Vector3.zero;

            if (disableWhileClimbing != null)
                foreach (var b in disableWhileClimbing)
                    if (b) b.enabled = true;
        }
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

        bool grounded = ovrCharacterController != null && ovrCharacterController.IsGrounded;

        if (anyGrabbing)
        {
            playerManager.energy = Mathf.Max(0f, playerManager.energy - drainPerSecond * Time.deltaTime);
            if (playerManager.energy <= 0f) playerManager.outOfStamina = true;
        }
        else
        {
            if (grounded)
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
