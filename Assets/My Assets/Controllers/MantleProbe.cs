using UnityEngine;

public class MantleProbe : MonoBehaviour
{
    public ClimbManager manager;
    public Renderer r;
    public Color inZone = Color.cyan;
    public Color outZone = Color.white;

    private void OnTriggerEnter(Collider other)
    {
        var zone = other.GetComponentInParent<MantleZone>();
        if (zone) {
            manager.SetMantleZone(zone);
            if (r)
            {
                r.material.color = inZone;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var zone = other.GetComponentInParent<MantleZone>();
        if (zone) {
            manager.SetMantleZone(null);
            if (r)
            {
                r.material.color = outZone;
            }
        }
    }
}
