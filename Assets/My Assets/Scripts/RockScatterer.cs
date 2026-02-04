using UnityEngine;
using System.Collections.Generic;

public class RockScatterer : MonoBehaviour
{
    public enum RockStyle { Pillar, Ledge, Boulder, PeakMix }

    [System.Serializable]
    public class StyleWeight
    {
        public RockStyle style;
        [Range(0, 100)] public float weight = 25f;
    }

    [Header("Generation Mode")]
    public bool useRandomWeights = true;
    public RockStyle fixedStyle = RockStyle.PeakMix;
    public List<StyleWeight> styleWeights = new List<StyleWeight>()
    {
        new StyleWeight { style = RockStyle.Pillar, weight = 15 },
        new StyleWeight { style = RockStyle.Ledge, weight = 50 },
        new StyleWeight { style = RockStyle.Boulder, weight = 15 },
        new StyleWeight { style = RockStyle.PeakMix, weight = 20 }
    };

    [Header("Scanner Focus")]
    [Range(0, 360)] public float centerAngle = 0f;
    [Range(1, 360)] public float fieldOfView = 60f;
    public float scanRadius = 50f;
    public float minHeight = 0f;
    public float maxHeight = 20f;

    [Header("Spawn Settings")]
    public int rockCount = 10;
    public int scatterSeed = 42;
    public float offsetFromWall = -0.5f;

    [Header("Base Values")]
    public float baseRockRadius = 5f;
    public float baseNoiseStrength = 2f;

    [Header("Hierarchical Scaling")]
    public bool autoScaleBasedOnLayer = true;
    public LayerMask targetLayer;
    [Tooltip("Size multiplier when nesting rocks")] public float sizeMultiplier = 0.4f;
    [Tooltip("Noise multiplier when nesting rocks")] public float noiseMultiplier = 0.5f;

    [Header("References")]
    public Material rockMaterial;

    private class RayData { public Vector3 start; public Vector3 end; public Vector3 normal; public bool hit; }
    private List<RayData> lastSpawnRays = new List<RayData>();

    [ContextMenu("Spawn Rocks")]
    public void SpawnRocks()
    {
        Random.InitState(scatterSeed);
        lastSpawnRays.Clear();

        // Cleanup existing rocks
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        int successCount = 0;
        int attempts = 0;

        // We want to shoot FROM the scanRadius back TOWARD the center
        while (successCount < rockCount && attempts < rockCount * 15)
        {
            attempts++;

            // 1. Calculate the angle based on your FOV settings
            float halfFOV = fieldOfView / 2f;
            float randomAngle = (centerAngle + Random.Range(-halfFOV, halfFOV)) * Mathf.Deg2Rad;

            // 2. Determine the starting point (The Outside Edge of the Cylinder)
            // We multiply by scanRadius to start far away from the center
            Vector3 edgePosition = new Vector3(Mathf.Sin(randomAngle), 0, Mathf.Cos(randomAngle)) * scanRadius;
            Vector3 origin = transform.position + edgePosition + new Vector3(0, Random.Range(minHeight, maxHeight), 0);

            // 3. The Direction is now INWARD (Point back at the center of this object)
            // We ignore the Y difference to keep the ray horizontal
            Vector3 centerAtRayHeight = new Vector3(transform.position.x, origin.y, transform.position.z);
            Vector3 directionInward = (centerAtRayHeight - origin).normalized;

            // 4. Raycast INWARD
            // We use scanRadius + a little extra padding for the distance
            if (Physics.Raycast(origin, directionInward, out RaycastHit hit, scanRadius + 5f, targetLayer))
            {
                CreateRockOnWall(hit.point, hit.normal, hit.collider.gameObject);
                lastSpawnRays.Add(new RayData { start = origin, end = hit.point, normal = hit.normal, hit = true });
                successCount++;
            }
            else
            {
                lastSpawnRays.Add(new RayData { start = origin, end = origin + directionInward * scanRadius, hit = false });
            }
        }
    }

