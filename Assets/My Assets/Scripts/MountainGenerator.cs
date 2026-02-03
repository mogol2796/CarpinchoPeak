using UnityEngine;

public class MountainGenerator : MonoBehaviour
{
    public RockScatterer bigScatterer;
    public RockScatterer midScatterer;
    public RockScatterer smallScatterer;

    [Header("Seed Management")]
    public bool randomizeOnGenerate = true;

    [Tooltip("Record these if you find a layout you like!")]
    public int lastBigSeed;
    public int lastMidSeed;
    public int lastSmallSeed;

    [Header("Settings")]
    public bool generateOnStart = true;

    [Header("Win Zone Setup")]
    public GameObject winZonePrefab;
    public float winZoneOffset = 5f;

    void Start()
    {
        if (generateOnStart) Generate();
    }

    [ContextMenu("Generate Full Mountain")]
    public void Generate()
    {
        if (randomizeOnGenerate)
        {
            lastBigSeed = Random.Range(1, 999999);
            lastMidSeed = Random.Range(1, 999999);
            lastSmallSeed = Random.Range(1, 999999);

            if (bigScatterer) bigScatterer.scatterSeed = lastBigSeed;
            if (midScatterer) midScatterer.scatterSeed = lastMidSeed;
            if (smallScatterer) smallScatterer.scatterSeed = lastSmallSeed;
        }

        Debug.Log($"Generating Mountain with Seeds: Big({lastBigSeed}) Mid({lastMidSeed}) Small({lastSmallSeed})");

        if (bigScatterer != null)
        {
            bigScatterer.SpawnRocks();
            SetLayerRecursive(bigScatterer.transform, LayerMask.NameToLayer("BigBoulder"));
        }

        Physics.SyncTransforms();

        if (midScatterer != null)
        {
            midScatterer.SpawnRocks();
            SetLayerRecursive(midScatterer.transform, LayerMask.NameToLayer("MidBoulder"));
        }

        Physics.SyncTransforms();

        if (smallScatterer != null)
        {
            smallScatterer.SpawnRocks();
        }

        Debug.Log("Mountain Generation Complete!");

        float truePeakY = 0;

        // We check the Big Scatterer since it's the "foundation"
        foreach (Transform rock in bigScatterer.transform)
        {
            if (rock.TryGetComponent<Collider>(out var col))
            {
                float rockTop = col.bounds.max.y;
                if (rockTop > truePeakY)
                {
                    truePeakY = rockTop;
                }
            }
        }

        PlaceWinZone(truePeakY);
    }

    void SetLayerRecursive(Transform parent, int newLayer)
    {
        if (newLayer == -1) return;
        foreach (Transform child in parent)
        {
            child.gameObject.layer = newLayer;
            foreach (Transform grandChild in child) grandChild.gameObject.layer = newLayer;
        }
    }

    public void PlaceWinZone(float highestY)
    {
        WinZone existingZone = FindFirstObjectByType<WinZone>();
        Vector3 spawnPos = new Vector3(0, highestY + winZoneOffset, 0);

        if (existingZone == null)
        {
            Instantiate(winZonePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            existingZone.transform.position = spawnPos;
        }
    }
}