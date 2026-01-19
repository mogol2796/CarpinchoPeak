using UnityEngine;

public class VRCharacterControllerAlign : MonoBehaviour
{
    public CharacterController controller;
    public Transform centerEye; // CenterEyeAnchor
    public float radius = 0.25f;
    public float minHeight = 1.0f;
    public float maxHeight = 2.0f;

    void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    void LateUpdate()
    {
        if (!controller || !centerEye) return;

        // Altura basada en la cabeza (local)
        float headHeight = Mathf.Clamp(centerEye.localPosition.y, minHeight, maxHeight);
        controller.height = headHeight;
        controller.radius = radius;

        // Centro XZ siguiendo a la cabeza (local)
        Vector3 c = controller.center;
        c.x = centerEye.localPosition.x;
        c.z = centerEye.localPosition.z;
        c.y = headHeight * 0.5f;
        controller.center = c;
    }
}
