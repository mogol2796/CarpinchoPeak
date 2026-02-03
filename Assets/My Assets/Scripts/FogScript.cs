using UnityEngine;

public class FogRise : MonoBehaviour
{
    [Header("Movement")]
    public float riseSpeed = 10f;

    [Header("Visuals")]
    public Transform playerTransform;
    public Color fogColor = new Color(0.8f, 0.8f, 0.8f);
    public float maxFogDensity = 0.05f;
    [Tooltip("How far below the player the fog starts appearing visually")]
    public float visualWarningDistance = 10f;

    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
    }

    void Update()
    {
        transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);

        float distanceToFog = playerTransform.position.y - transform.position.y;


        float t = 1f - Mathf.Clamp01(distanceToFog / visualWarningDistance);
        RenderSettings.fogDensity = t * maxFogDensity;
    }
}