using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Systems_RespawnManager : MonoBehaviour
{
    [System.Serializable]
    public class NamedRespawnPoint
    {
        [Tooltip("Name hazards use to request this respawn point.")]
        public string pointName;

        [Tooltip("Transform used as the respawn position and rotation.")]
        public Transform point;
    }

    public enum RespawnMode
    {
        InstantiatePrefab,
        MoveExistingObject,
        CloneAndDestroyOriginal
    }

    public static Systems_RespawnManager Instance { get; private set; }

    public event Action<GameObject, GameObject> RespawnCompleted;

    [Header("Respawn Timing")]
    [SerializeField] private float respawnDelay = 1f;

    [Header("First Respawn Override")]
    [Tooltip("When enabled, the first successful respawn request in this scene uses First Respawn Point, then all later respawns use the normal rules.")]
    [SerializeField] private bool useFirstRespawnPointOnce;
    [SerializeField] private Transform firstRespawnPoint;

    [Header("Respawn Points")]
    [Tooltip("When enabled, respawn requests use the closest listed respawn point to the object being respawned instead of the requested point name.")]
    [SerializeField] private bool useClosestRespawnPoint;
    [Tooltip("When enabled, respawns use the closest point on the selected respawn point's collider instead of the respawn point transform position.")]
    [SerializeField] private bool useClosestPointOnRespawnCollider;
    [SerializeField] private List<NamedRespawnPoint> respawnPoints = new List<NamedRespawnPoint>();

    private readonly HashSet<GameObject> respawningObjects = new HashSet<GameObject>();
    private bool hasUsedFirstRespawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple {nameof(Systems_RespawnManager)} instances found. Disabling duplicate on {name}.", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool RequestRespawn(
        GameObject objectToRespawn,
        string respawnPointName,
        Systems_RespawnManager.RespawnMode respawnMode,
        GameObject respawnPrefab,
        bool useRespawnPointRotation)
    {
        if (objectToRespawn == null || respawningObjects.Contains(objectToRespawn))
            return false;

        if (!TryResolveRespawnPoint(objectToRespawn, respawnPointName, out Transform respawnPoint))
            return false;

        if (respawnMode == RespawnMode.InstantiatePrefab && respawnPrefab == null)
        {
            Debug.LogWarning($"{nameof(Systems_RespawnManager)} cannot respawn {objectToRespawn.name}: missing respawn prefab.", this);
            return false;
        }

        if (useFirstRespawnPointOnce && !hasUsedFirstRespawnPoint && respawnPoint == firstRespawnPoint)
            hasUsedFirstRespawnPoint = true;

        Vector3 requestPosition = objectToRespawn.transform.position;
        respawningObjects.Add(objectToRespawn);
        StartCoroutine(RespawnAfterDelay(objectToRespawn, respawnPoint, requestPosition, respawnMode, respawnPrefab, useRespawnPointRotation));
        return true;
    }

    private bool TryResolveRespawnPoint(GameObject objectToRespawn, string respawnPointName, out Transform respawnPoint)
    {
        respawnPoint = null;

        if (useFirstRespawnPointOnce && !hasUsedFirstRespawnPoint)
        {
            if (firstRespawnPoint == null)
            {
                Debug.LogWarning($"{nameof(Systems_RespawnManager)} cannot use first respawn override for {objectToRespawn.name}: no first respawn point assigned.", this);
                return false;
            }

            respawnPoint = firstRespawnPoint;
            return true;
        }

        if (useClosestRespawnPoint)
        {
            if (TryGetClosestRespawnPoint(objectToRespawn.transform.position, out respawnPoint))
                return true;

            Debug.LogWarning($"{nameof(Systems_RespawnManager)} cannot respawn {objectToRespawn.name}: no valid respawn points configured.", this);
            return false;
        }

        if (TryGetRespawnPoint(respawnPointName, out respawnPoint))
            return true;

        Debug.LogWarning($"{nameof(Systems_RespawnManager)} cannot respawn {objectToRespawn.name}: no respawn point named '{respawnPointName}' found.", this);
        return false;
    }

    public bool TryGetRespawnPoint(string respawnPointName, out Transform respawnPoint)
    {
        respawnPoint = null;

        if (string.IsNullOrWhiteSpace(respawnPointName) || respawnPoints == null)
            return false;

        string requestedName = respawnPointName.Trim();

        for (int i = 0; i < respawnPoints.Count; i++)
        {
            NamedRespawnPoint entry = respawnPoints[i];
            if (entry == null || entry.point == null || string.IsNullOrWhiteSpace(entry.pointName))
                continue;

            if (entry.pointName.Trim() == requestedName)
            {
                respawnPoint = entry.point;
                return true;
            }
        }

        return false;
    }

    public bool TryGetClosestRespawnPoint(Vector3 worldPosition, out Transform respawnPoint)
    {
        respawnPoint = null;

        if (respawnPoints == null)
            return false;

        float closestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < respawnPoints.Count; i++)
        {
            NamedRespawnPoint entry = respawnPoints[i];
            if (entry == null || entry.point == null)
                continue;

            Vector3 candidatePosition = GetSpawnPosition(entry.point, worldPosition);
            float distanceSqr = (candidatePosition - worldPosition).sqrMagnitude;
            if (distanceSqr >= closestDistanceSqr)
                continue;

            closestDistanceSqr = distanceSqr;
            respawnPoint = entry.point;
        }

        return respawnPoint != null;
    }

    private IEnumerator RespawnAfterDelay(
        GameObject objectToRespawn,
        Transform respawnPoint,
        Vector3 requestPosition,
        RespawnMode respawnMode,
        GameObject respawnPrefab,
        bool useRespawnPointRotation)
    {
        Vector3 spawnPosition = GetSpawnPosition(respawnPoint, requestPosition);
        Quaternion spawnRotation = useRespawnPointRotation ? respawnPoint.rotation : objectToRespawn.transform.rotation;

        DeactivateObject(objectToRespawn);

        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        if (respawnMode == RespawnMode.InstantiatePrefab)
        {
            GameObject respawnedObject = Instantiate(respawnPrefab, spawnPosition, spawnRotation);
            Destroy(objectToRespawn);
            respawningObjects.Remove(objectToRespawn);
            RespawnCompleted?.Invoke(objectToRespawn, respawnedObject);
            yield break;
        }

        if (respawnMode == RespawnMode.CloneAndDestroyOriginal)
        {
            GameObject clone = Instantiate(objectToRespawn, spawnPosition, spawnRotation);
            clone.SetActive(true);
            Destroy(objectToRespawn);
            respawningObjects.Remove(objectToRespawn);
            RespawnCompleted?.Invoke(objectToRespawn, clone);
            yield break;
        }

        RespawnExistingObject(objectToRespawn, spawnPosition, spawnRotation);
        objectToRespawn.SetActive(true);
        respawningObjects.Remove(objectToRespawn);
        RespawnCompleted?.Invoke(objectToRespawn, objectToRespawn);
    }

    private Vector3 GetSpawnPosition(Transform respawnPoint, Vector3 requestPosition)
    {
        if (!useClosestPointOnRespawnCollider)
            return respawnPoint.position;

        Collider respawnCollider = respawnPoint.GetComponent<Collider>();
        if (respawnCollider == null)
            return respawnPoint.position;

        return respawnCollider.ClosestPoint(requestPosition);
    }

    private void DeactivateObject(GameObject objectToRespawn)
    {
        Rigidbody rb = objectToRespawn.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        objectToRespawn.SetActive(false);
    }

    private void RespawnExistingObject(GameObject objectToRespawn, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        Rigidbody rb = objectToRespawn.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        objectToRespawn.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
    }
}
