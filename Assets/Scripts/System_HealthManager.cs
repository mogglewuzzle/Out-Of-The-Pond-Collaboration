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
    [Tooltip("Prefab to instantiate after the current player is destroyed.")]
    [SerializeField] private GameObject playerPrefab;
    [Tooltip("Where the new player prefab spawns.")]
    [SerializeField] private Transform respawnPoint;
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

        foundPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        foundPlayerHealth = foundPlayer.GetComponent<Player_Health>();

        if (foundPlayerHealth == null)
        {
            Debug.LogWarning($"{nameof(System_HealthManager)} on {name} instantiated player prefab '{playerPrefab.name}' but could not find {nameof(Player_Health)} on it or its children.", this);
            isRespawning = false;
            yield break;
        }

        SyncCurrentHealth();
        isRespawning = false;
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
