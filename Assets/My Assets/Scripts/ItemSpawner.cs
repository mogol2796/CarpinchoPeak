using UnityEngine;

[RequireComponent(typeof(ProceduralRock))]
public class ItemSpawner : MonoBehaviour
{
    [Header("Item Settings")]
    public GameObject[] itemPrefabs;
    [Range(0, 5)]
    public int maxItemsPerRock = 1; 
    [Range(0f, 1f)]
    public float spawnChance = 0.3f;

    [Header("Placement Rules")]
    public LayerMask rockLayer;
    public float maxSlopeAngle = 20f;

    public void SpawnItems()
    {
        if (Random.value > spawnChance) return;

        ProceduralRock rock = GetComponent<ProceduralRock>();
        if (rock == null || itemPrefabs == null || itemPrefabs.Length == 0) return;

        int spawnedCount = 0;
        int attempts = 0;

        while (spawnedCount < maxItemsPerRock && attempts < 15)
        {
            attempts++;

            Vector2 randomPoint = Random.insideUnitCircle * rock.topRadius;
            Vector3 localPos = new Vector3(
                rock.topCenterLocal.x + randomPoint.x,
                rock.topCenterLocal.y + 2f, 
                rock.topCenterLocal.z + randomPoint.y
            );

            Vector3 worldOrigin = transform.TransformPoint(localPos);

            if (Physics.Raycast(worldOrigin, -transform.up, out RaycastHit hit, 5f, rockLayer))
            {
                if (hit.collider.gameObject != this.gameObject) continue;

                float angle = Vector3.Angle(transform.up, hit.normal);
                if (angle <= maxSlopeAngle)
                {
                    
                    float plantAvoidanceRadius = 0.5f;
                    Collider[] nearbyDecorations = Physics.OverlapSphere(hit.point, plantAvoidanceRadius);

                    bool tooCloseToPlant = false;
                    foreach (var decor in nearbyDecorations)
                    {
                        if (decor.name.Contains("RockDecoration_Clone"))
                        {
                            tooCloseToPlant = true;
                            break;
                        }
                    }

                    if (!tooCloseToPlant)
                    {
                        PlaceItem(hit.point, hit.normal);
                        spawnedCount++;
                    }
                }
            }
        }
    }

    void PlaceItem(Vector3 pos, Vector3 normal)
    {
        GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        Vector3 finalPos = pos + (normal * 0.05f);

        GameObject item = Instantiate(prefab, finalPos, Quaternion.identity);
        item.transform.parent = this.transform;

        item.transform.Rotate(Vector3.up, Random.Range(0, 360));
    }
}