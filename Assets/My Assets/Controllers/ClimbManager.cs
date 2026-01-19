using System.Collections.Generic;
using UnityEngine;
using OvrCharacterController = Oculus.Interaction.Locomotion.CharacterController;

public class ClimbManager : MonoBehaviour
{
    [Header("Refs (Building Blocks)")]
    public Transform playerRoot;
    public OvrCharacterController ovrCharacterController;
    public MonoBehaviour locomotionScript;

    [Header("Disable while climbing (BB safety)")]
    public MonoBehaviour[] disableWhileClimbing;

    [Header("Tuning")]
    public float climbStrength = 2.0f;
    public float maxClimbSpeed = 4.0f;

    [Header("Smoothing")]
    public float moveSmoothing = 20f; // 10-30
    private Vector3 smoothedApplied;

    private readonly List<ClimbHand> hands = new();
    private ClimbHand activeHand;
    private bool isClimbing;
    private Vector3 lastHandPos;

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
        if (hand == null || !hand.IsGrabbing || !hand.HasClimbContact) return;
        if (!ovrCharacterController) return;

        activeHand = hand;

        if (!isClimbing)
        {
            isClimbing = true;
            if (locomotionScript) locomotionScript.enabled = false;

            if (disableWhileClimbing != null)
            {
                foreach (var b in disableWhileClimbing)
                    if (b) b.enabled = false;
            }
        }

        lastHandPos = hand.HandWorldPos;
        smoothedApplied = Vector3.zero;
    }

    public void TryEndClimb(ClimbHand hand)
    {
        if (hand == null) return;
        if (hand != activeHand) return;

        var fb = FindFallbackHand(hand);
        if (fb != null)
        {
            activeHand = fb;
            lastHandPos = fb.HandWorldPos;
            smoothedApplied = Vector3.zero;
            return;
        }

        StopClimb();
    }

    private void LateUpdate()
    {
        if (!isClimbing || activeHand == null || ovrCharacterController == null || playerRoot == null) return;

        Vector3 current = activeHand.HandWorldPos;
        Vector3 handDelta = current - lastHandPos;

        Vector3 move = -handDelta * climbStrength;

        // ✅ Subir + lateral: movimiento paralelo a la pared
        Vector3 n = activeHand.WallNormal;
        if (n != Vector3.zero)
        {
            move = Vector3.ProjectOnPlane(move, n);
        }

        float maxStep = maxClimbSpeed * Time.deltaTime;
        if (move.magnitude > maxStep) move = move.normalized * maxStep;

        ApplyMoveWithCollision(move);

        lastHandPos = current;
    }

    private void ApplyMoveWithCollision(Vector3 move)
    {
        Transform ccT = ovrCharacterController.transform;
        Vector3 before = ccT.position;

        ovrCharacterController.Move(move);

        Vector3 after = ccT.position;
        Vector3 appliedDelta = after - before;

        // ✅ suavizado anti-jitter
        smoothedApplied = Vector3.Lerp(smoothedApplied, appliedDelta, 1f - Mathf.Exp(-moveSmoothing * Time.deltaTime));
        playerRoot.position += smoothedApplied;

        ccT.position = before;
    }

    private ClimbHand FindFallbackHand(ClimbHand ignored)
    {
        for (int i = 0; i < hands.Count; i++)
        {
            var h = hands[i];
            if (h && h != ignored && h.IsGrabbing && h.HasClimbContact) return h;
        }
        return null;
    }

    private void StopClimb()
    {
        isClimbing = false;
        activeHand = null;

        if (disableWhileClimbing != null)
        {
            foreach (var b in disableWhileClimbing)
                if (b) b.enabled = true;
        }

        if (locomotionScript) locomotionScript.enabled = true;
    }
}
