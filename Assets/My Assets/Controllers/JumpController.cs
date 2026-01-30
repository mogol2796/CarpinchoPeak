using UnityEngine;
using UnityEngine.InputSystem;
using OvrCharacterController = Oculus.Interaction.Locomotion.CharacterController;

public class JumpController : MonoBehaviour
{
    [Header("Refs")]
    public OvrCharacterController ovrCC;
    public InputActionProperty jumpAction;
    // public ClimbManager climbManager;

    [Header("Tuning")]
    public float jumpVelocity = 3.0f;
    public float gravity = 9.81f;
    public float groundedStick = 1.0f;

    private float verticalVel;

    private void OnEnable() => jumpAction.action?.Enable();
    private void OnDisable() => jumpAction.action?.Disable();

    private void Update()
    {
        // if (climbManager && climbManager.IsClimbing()) return;
        if (!ovrCC || jumpAction.action == null) return;

        bool grounded = IsGroundedApprox();

        if (grounded && verticalVel < 0f)
            verticalVel = -groundedStick;

        if (grounded && jumpAction.action.WasPressedThisFrame())
            verticalVel = jumpVelocity;

        verticalVel -= gravity * Time.deltaTime;

        ovrCC.Move(Vector3.up * verticalVel * Time.deltaTime);
    }

    private bool IsGroundedApprox()
    {
        if (!ovrCC) return false;

        return ovrCC.IsGrounded;
    }
}
