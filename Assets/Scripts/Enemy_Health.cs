using TMPro;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    private enum DeathAction
    {
        DestroyObject,
        DeactivateObject,
        DoNothing
    }

    [Header("Health")]
    [SerializeField] private int startingHealth = 3;
    [Tooltip("When enabled, health cannot go below 0 after taking damage.")]
    [SerializeField] private bool clampToZero = true;
    [SerializeField] private DeathAction deathAction = DeathAction.DestroyObject;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI healthText;
    [Tooltip("When enabled, the UI displays 'Health: 3'. When disabled, it displays only the number.")]
    [SerializeField] private bool showLabel;

    public int CurrentHealth { get; private set; }
    public int StartingHealth => startingHealth;

    private void Awake()
    {
        CurrentHealth = startingHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return;

        CurrentHealth -= amount;

        if (clampToZero && CurrentHealth < 0)
            CurrentHealth = 0;

        UpdateHealthUI();

        if (CurrentHealth <= 0)
            Die();
    }

    public void RestoreHealth()
    {
        CurrentHealth = startingHealth;
        UpdateHealthUI();
    }

    public int GetHealth()
    {
        return CurrentHealth;
    }

    private void Die()
    {
        if (deathAction == DeathAction.DestroyObject)
        {
            Destroy(gameObject);
            return;
        }

        if (deathAction == DeathAction.DeactivateObject)
            gameObject.SetActive(false);
    }

    private void UpdateHealthUI()
    {
        if (healthText == null)
            return;

        healthText.text = showLabel ? $"Health: {CurrentHealth}" : CurrentHealth.ToString();
    }
}
