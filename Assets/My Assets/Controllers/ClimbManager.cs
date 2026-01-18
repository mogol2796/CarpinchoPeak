using UnityEngine;

public class ClimbManager : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController controller;     // el del Player
    public MonoBehaviour locomotionScript;     // tu script de movimiento con joystick (para desactivar)

    [Header("Tuning")]
    public float maxClimbSpeed = 4.0f;         // límite por seguridad

    private ClimbHand activeHand;
    private bool isClimbing;
    private Vector3 lastHandPos;

    private void Reset()
    {
        controller = FindFirstObjectByType<CharacterController>();
    }

    public void TryBeginClimb(ClimbHand hand)
    {
        // si ya estamos escalando con esa mano, nada
        if (isClimbing && activeHand == hand) return;

        // si no estamos escalando, o cambiamos a otra mano:
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

    private void Update()
    {
        if (!isClimbing || activeHand == null || controller == null) return;

        Vector3 current = activeHand.HandWorldPos;
        Vector3 handDelta = current - lastHandPos;      // mano se movió así
        Vector3 move = -handDelta;                      // cuerpo va al contrario

        // límite de velocidad (evita saltos por tracking)
        float maxStep = maxClimbSpeed * Time.deltaTime;
        if (move.magnitude > maxStep)
            move = move.normalized * maxStep;

        controller.Move(move);
        lastHandPos = current;
    }
}
