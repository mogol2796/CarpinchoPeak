using UnityEngine;

public class MasterItemSpawner : MonoBehaviour
{
    [Header("Generation Settings")]
    public GameObject[] itemPrefabs;
    public int totalSpawnAttempts = 500;
    public int maxItems = 25;           
    public Vector2 spawnAreaSize = new Vector2(50f, 50f);
    public LayerMask rockLayer;
    public float maxSlopeAngle = 20f;
    public float itemOffset = 0.05f;
    public void SpawnItems()
    {
        int spawnedCount = 0;
        int attempts = 0;

        while (spawnedCount < maxItems && attempts < totalSpawnAttempts)
        {
            attempts++;

            float randomX = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
            float randomZ = Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);

            Vector3 rayOrigin = new Vector3(
                transform.position.x + randomX,
                transform.position.y + 100f,
                transform.position.z + randomZ
            );

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 200f, rockLayer))
            {
                float slope = Vector3.Angle(Vector3.up, hit.normal);

                if (slope <= maxSlopeAngle)
                {
                    if (!IsPositionOccupied(hit.point))
                    {
                        PlaceItem(hit.point, hit.normal);
                        spawnedCount++; 
                    }
                }
            }
        }

        Debug.Log($"Item Generation Finished: Spawned {spawnedCount} items in {attempts} attempts.");
    }

    bool IsPositionOccupied(Vector3 pos)
    {
        Collider[] hitColliders = Physics.OverlapSphere(pos, 0.5f);
        foreach (var col in hitColliders)
        {
            if (col.name.Contains("RockDecoration_Clone") || col.CompareTag("Item"))
                return true;
        }
        return false;
    }

    void PlaceItem(Vector3 pos, Vector3 normal)
    {
        GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        GameObject item = Instantiate(prefab, pos + (normal * itemOffset), Quaternion.identity);

        item.transform.SetParent(this.transform);

        item.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        item.transform.Rotate(Vector3.up, Random.Range(0, 360));
        item.tag = "Item";
    }

    [Header("Debug")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.yellow;

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;

        Vector3 center = transform.position;
        Vector3 size = new Vector3(spawnAreaSize.x, 100f, spawnAreaSize.y);
        Gizmos.DrawWireCube(center + Vector3.up * 50f, size);

        
        int linesPerRow = 10;
        float xSpacing = spawnAreaSize.x / (linesPerRow - 1);
        float zSpacing = spawnAreaSize.y / (linesPerRow - 1);

        for (int x = 0; x < linesPerRow; x++)
        {
            for (int z = 0; z < linesPerRow; z++)
            {
                float xPos = (x * xSpacing) - (spawnAreaSize.x / 2);
                float zPos = (z * zSpacing) - (spawnAreaSize.y / 2);

                Vector3 start = new Vector3(center.x + xPos, center.y + 100f, center.z + zPos);
                Vector3 end = start + Vector3.down * 150f;

                Gizmos.DrawLine(start, end);

                Gizmos.DrawSphere(start, 0.2f);
            }
        }
    }
}
