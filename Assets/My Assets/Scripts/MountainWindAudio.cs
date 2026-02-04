using UnityEngine;
[RequireComponent(typeof(AudioSource))]

public class MountainWindAudio : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public AudioSource windAudioSource;

    [Header("Height Settings")]
    public float minHeight = 0f;
    public float maxHeight = 100f; // Set this to your mountain's peak height

    [Header("Volume & Pitch")]
    public float minVolume = 0.1f;
    public float maxVolume = 0.8f;
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;

    [Header("Dynamic Gusts")]
    public float gustFrequency = 0.5f;
    private float noiseOffset;

    void Start()
    {
        if (windAudioSource == null) windAudioSource = GetComponent<AudioSource>();
        windAudioSource.loop = true;
        windAudioSource.Play();
        noiseOffset = Random.Range(0f, 1000f);
    }

    void Update()
    {
        // 1. Calculate height percentage (0 to 1)
        float currentHeight = playerTransform.position.y;
        float heightT = Mathf.Clamp01((currentHeight - minHeight) / (maxHeight - minHeight));

        // 2. Add "Perlin Gusts" to make the wind feel alive
        // This prevents the wind from being a static, boring loop
        float gustValue = Mathf.PerlinNoise(Time.time * gustFrequency, noiseOffset);

        // 3. Combine height-based intensity with gusts
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, heightT) * gustValue;
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, heightT) + (gustValue * 0.1f);

        // 4. Smoothly apply the values to avoid audio pops
        windAudioSource.volume = Mathf.Lerp(windAudioSource.volume, targetVolume, Time.deltaTime);
        windAudioSource.pitch = Mathf.Lerp(windAudioSource.pitch, targetPitch, Time.deltaTime);
    }
}