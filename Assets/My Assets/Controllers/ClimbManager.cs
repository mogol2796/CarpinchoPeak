using UnityEngine;

public class ClimbManager : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController controller;
    public MonoBehaviour locomotionScript;
    public LayerMask climbableMask;


    [Header("Tuning")]
    public float maxClimbSpeed = 4.0f;
    public float climbStrength = 1.5f;
    public float wallProbeDistance = 0.3f;
    public float wallPushOut = 0.02f;

    private ClimbHand activeHand;
    private bool isClimbing;
    private Vector3 lastHandPos;

    private void Reset()
    {
        controller = FindFirstObjectByType<CharacterController>();
    }

    public void TryBeginClimb(ClimbHand hand)
    {
        if (isClimbing && activeHand == hand) return;

        activeHand = hand;

        if (!isClimbing)
        {
            isClimbing = true;
            if (locomotionScript) locomotionScript.enabled = false;
        }

        lastHandPos = activeHand.HandWorldPos;
        

    }

    public void TryEndClimb(ClimbHand hand)
    {
        // si suelta una mano que no es la activa, ignorar
        if (hand != activeHand) return;

        // deja de escalar
        isClimbing = false;
        activeHand = null;

        if (locomotionScript) locomotionScript.enabled = true;

    }

    private void LateUpdate()
    {
        if (!isClimbing || activeHand == null || controller == null) return;

        Vector3 current = activeHand.HandWorldPos;
        Vector3 handDelta = current - lastHandPos;
        Vector3 move = -handDelta * climbStrength;

        Vector3 n = activeHand.WallNormal;
        if (n != Vector3.zero)
        {
            move = Vector3.ProjectOnPlane(move, n);
            move += n * wallPushOut;
        }
        float maxStep = maxClimbSpeed * Time.deltaTime;
        if (move.magnitude > maxStep)
            move = move.normalized * maxStep;

        controller.Move(move);
        lastHandPos = current;
    }

}
