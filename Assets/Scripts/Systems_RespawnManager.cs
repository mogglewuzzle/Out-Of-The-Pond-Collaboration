using System.Collections;
using System.Collections.Generic;
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

    [Header("Respawn Timing")]
    [SerializeField] private float respawnDelay = 1f;

    [Header("Respawn Points")]
    [SerializeField] private List<NamedRespawnPoint> respawnPoints = new List<NamedRespawnPoint>();

    private readonly HashSet<GameObject> respawningObjects = new HashSet<GameObject>();

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

    public void RequestRespawn(
        GameObject objectToRespawn,
        string respawnPointName,
        Systems_RespawnManager.RespawnMode respawnMode,
        GameObject respawnPrefab,
        bool useRespawnPointRotation)
    {
        if (objectToRespawn == null || respawningObjects.Contains(objectToRespawn))
            return;

        if (!TryGetRespawnPoint(respawnPointName, out Transform respawnPoint))
        {
            Debug.LogWarning($"{nameof(Systems_RespawnManager)} cannot respawn {objectToRespawn.name}: no respawn point named '{respawnPointName}' found.", this);
            return;
        }

        if (respawnMode == RespawnMode.InstantiatePrefab && respawnPrefab == null)
        {
            Debug.LogWarning($"{nameof(Systems_RespawnManager)} cannot respawn {objectToRespawn.name}: missing respawn prefab.", this);
            return;
        }

        respawningObjects.Add(objectToRespawn);
        StartCoroutine(RespawnAfterDelay(objectToRespawn, respawnPoint, respawnMode, respawnPrefab, useRespawnPointRotation));
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

    private IEnumerator RespawnAfterDelay(
        GameObject objectToRespawn,
        Transform respawnPoint,
        RespawnMode respawnMode,
        GameObject respawnPrefab,
        bool useRespawnPointRotation)
    {
        Vector3 spawnPosition = respawnPoint.position;
        Quaternion spawnRotation = useRespawnPointRotation ? respawnPoint.rotation : objectToRespawn.transform.rotation;

        DeactivateObject(objectToRespawn);

        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        if (respawnMode == RespawnMode.InstantiatePrefab)
        {
            Instantiate(respawnPrefab, spawnPosition, spawnRotation);
            Destroy(objectToRespawn);
            respawningObjects.Remove(objectToRespawn);
            yield break;
        }

        if (respawnMode == RespawnMode.CloneAndDestroyOriginal)
        {
            GameObject clone = Instantiate(objectToRespawn, spawnPosition, spawnRotation);
            clone.SetActive(true);
            Destroy(objectToRespawn);
            respawningObjects.Remove(objectToRespawn);
            yield break;
        }

        RespawnExistingObject(objectToRespawn, spawnPosition, spawnRotation);
        objectToRespawn.SetActive(true);
        respawningObjects.Remove(objectToRespawn);
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
