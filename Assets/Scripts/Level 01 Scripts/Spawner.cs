using UnityEngine;
using System.Collections.Generic;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject prefabToSpawn;

    [Header("Spawn Points (Assign up to 3)")]
    public Transform spawnPoint1;
    public Transform spawnPoint2;
    public Transform spawnPoint3;

    [Header("Spawn Settings")]
    public bool randomizeSpawnPoint = true;

    [Tooltip("Delay before the first spawn happens")]
    public float startDelay = 2f;

    [Tooltip("Time between each spawn")]
    public float spawnInterval = 3f;

    [Tooltip("Maximum number of times to spawn (0 = infinite)")]
    public int maxSpawnCount = 10;

    public bool autoSpawn = true;

    private float timer;
    private int spawnCount = 0;
    private bool hasStarted = false;

    void Update()
    {
        if (!autoSpawn) return;

        timer += Time.deltaTime;

        // Wait for initial delay
        if (!hasStarted)
        {
            if (timer >= startDelay)
            {
                SpawnPrefab();
                spawnCount++;
                timer = 0f;
                hasStarted = true;
            }
            return;
        }

        // Stop if reached max spawns (unless infinite)
        if (maxSpawnCount > 0 && spawnCount >= maxSpawnCount)
            return;

        // Handle repeated spawning
        if (timer >= spawnInterval)
        {
            SpawnPrefab();
            spawnCount++;
            timer = 0f;
        }
    }

    public void SpawnPrefab()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("No prefab assigned!");
            return;
        }

        Transform chosenPoint = GetSpawnPoint();

        if (chosenPoint == null)
        {
            Debug.LogWarning("No spawn points assigned!");
            return;
        }

        Instantiate(prefabToSpawn, chosenPoint.position, chosenPoint.rotation);
    }

    Transform GetSpawnPoint()
    {
        Transform[] points = new Transform[3] { spawnPoint1, spawnPoint2, spawnPoint3 };

        List<Transform> validPoints = new List<Transform>();

        foreach (Transform p in points)
        {
            if (p != null)
                validPoints.Add(p);
        }

        if (validPoints.Count == 0) return null;

        if (randomizeSpawnPoint)
        {
            int index = Random.Range(0, validPoints.Count);
            return validPoints[index];
        }
        else
        {
            return validPoints[0];
        }
    }
}