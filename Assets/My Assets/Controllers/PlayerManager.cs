using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Base")]
    public float baseMaxEnergy = 100f;

    [Header("Stats")]
    [Range(0, 100)] public float health = 100f;
    [Range(0, 100)] public float hunger = 100f;
    [Range(0, 100)] public float cold = 0f;

    [Header("Energy")]
    public float energy = 100f;
    public float maxEnergy;

    [Header("Hunger Drain")]
    public bool drainHunger = true;
    public float hungerDrainPerMinute = 6f;
    public float minHunger = 0f;

    private const float MAX = 100f;
    public float EnergyPercent01 => baseMaxEnergy <= 0 ? 0 : maxEnergy / baseMaxEnergy;
    public bool outOfStamina = false;

    private void Update()
    {
        UpdateHunger(Time.deltaTime);
        RecalculateEnergy();
    }

    private void UpdateHunger(float dt)
    {
        if (!drainHunger) return;

        float drainPerSecond = hungerDrainPerMinute / 60f;

        hunger = Mathf.Max(minHunger, hunger - drainPerSecond * dt);
    }

    private void RecalculateEnergy()
    {
        float h01  = Mathf.Clamp01(health / MAX);
        float hu01 = Mathf.Clamp01(hunger / MAX);
        float c01  = Mathf.Clamp01(cold / MAX);
        if (h01 <=0f || hu01 <= 0f || c01 >= 1f)
        {
            maxEnergy = 0f;
            energy = 0f;
            outOfStamina = true;
            //AÑADIR CODNICIÓN DE MUERTE AQUÍ
            return;
        }
        float healthPenalty = h01 >= 0.7f ? 1f : Mathf.Lerp(0.5f, 1f, h01 / 0.7f);
        float hungerPenalty = hu01 >= 0.7f ? 1f : Mathf.Lerp(0.5f, 1f, hu01 / 0.7f);

        float tCold = (c01 - 0.3f) / 0.7f;
        float coldPenalty = c01 <= 0.3f ? 1f : Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(tCold));

        maxEnergy = baseMaxEnergy * healthPenalty * hungerPenalty * coldPenalty;
        energy = Mathf.Min(energy, maxEnergy);
    }

    public void TakeDamage(float dmg)
    {
        health = Mathf.Clamp(health - dmg, 0f, 100f);
    }

    public void Heal(float amount)
    {
        health = Mathf.Clamp(health + amount, 0f, 100f);
    }

    public void Eat(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
    }

}
