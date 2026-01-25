using System.Collections.Generic;
using UnityEngine;
using OvrCharacterController = Oculus.Interaction.Locomotion.CharacterController;

public class ClimbManager : MonoBehaviour
{
    [Header("Refs (Building Blocks)")]
    public Transform playerRoot;                         // GameObject "Player"
    public OvrCharacterController ovrCharacterController;// Locomotor/PlayerController
    public MonoBehaviour locomotionScript;               // FirstPersonLocomotor o equivalente

    [Header("Disable while climbing (BB safety)")]
    public MonoBehaviour[] disableWhileClimbing;         // WallPenetrationTunneling, SmoothMovementTunneling…

    [Header("Climb Tuning")]
    public float climbStrength = 2.5f;
    public float maxClimbSpeed = 6f;

    [Header("Smoothing")]
    public float moveSmoothing = 20f; // 10–30

    [Header("Release / Gravity")]
    public float releaseGravity = 9.81f;
    public float releaseSeconds = 0.25f; // 0.15–0.35

    private readonly List<ClimbHand> hands = new();
    private ClimbHand activeHand;
    private bool isClimbing;

    private Vector3 lastHandPos;
    private Vector3 smoothedApplied;

    private float releaseTimer = 0f;
    private float fallVelocity = 0f;

    /* ---------------- REGISTRO DE MANOS ---------------- */

    public void RegisterHand(ClimbHand hand)
    {
        if (hand && !hands.Contains(hand))
            hands.Add(hand);
    }

    public void UnregisterHand(ClimbHand hand)
    {
        hands.Remove(hand);
        if (hand == activeHand)
            StopClimb();
    }

    /* ---------------- ESCALADA ---------------- */

    public void TryBeginClimb(ClimbHand hand)
    {
        if (hand == null || !hand.IsGrabbing || !hand.HasClimbContact) return;
        if (!ovrCharacterController || !playerRoot) return;

        activeHand = hand;

        if (!isClimbing)
        {
            isClimbing = true;

            if (locomotionScript) locomotionScript.enabled = false;

            if (disableWhileClimbing != null)
                foreach (var b in disableWhileClimbing)
                    if (b) b.enabled = false;
        }

        lastHandPos = hand.HandWorldPos;
        smoothedApplied = Vector3.zero;
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

    /* ---------------- UPDATE ---------------- */

    private void LateUpdate()
    {
        // Caída natural tras soltar
        if (!isClimbing && releaseTimer > 0f)
        {
            HandleReleaseFall();
            return;
        }

        if (!isClimbing || activeHand == null || !ovrCharacterController || !playerRoot)
            return;

        Vector3 current = activeHand.HandWorldPos;
        Vector3 handDelta = current - lastHandPos;

        Vector3 move = -handDelta * climbStrength;

        // Movimiento paralelo a la pared (vertical + lateral)
        Vector3 n = activeHand.WallNormal;
        if (n != Vector3.zero)
            move = Vector3.ProjectOnPlane(move, n);

        float maxStep = maxClimbSpeed * Time.deltaTime;
        if (move.magnitude > maxStep)
            move = move.normalized * maxStep;

        ApplyMoveWithCollision(move);

        lastHandPos = current;
    }

    /* ---------------- MOVIMIENTO CON COLISIÓN ---------------- */

    private void ApplyMoveWithCollision(Vector3 move)
    {
        Transform ccT = ovrCharacterController.transform;
        Vector3 before = ccT.position;

        ovrCharacterController.Move(move);

        Vector3 after = ccT.position;
        Vector3 appliedDelta = after - before;

        smoothedApplied = Vector3.Lerp(
            smoothedApplied,
            appliedDelta,
            1f - Mathf.Exp(-moveSmoothing * Time.deltaTime)
        );

        playerRoot.position += smoothedApplied;

        // Revertimos CC para que no se despegue del rig
        ccT.position = before;
    }

    /* ---------------- CAÍDA TRAS SOLTAR ---------------- */

    private void HandleReleaseFall()
    {
        releaseTimer -= Time.deltaTime;

        fallVelocity -= releaseGravity * Time.deltaTime;
        Vector3 fallMove = Vector3.up * fallVelocity * Time.deltaTime;

        ApplyMoveWithCollision(fallMove);

        if (releaseTimer <= 0f)
        {
            if (disableWhileClimbing != null)
                foreach (var b in disableWhileClimbing)
                    if (b) b.enabled = true;

            if (locomotionScript) locomotionScript.enabled = true;
        }
    }

    private void StopClimb()
    {
        isClimbing = false;
        activeHand = null;

        // Inicia caída natural
        releaseTimer = releaseSeconds;
        fallVelocity = 0f;
    }

    /* ---------------- UTILIDADES ---------------- */

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
