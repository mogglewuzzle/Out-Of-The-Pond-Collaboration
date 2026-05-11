using UnityEngine;

public class Object_Hazard : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private Systems_RespawnManager respawnManager;
    [Tooltip("Must match a Point Name in Systems_RespawnManager.")]
    [SerializeField] private string respawnPointName;
    [SerializeField] private Systems_RespawnManager.RespawnMode respawnMode = Systems_RespawnManager.RespawnMode.MoveExistingObject;
    [Tooltip("Prefab to instantiate at the respawn point when Respawn Mode is Instantiate Prefab.")]
    [SerializeField] private GameObject respawnPrefab;
    [SerializeField] private bool useRespawnPointRotation = true;

    [Header("Affected Tags")]
    [Tooltip("Objects with one of these tags are respawned when they collide with this hazard.")]
    [SerializeField] private string[] affectedTags;

    private void Awake()
    {
        if (respawnManager == null)
            respawnManager = Systems_RespawnManager.Instance;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryRequestRespawn(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRequestRespawn(other.gameObject);
    }

    private void TryRequestRespawn(GameObject objectToRespawn)
    {
        if (objectToRespawn == null || !HasAffectedTag(objectToRespawn))
            return;

        if (respawnManager == null)
        {
            respawnManager = Systems_RespawnManager.Instance;
            if (respawnManager == null)
            {
                Debug.LogWarning($"{nameof(Object_Hazard)} on {name} cannot respawn {objectToRespawn.name}: no {nameof(Systems_RespawnManager)} found.", this);
                return;
            }
        }

        respawnManager.RequestRespawn(objectToRespawn, respawnPointName, respawnMode, respawnPrefab, useRespawnPointRotation);
    }

    private bool HasAffectedTag(GameObject candidate)
    {
        if (affectedTags == null)
            return false;

        for (int i = 0; i < affectedTags.Length; i++)
        {
            if (!string.IsNullOrEmpty(affectedTags[i]) && candidate.tag == affectedTags[i])
                return true;
        }

        return false;
    }
}
