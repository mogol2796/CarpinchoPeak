using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralRock : MonoBehaviour
{
    [Header("Rock Shape & Style")]
    public int resolution = 10;
    public float baseRadius = 1f;
    public Vector3 shapeStretch = new Vector3(1.2f, 0.8f, 1.1f);
    public bool useFlatShading = true;

    [Header("Noise & Jaggedness")]
    public float noiseStrength = 0.5f;
    public float noiseFrequency = 1.5f;
    [Range(0f, 1f)]
    public float jaggedness = 0.3f;
    public int seed = 0;

    [Header("Flat Top Settings")]
    public bool useFlatTop = false;
    public float flatTopHeight = 0.5f;
    [Range(0f, 1f)]
    public float flatnessImperfection = 0.1f;

    [Header("Spawn Zone (Read Only)")]
    public Vector3 topCenterLocal;
    public float topRadius;

    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) GenerateRock(); };
#endif
    }

    void Start() => GenerateRock();

    public void GenerateRock()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null || mf.sharedMesh.name != "Procedural Rock")
        {
            mesh = new Mesh { name = "Procedural Rock" };
            mf.sharedMesh = mesh;
        }
        else mesh = mf.sharedMesh;

        vertices = new List<Vector3>();
        triangles = new List<int>();

        CreateStretchedSphere();

        ApplyAdvancedNoise();

        if (useFlatTop)
        {
            ApplyFlatTop();
        }

        if (useFlatShading)
        {
            FlatShade();
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        if (TryGetComponent<MeshCollider>(out var col)) col.sharedMesh = mesh;
    }

    void CreateStretchedSphere()
    {
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        foreach (var dir in directions) CreateFace(dir);
    }

    void CreateFace(Vector3 localUp)
    {
        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);
        int vIndexStart = vertices.Count;
        int res = Mathf.Clamp(resolution, 2, 50);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 percent = new Vector2(x, y) / (res - 1);
                Vector3 pointOnCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;

                Vector3 pointOnSphere = pointOnCube.normalized;

                pointOnSphere.x *= shapeStretch.x;
                pointOnSphere.y *= shapeStretch.y;
                pointOnSphere.z *= shapeStretch.z;

                vertices.Add(pointOnSphere * baseRadius);

                if (x != res - 1 && y != res - 1)
                {
                    int i = vIndexStart + x + y * res;
                    triangles.Add(i); triangles.Add(i + res + 1); triangles.Add(i + res);
                    triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + res + 1);
                }
            }
        }
    }

    void ApplyAdvancedNoise()
    {
        Random.InitState(seed);

        float scaleFactor = baseRadius * 0.1f;

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 v = vertices[i];

            
            float featureFreq = noiseFrequency / (baseRadius * 0.05f);
            float bigNoise = Mathf.PerlinNoise(v.x * featureFreq + seed, v.y * featureFreq + seed);

            
            float terracedNoise = Mathf.Floor(bigNoise * 6f) / 6f;
            float finalNoise = Mathf.Lerp(bigNoise, terracedNoise, 0.5f);

            v += v.normalized * (finalNoise * noiseStrength * scaleFactor);

           
            float crunch = Mathf.PerlinNoise(v.y * 0.5f + seed, v.z * 0.5f + seed);
            if (crunch > 0.5f)
            {
                v += new Vector3(
                    Random.Range(-jaggedness, jaggedness),
                    Random.Range(-jaggedness, jaggedness),
                    Random.Range(-jaggedness, jaggedness)
                ) * scaleFactor;
            }

            vertices[i] = v;
        }
    }

    void ApplyFlatTop()
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 v = vertices[i];
            if (v.y > flatTopHeight)
            {
                v.y = flatTopHeight + ((v.y - flatTopHeight) * flatnessImperfection);
                vertices[i] = v;
            }
        }
    }

    //void CalculateSpawnZone()
    //{
    //    Vector3 sum = Vector3.zero;
    //    int count = 0;
    //    List<Vector3> topPoints = new List<Vector3>();

    //    foreach (Vector3 v in vertices)
    //    {
    //        if (v.y >= flatTopHeight - 0.05f)
    //        {
    //            sum += v;
    //            topPoints.Add(v);
    //            count++;
    //        }
    //    }

    //    if (count > 0)
    //    {
    //        topCenterLocal = sum / count;
    //        float maxDistSq = 0;
    //        foreach (Vector3 p in topPoints)
    //        {
    //            float distSq = (new Vector3(p.x, 0, p.z) - new Vector3(topCenterLocal.x, 0, topCenterLocal.z)).sqrMagnitude;
    //            if (distSq > maxDistSq) maxDistSq = distSq;
    //        }
    //        topRadius = Mathf.Sqrt(maxDistSq) * 0.85f;
    //    }
    //}

    void FlatShade()
    {
        List<Vector3> flatVertices = new List<Vector3>();
        List<int> flatTriangles = new List<int>();
        for (int i = 0; i < triangles.Count; i++)
        {
            flatVertices.Add(vertices[triangles[i]]);
            flatTriangles.Add(i);
        }
        vertices = flatVertices;
        triangles = flatTriangles;
    }

    void OnDrawGizmosSelected()
    {
        if (useFlatTop)
        {
            Gizmos.color = Color.green;
            Vector3 worldCenter = transform.TransformPoint(topCenterLocal);
            DrawWireCylinder(worldCenter, topRadius, 0.5f);
        }
    }

    void DrawWireCylinder(Vector3 center, float radius, float height)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.DrawWireDisc(center, transform.up, radius);
        UnityEditor.Handles.DrawWireDisc(center + transform.up * height, transform.up, radius);
        Gizmos.DrawLine(center + transform.right * radius, center + transform.right * radius + transform.up * height);
        Gizmos.DrawLine(center - transform.right * radius, center - transform.right * radius + transform.up * height);
#endif
    }
}