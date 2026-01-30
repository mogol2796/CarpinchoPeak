using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using OvrCharacterController = Oculus.Interaction.Locomotion.CharacterController;

public class ClimbManager : MonoBehaviour
{
    [Header("Refs (Building Blocks)")]
    public Transform playerRoot;                           // GameObject "Player" (root del rig)
    public OvrCharacterController ovrCharacterController;   // Locomotor/PlayerController (BB)

    [Header("Disable while climbing/mantling (BB safety)")]
    public MonoBehaviour[] disableWhileClimbing;           // WallPenetrationTunneling, SmoothMovementTunneling…

    [Header("Body Collider (Anti-penetration)")]
    public CapsuleCollider bodyCollider;                   // CapsuleCollider NO trigger (hijo de playerRoot)
    public LayerMask climbableMask;                        // Layer(s) de paredes escalables
    public LayerMask worldCollisionMask; // pon aquí Default/Environment/Climbable/etc


    [Header("Climb Tuning")]
    public float climbStrength = 2.5f;
    public float maxClimbSpeed = 6f;

    [Header("Smoothing")]
    public float moveSmoothing = 20f; // 10–30

    [Header("Release / Gravity")]
    public float releaseGravity = 9.81f;
    public float releaseSeconds = 0.25f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float stamina = 100f;
    public float drainPerSecond = 18f;
    public float regenPerSecond = 25f;
    public float minStaminaToGrab = 15f;
    public bool outOfStamina = false;
    public float Stamina01 => maxStamina <= 0 ? 0 : stamina / maxStamina;

    [Header("Mantle (by Zone)")]
    public InputActionProperty mantleAction;  // Botón (ej A)
    public float mantleDuration = 0.25f;
    public float mantleFallbackUp = 0.55f;       // si no hay standPoint
    public float mantleFallbackForward = 0.35f;  // si no hay standPoint

    // ---- runtime
    private readonly List<ClimbHand> hands = new();
    private ClimbHand activeHand;
    private bool isClimbing;

    private Vector3 lastHandPos;
    private Vector3 smoothedApplied;

    private float releaseTimer = 0f;
    private float fallVelocity = 0f;

    private bool isMantling;
    private float mantleT;
    private Vector3 mantleStart;
    private Vector3 mantleTarget;

    private MantleZone currentZone;

    /* ===========================
       Public helpers
       =========================== */

    public bool IsClimbing => isClimbing;
    public bool IsMantling => isMantling;

    public void SetMantleZone(MantleZone z) => currentZone = z;

    public void RegisterHand(ClimbHand hand)
    {
        if (hand && !hands.Contains(hand)) hands.Add(hand);
    }

    public void UnregisterHand(ClimbHand hand)
    {
        hands.Remove(hand);
        if (hand == activeHand) StopClimb();
    }

    /* ===========================
       Climb start/stop
       =========================== */

    public void TryBeginClimb(ClimbHand hand)
    {
        if (outOfStamina) return;
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

        lastHandPos = hand.HandWorldPos;
        smoothedApplied = Vector3.zero;

        releaseTimer = 0f;
        fallVelocity = 0f;
    }

    public void TryEndClimb(ClimbHand hand)
    {
        if (hand == null || hand != activeHand) return;

        ClimbHand fallback = FindFallbackHand(hand);
        if (fallback != null)
        {
            activeHand = fallback;
            lastHandPos = fallback.HandWorldPos;
            smoothedApplied = Vector3.zero;
            return;
        }

        StopClimb();
    }

    private void StopClimb()
    {
        isClimbing = false;
        activeHand = null;

        if (isMantling) return;

        releaseTimer = releaseSeconds;
        fallVelocity = 0f;
        smoothedApplied = Vector3.zero;
    }

    /* ===========================
       Main loop
       =========================== */

    private void OnEnable() => mantleAction.action?.Enable();
    private void OnDisable() => mantleAction.action?.Disable();

    private void LateUpdate()
    {
        // Mantle input (zona + botón)
        if (!isMantling && currentZone && mantleAction.action != null && mantleAction.action.WasPressedThisFrame())
        {
            StartMantle(currentZone);
            return;
        }

        // Mantle motion
        if (isMantling)
        {
            UpdateMantle();
            return;
        }

        // Stamina (siempre)
        UpdateStamina();

        // Caída tras soltar
        if (!isClimbing && releaseTimer > 0f)
        {
            HandleReleaseFall();
            return;
        }

        // Movimiento de escalada
        if (!isClimbing || activeHand == null || !ovrCharacterController || !playerRoot)
            return;

        Vector3 current = activeHand.HandWorldPos;
        Vector3 handDelta = current - lastHandPos;

        Vector3 move = -handDelta * climbStrength;

        // Evita “meterte” hacia la pared con normal de la mano (útil, pero no suficiente)
        Vector3 n = activeHand.WallNormal;
        // Vector3 n = activeHand.LockedNormal;
        if (n != Vector3.zero)
        {
            // solo bloquea la componente hacia dentro (más estable que ProjectOnPlane)
            float into = Vector3.Dot(move, n);
            if (into < 0f) move -= n * into;
        }

        // clamp velocidad
        float maxStep = maxClimbSpeed * Time.deltaTime;
        if (move.magnitude > maxStep) move = move.normalized * maxStep;

        ApplyMoveWithCollision(move);

        // Solo empujar fuera si el movimiento iba hacia la pared
        Vector3 n2 = activeHand.WallNormal;
        if (n2 != Vector3.zero && Vector3.Dot(move, n2) < 0f)
        {
            ResolveBodyPenetration();
        }


        lastHandPos = current;
    }

