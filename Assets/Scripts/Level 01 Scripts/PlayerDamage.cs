using UnityEngine;
using System.Collections;

public class PlayerDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public string damagingTag = "Enemy"; // tag of objects that hurt the player
    public int damageAmount = 1;         // amount of health to remove
    public float invulnerabilityTime = 1.5f; // seconds before can take damage again

    [Header("Health Reference")]
    public PlayerHealth playerHealth; // reference to your health script

    private bool canTakeDamage = true;

    void OnCollisionEnter(Collision collision)
    {
        if (!canTakeDamage) return;

        if (collision.gameObject.CompareTag(damagingTag))
        {
            // Apply damage
            if (playerHealth != null)
                playerHealth.DecreaseHealth(damageAmount);

            // Start invulnerability
            StartCoroutine(InvulnerabilityCooldown());
        }
    }

    IEnumerator InvulnerabilityCooldown()
    {
        canTakeDamage = false;
        yield return new WaitForSeconds(invulnerabilityTime);
        canTakeDamage = true;
    }
}