using System.Collections;
using TMPro;
using UnityEngine;

public class System_HealthManager : MonoBehaviour
{
    public static System_HealthManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Runtime display only. Found with Player Tag.")]
    [SerializeField] private GameObject foundPlayer;
    [Tooltip("Runtime display only. Component found on Found Player root.")]
    [SerializeField] private Player_Health foundPlayerHealth;

    [Header("Tracked Health")]
    [SerializeField] private int currentHealth;

    [Header("Lives")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int currentLives;

    [Header("Lives UI")]
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private bool showLivesLabel;

    [Header("Respawn")]
    [Tooltip("When available, delegate player respawning to Systems_RespawnManager so scene respawn rules stay in one place.")]
    [SerializeField] private bool useSystemsRespawnManager = true;
    [Tooltip("Prefab to instantiate after the current player is destroyed.")]
    [SerializeField] private GameObject playerPrefab;
    [Tooltip("Where the new player prefab spawns.")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("Respawn point name to request from Systems_RespawnManager when it is not using closest-point mode.")]
    [SerializeField] private string respawnPointName = "SpawnPoint_00";
    [SerializeField] private Systems_RespawnManager.RespawnMode respawnMode = Systems_RespawnManager.RespawnMode.InstantiatePrefab;
    [SerializeField] private bool useRespawnPointRotation = true;
    [Tooltip("Seconds to wait after health reaches 0 before destroying the current player.")]
    [SerializeField] private float destroyDelayAfterDeath = 0f;
    [Tooltip("Fallback delay used only when Systems_RespawnManager is unavailable or cannot handle the respawn.")]
    [SerializeField] private float respawnDelay = 1f;

    private bool isRespawning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple {nameof(System_HealthManager)} instances found. Disabling duplicate on {name}.", this);
            enabled = false;
            return;
        }

        Instance = this;
        currentLives = startingLives;
        UpdateLivesUI();
        FindPlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (isRespawning)
            return;

        if (foundPlayer == null || foundPlayerHealth == null)
            FindPlayer();

        SyncCurrentHealth();

        if (foundPlayerHealth != null && foundPlayerHealth.CurrentHealth <= 0)
            StartCoroutine(DestroyAndRespawnPlayer());
    }

    private IEnumerator DestroyAndRespawnPlayer()
    {
        if (isRespawning)
            yield break;

        isRespawning = true;
        LoseLife();

        GameObject oldPlayer = foundPlayer;
        foundPlayer = null;
        foundPlayerHealth = null;
        currentHealth = 0;

        if (destroyDelayAfterDeath > 0f)
            yield return new WaitForSeconds(destroyDelayAfterDeath);

        if (useSystemsRespawnManager && Systems_RespawnManager.Instance != null)
        {
            yield return RespawnPlayerWithRespawnManager(oldPlayer);
            yield break;
        }

        yield return RespawnPlayerDirectly(oldPlayer);
    }

    private IEnumerator RespawnPlayerWithRespawnManager(GameObject oldPlayer)
    {
        if (oldPlayer == null)
        {
            isRespawning = false;
            yield break;
        }

        Systems_RespawnManager respawnManager = Systems_RespawnManager.Instance;
        bool respawnComplete = false;
        GameObject respawnedPlayer = null;

        void HandleRespawnCompleted(GameObject originalObject, GameObject newObject)
        {
            if (originalObject != oldPlayer)
                return;

            respawnedPlayer = newObject;
            respawnComplete = true;
        }

        respawnManager.RespawnCompleted += HandleRespawnCompleted;
        bool requestStarted = respawnManager.RequestRespawn(oldPlayer, respawnPointName, respawnMode, playerPrefab, useRespawnPointRotation);

        if (!requestStarted)
        {
            respawnManager.RespawnCompleted -= HandleRespawnCompleted;
            yield return RespawnPlayerDirectly(oldPlayer);
            yield break;
        }

        yield return new WaitUntil(() => respawnComplete);
        respawnManager.RespawnCompleted -= HandleRespawnCompleted;

        TrackRespawnedPlayer(respawnedPlayer);
        isRespawning = false;
    }

    private IEnumerator RespawnPlayerDirectly(GameObject oldPlayer)
    {
        if (oldPlayer != null)
            Destroy(oldPlayer);

        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        if (playerPrefab == null)
        {
            Debug.LogWarning($"{nameof(System_HealthManager)} on {name} cannot respawn player: Player Prefab is missing.", this);
            isRespawning = false;
            yield break;
        }

        Vector3 spawnPosition = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = respawnPoint != null ? respawnPoint.rotation : Quaternion.identity;

        TrackRespawnedPlayer(Instantiate(playerPrefab, spawnPosition, spawnRotation));
        isRespawning = false;
    }

    private void TrackRespawnedPlayer(GameObject respawnedPlayer)
    {
        foundPlayer = respawnedPlayer;
        foundPlayerHealth = foundPlayer != null ? foundPlayer.GetComponentInChildren<Player_Health>() : null;

        if (foundPlayerHealth == null)
        {
            string prefabName = playerPrefab != null ? playerPrefab.name : "unknown";
            Debug.LogWarning($"{nameof(System_HealthManager)} on {name} respawned player prefab '{prefabName}' but could not find {nameof(Player_Health)} on it or its children.", this);
            return;
        }

        SyncCurrentHealth();
    }

    private void FindPlayer()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
            return;

        GameObject[] taggedPlayers;
        try
        {
            taggedPlayers = GameObject.FindGameObjectsWithTag(playerTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"{nameof(System_HealthManager)} on {name} cannot find player: tag '{playerTag}' does not exist.", this);
            return;
        }

        foundPlayer = null;
        foundPlayerHealth = null;

        for (int i = 0; i < taggedPlayers.Length; i++)
        {
            Player_Health candidateHealth = taggedPlayers[i].GetComponent<Player_Health>();
            if (candidateHealth == null)
                continue;

            foundPlayer = taggedPlayers[i];
            foundPlayerHealth = candidateHealth;
            break;
        }

        if (foundPlayer == null || foundPlayerHealth == null)
        {
            Debug.LogWarning($"{nameof(System_HealthManager)} on {name} cannot find an object tagged '{playerTag}' with {nameof(Player_Health)} on the same object.", this);
            return;
        }
        SyncCurrentHealth();
    }

    private void SyncCurrentHealth()
    {
        if (foundPlayerHealth != null)
            currentHealth = foundPlayerHealth.CurrentHealth;
    }

    private void LoseLife()
    {
        if (currentLives > 0)
            currentLives--;

        UpdateLivesUI();
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = showLivesLabel ? $"Lives: {currentLives}" : currentLives.ToString();
    }
}
