using UnityEngine;

public class MantleZone : MonoBehaviour
{
    public Transform standPoint;

    private void OnTriggerEnter(Collider other)
    {
        var cm = other.GetComponentInParent<ClimbManager>();
        if (cm) cm.SetMantleZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var cm = other.GetComponentInParent<ClimbManager>();
        if (cm) cm.SetMantleZone(null);
    }
}
