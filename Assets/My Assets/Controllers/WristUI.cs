using UnityEngine;
using UnityEngine.UI;

public class WristUI : MonoBehaviour
{
    public PlayerManager pm;

    public Image energyFill;
    public Image healthFill;
    public Image hungerFill;
    public Image coldFill;

    void Update()
    {
        if (!pm) return;

        energyFill.fillAmount = pm.energy / pm.maxEnergy;
        healthFill.fillAmount = pm.health / 100f;
        hungerFill.fillAmount = pm.hunger / 100f;
        coldFill.fillAmount   = pm.cold / 100f;
    }
}
