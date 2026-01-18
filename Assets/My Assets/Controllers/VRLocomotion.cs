using UnityEngine;
using UnityEngine.InputSystem;

public class VRLocomotion : MonoBehaviour
{
    public CharacterController controller;
    public Transform centerEye;                 // CenterEyeAnchor
    public InputActionProperty move;            // Left joystick
    public float speed = 2.0f;
    public float gravity = -9.81f;

    private float verticalVelocity;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        move.action?.Enable();
    }

    private void OnDisable()
    {
        move.action?.Disable();
    }

    void Update()
    {
        Vector2 input = move.action.ReadValue<Vector2>();

        // Dirección relativa a la mirada
        Vector3 forward = centerEye.forward;
        Vector3 right = centerEye.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (right * input.x + forward * input.y) * speed;

        // Gravedad / suelo
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        moveDir.y = verticalVelocity;

        controller.Move(moveDir * Time.deltaTime);
    }
}