    /* ===========================
       Stamina
       =========================== */

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
            stamina = Mathf.Max(0f, stamina - drainPerSecond * Time.deltaTime);
            if (stamina <= 0f) outOfStamina = true;
        }
        else
        {
            if (grounded)
                stamina = Mathf.Min(maxStamina, stamina + regenPerSecond * Time.deltaTime);

            if (outOfStamina && stamina >= minStaminaToGrab)
                outOfStamina = false;
        }

        if (outOfStamina && isClimbing)
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

    /* ===========================
       Move + collision (BB)
       =========================== */

    private void ApplyMoveWithCollision(Vector3 move)
    {
        Transform ccT = ovrCharacterController.transform;

        // guardamos local pos para no “despegar” el CC del rig
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

        // revert CC local
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

        // SIN smoothing
        playerRoot.position += appliedDelta;

        ccT.localPosition = localBefore;
    }


    /* ===========================
       Anti-penetration (BodyCollider)
       =========================== */

    private void ResolveBodyPenetration()
    {
        if (!bodyCollider || !playerRoot) return;

        // Asegura que el collider esté donde toca
        // (si es hijo del playerRoot normalmente ya está)

        // OverlapCapsule usando geometría del CapsuleCollider
        Vector3 center = bodyCollider.transform.TransformPoint(bodyCollider.center);

        float radius = bodyCollider.radius * Mathf.Max(bodyCollider.transform.lossyScale.x, bodyCollider.transform.lossyScale.z);
        float height = Mathf.Max(bodyCollider.height * bodyCollider.transform.lossyScale.y, radius * 2f);

        Vector3 up = bodyCollider.transform.up;

        float half = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 p1 = center + up * half;
        Vector3 p2 = center - up * half;

        Collider[] hits = Physics.OverlapCapsule(p1, p2, radius, worldCollisionMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider other = hits[i];
            if (!other || other == bodyCollider) continue;

            if (Physics.ComputePenetration(
                bodyCollider, bodyCollider.transform.position, bodyCollider.transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 dir, out float dist))
            {
                // empuja al playerRoot fuera de la pared
                Vector3 push = dir * (dist + 0.001f);
                ApplyMoveWithCollision_NoSmooth(push);

            }
        }
    }

    /* ===========================
       Release fall
       =========================== */

    private void HandleReleaseFall()
    {
        releaseTimer -= Time.deltaTime;

        fallVelocity -= releaseGravity * Time.deltaTime;
        Vector3 fallMove = Vector3.up * fallVelocity * Time.deltaTime;

        ApplyMoveWithCollision(fallMove);

        if (releaseTimer <= 0f)
        {
            smoothedApplied = Vector3.zero;

            if (disableWhileClimbing != null)
                foreach (var b in disableWhileClimbing)
                    if (b) b.enabled = true;
        }
    }

    /* ===========================
       Mantle (Zone)
       =========================== */

    private void StartMantle(MantleZone zone)
    {
        if (zone == null || !playerRoot || !ovrCharacterController) return;

        isMantling = true;
        mantleT = 0f;

        // corta escalada + caída
        isClimbing = false;
        activeHand = null;
        releaseTimer = 0f;
        fallVelocity = 0f;
        smoothedApplied = Vector3.zero;

        mantleStart = playerRoot.position;

        if (zone.standPoint != null)
        {
            mantleTarget = zone.standPoint.position;
        }
        else
        {
            mantleTarget = mantleStart + Vector3.up * mantleFallbackUp + playerRoot.forward * mantleFallbackForward;
        }

        // durante mantle, desactiva safety (opcional)
        if (disableWhileClimbing != null)
            foreach (var b in disableWhileClimbing)
                if (b) b.enabled = false;
    }

    private void UpdateMantle()
    {
        mantleT += Time.deltaTime / Mathf.Max(0.01f, mantleDuration);
        float t = Mathf.Clamp01(mantleT);

        // smoothstep
        t = t * t * (3f - 2f * t);

        Vector3 desired = Vector3.Lerp(mantleStart, mantleTarget, t);
        Vector3 delta = desired - playerRoot.position;

        ApplyMoveWithCollision(delta);
        ResolveBodyPenetration();

        if (mantleT >= 1f)
        {
            isMantling = false;
            smoothedApplied = Vector3.zero;

            // reactiva safety
            if (disableWhileClimbing != null)
                foreach (var b in disableWhileClimbing)
                    if (b) b.enabled = true;
        }
    }

    /* ===========================
       Utils
       =========================== */

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
