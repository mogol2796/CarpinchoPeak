using UnityEngine;

public class FogRise : MonoBehaviour
{
    [Header("Movement")]
    public float riseSpeed = 10f;

    [Header("Visuals")]
    public Transform playerTransform;
    public Color fogColor = new Color(0.8f, 0.8f, 0.8f);
    public float maxFogDensity = 0.05f;
    [Tooltip("How far above the top of the cube the fog starts appearing visually")]
    public float visualWarningDistance = 10f;

    [Header("Damage Settings")]
    public float damagePerSecond = 10f;
    private float coldTickTimer = 0f;
    private float coldInterval = 0.5f; 

    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
    }

    void Update()
    {
        transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);

        float cubeTopY = transform.position.y + (transform.localScale.y / 2f);
        float distanceToFog = playerTransform.position.y - cubeTopY;

        float t = 1f - Mathf.Clamp01(distanceToFog / visualWarningDistance);

        if (distanceToFog < 0) t = 1f;

        RenderSettings.fogDensity = t * maxFogDensity;
    }

    private void OnTriggerStay(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            coldTickTimer += Time.deltaTime;

            if (coldTickTimer >= coldInterval)
            {
                //AQUI VA LA LOGICA PARA PONERLE FRIO AL PLAYER
            }
        }
    }
}