    void CreateRockOnWall(Vector3 position, Vector3 wallNormal, GameObject hitObject)
    {
        GameObject rockObj = new GameObject("ProceduralRock_Instance");
        rockObj.transform.position = position + (wallNormal * offsetFromWall);
        rockObj.transform.parent = this.transform;

        rockObj.transform.rotation = Quaternion.LookRotation(wallNormal, Vector3.up);

        ProceduralRock rockScript = rockObj.AddComponent<ProceduralRock>();

        rockScript.baseRadius = baseRockRadius;
        rockScript.noiseStrength = baseNoiseStrength;
        rockScript.seed = Random.Range(0, 10000);
        rockScript.useFlatTop = true;
       
        if (autoScaleBasedOnLayer && hitObject.layer != LayerMask.NameToLayer("Mountain"))
        {
            rockScript.baseRadius *= sizeMultiplier;
            rockScript.noiseStrength *= noiseMultiplier;
            rockScript.resolution = Mathf.Max(4, (int)(rockScript.resolution * 0.7f));
        }

        RockStyle style = useRandomWeights ? GetWeightedRandomStyle() : fixedStyle;
        rockScript.shapeStretch = GetStretchForStyle(style);

        if (rockMaterial != null) rockObj.GetComponent<MeshRenderer>().sharedMaterial = rockMaterial;

        PlantSpawner pSpawner = rockObj.AddComponent<PlantSpawner>();

        rockObj.GetComponent<MeshCollider>().providesContacts = true;
        rockObj.layer = 3;

        rockScript.GenerateRock();
    }

    RockStyle GetWeightedRandomStyle()
    {
        float total = 0;
        foreach (var sw in styleWeights) total += sw.weight;
        float r = Random.Range(0, total);
        float current = 0;
        foreach (var sw in styleWeights)
        {
            current += sw.weight;
            if (r <= current) return sw.style;
        }
        return RockStyle.Boulder;
    }

    Vector3 GetStretchForStyle(RockStyle style)
    {
        switch (style)
        {
            case RockStyle.Pillar: return new Vector3(Random.Range(0.6f, 0.9f), Random.Range(2.5f, 4.5f), Random.Range(0.6f, 0.9f));
            case RockStyle.Ledge: return new Vector3(Random.Range(2.5f, 5.0f), Random.Range(0.3f, 0.7f), Random.Range(2.0f, 3.5f));
            case RockStyle.Boulder: return new Vector3(Random.Range(1.0f, 1.8f), Random.Range(1.0f, 1.8f), Random.Range(1.0f, 1.8f));
            case RockStyle.PeakMix: return new Vector3(Random.Range(0.8f, 2.5f), Random.Range(0.8f, 2.5f), Random.Range(0.8f, 2.5f));
            default: return Vector3.one;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;

        Gizmos.DrawLine(center + Vector3.up * minHeight, center + Vector3.up * maxHeight);

#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1, 1, 0, 0.1f); // Transparent yellow
        Vector3 forward = Quaternion.Euler(0, centerAngle, 0) * Vector3.forward;

        UnityEditor.Handles.DrawSolidArc(
            center + Vector3.up * ((minHeight + maxHeight) / 2),
            Vector3.up,
            Quaternion.Euler(0, -fieldOfView / 2, 0) * forward,
            fieldOfView,
            scanRadius
        );

        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireArc(
            center + Vector3.up * ((minHeight + maxHeight) / 2),
            Vector3.up,
            Quaternion.Euler(0, -fieldOfView / 2, 0) * forward,
            fieldOfView,
            scanRadius
        );
#endif

        foreach (var ray in lastSpawnRays)
        {
            Gizmos.color = ray.hit ? Color.green : Color.red;
            Gizmos.DrawLine(ray.start, ray.end);
            if (ray.hit) Gizmos.DrawSphere(ray.end, 0.1f);
        }
    }
}