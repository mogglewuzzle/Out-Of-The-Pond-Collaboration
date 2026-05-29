using UnityEngine;
using System.Collections.Generic;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject prefabToSpawn;
    [Tooltip("Optional. If this list has entries, the spawner randomly picks one instead of always using Prefab To Spawn.")]
    public List<GameObject> prefabsToSpawn = new List<GameObject>();

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
        GameObject selectedPrefab = GetPrefabToSpawn();

        if (selectedPrefab == null)
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

        Instantiate(selectedPrefab, chosenPoint.position, chosenPoint.rotation);
    }

    GameObject GetPrefabToSpawn()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0)
            return prefabToSpawn;

        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (GameObject prefab in prefabsToSpawn)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0)
            return prefabToSpawn;

        int index = Random.Range(0, validPrefabs.Count);
        return validPrefabs[index];
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
