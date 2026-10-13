using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages endless road recycling ahead of the player car.
/// Recycles existing segments to avoid constant Instantiate/Destroy overhead.
/// </summary>
public class RoadManager : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject roadSegmentPrefab;
    [SerializeField] private int initialSegments = 6;
    [SerializeField] private float segmentLength = 40f;

    private List<GameObject> activeSegments = new List<GameObject>();
    private float nextSpawnZ = 0f;

    private void Start()
    {
        InitializeRoad();
    }

    public void InitializeRoad()
    {
        // Clean up any existing segments
        foreach (var seg in activeSegments)
        {
            if (seg != null) Destroy(seg);
        }
        activeSegments.Clear();
        nextSpawnZ = 0f;

        // Spawn initial road chain
        for (int i = 0; i < initialSegments; i++)
        {
            SpawnSegment();
        }
    }

    private void SpawnSegment()
    {
        Vector3 spawnPos = new Vector3(0f, 0f, nextSpawnZ);
        GameObject segment = Instantiate(roadSegmentPrefab, spawnPos, Quaternion.identity, transform);
        EnsureSegmentDecorations(segment);
        activeSegments.Add(segment);
        nextSpawnZ += segmentLength;
    }

    private void EnsureSegmentDecorations(GameObject segment)
    {
        if (segment == null || segment.transform.Find("SceneryRoot") != null) return;

        GameObject sceneryRoot = new GameObject("SceneryRoot");
        sceneryRoot.transform.SetParent(segment.transform, false);

        // Fetch shared materials from segment renderers if available
        Material poleMat = null;
        Material grassMat = null;
        foreach (var r in segment.GetComponentsInChildren<Renderer>())
        {
            if (r.gameObject.name.Contains("GuardRail") && poleMat == null) poleMat = r.sharedMaterial;
            if (r.gameObject.name.Contains("Grass") && grassMat == null) grassMat = r.sharedMaterial;
        }

        // 1. Streetlights (Poles at X = -6.8 and X = +6.8, safely outside the road and high above traffic)
        CreateStreetlight(sceneryRoot.transform, new Vector3(-6.8f, 0f, 12f), true, poleMat);
        CreateStreetlight(sceneryRoot.transform, new Vector3(6.8f, 0f, 28f), false, poleMat);

        // 2. Simple background buildings (Set far back at X = -13.5 and +13.5 on grass, never blocking view)
        CreateBuilding(sceneryRoot.transform, new Vector3(-13.5f, 0f, 8f), new Vector3(4.2f, 6.5f, 5.5f), new Color(0.25f, 0.28f, 0.35f));
        CreateBuilding(sceneryRoot.transform, new Vector3(13.5f, 0f, 26f), new Vector3(4.5f, 8.5f, 6.5f), new Color(0.32f, 0.35f, 0.42f));
    }

    private void CreateStreetlight(Transform parent, Vector3 basePos, bool onLeft, Material mat)
    {
        GameObject lightObj = new GameObject("Streetlight");
        lightObj.transform.SetParent(parent, false);

        // Vertical Pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.name = "Pole";
        pole.transform.SetParent(lightObj.transform, false);
        pole.transform.localPosition = basePos + new Vector3(0f, 1.8f, 0f);
        pole.transform.localScale = new Vector3(0.16f, 3.6f, 0.16f);
        if (mat != null) pole.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(pole.GetComponent<Collider>());

        // Horizontal Arm extending towards road
        float armOffsetX = onLeft ? 0.35f : -0.35f;
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.transform.SetParent(lightObj.transform, false);
        arm.transform.localPosition = basePos + new Vector3(armOffsetX, 3.6f, 0f);
        arm.transform.localScale = new Vector3(0.7f, 0.12f, 0.16f);
        if (mat != null) arm.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(arm.GetComponent<Collider>());

        // Glowing Lamp Head
        float headOffsetX = onLeft ? 0.7f : -0.7f;
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "LampHead";
        head.transform.SetParent(lightObj.transform, false);
        head.transform.localPosition = basePos + new Vector3(headOffsetX, 3.52f, 0f);
        head.transform.localScale = new Vector3(0.25f, 0.12f, 0.25f);
        head.GetComponent<Renderer>().material.color = new Color(1f, 0.95f, 0.6f);
        Destroy(head.GetComponent<Collider>());
    }

    private void CreateBuilding(Transform parent, Vector3 basePos, Vector3 size, Color color)
    {
        GameObject bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bld.name = "Building";
        bld.transform.SetParent(parent, false);
        bld.transform.localPosition = basePos + new Vector3(0f, size.y * 0.5f, 0f);
        bld.transform.localScale = size;
        bld.GetComponent<Renderer>().material.color = color;
        Destroy(bld.GetComponent<Collider>());
    }

    private void Update()
    {
        if (playerTransform == null || activeSegments.Count == 0) return;

        // If player has moved past the first segment, recycle it to the front
        GameObject oldestSegment = activeSegments[0];
        if (playerTransform.position.z > oldestSegment.transform.position.z + segmentLength + 10f)
        {
            // Move oldest to the very front
            oldestSegment.transform.position = new Vector3(0f, 0f, nextSpawnZ);
            nextSpawnZ += segmentLength;

            // Reorder list: pop first and append to back
            activeSegments.RemoveAt(0);
            activeSegments.Add(oldestSegment);
        }
    }

    public void ResetRoad(Transform player)
    {
        playerTransform = player;
        InitializeRoad();
    }
}
