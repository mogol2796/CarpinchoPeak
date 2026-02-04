using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ProceduralRock))]
public class PlantSpawner : MonoBehaviour
{
    [Header("Plant Settings")]
    public GameObject[] plantPrefabs;
    public int plantCount = 5;
    public Vector2 scaleRange = new Vector2(2f, 5f);

    [Header("Alignment")]
    public bool matchRockRotation = false;

    [Header("Density Settings")]
    [Tooltip("1 = Random, Higher = More clustered in the center")]
    public float centerClustering = 1.0f;

    [Header("Slope Limits")]
    [Range(0f, 90f)]
    public float maxSlopeAngle = 30f;

    public void SpawnPlants()
    {
        ProceduralRock rock = GetComponent<ProceduralRock>();
        if (rock == null || plantPrefabs == null || plantPrefabs.Length == 0) return;

        ClearPlants();

        int mountainLayer = LayerMask.GetMask("MountainRock");

        for (int i = 0; i < plantCount; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float biasedDistance = Mathf.Pow(Random.value, centerClustering);
            Vector2 finalPoint = randomDir * (biasedDistance * rock.topRadius);

            Vector3 spawnPosLocal = new Vector3(
                rock.topCenterLocal.x + finalPoint.x,
                rock.topCenterLocal.y + 2f, // Start raycast above the rock
                rock.topCenterLocal.y + finalPoint.y
            );

            Vector3 worldOrigin = transform.TransformPoint(spawnPosLocal);

            RaycastHit hit;
            if (Physics.Raycast(worldOrigin, -transform.up, out hit, 5f))
            {
                if (hit.collider.gameObject != this.gameObject)
                {
                    continue; 
                }

                float angle = Vector3.Angle(transform.up, hit.normal);

                if (angle <= maxSlopeAngle)
                {
                    float checkRadius = 0.2f;
                    Collider[] neighbors = Physics.OverlapSphere(hit.point, checkRadius, mountainLayer);

                    bool isBuried = false;
                    foreach (var col in neighbors)
                    {
                        if (col.gameObject != this.gameObject)
                        {
                            isBuried = true;
                            break;
                        }
                    }

                    if (isBuried) continue;

                    GameObject prefab = plantPrefabs[Random.Range(0, plantPrefabs.Length)];
                    GameObject plant = Instantiate(prefab, hit.point, Quaternion.identity);

                    plant.transform.parent = this.transform;
                    plant.name = "RockDecoration_Clone";

                    float s = Random.Range(scaleRange.x, scaleRange.y);
                    plant.transform.localScale = Vector3.one * s;
                    plant.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    if (matchRockRotation) plant.transform.up = hit.normal;
                }
            }
        }
    }

    public void ClearPlants()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.name.Contains("(Clone)"))
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }
}