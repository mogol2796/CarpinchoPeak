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
        float currentHeight = playerTransform.position.y;
        float heightT = Mathf.Clamp01((currentHeight - minHeight) / (maxHeight - minHeight));

        float gustValue = Mathf.PerlinNoise(Time.time * gustFrequency, noiseOffset);

        float targetVolume = Mathf.Lerp(minVolume, maxVolume, heightT) * gustValue;
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, heightT) + (gustValue * 0.1f);

        windAudioSource.volume = Mathf.Lerp(windAudioSource.volume, targetVolume, Time.deltaTime);
        windAudioSource.pitch = Mathf.Lerp(windAudioSource.pitch, targetPitch, Time.deltaTime);
    }
}