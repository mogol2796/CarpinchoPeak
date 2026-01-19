using System.Collections.Generic;
using UnityEngine;
using OvrCharacterController = Oculus.Interaction.Locomotion.CharacterController;

public class ClimbManager : MonoBehaviour
{
    [Header("Refs (Building Blocks)")]
    public Transform playerRoot;
    public OvrCharacterController ovrCharacterController;  // Locomotor/PlayerController
    public MonoBehaviour locomotionScript;                 // FirstPersonLocomotor del BB

    [Header("Tuning")]
    public float climbStrength = 2.0f;
    public float maxClimbSpeed = 4.0f;

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
        }

        lastHandPos = hand.HandWorldPos;
    }

    public void TryEndClimb(ClimbHand hand)
    {
        if (hand == null) return;
        if (hand != activeHand) return;

        // fallback: si otra mano sigue agarrada, pásate
        var fb = FindFallbackHand(hand);
        if (fb != null)
        {
            activeHand = fb;
            lastHandPos = fb.HandWorldPos;
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

        // Bloquea solo empuje hacia dentro (no ProjectOnPlane aquí)
        Vector3 n = activeHand.WallNormal;
        if (n != Vector3.zero)
        {
            float intoWall = Vector3.Dot(move, n);
            if (intoWall < 0f) move -= n * intoWall;
        }

        float maxStep = maxClimbSpeed * Time.deltaTime;
        if (move.magnitude > maxStep) move = move.normalized * maxStep;

        ApplyMoveWithCollision(move);

        lastHandPos = current;
    }

    private void ApplyMoveWithCollision(Vector3 move)
    {
        // 1) Deja el controller donde está
        Transform ccT = ovrCharacterController.transform;
        Vector3 before = ccT.position;

        // 2) Pide al BB que resuelva movimiento+colisión
        ovrCharacterController.Move(move);

        // 3) Delta real aplicado (después de colisión)
        Vector3 after = ccT.position;
        Vector3 appliedDelta = after - before;

        // 4) Mueve el root del jugador (esto mueve la cámara/rig)
        playerRoot.position += appliedDelta;

        // 5) Revertimos la pos del cc para que no se “despegue” del rig
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
        if (locomotionScript) locomotionScript.enabled = true;
    }
}
