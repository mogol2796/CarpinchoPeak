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

        // Mover el capsule para que el Player siga al rig/cabeza en plano XZ
        bool headsetIsChild = centerEye.IsChildOf(controller.transform);
        if (!headsetIsChild)
        {
            Vector3 rigOffset = centerEye.position - controller.transform.position;
            rigOffset.y = 0f;
            if (rigOffset.sqrMagnitude > 0.0001f)
                controller.Move(rigOffset);
        }

        // Recalcular posicion de cabeza relativa al capsule (despues de movernos)
        Vector3 headLocal = controller.transform.InverseTransformPoint(centerEye.position);

        // Altura basada en la cabeza (local)
        float headHeight = Mathf.Clamp(headLocal.y, minHeight, maxHeight);
        controller.height = headHeight;
        controller.radius = radius;

        // Centro XZ siguiendo a la cabeza (local)
        Vector3 c = controller.center;
        c.x = headLocal.x;
        c.z = headLocal.z;
        c.y = headHeight * 0.5f;
        controller.center = c;
    }
}
