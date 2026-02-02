using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EnergyTunnelVision : MonoBehaviour
{
    [Header("Refs")]
    public PlayerManager player;
    public Volume volume;

    [Header("Thresholds (energy current)")]
    public float t1 = 50f;  // empieza túnel
    public float t2 = 30f;  // más fuerte
    public float t3 = 15f;  // crítico

    [Header("Vignette Intensities")]
    [Range(0f, 1f)] public float i1 = 0.25f;
    [Range(0f, 1f)] public float i2 = 0.45f;
    [Range(0f, 1f)] public float i3 = 0.65f;

    [Header("Smoothing")]
    public float lerpSpeed = 8f;

    private Vignette _vig;
    private float _currentIntensity;

    void Awake()
    {
        if (!volume) volume = FindFirstObjectByType<Volume>();

        if (volume && volume.profile && volume.profile.TryGet(out _vig))
        {
            _vig.active = true;
            _vig.intensity.Override(0f);
            _currentIntensity = 0f;
        }
        else
        {
            Debug.LogError("EnergyTunnelVision: No encuentro Vignette en el Volume Profile.");
        }
    }

    void Update()
    {
        if (!player || _vig == null) return;

        float e = player.energy; // energía ACTUAL, no maxEnergy
        float target = GetTargetIntensity(e);

        _currentIntensity = Mathf.Lerp(_currentIntensity, target, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
        _vig.intensity.Override(_currentIntensity);
    }

    float GetTargetIntensity(float e)
    {
        // Por encima de t1: nada
        if (e >= t1) return 0f;

        // Entre t1 y t2: 0 -> i1
        if (e >= t2)
        {
            float t = Mathf.InverseLerp(t1, t2, e); // e baja => t baja
            return Mathf.Lerp(i1, 0f, t);           // cuando e=t1 => 0, cuando e=t2 => i1
        }

        // Entre t2 y t3: i1 -> i2
        if (e >= t3)
        {
            float t = Mathf.InverseLerp(t2, t3, e);
            return Mathf.Lerp(i2, i1, t);           // e=t2 => i1, e=t3 => i2
        }

        // Debajo de t3: i3 (crítico)
        return i3;
    }
}
