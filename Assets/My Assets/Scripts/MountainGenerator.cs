using UnityEngine;
using System.Collections;

public class MountainGenerator : MonoBehaviour
{
    public RockScatterer bigScatterer;
    public RockScatterer midScatterer;
    public RockScatterer smallScatterer;

    [Header("Seed Management")]
    public bool randomizeOnGenerate = true;
    public int lastBigSeed;
    public int lastMidSeed;
    public int lastSmallSeed;

    [Header("Settings")]
    public bool generateOnStart = true;

    [Header("Win Zone Setup")]
    public GameObject winZonePrefab;
    public float winZoneOffset = 5f;

    [Header("Global Prefabs")]
    public GameObject[] globalPlantPrefabs;
    public GameObject[] globalItemPrefabs;

    [Header("Skybox Sync")]
    public Material skyboxMaterial;
    public Light directionalLight;

    void Start()
    {
        if (generateOnStart) Generate();
    }


    void Update()
    {
        if (skyboxMaterial != null && directionalLight != null)
        {
            Vector3 sunDir = -directionalLight.transform.forward;
            skyboxMaterial.SetVector("_SunDirection", sunDir);
        }
    }

    [ContextMenu("Generate Full Mountain")]
    public void Generate()
    {
        StartCoroutine(GenerationRoutine());
    }

    IEnumerator GenerationRoutine()
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

        yield return new WaitForFixedUpdate();

        ProceduralRock[] allRocks = FindObjectsByType<ProceduralRock>(FindObjectsSortMode.None);
        foreach (ProceduralRock rock in allRocks)
        {
            if (rock.TryGetComponent<PlantSpawner>(out var spawner))
            {
                spawner.plantPrefabs = globalPlantPrefabs;
                spawner.SpawnPlants();
            }
        }

        float truePeakY = 0;
        foreach (Transform rock in bigScatterer.transform)
        {
            if (rock.TryGetComponent<Collider>(out var col))
            {
                float rockTop = col.bounds.max.y;
                if (rockTop > truePeakY) truePeakY = rockTop;
            }
        }

        PlaceWinZone(truePeakY);

        MasterItemSpawner masterItemSpawner = FindFirstObjectByType<MasterItemSpawner>();
        if (masterItemSpawner != null)
        {
            masterItemSpawner.SpawnItems();
        }

        Debug.Log("Mountain Generation and Decoration Complete!");
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

        if (existingZone == null) Instantiate(winZonePrefab, spawnPos, Quaternion.identity);
        else existingZone.transform.position = spawnPos;
    }
}