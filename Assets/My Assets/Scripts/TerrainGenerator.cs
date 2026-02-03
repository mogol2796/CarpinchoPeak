using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{

    private Terrain[] terrains;

    [Header ("Terrain Settings")]
    public int width = 256;
    public int height = 60;
    public int depth = 256;

    [Header ("Noise Settings")]
    public float scale = 20f;
    public float offsetX;
    public float offsetZ;

    void Awake()
    {
        offsetX = Random.Range(0f, 99999f);
        offsetZ = Random.Range(0f, 99999f);
    }

    void Start()
    {
        terrains = GetComponentsInChildren<Terrain>();
        int index = 0;
        foreach (Terrain terrain in terrains)
        {
            TerrainData uniqueTerrain = Instantiate(terrain.terrainData);
            terrain.terrainData = uniqueTerrain;

            GenerateTerrainData(terrain, index);


            index++;
        }
    }


    void GenerateTerrainData(Terrain terrain, int index)
    {
        terrain.transform.position = new Vector3(width * index, 0, 0);
        terrain.terrainData.heightmapResolution = width + 1;
        terrain.terrainData.size = new Vector3(width, height, depth);
        terrain.terrainData.SetHeights(0, 0, CalculateHeights(index));
    }

    float[,] CalculateHeights(int index)
    {
        float[,] heights = new float[width, depth];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int globalX = x + (index * width);
                if(z == 100 && x == 123)
                {
                    int h = 1;
                }
                heights[x, z] = CalculateNoise(globalX, z);
            }
        }
        return heights;
    }

    float CalculateNoise(int x, int z)
    {
        float totalWidth = width * terrains.Length;
        float progress = x / totalWidth;

        float coordinatesX = ((float)x / width * scale) + offsetX;
        float coordinatesZ = ((float)z / depth * scale) + offsetZ;
        float noise = Mathf.PerlinNoise(coordinatesX, coordinatesZ);

        // Increase spikiness (sharper peaks) and height as we advance along the map
        float spikiness = 1f + (progress * 3f);
        float heightMultiplier = Mathf.Pow(progress, 2f);

        return noise * heightMultiplier;
    }

}
