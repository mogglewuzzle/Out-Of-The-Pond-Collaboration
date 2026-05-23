using UnityEngine;

public class PlayerHealth02 : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;

    private PlayerRespawn playerRespawn;

    void Start()
    {
        currentHealth = maxHealth;
        playerRespawn = GetComponent<PlayerRespawn>();
        Debug.Log("Player health set to " + currentHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("Player took damage. Health now: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Player died");
            currentHealth = maxHealth;

            if (playerRespawn != null)
            {
                playerRespawn.Respawn();
            }
        }
    }
}