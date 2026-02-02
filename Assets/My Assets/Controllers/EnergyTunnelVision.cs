using Oculus.Haptics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EnergyTunnelVision : MonoBehaviour
{
    [Header("Refs")]
    public PlayerManager player;
    public Volume volume;
    public HapticSource lowEnergyHaptics;

    [Header("Thresholds (energy current)")]
    public float t1 = 50f;
    public float t2 = 30f;
    public float t3 = 15f;

    [Header("Vignette Intensities")]
    [Range(0f, 1f)] public float i1 = 0.25f;
    [Range(0f, 1f)] public float i2 = 0.45f;
    [Range(0f, 1f)] public float i3 = 0.65f;

    [Header("Smoothing")]
    public float lerpSpeed = 8f;

    private Vignette _vig;
    private float _currentIntensity;

    // 0 = normal, 1 = low, 2 = very low, 3 = critical
    private int _level = -1;

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

    void OnDisable()
    {
        StopHaptics();
    }

    void Update()
    {
        if (!player || _vig == null) return;

        float e = player.energy;

        int newLevel = GetLevel(e);
        if (newLevel != _level)
        {
            _level = newLevel;
            ApplyHapticsForLevel(_level);
        }

        float target = GetTargetIntensity(e);
        _currentIntensity = Mathf.Lerp(_currentIntensity, target, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
        _vig.intensity.Override(_currentIntensity);
    }

    int GetLevel(float e)
    {
        if (e >= t1) return 0;
        if (e >= t2) return 1;
        if (e >= t3) return 2;
        return 3;
    }

    float GetTargetIntensity(float e)
    {
        if (e >= t1) return 0f;

        // Entre t1 y t2: 0 -> i1
        if (e >= t2)
        {
            float t = Mathf.InverseLerp(t1, t2, e); // OJO: t1>t2, por eso invertimos el lerp
            // cuando e=t1 => t=0 ; e=t2 => t=1
            return Mathf.Lerp(0f, i1, t);
        }

        // Entre t2 y t3: i1 -> i2
        if (e >= t3)
        {
            float t = Mathf.InverseLerp(t2, t3, e);
            return Mathf.Lerp(i1, i2, t);
        }

        // < t3
        return i3;
    }

    void ApplyHapticsForLevel(int level)
    {
        if (!lowEnergyHaptics) return;

        // Siempre paramos primero para no “enganchar” loops
        StopHaptics();

        if (level == 2)
        {
            lowEnergyHaptics.loop = false;
            lowEnergyHaptics.Play();
        }
        else if (level == 3)
        {
            lowEnergyHaptics.loop = true;
            lowEnergyHaptics.Play();
        }
        // level 0 y 1 => sin haptics
    }

    void StopHaptics()
    {
        if (!lowEnergyHaptics) return;
        lowEnergyHaptics.loop = false;
        lowEnergyHaptics.Stop();
    }
}
