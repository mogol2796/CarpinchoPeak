using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Base")]
    public float baseMaxEnergy = 100f;

    [Header("Stats")]
    public float health = 100f;
    public float hunger = 100f;
    public float cold   = 0f;

    [Header("Energy")]
    public float energy = 100f;
    public float maxEnergy;

    private const float MAX = 100f;

    private void Update()
    {
        RecalculateEnergy();
    }

    private void RecalculateEnergy()
    {
        float h01 = health / MAX;
        float hu01 = hunger / MAX;
        float c01 = cold / MAX;

        float healthPenalty = h01 >= 0.7f ? 1f : Mathf.Lerp(0.5f, 1f, h01 / 0.7f);
        float hungerPenalty = hu01 >= 0.7f ? 1f : Mathf.Lerp(0.5f, 1f, hu01 / 0.7f);
        float coldPenalty   = c01 <= 0.3f ? 1f : Mathf.Lerp(1f, 0.5f, (c01 - 0.3f) / 0.7f);

        maxEnergy = baseMaxEnergy * healthPenalty * hungerPenalty * coldPenalty;
        energy = Mathf.Min(energy, maxEnergy);
    }
}
