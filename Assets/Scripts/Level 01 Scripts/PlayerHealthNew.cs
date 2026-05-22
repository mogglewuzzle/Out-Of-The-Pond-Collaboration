using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI healthText; // assign your TMP text in the inspector

    [Header("Health Settings")]
    public int startingHealth = 10;



    [Header("Collision Heal Settings")]
    public string healTag = "HealthPickup";  // Tag that triggers healing

    private int health;

    void Start()
    {
        health = startingHealth;
        UpdateHealthUI();
    }

    // Increase health (now just sets it to startingHealth)
    public void RestoreHealth()
    {
        health = startingHealth;
        UpdateHealthUI();
    }

    // Decrease health
    public void DecreaseHealth(int amount)
    {
        health -= amount;
        UpdateHealthUI();
    }

    // Update the TextMeshPro UI
    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {health}";
        }
    }

    // Collision check for healing
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(healTag))
        {
            RestoreHealth();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(healTag))
        {
            RestoreHealth();
        }
    }

        public int GetHealth()
            {
                return health;
            }
}