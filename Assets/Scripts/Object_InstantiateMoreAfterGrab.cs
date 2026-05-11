using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Object/Instantiate More After Grab")]
public class Object_InstantiateMoreAfterGrab : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject prefabToInstantiate;
    [Tooltip("Optional. If empty, this object's transform is used.")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private bool useSpawnPointRotation = true;
    [SerializeField] private bool parentSpawnedObjectToThisObject;

    [Header("Spawn Random Offset")]
    [SerializeField] private float randomOffsetAmount;
    [SerializeField] private bool randomizeOffsetX;
    [SerializeField] private bool randomizeOffsetY;
    [SerializeField] private bool randomizeOffsetZ;

    [Header("Box Contents")]
    [Tooltip("The box keeps at least this many matching objects inside its trigger volume.")]
    [SerializeField] private int requiredObjectCount = 1;
    [SerializeField] private string requiredTag = "GrabPoint";

    [Header("Events")]
    [SerializeField] private UnityEvent onSpawnQueued;
    [SerializeField] private UnityEvent onSpawned;

    private readonly Dictionary<GameObject, int> objectsInBox = new Dictionary<GameObject, int>();
    private Coroutine spawnRoutine;

    private void Update()
    {
        RemoveMissingObjects();
        UpdateSpawnQueue();
    }

    private void OnTriggerEnter(Collider other)
    {
        AddObject(other);
    }

    private void OnTriggerStay(Collider other)
    {
        GameObject trackedObject = GetTrackedObject(other);
        if (trackedObject != null && !objectsInBox.ContainsKey(trackedObject))
            objectsInBox.Add(trackedObject, 1);
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveObject(other);
    }

    private void AddObject(Collider candidateCollider)
    {
        GameObject trackedObject = GetTrackedObject(candidateCollider);
        if (trackedObject == null)
            return;

        if (!objectsInBox.ContainsKey(trackedObject))
            objectsInBox.Add(trackedObject, 0);

        objectsInBox[trackedObject]++;
    }

    private void RemoveObject(Collider candidateCollider)
    {
        GameObject trackedObject = GetTrackedObject(candidateCollider);
        if (trackedObject == null || !objectsInBox.ContainsKey(trackedObject))
            return;

        objectsInBox[trackedObject]--;
        if (objectsInBox[trackedObject] <= 0)
            objectsInBox.Remove(trackedObject);
    }

    private void UpdateSpawnQueue()
    {
        if (objectsInBox.Count < requiredObjectCount)
        {
            QueueSpawn();
            return;
        }

        CancelQueuedSpawn();
    }

    private void QueueSpawn()
    {
        if (prefabToInstantiate == null)
        {
            Debug.LogWarning($"{nameof(Object_InstantiateMoreAfterGrab)} on {name} cannot spawn because no prefab is assigned.", this);
            return;
        }

        if (spawnRoutine != null)
            return;

        onSpawnQueued?.Invoke();
        spawnRoutine = StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        Transform targetSpawnPoint = spawnPoint != null ? spawnPoint : transform;
        Vector3 spawnPosition = targetSpawnPoint.position + GetRandomSpawnOffset();
        Quaternion spawnRotation = useSpawnPointRotation ? targetSpawnPoint.rotation : Quaternion.identity;
        Transform parent = parentSpawnedObjectToThisObject ? transform : null;

        Instantiate(prefabToInstantiate, spawnPosition, spawnRotation, parent);
        onSpawned?.Invoke();
        spawnRoutine = null;
    }

    private Vector3 GetRandomSpawnOffset()
    {
        if (randomOffsetAmount <= 0f)
            return Vector3.zero;

        return new Vector3(
            randomizeOffsetX ? Random.Range(-randomOffsetAmount, randomOffsetAmount) : 0f,
            randomizeOffsetY ? Random.Range(-randomOffsetAmount, randomOffsetAmount) : 0f,
            randomizeOffsetZ ? Random.Range(-randomOffsetAmount, randomOffsetAmount) : 0f);
    }

    private void CancelQueuedSpawn()
    {
        if (spawnRoutine == null)
            return;

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private void RemoveMissingObjects()
    {
        if (objectsInBox.Count == 0)
            return;

        List<GameObject> missingObjects = new List<GameObject>();
        foreach (GameObject trackedObject in objectsInBox.Keys)
        {
            if (trackedObject == null)
                missingObjects.Add(trackedObject);
        }

        for (int i = 0; i < missingObjects.Count; i++)
            objectsInBox.Remove(missingObjects[i]);
    }

    private GameObject GetTrackedObject(Collider candidateCollider)
    {
        if (candidateCollider == null || string.IsNullOrWhiteSpace(requiredTag))
            return null;

        string trimmedTag = requiredTag.Trim();
        Transform current = candidateCollider.transform;

        while (current != null)
        {
            if (HasTag(current.gameObject, trimmedTag))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private bool HasTag(GameObject candidate, string checkedTag)
    {
        try
        {
            return candidate != null && candidate.CompareTag(checkedTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"{nameof(Object_InstantiateMoreAfterGrab)} on {name} cannot check undefined tag '{checkedTag}'.", this);
            return false;
        }
    }

    private void OnValidate()
    {
        requiredObjectCount = Mathf.Max(0, requiredObjectCount);
        spawnDelay = Mathf.Max(0f, spawnDelay);
        randomOffsetAmount = Mathf.Max(0f, randomOffsetAmount);
    }
}
